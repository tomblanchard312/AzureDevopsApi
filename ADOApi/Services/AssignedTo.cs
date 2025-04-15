using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADOApi.Services
{
    public class AssignedTo : IdentityRef
    {
        public new string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public new string DisplayName { get; set; } = string.Empty;
        public new string UniqueName { get; set; } = string.Empty;
        public new string Descriptor { get; set; } = string.Empty;

        public AssignedTo()
        {
            Id = string.Empty;
            Name = string.Empty;
            DisplayName = string.Empty;
            UniqueName = string.Empty;
            Descriptor = string.Empty;
        }
    }
}