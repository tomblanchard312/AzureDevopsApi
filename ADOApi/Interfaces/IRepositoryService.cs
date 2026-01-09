using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ADOApi.Interfaces
{
    public interface IRepositoryService
    {
        Task<List<GitRepository>> GetRepositoriesAsync(string project);
        Task<GitItem> GetFileContentAsync(string project, string repositoryId, string path, string? version = null);
        Task<List<GitItem>> GetDirectoryContentsAsync(string project, string repositoryId, string path, string? version = null);
        Task<GitCommit> GetCommitAsync(string project, string repositoryId, string commitId);
        Task<List<GitCommit>> GetCommitsAsync(string project, string repositoryId, string branch = "main", int top = 10);
        Task<GitRef> GetBranchAsync(string project, string repositoryId, string branchName);
        Task<List<GitRef>> GetBranchesAsync(string project, string repositoryId);
        Task<GitPush> CreateBranchAsync(string project, string repositoryId, string newBranchName, string sourceBranch);
        Task<GitPush> CreateFileAsync(string project, string repositoryId, string path, string content, string branch, string commitMessage);
        Task<GitPush> UpdateFileAsync(string project, string repositoryId, string path, string content, string branch, string commitMessage, string baseCommitId);
        Task DeleteFileAsync(string project, string repositoryId, string path, string branch, string commitMessage, string baseCommitId);
        Task<RepositoryStructure> GetRepositoryStructureAsync(string project, string repositoryId, string? path = null);
    }

    public class RepositoryStructure
    {
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public List<RepositoryStructure> Children { get; set; } = new List<RepositoryStructure>();
    }
} 