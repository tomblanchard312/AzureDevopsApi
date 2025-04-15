using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ADOApi.Interfaces;
using ADOApi.Exceptions;
using Polly;
using Polly.Retry;

namespace ADOApi.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly GitHttpClient _gitClient;
        private readonly ILogger<RepositoryService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public RepositoryService(GitHttpClient gitClient, ILogger<RepositoryService> logger)
        {
            _gitClient = gitClient;
            _logger = logger;

            // Configure retry policy
            _retryPolicy = Polly.Policy
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

        public async Task<List<GitRepository>> GetRepositoriesAsync(string project)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var repositories = await _gitClient.GetRepositoriesAsync(project);
                    return repositories;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get repositories for project {Project}", project);
                throw new AzureDevOpsApiException("Failed to get repositories", ex);
            }
        }

        public async Task<GitItem> GetFileContentAsync(string project, string repositoryId, string path, string? version = null)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var item = await _gitClient.GetItemAsync(
                        repositoryId: repositoryId,
                        path: path,
                        project: project,
                        versionDescriptor: version != null ? new GitVersionDescriptor { Version = version } : null);
                    return item;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get file content for {Path} in repository {RepositoryId}", path, repositoryId);
                throw new AzureDevOpsApiException("Failed to get file content", ex);
            }
        }

        public async Task<List<GitItem>> GetDirectoryContentsAsync(string project, string repositoryId, string path, string? version = null)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var items = await _gitClient.GetItemsAsync(
                        project,
                        repositoryId,
                        path,
                        versionDescriptor: version != null ? new GitVersionDescriptor { Version = version } : null);
                    return items;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get directory contents for {Path} in repository {RepositoryId}", path, repositoryId);
                throw new AzureDevOpsApiException("Failed to get directory contents", ex);
            }
        }

        public async Task<GitCommit> GetCommitAsync(string project, string repositoryId, string commitId)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var commit = await _gitClient.GetCommitAsync(commitId, repositoryId, project);
                    return commit;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get commit {CommitId} in repository {RepositoryId}", commitId, repositoryId);
                throw new AzureDevOpsApiException("Failed to get commit", ex);
            }
        }

        public async Task<List<GitCommit>> GetCommitsAsync(string project, string repositoryId, string branch = "main", int top = 10)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var commits = await _gitClient.GetCommitsAsync(
                        project,
                        repositoryId,
                        searchCriteria: new GitQueryCommitsCriteria { Top = top });
                    return commits.Select(c => new GitCommit
                    {
                        CommitId = c.CommitId,
                        Comment = c.Comment,
                        Author = c.Author,
                        Committer = c.Committer,
                        ChangeCounts = c.ChangeCounts,
                        Parents = c.Parents,
                        Push = c.Push,
                        RemoteUrl = c.RemoteUrl,
                        Url = c.Url
                    }).ToList();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get commits for branch {Branch} in repository {RepositoryId}", branch, repositoryId);
                throw new AzureDevOpsApiException("Failed to get commits", ex);
            }
        }

        public async Task<GitRef> GetBranchAsync(string project, string repositoryId, string branchName)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var branch = await _gitClient.GetRefsAsync(
                        project,
                        repositoryId,
                        filter: $"refs/heads/{branchName}",
                        includeStatuses: true,
                        includeLinks: true);
                    var result = branch.FirstOrDefault();
                    if (result == null)
                    {
                        throw new AzureDevOpsApiException($"Branch {branchName} not found");
                    }
                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get branch {BranchName} in repository {RepositoryId}", branchName, repositoryId);
                throw new AzureDevOpsApiException("Failed to get branch", ex);
            }
        }

        public async Task<List<GitRef>> GetBranchesAsync(string project, string repositoryId)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var branches = await _gitClient.GetRefsAsync(
                        project,
                        repositoryId,
                        filter: null,
                        includeStatuses: true,
                        includeLinks: true);
                    return branches;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get branches for repository {RepositoryId}", repositoryId);
                throw new AzureDevOpsApiException("Failed to get branches", ex);
            }
        }

        public async Task<GitPush> CreateBranchAsync(string project, string repositoryId, string newBranchName, string sourceBranch)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var sourceRef = await GetBranchAsync(project, repositoryId, sourceBranch);
                    if (sourceRef == null)
                    {
                        throw new AzureDevOpsApiException($"Source branch {sourceBranch} not found");
                    }

                    var newRef = new GitRefUpdate
                    {
                        Name = $"refs/heads/{newBranchName}",
                        OldObjectId = "0000000000000000000000000000000000000000",
                        NewObjectId = sourceRef.ObjectId
                    };

                    var refUpdateResult = await _gitClient.UpdateRefsAsync(
                        new List<GitRefUpdate> { newRef },
                        project,
                        repositoryId,
                        repositoryId);
                    
                    if (refUpdateResult.FirstOrDefault()?.Success != true)
                    {
                        throw new AzureDevOpsApiException("Failed to create branch");
                    }

                    // Create an empty push object since the interface requires it
                    return new GitPush 
                    { 
                        RefUpdates = new List<GitRefUpdate> { newRef },
                        Repository = new GitRepository { Id = Guid.Parse(repositoryId) }
                    };
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create branch {NewBranchName} from {SourceBranch} in repository {RepositoryId}", 
                    newBranchName, sourceBranch, repositoryId);
                throw new AzureDevOpsApiException("Failed to create branch", ex);
            }
        }

        public async Task<GitPush> CreateFileAsync(string project, string repositoryId, string path, string content, string branch, string commitMessage)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var branchRef = await GetBranchAsync(project, repositoryId, branch);
                    if (branchRef == null)
                    {
                        throw new AzureDevOpsApiException($"Branch {branch} not found");
                    }

                    var change = new GitChange
                    {
                        ChangeType = VersionControlChangeType.Add,
                        Item = new GitItem { Path = path },
                        NewContent = new ItemContent
                        {
                            Content = content,
                            ContentType = ItemContentType.RawText
                        }
                    };

                    var push = new GitPush
                    {
                        RefUpdates = new List<GitRefUpdate>
                        {
                            new GitRefUpdate
                            {
                                Name = $"refs/heads/{branch}",
                                OldObjectId = branchRef.ObjectId
                            }
                        },
                        Commits = new List<GitCommitRef>
                        {
                            new GitCommitRef
                            {
                                Comment = commitMessage,
                                Changes = new List<GitChange> { change }
                            }
                        }
                    };

                    return await _gitClient.CreatePushAsync(push, repositoryId, project);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create file {Path} in branch {Branch} of repository {RepositoryId}", 
                    path, branch, repositoryId);
                throw new AzureDevOpsApiException("Failed to create file", ex);
            }
        }

        public async Task<GitPush> UpdateFileAsync(string project, string repositoryId, string path, string content, string branch, string commitMessage)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var branchRef = await GetBranchAsync(project, repositoryId, branch);
                    if (branchRef == null)
                    {
                        throw new AzureDevOpsApiException($"Branch {branch} not found");
                    }

                    var change = new GitChange
                    {
                        ChangeType = VersionControlChangeType.Edit,
                        Item = new GitItem { Path = path },
                        NewContent = new ItemContent
                        {
                            Content = content,
                            ContentType = ItemContentType.RawText
                        }
                    };

                    var push = new GitPush
                    {
                        RefUpdates = new List<GitRefUpdate>
                        {
                            new GitRefUpdate
                            {
                                Name = $"refs/heads/{branch}",
                                OldObjectId = branchRef.ObjectId
                            }
                        },
                        Commits = new List<GitCommitRef>
                        {
                            new GitCommitRef
                            {
                                Comment = commitMessage,
                                Changes = new List<GitChange> { change }
                            }
                        }
                    };

                    return await _gitClient.CreatePushAsync(push, repositoryId, project);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update file {Path} in branch {Branch} of repository {RepositoryId}", 
                    path, branch, repositoryId);
                throw new AzureDevOpsApiException("Failed to update file", ex);
            }
        }

        public async Task DeleteFileAsync(string project, string repositoryId, string path, string branch, string commitMessage)
        {
            try
            {
                await _retryPolicy.ExecuteAsync(async () =>
                {
                    var branchRef = await GetBranchAsync(project, repositoryId, branch);
                    if (branchRef == null)
                    {
                        throw new AzureDevOpsApiException($"Branch {branch} not found");
                    }

                    var change = new GitChange
                    {
                        ChangeType = VersionControlChangeType.Delete,
                        Item = new GitItem { Path = path }
                    };

                    var push = new GitPush
                    {
                        RefUpdates = new List<GitRefUpdate>
                        {
                            new GitRefUpdate
                            {
                                Name = $"refs/heads/{branch}",
                                OldObjectId = branchRef.ObjectId
                            }
                        },
                        Commits = new List<GitCommitRef>
                        {
                            new GitCommitRef
                            {
                                Comment = commitMessage,
                                Changes = new List<GitChange> { change }
                            }
                        }
                    };

                    await _gitClient.CreatePushAsync(push, repositoryId, project);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete file {Path} in branch {Branch} of repository {RepositoryId}", 
                    path, branch, repositoryId);
                throw new AzureDevOpsApiException("Failed to delete file", ex);
            }
        }

        public async Task<RepositoryStructure> GetRepositoryStructureAsync(string project, string repositoryId, string? path = null)
        {
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    var items = await _gitClient.GetItemsAsync(
                        project,
                        repositoryId,
                        path ?? "/",
                        recursionLevel: VersionControlRecursionType.Full);

                    var root = new RepositoryStructure
                    {
                        Path = path ?? "/",
                        IsDirectory = true
                    };

                    var pathMap = new Dictionary<string, RepositoryStructure> { { root.Path, root } };

                    foreach (var item in items)
                    {
                        var itemPath = item.Path;
                        var parentPath = System.IO.Path.GetDirectoryName(itemPath)?.Replace("\\", "/") ?? "/";
                        
                        if (!pathMap.TryGetValue(parentPath, out var parent))
                        {
                            parent = new RepositoryStructure
                            {
                                Path = parentPath,
                                IsDirectory = true
                            };
                            pathMap[parentPath] = parent;
                        }

                        var structure = new RepositoryStructure
                        {
                            Path = itemPath,
                            IsDirectory = item.IsFolder
                        };

                        parent.Children.Add(structure);
                        pathMap[itemPath] = structure;
                    }

                    return root;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get repository structure for repository {RepositoryId}", repositoryId);
                throw new AzureDevOpsApiException("Failed to get repository structure", ex);
            }
        }
    }
} 