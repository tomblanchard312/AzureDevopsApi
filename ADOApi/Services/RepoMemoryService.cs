using Microsoft.Extensions.Logging;
using ADOApi.Interfaces;
using ADOApi.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace ADOApi.Services
{
    public class RepoMemoryService : IRepoMemoryService
    {
        private readonly SecurityAdvisorDbContext _context;
        private readonly ILogger<RepoMemoryService> _logger;

        public RepoMemoryService(SecurityAdvisorDbContext context, ILogger<RepoMemoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RepoSnapshot> CreateSnapshotAsync(string repoKey, string branch, string commitSha, string? summary, string? statsJson)
        {
            var snapshot = new RepoSnapshot
            {
                RepoKey = repoKey,
                Branch = branch,
                CommitSha = commitSha,
                Summary = summary,
                StatsJson = statsJson,
                CapturedUtc = DateTime.UtcNow
            };

            _context.RepoSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created snapshot for {RepoKey} at {CommitSha}", repoKey, commitSha);
            return snapshot;
        }

        public async Task<RepositoryMemory> CreateMemoryAsync(string repoKey, string memoryType, string title, string content, string source, double confidence, string userId)
        {
            var memory = new RepositoryMemory
            {
                RepoKey = repoKey,
                MemoryType = memoryType,
                Title = title,
                Content = content,
                Source = source,
                Confidence = confidence,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow,
                IsActive = true
            };

            _context.RepositoryMemories.Add(memory);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created memory '{Title}' for {RepoKey} by {UserId}", title, repoKey, userId);
            return memory;
        }

        public async Task<RepositoryMemory?> GetMemoryAsync(Guid id)
        {
            return await _context.RepositoryMemories.FindAsync(id);
        }

        public async Task<IEnumerable<RepositoryMemory>> GetMemoriesAsync(string repoKey, string? memoryType = null, bool activeOnly = true)
        {
            var query = _context.RepositoryMemories.Where(m => m.RepoKey == repoKey);

            if (activeOnly)
                query = query.Where(m => m.IsActive);

            if (!string.IsNullOrEmpty(memoryType))
                query = query.Where(m => m.MemoryType == memoryType);

            return await query.OrderByDescending(m => m.UpdatedUtc).ToListAsync();
        }

        public async Task<RepositoryMemory> UpdateMemoryAsync(Guid id, string title, string content, double confidence, string userId)
        {
            var memory = await _context.RepositoryMemories.FindAsync(id);
            if (memory == null)
                throw new KeyNotFoundException($"Memory {id} not found");

            memory.Title = title;
            memory.Content = content;
            memory.Confidence = confidence;
            memory.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated memory '{Title}' by {UserId}", title, userId);
            return memory;
        }

        public async Task DeactivateMemoryAsync(Guid id, string userId)
        {
            var memory = await _context.RepositoryMemories.FindAsync(id);
            if (memory == null)
                throw new KeyNotFoundException($"Memory {id} not found");

            memory.IsActive = false;
            memory.UpdatedUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deactivated memory '{Title}' by {UserId}", memory.Title, userId);
        }

        public async Task<bool> ValidateMemoryAsync(Guid id)
        {
            var memory = await _context.RepositoryMemories.FindAsync(id);
            if (memory == null)
                return false;

            memory.LastValidatedUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Validated memory '{Title}'", memory.Title);
            return true;
        }
    }
}