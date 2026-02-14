using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace ADOApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.ReadOnly")]
    public class RepositoryController : ControllerBase
    {
        private readonly IRepositoryService _repositoryService;
        private readonly ILogger<RepositoryController> _logger;

        private readonly IAuditLogger _auditLogger;

        public RepositoryController(IRepositoryService repositoryService, ILogger<RepositoryController> logger, IAuditLogger auditLogger)
        {
            _repositoryService = repositoryService;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        private static string SanitizeForLog(string input) => input?.Replace("\n", "").Replace("\r", "").Replace("\t", "") ?? "";

        [HttpGet("repositories/{project}")]
        public async Task<ActionResult<List<GitRepository>>> GetRepositories(string project)
        {
            try
            {
                var repositories = await _repositoryService.GetRepositoriesAsync(project);

                // Compute ETag
                var json = JsonSerializer.Serialize(repositories);
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                var etag = '"' + Convert.ToBase64String(hash) + '"';

                var clientEtag = Request.Headers["If-None-Match"].ToString();
                if (!string.IsNullOrEmpty(clientEtag) && clientEtag == etag)
                {
                    return StatusCode(304);
                }

                Response.Headers["ETag"] = etag;
                return Ok(repositories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repositories for project {Project}", SanitizeForLog(project));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("content/{project}/{repositoryId}")]
        public async Task<ActionResult<GitItem>> GetFileContent(string project, string repositoryId, [FromQuery] string path, [FromQuery] string? version = null)
        {
            try
            {
                var content = await _repositoryService.GetFileContentAsync(project, repositoryId, path, version);
                return Ok(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file content for {Path} in repository {RepositoryId}", SanitizeForLog(path), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("directory/{project}/{repositoryId}")]
        public async Task<ActionResult<List<GitItem>>> GetDirectoryContents(string project, string repositoryId, [FromQuery] string path, [FromQuery] string? version = null)
        {
            try
            {
                var contents = await _repositoryService.GetDirectoryContentsAsync(project, repositoryId, path, version);
                return Ok(contents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting directory contents for {Path} in repository {RepositoryId}", SanitizeForLog(path), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("commits/{project}/{repositoryId}/{commitId}")]
        public async Task<ActionResult<GitCommit>> GetCommit(string project, string repositoryId, string commitId)
        {
            try
            {
                var commit = await _repositoryService.GetCommitAsync(project, repositoryId, commitId);
                return Ok(commit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commit {CommitId} in repository {RepositoryId}", SanitizeForLog(commitId), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("commits/{project}/{repositoryId}")]
        public async Task<ActionResult<List<GitCommit>>> GetCommits(string project, string repositoryId, [FromQuery] string branch = "main", [FromQuery] int top = 10)
        {
            try
            {
                var commits = await _repositoryService.GetCommitsAsync(project, repositoryId, branch, top);
                return Ok(commits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting commits for branch {Branch} in repository {RepositoryId}", SanitizeForLog(branch), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("branches/{project}/{repositoryId}/{branchName}")]
        public async Task<ActionResult<GitRef>> GetBranch(string project, string repositoryId, string branchName)
        {
            try
            {
                var branch = await _repositoryService.GetBranchAsync(project, repositoryId, branchName);
                return Ok(branch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branch {BranchName} in repository {RepositoryId}", SanitizeForLog(branchName), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("branches/{project}/{repositoryId}")]
        public async Task<ActionResult<List<GitRef>>> GetBranches(string project, string repositoryId)
        {
            try
            {
                var branches = await _repositoryService.GetBranchesAsync(project, repositoryId);

                var json = JsonSerializer.Serialize(branches);
                using var sha = SHA256.Create();
                var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
                var etag = '"' + Convert.ToBase64String(hash) + '"';

                var clientEtag = Request.Headers["If-None-Match"].ToString();
                if (!string.IsNullOrEmpty(clientEtag) && clientEtag == etag)
                {
                    return StatusCode(304);
                }

                Response.Headers["ETag"] = etag;
                return Ok(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branches for repository {RepositoryId}", SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpPost("branches/{project}/{repositoryId}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Contributor")]
        public async Task<ActionResult<GitPush>> CreateBranch(string project, string repositoryId, [FromBody] CreateBranchRequest request)
        {
            var evt = new ADOApi.Models.AuditEvent
            {
                Action = "CreateBranch",
                TargetType = "branch",
                TargetId = request.NewBranchName,
                Project = project,
                RepositoryId = repositoryId,
                CorrelationId = HttpContext.Items["CorrelationId"] as string,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var push = await _repositoryService.CreateBranchAsync(project, repositoryId, request.NewBranchName, request.SourceBranch);
                evt.Success = true;
                evt.TargetId = request.NewBranchName;
                await _auditLogger.AuditAsync(evt);
                return Ok(push);
            }
            catch (Exception ex)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogError(ex, "Error creating branch {NewBranchName} from {SourceBranch} in repository {RepositoryId}", 
                    SanitizeForLog(request.NewBranchName), SanitizeForLog(request.SourceBranch), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpPost("files/{project}/{repositoryId}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Contributor")]
        public async Task<ActionResult<GitPush>> CreateFile(string project, string repositoryId, [FromBody] CreateFileRequest request)
        {
            var evt = new ADOApi.Models.AuditEvent
            {
                Action = "CreateFile",
                TargetType = "file",
                TargetId = request.Path,
                Project = project,
                RepositoryId = repositoryId,
                CorrelationId = HttpContext.Items["CorrelationId"] as string,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            try
            {
                var push = await _repositoryService.CreateFileAsync(project, repositoryId, request.Path, request.Content, request.Branch, request.CommitMessage);
                evt.Success = true;
                await _auditLogger.AuditAsync(evt);
                return Ok(push);
            }
            catch (Exception ex)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogError(ex, "Error creating file {Path} in branch {Branch} of repository {RepositoryId}", 
                    SanitizeForLog(request.Path), SanitizeForLog(request.Branch), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpPut("files/{project}/{repositoryId}")]
        [ProducesResponseType(typeof(GitPush), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Contributor")]
        public async Task<ActionResult<GitPush>> UpdateFile(string project, string repositoryId, [FromBody] UpdateFileRequest request)
        {
            var evt = new ADOApi.Models.AuditEvent
            {
                Action = "UpdateFile",
                TargetType = "file",
                TargetId = request.Path,
                Project = project,
                RepositoryId = repositoryId,
                CorrelationId = HttpContext.Items["CorrelationId"] as string,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            if (string.IsNullOrEmpty(request.BaseCommitId))
            {
                return BadRequest("BaseCommitId is required for optimistic concurrency");
            }

            try
            {
                var push = await _repositoryService.UpdateFileAsync(project, repositoryId, request.Path, request.Content, request.Branch, request.CommitMessage, request.BaseCommitId);
                evt.Success = true;
                await _auditLogger.AuditAsync(evt);
                return Ok(push);
            }
            catch (ADOApi.Exceptions.AzureDevOpsApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogWarning(ex, "Conflict updating file {Path} in branch {Branch} of repository {RepositoryId}", SanitizeForLog(request.Path), SanitizeForLog(request.Branch), SanitizeForLog(repositoryId));
                return Conflict(new { error = "Conflict", details = "The resource has been modified. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogError(ex, "Error updating file {Path} in branch {Branch} of repository {RepositoryId}", 
                    SanitizeForLog(request.Path), SanitizeForLog(request.Branch), SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpDelete("files/{project}/{repositoryId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "ADO.Contributor")]
        public async Task<ActionResult> DeleteFile(string project, string repositoryId, [FromBody] DeleteFileRequest request)
        {
            var evt = new ADOApi.Models.AuditEvent
            {
                Action = "DeleteFile",
                TargetType = "file",
                TargetId = request.Path,
                Project = project,
                RepositoryId = repositoryId,
                CorrelationId = HttpContext.Items["CorrelationId"] as string,
                ClientIp = HttpContext.Items["ClientIp"] as string,
                UserAgent = HttpContext.Items["UserAgent"] as string,
                ActorObjectId = HttpContext.Items["ActorObjectId"] as string,
                ActorUpn = HttpContext.Items["ActorUpn"] as string
            };

            if (string.IsNullOrEmpty(request.BaseCommitId))
            {
                return BadRequest("BaseCommitId is required for optimistic concurrency");
            }

            try
            {
                await _repositoryService.DeleteFileAsync(project, repositoryId, request.Path, request.Branch, request.CommitMessage, request.BaseCommitId);
                evt.Success = true;
                await _auditLogger.AuditAsync(evt);
                return NoContent();
            }
            catch (ADOApi.Exceptions.AzureDevOpsApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogWarning(ex, "Conflict deleting file {Path} in branch {Branch} of repository {RepositoryId}", SanitizeForLog(request.Path), SanitizeForLog(request.Branch), SanitizeForLog(repositoryId));
                return Conflict(new { error = "Conflict", details = "The resource has been modified. Please refresh and try again." });
            }
            catch (Exception ex)
            {
                evt.Success = false;
                evt.ErrorMessage = ex.Message;
                await _auditLogger.AuditAsync(evt);
                _logger.LogError(ex, "Error deleting file in repository {RepositoryId}", SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }

        [HttpGet("structure/{project}/{repositoryId}")]
        public async Task<ActionResult<RepositoryStructure>> GetRepositoryStructure(string project, string repositoryId, [FromQuery] string? path = null)
        {
            try
            {
                var structure = await _repositoryService.GetRepositoryStructureAsync(project, repositoryId, path);
                return Ok(structure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository structure for repository {RepositoryId}", SanitizeForLog(repositoryId));
                return StatusCode(500, "An internal error occurred");
            }
        }
    }

    public class CreateBranchRequest
    {
        public required string NewBranchName { get; set; }
        public required string SourceBranch { get; set; }
    }

    public class CreateFileRequest
    {
        public required string Path { get; set; }
        public required string Content { get; set; }
        public required string Branch { get; set; }
        public required string CommitMessage { get; set; }
    }

    public class UpdateFileRequest
    {
        public required string Path { get; set; }
        public required string Content { get; set; }
        public required string Branch { get; set; }
        public required string CommitMessage { get; set; }
        // The base commit id (objectId) that the client observed. Required for optimistic concurrency.
        public required string BaseCommitId { get; set; }
    }

    public class DeleteFileRequest
    {
        public required string Path { get; set; }
        public required string Branch { get; set; }
        public required string CommitMessage { get; set; }
        // The base commit id (objectId) that the client observed. Required for optimistic concurrency.
        public required string BaseCommitId { get; set; }
    }
} 