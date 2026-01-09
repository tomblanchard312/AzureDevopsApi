using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ADOApi.Services
{
    public class FingerprintService : IFingerprintService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<FingerprintService> _logger;

        public FingerprintService(SecurityAdvisorDbContext context, ILogger<FingerprintService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public string CreateFingerprint(string ruleId, string? filePath, string? codeSnippet, string message)
        {
            using var sha256 = SHA256.Create();
            var input = $"{ruleId}:{filePath}:{codeSnippet}:{message}";
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public string CreateFingerprint(string ruleId, string filePath, int? startLine, int? endLine, string message)
        {
            using var sha256 = SHA256.Create();
            var input = $"{ruleId}:{filePath}:{startLine}:{endLine}:{message}";
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}