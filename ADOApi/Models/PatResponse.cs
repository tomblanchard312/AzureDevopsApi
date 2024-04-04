namespace ADOApi.Models
{
    public class PatResponse
    {
        public string AccessToken { get; set; }
        public string DisplayName { get; set; }
        public string Scope { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
