using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace ADOApi.Services
{
    public class DocsGenerationService : IDocsGenerationService
    {
        private readonly IRepositoryService _repositoryService;
        private readonly ISemanticChatService _chatService;
        private readonly ILogger<DocsGenerationService> _logger;

        private static readonly HashSet<string> AllowedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "README.md",
            "ARCHITECTURE.md",
            "DEV_GUIDE.md",
            "SECURITY.md",
            "DEPLOYMENT.md"
        };

        public DocsGenerationService(IRepositoryService repositoryService, ISemanticChatService chatService, ILogger<DocsGenerationService> logger)
        {
            _repositoryService = repositoryService;
            _chatService = chatService;
            _logger = logger;
        }

        public async Task<DocsPreviewResponse> GenerateDocumentationAsync(string project, string repositoryId, List<string> filesToGenerate)
        {
            _logger.LogInformation("Generating documentation for project {Project}, repo {RepositoryId}", project, repositoryId);

            // Validate allowed files
            var invalidFiles = filesToGenerate.Where(f => !AllowedFiles.Contains(f)).ToList();
            if (invalidFiles.Any())
            {
                throw new ArgumentException($"The following files are not allowed for generation: {string.Join(", ", invalidFiles)}. Only documentation files are permitted.");
            }

            // Read repository structure
            var structure = await _repositoryService.GetRepositoryStructureAsync(project, repositoryId);

            // Read key files for better summarization
            var keyFilesContent = await ReadKeyFilesAsync(project, repositoryId);

            var structureJson = JsonSerializer.Serialize(structure);
            var keyFilesJson = JsonSerializer.Serialize(keyFilesContent);

            var response = new DocsPreviewResponse();

            foreach (var fileName in filesToGenerate)
            {
                if (!fileName.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue; // Only generate .md files
                }

                var systemMessage = GetSystemMessageForFile(fileName);
                var userMessage = $"Repository structure: {structureJson}\n\nKey files content: {keyFilesJson}";

                var content = await _chatService.GetChatResponseAsync(systemMessage, userMessage);

                response.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = fileName,
                    Content = content
                });
            }

            return response;
        }

        private async Task<Dictionary<string, string>> ReadKeyFilesAsync(string project, string repositoryId)
        {
            var keyFiles = new Dictionary<string, string>
            {
                { "README.md", "" },
                { "Program.cs", "" },
                { "ADOApi.csproj", "" },
                { "ARCHITECTURE.md", "" }
            };

            foreach (var file in keyFiles.Keys.ToList())
            {
                try
                {
                    var item = await _repositoryService.GetFileContentAsync(project, repositoryId, file);
                    if (item != null)
                    {
                        keyFiles[file] = item.Content ?? "";
                    }
                }
                catch
                {
                    // File not found, keep empty
                }
            }

            return keyFiles;
        }

        private string GetSystemMessageForFile(string fileName)
        {
            return fileName.ToLower() switch
            {
                "readme.md" => "Generate a comprehensive README.md for a .NET API project. Include sections for overview, features, installation, usage, API endpoints, and contributing. Base it on the provided repository structure and key files content.",
                "architecture.md" => "Generate an ARCHITECTURE.md document describing the system architecture. Include components, data flow, external dependencies, security boundaries, and diagrams. Use the repository structure and key files to infer the architecture.",
                "dev_guide.md" => "Generate a DEV_GUIDE.md for developers. Include setup instructions, coding conventions, testing, debugging, and common pitfalls. Base on the .NET project structure and key files.",
                "security.md" => "Generate a SECURITY.md document covering authentication, authorization, secrets management, and security considerations. Analyze the codebase from the key files and structure.",
                "deployment.md" => "Generate a DEPLOYMENT.md for deploying to Azure App Service. Include steps for setup, configuration, monitoring, and troubleshooting. Base on the project structure.",
                _ => $"Generate content for {fileName} based on the repository structure and key files content."
            };
        }
    }
}