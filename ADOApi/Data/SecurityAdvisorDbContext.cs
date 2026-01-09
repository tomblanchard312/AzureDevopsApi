using Microsoft.EntityFrameworkCore;
using ADOApi.Models;

namespace ADOApi.Data
{
    public class SecurityAdvisorDbContext : DbContext
    {
        public SecurityAdvisorDbContext(DbContextOptions<SecurityAdvisorDbContext> options)
            : base(options)
        {
        }

        public DbSet<SecurityEventEntity> SecurityEvents { get; set; } = null!;
        public DbSet<RiskAcceptanceEntity> RiskAcceptances { get; set; } = null!;
        public DbSet<PolicyOverrideEntity> PolicyOverrides { get; set; } = null!;
        public DbSet<AnalysisMetadataEntity> AnalysisMetadata { get; set; } = null!;

        // Repository Memory Layer entities
        public DbSet<RepoSnapshot> RepoSnapshots { get; set; } = null!;
        public DbSet<RepositoryMemory> RepositoryMemories { get; set; } = null!;
        public DbSet<CodeInsight> CodeInsights { get; set; } = null!;
        public DbSet<WorkItemLink> WorkItemLinks { get; set; } = null!;
        public DbSet<AgentRun> AgentRuns { get; set; } = null!;
        public DbSet<AgentDecision> AgentDecisions { get; set; } = null!;
        public DbSet<WorkItemProposalEntity> WorkItemProposals { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // SecurityEvent configuration
            modelBuilder.Entity<SecurityEventEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Organization).HasMaxLength(200);
                entity.Property(e => e.Project).HasMaxLength(200);
                entity.Property(e => e.Repository).HasMaxLength(200);
                entity.Property(e => e.FindingId).HasMaxLength(100);
                entity.Property(e => e.UserId).HasMaxLength(200);
                entity.Property(e => e.UserRole).HasMaxLength(50);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);
                entity.Property(e => e.PromptVersion).HasMaxLength(20);
                entity.Property(e => e.PolicyVersion).HasMaxLength(20);
                entity.Property(e => e.PropertiesJson).HasColumnType("TEXT");

                entity.Ignore(e => e.Properties);

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.EventType);
                entity.HasIndex(e => new { e.Organization, e.Project });
            });

            // RiskAcceptance configuration
            modelBuilder.Entity<RiskAcceptanceEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.FindingId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Organization).HasMaxLength(200);
                entity.Property(e => e.Project).HasMaxLength(200);
                entity.Property(e => e.Repository).HasMaxLength(200);
                entity.Property(e => e.Scope).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Severity).HasMaxLength(50);
                entity.Property(e => e.Justification).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.AcceptedBy).HasMaxLength(200).IsRequired();
                entity.Property(e => e.AcceptedAt).IsRequired();
                entity.Property(e => e.ExpiresAt);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.Fingerprint).HasMaxLength(500);
                entity.Property(e => e.MetadataJson).HasColumnType("TEXT");

                entity.Ignore(e => e.Metadata);

                entity.HasIndex(e => e.FindingId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.Organization, e.Project });
            });

            // PolicyOverride configuration
            modelBuilder.Entity<PolicyOverrideEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.FindingId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Organization).HasMaxLength(200);
                entity.Property(e => e.Project).HasMaxLength(200);
                entity.Property(e => e.Repository).HasMaxLength(200);
                entity.Property(e => e.OverrideType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Justification).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.RequestedBy).HasMaxLength(200).IsRequired();
                entity.Property(e => e.ApprovedBy).HasMaxLength(200);
                entity.Property(e => e.RequestedAt).IsRequired();
                entity.Property(e => e.ApprovedAt);
                entity.Property(e => e.ExpiresAt);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.MetadataJson).HasColumnType("TEXT");

                entity.Ignore(e => e.Metadata);

                entity.HasIndex(e => e.FindingId);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => new { e.Organization, e.Project });
            });

            // AnalysisMetadata configuration
            modelBuilder.Entity<AnalysisMetadataEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.AnalysisId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ModelProvider).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PromptVersion).HasMaxLength(20).IsRequired();
                entity.Property(e => e.PolicyVersion).HasMaxLength(20).IsRequired();
                entity.Property(e => e.ConfidenceBreakdownJson).HasColumnType("TEXT");
                entity.Property(e => e.InputsUsedJson).HasColumnType("TEXT");
                entity.Property(e => e.CreatedUtc).IsRequired();

                entity.Ignore(e => e.ConfidenceBreakdown);
                entity.Ignore(e => e.InputsUsed);

                entity.HasIndex(e => e.AnalysisId);
                entity.HasIndex(e => e.CreatedUtc);
                entity.HasIndex(e => new { e.ModelProvider, e.ModelName });
            });

            // RepoSnapshot configuration
            modelBuilder.Entity<RepoSnapshot>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Branch).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CommitSha).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CapturedUtc).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(1000);
                entity.Property(e => e.StatsJson).HasColumnType("TEXT");

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.Branch, e.CommitSha });
            });

            // RepositoryMemory configuration
            modelBuilder.Entity<RepositoryMemory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.MemoryType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Content).HasColumnType("TEXT").IsRequired();
                entity.Property(e => e.Source).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.CreatedUtc).IsRequired();
                entity.Property(e => e.UpdatedUtc).IsRequired();
                entity.Property(e => e.IsActive).IsRequired();

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.MemoryType, e.Title });
                entity.HasIndex(e => e.IsActive);
            });

            // CodeInsight configuration
            modelBuilder.Entity<CodeInsight>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.InsightType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.RuleId).HasMaxLength(200);
                entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Description).HasColumnType("TEXT").IsRequired();
                entity.Property(e => e.Severity).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(1000);
                entity.Property(e => e.Fingerprint).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.FirstSeenUtc).IsRequired();
                entity.Property(e => e.LastSeenUtc).IsRequired();
                entity.Property(e => e.TagsJson).HasColumnType("TEXT");
                entity.Property(e => e.EvidenceJson).HasColumnType("TEXT");
                entity.Property(e => e.RelatedMemoryIdsJson).HasColumnType("TEXT");

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.Fingerprint }).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.Severity);
                entity.HasIndex(e => e.FirstSeenUtc);
                entity.HasIndex(e => e.LastSeenUtc);

                entity.HasOne(e => e.Snapshot)
                    .WithMany()
                    .HasForeignKey(e => e.SnapshotId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // WorkItemLink configuration
            modelBuilder.Entity<WorkItemLink>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.InsightId).IsRequired();
                entity.Property(e => e.WorkItemId).IsRequired();
                entity.Property(e => e.WorkItemType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.State).HasMaxLength(50);
                entity.Property(e => e.Disposition).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Reason).HasMaxLength(500);
                entity.Property(e => e.CreatedBy).HasMaxLength(200).IsRequired();
                entity.Property(e => e.CreatedUtc).IsRequired();
                entity.Property(e => e.UpdatedUtc).IsRequired();

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.InsightId });
                entity.HasIndex(e => e.WorkItemId);
                entity.HasIndex(e => e.Disposition);

                entity.HasOne(e => e.Insight)
                    .WithMany()
                    .HasForeignKey(e => e.InsightId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // AgentRun configuration
            modelBuilder.Entity<AgentRun>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.RunType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ModelProvider).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ModelName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.PromptVersion).HasMaxLength(20).IsRequired();
                entity.Property(e => e.PolicyVersion).HasMaxLength(20).IsRequired();
                entity.Property(e => e.StartedUtc).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.InputSummaryJson).HasColumnType("TEXT");
                entity.Property(e => e.OutputSummaryJson).HasColumnType("TEXT");
                entity.Property(e => e.Error).HasColumnType("TEXT");
                entity.Property(e => e.CorrelationId).HasMaxLength(100);

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => e.RunType);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.StartedUtc);
            });

            // AgentDecision configuration
            modelBuilder.Entity<AgentDecision>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.RunId).IsRequired();
                entity.Property(e => e.DecisionType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TargetType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.TargetId).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Decision).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Justification).HasColumnType("TEXT").IsRequired();
                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.CreatedBy).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedUtc).IsRequired();

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.RunId });
                entity.HasIndex(e => e.DecisionType);
                entity.HasIndex(e => e.TargetType);

                entity.HasOne(e => e.Run)
                    .WithMany(r => r.Decisions)
                    .HasForeignKey(e => e.RunId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // WorkItemProposal configuration
            modelBuilder.Entity<WorkItemProposalEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.RepoKey).HasMaxLength(500).IsRequired();
                entity.Property(e => e.ChatMessageId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
                entity.Property(e => e.Description).HasColumnType("TEXT").IsRequired();
                entity.Property(e => e.WorkItemType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Confidence).IsRequired();
                entity.Property(e => e.Source).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasIndex(e => e.RepoKey);
                entity.HasIndex(e => new { e.RepoKey, e.ChatMessageId });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}