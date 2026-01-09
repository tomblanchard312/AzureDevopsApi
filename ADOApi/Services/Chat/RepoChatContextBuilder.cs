using ADOApi.Interfaces;
using ADOApi.Models.Chat;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System.IO;

namespace ADOApi.Services.Chat
{
    public class RepoChatContextBuilder
    {
        private readonly IRepositoryService _repositoryService;
        private readonly IWorkItemService _workItemService;

        public RepoChatContextBuilder(
            IRepositoryService repositoryService,
            IWorkItemService workItemService)
        {
            _repositoryService = repositoryService;
            _workItemService = workItemService;
        }

        public async Task<ChatContext> BuildContextAsync(RepoChatRequest request)
        {
            var context = new ChatContext
            {
                RepositoryName = ExtractRepositoryName(request.RepoKey),
                OrganizationName = ExtractOrganizationName(request.RepoKey),
                ProjectName = ExtractProjectName(request.RepoKey)
            };

            // Build repository files context
            await BuildRepositoryFilesContextAsync(context, request);

            // Build recent commits context
            await BuildRecentCommitsContextAsync(context, request);

            // Build work items context
            await BuildWorkItemsContextAsync(context, request);

            return context;
        }

        private string ExtractRepositoryName(string repoKey)
        {
            // RepoKey format: "organization/project/repository"
            var parts = repoKey.Split('/');
            return parts.Length >= 3 ? parts[2] : repoKey;
        }

        private string ExtractOrganizationName(string repoKey)
        {
            // RepoKey format: "organization/project/repository"
            var parts = repoKey.Split('/');
            return parts.Length >= 1 ? parts[0] : "unknown";
        }

        private string ExtractProjectName(string repoKey)
        {
            // RepoKey format: "organization/project/repository"
            var parts = repoKey.Split('/');
            return parts.Length >= 2 ? parts[1] : "unknown";
        }

        private async Task BuildRepositoryFilesContextAsync(ChatContext context, RepoChatRequest request)
        {
            try
            {
                // If specific files are requested, get their content
                if (request.Context.FilePaths.Length > 0)
                {
                    foreach (var filePath in request.Context.FilePaths.Take(5)) // Limit to 5 files
                    {
                        try
                        {
                            var fileItem = await _repositoryService.GetFileContentAsync(
                                context.ProjectName,
                                context.RepositoryName,
                                filePath,
                                request.Context.CommitSha);

                            // GitItem.Content is a stream, we need to read it
                            if (fileItem?.Content != null)
                            {
                                using var reader = new StreamReader(fileItem.Content);
                                var content = await reader.ReadToEndAsync();

                                // Truncate content to reasonable size
                                var truncatedContent = content.Length > 2000
                                    ? content.Substring(0, 2000) + "..."
                                    : content;

                                context.RepositoryFiles[filePath] = truncatedContent;
                            }
                        }
                        catch
                        {
                            // File not found or error, skip it
                            context.RepositoryFiles[filePath] = "[File not accessible]";
                        }
                    }
                }
                else
                {
                    // Get a sample of important files
                    var importantFiles = new[] { "README.md", "package.json", "requirements.txt", ".csproj", "Dockerfile" };
                    foreach (var fileName in importantFiles)
                    {
                        try
                        {
                            var fileItem = await _repositoryService.GetFileContentAsync(
                                context.ProjectName,
                                context.RepositoryName,
                                fileName);

                            if (fileItem?.Content != null)
                            {
                                using var reader = new StreamReader(fileItem.Content);
                                var content = await reader.ReadToEndAsync();

                                var truncatedContent = content.Length > 1000
                                    ? content.Substring(0, 1000) + "..."
                                    : content;

                                context.RepositoryFiles[fileName] = truncatedContent;
                            }
                        }
                        catch
                        {
                            // File not found, skip it
                        }
                    }
                }
            }
            catch
            {
                // Repository access failed, continue with empty context
            }
        }

        private async Task BuildRecentCommitsContextAsync(ChatContext context, RepoChatRequest request)
        {
            try
            {
                var commits = await _repositoryService.GetCommitsAsync(
                    context.ProjectName,
                    context.RepositoryName,
                    "main", // Default branch
                    5); // Get last 5 commits

                context.RecentCommits.AddRange(commits.Select(c =>
                    $"{c.CommitId?.Substring(0, 8) ?? "unknown"}: {c.Comment ?? "No comment"}"));
            }
            catch
            {
                // Commit access failed, continue with empty context
            }
        }

        private async Task BuildWorkItemsContextAsync(ChatContext context, RepoChatRequest request)
        {
            try
            {
                // Query for recent work items in the project
                var query = new Wiql
                {
                    Query = $"SELECT [System.Id], [System.Title] FROM WorkItems WHERE [System.TeamProject] = '{context.ProjectName}' ORDER BY [System.CreatedDate] DESC"
                };

                var result = await _workItemService.QueryByWiqlAsync(query, context.ProjectName);
                var workItemIds = result.WorkItems?.Take(5).Select(wi => wi.Id) ?? Enumerable.Empty<int>();

                if (workItemIds.Any())
                {
                    var workItems = await _workItemService.GetWorkItemsAsync(workItemIds);
                    context.ActiveWorkItems.AddRange(workItems.Select(wi =>
                        $"#{wi.Id}: {wi.Fields["System.Title"] ?? "No title"}"));
                }
            }
            catch
            {
                // Work item access failed, continue with empty context
            }
        }
    }
}