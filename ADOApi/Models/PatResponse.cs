namespace ADOApi.Models
{
    public class PatResponse
    {
        public required string AccessToken { get; set; }
        public required string DisplayName { get; set; }
        public required string Scope { get; set; }
        public DateTime ValidTo { get; set; }
        public bool AllOrgs { get; set; }
    }
}
