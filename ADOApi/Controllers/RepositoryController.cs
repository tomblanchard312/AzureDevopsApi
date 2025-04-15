using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
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
    public class RepositoryController : ControllerBase
    {
        private readonly IRepositoryService _repositoryService;
        private readonly ILogger<RepositoryController> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RepositoryController(IRepositoryService repositoryService, ILogger<RepositoryController> logger)
        {
            _repositoryService = repositoryService;
            _logger = logger;

            // Configure retry policy
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => 
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, 
                            "Retry {RetryCount} after {Delay}ms due to: {Message}", 
                            retryCount, timeSpan.TotalMilliseconds, exception.Message);
                    });
        }

        [HttpGet("repositories/{project}")]
        public async Task<ActionResult<List<GitRepository>>> GetRepositories(string project)
        {
            try
            {
                var repositories = await _repositoryService.GetRepositoriesAsync(project);
                return Ok(repositories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repositories for project {Project}", project);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting file content for {Path} in repository {RepositoryId}", path, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting directory contents for {Path} in repository {RepositoryId}", path, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting commit {CommitId} in repository {RepositoryId}", commitId, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting commits for branch {Branch} in repository {RepositoryId}", branch, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting branch {BranchName} in repository {RepositoryId}", branchName, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("branches/{project}/{repositoryId}")]
        public async Task<ActionResult<List<GitRef>>> GetBranches(string project, string repositoryId)
        {
            try
            {
                var branches = await _repositoryService.GetBranchesAsync(project, repositoryId);
                return Ok(branches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting branches for repository {RepositoryId}", repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("branches/{project}/{repositoryId}")]
        public async Task<ActionResult<GitPush>> CreateBranch(string project, string repositoryId, [FromBody] CreateBranchRequest request)
        {
            try
            {
                var push = await _repositoryService.CreateBranchAsync(project, repositoryId, request.NewBranchName, request.SourceBranch);
                return Ok(push);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating branch {NewBranchName} from {SourceBranch} in repository {RepositoryId}", 
                    request.NewBranchName, request.SourceBranch, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPost("files/{project}/{repositoryId}")]
        public async Task<ActionResult<GitPush>> CreateFile(string project, string repositoryId, [FromBody] CreateFileRequest request)
        {
            try
            {
                var push = await _repositoryService.CreateFileAsync(project, repositoryId, request.Path, request.Content, request.Branch, request.CommitMessage);
                return Ok(push);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file {Path} in branch {Branch} of repository {RepositoryId}", 
                    request.Path, request.Branch, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpPut("files/{project}/{repositoryId}")]
        public async Task<ActionResult<GitPush>> UpdateFile(string project, string repositoryId, [FromBody] UpdateFileRequest request)
        {
            try
            {
                var push = await _repositoryService.UpdateFileAsync(project, repositoryId, request.Path, request.Content, request.Branch, request.CommitMessage);
                return Ok(push);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file {Path} in branch {Branch} of repository {RepositoryId}", 
                    request.Path, request.Branch, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpDelete("files/{project}/{repositoryId}")]
        public async Task<ActionResult> DeleteFile(string project, string repositoryId, [FromBody] DeleteFileRequest request)
        {
            try
            {
                await _repositoryService.DeleteFileAsync(project, repositoryId, request.Path, request.Branch, request.CommitMessage);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {Path} in branch {Branch} of repository {RepositoryId}", 
                    request.Path, request.Branch, repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
                _logger.LogError(ex, "Error getting repository structure for repository {RepositoryId}", repositoryId);
                return StatusCode(500, $"Error: {ex.Message}");
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
    }

    public class DeleteFileRequest
    {
        public required string Path { get; set; }
        public required string Branch { get; set; }
        public required string CommitMessage { get; set; }
    }
} 