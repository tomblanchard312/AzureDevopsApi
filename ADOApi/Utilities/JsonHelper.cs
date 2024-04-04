using System.Text.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace ADOApi.Utilities
{
    public class JsonHelper
    {
        public static string EscapeSpecialCharacters(string json)
        {
            // Define a regular expression pattern to match the characters "<" and ">"
            string pattern = @"<(.*?)>";

            // Replace all occurrences of "<" and ">" with their corresponding HTML entities
            string escapedJson = Regex.Replace(json, pattern, "&lt;$1&gt;");

            return escapedJson;
        }
        // Method to unescape the JSON string
        public static string Unescape(string escapedString)
        {
            return Regex.Unescape(escapedString);
        }
    }
}
