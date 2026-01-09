using System.Collections.Generic;

namespace ADOApi.Models
{
    public class DocsPreviewRequest
    {
        public string Project { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public List<string> FilesToGenerate { get; set; } = new List<string>();
    }

    public class DocsPreviewResponse
    {
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
    }

    public class GeneratedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class DocsApplyRequest
    {
        public string Project { get; set; } = string.Empty;
        public string RepositoryId { get; set; } = string.Empty;
        public List<GeneratedFile> FilesToApply { get; set; } = new List<GeneratedFile>();
        public string Branch { get; set; } = "master";
        public string CommitMessage { get; set; } = "Update documentation";
    }

    public class DocsApplyResponse
    {
        public List<string> FilesWritten { get; set; } = new List<string>();
        public string Branch { get; set; } = string.Empty;
        public string CommitId { get; set; } = string.Empty;
    }
}