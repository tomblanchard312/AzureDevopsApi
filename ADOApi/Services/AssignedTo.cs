using ADOApi.Models;

namespace ADOApi.Services
{
    internal class AssignedTo : IdentityRef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string UniqueName { get; set; }
        public string Descriptor { get; set; }
    }
}