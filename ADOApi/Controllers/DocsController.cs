using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    public class DocsController : ControllerBase
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IDocsGenerationService _docsGenerationService;
        private readonly ILogger<DocsController> _logger;
        private readonly IAuditLogger _auditLogger;

        public DocsController(IRepositoryService repositoryService, IDocsGenerationService docsGenerationService, ILogger<DocsController> logger, IAuditLogger auditLogger)
        {
            _repositoryService = repositoryService;
            _docsGenerationService = docsGenerationService;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        [HttpPost("preview")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.ReadOnly")]
        public async Task<ActionResult<DocsPreviewResponse>> Preview([FromBody] DocsPreviewRequest request)
        {
            try
            {
                _logger.LogInformation("Previewing documentation for project {Project}, repo {RepositoryId}", request.Project, request.RepositoryId);

                var response = await _docsGenerationService.GenerateDocumentationAsync(request.Project, request.RepositoryId, request.FilesToGenerate);

                var evt = new AuditEvent
                {
                    Action = "Docs preview",
                    Project = request.Project,
                    RepositoryId = request.RepositoryId,
                    Success = true,
                    ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                    ActorUpn = HttpContext.Items["ActorUpn"] as string
                };
                await _auditLogger.AuditAsync(evt);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in docs preview");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("apply")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Contributor")]
        public async Task<ActionResult<DocsApplyResponse>> Apply([FromBody] DocsApplyRequest request)
        {
            try
            {
                _logger.LogInformation("Applying documentation changes to branch {Branch}", request.Branch);

                // Validate allowed files
                var allowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "README.md",
                    "ARCHITECTURE.md",
                    "DEV_GUIDE.md",
                    "SECURITY.md",
                    "DEPLOYMENT.md"
                };

                var invalidFiles = request.FilesToApply.Where(f => !allowedFiles.Contains(f.FileName)).ToList();
                if (invalidFiles.Any())
                {
                    return BadRequest($"The following files are not allowed for writing: {string.Join(", ", invalidFiles.Select(f => f.FileName))}. Only documentation files at repository root are permitted.");
                }

                var response = new DocsApplyResponse { Branch = request.Branch };
                string? lastCommitId = null;

                foreach (var file in request.FilesToApply)
                {
                    if (!file.FileName.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase) || file.FileName.Contains("/"))
                    {
                        _logger.LogWarning("Skipping invalid file: {FileName}", file.FileName);
                        continue; // Only .md files at root
                    }

                    try
                    {
                        // Check if file exists
                        GitItem? existingFile = null;
                        try
                        {
                            existingFile = await _repositoryService.GetFileContentAsync(request.Project, request.RepositoryId, file.FileName, request.Branch);
                        }
                        catch
                        {
                            // File does not exist
                        }

                        GitPush push;
                        if (existingFile != null)
                        {
                            // Update existing file
                            var branchRef = await _repositoryService.GetBranchAsync(request.Project, request.RepositoryId, request.Branch);
                            var baseCommitId = branchRef.ObjectId;

                            push = await _repositoryService.UpdateFileAsync(request.Project, request.RepositoryId, file.FileName, file.Content, request.Branch, request.CommitMessage, baseCommitId);
                        }
                        else
                        {
                            // Create new file
                            push = await _repositoryService.CreateFileAsync(request.Project, request.RepositoryId, file.FileName, file.Content, request.Branch, request.CommitMessage);
                        }

                        response.FilesWritten.Add(file.FileName);
                        lastCommitId = push.Commits.FirstOrDefault()?.CommitId;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error writing file {FileName}", file.FileName);
                    }
                }

                response.CommitId = lastCommitId ?? string.Empty;

                var evt = new AuditEvent
                {
                    Action = "Docs apply",
                    Project = request.Project,
                    RepositoryId = request.RepositoryId,
                    TargetType = "Files",
                    TargetId = string.Join(",", response.FilesWritten),
                    Success = true,
                    ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                    ActorUpn = HttpContext.Items["ActorUpn"] as string
                };
                await _auditLogger.AuditAsync(evt);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in docs apply");
                return StatusCode(500, "Internal server error");
            }
        }

    }
}