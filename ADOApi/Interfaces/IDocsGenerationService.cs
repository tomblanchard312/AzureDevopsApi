using System.Threading.Tasks;
using ADOApi.Models;
using System.Collections.Generic;

namespace ADOApi.Interfaces
{
    public interface IDocsGenerationService
    {
        Task<DocsPreviewResponse> GenerateDocumentationAsync(string project, string repositoryId, List<string> filesToGenerate);
    }
}