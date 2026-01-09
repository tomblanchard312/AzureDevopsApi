import { z } from 'zod';

// Project types
export interface Project {
  id: string;
  name: string;
  description?: string;
}

export interface Repository {
  id: string;
  name: string;
  url: string;
  project: Project;
}

// Documentation types
export interface GeneratedFile {
  fileName: string;
  content: string;
}

export interface DocsPreviewRequest {
  project: string;
  repositoryId: string;
  branch?: string;
  options?: {
    includeArchitecture?: boolean;
    includeDevGuide?: boolean;
    includeSecurity?: boolean;
    includeDeployment?: boolean;
  };
}

export interface DocsPreviewResponse {
  generatedFiles: GeneratedFile[];
  correlationId: string;
}

export interface DocsApplyRequest {
  project: string;
  repositoryId: string;
  branch?: string;
  commitMessage: string;
  changeSet: GeneratedFile[];
}

export interface DocsApplyResponse {
  filesWritten: string[];
  branch: string;
  commitId: string;
  correlationId: string;
}

// Zod schemas for validation
export const ProjectSchema = z.object({
  id: z.string(),
  name: z.string(),
  description: z.string().optional(),
});

export const RepositorySchema = z.object({
  id: z.string(),
  name: z.string(),
  url: z.string(),
  project: ProjectSchema,
});

export const GeneratedFileSchema = z.object({
  fileName: z.string(),
  content: z.string(),
});

export const DocsPreviewRequestSchema = z.object({
  project: z.string(),
  repositoryId: z.string(),
  branch: z.string().optional(),
  options: z.object({
    includeArchitecture: z.boolean().optional(),
    includeDevGuide: z.boolean().optional(),
    includeSecurity: z.boolean().optional(),
    includeDeployment: z.boolean().optional(),
  }).optional(),
});

export const DocsPreviewResponseSchema = z.object({
  generatedFiles: z.array(GeneratedFileSchema),
  correlationId: z.string(),
});

export const DocsApplyRequestSchema = z.object({
  project: z.string(),
  repositoryId: z.string(),
  branch: z.string().optional(),
  commitMessage: z.string(),
  changeSet: z.array(GeneratedFileSchema),
});

export const DocsApplyResponseSchema = z.object({
  filesWritten: z.array(z.string()),
  branch: z.string(),
  commitId: z.string(),
  correlationId: z.string(),
});

// Security Advisor Zod schemas
export const SecurityFindingSchema = z.object({
  id: z.string(),
  title: z.string(),
  description: z.string(),
  severity: z.enum(['Critical', 'High', 'Medium', 'Low', 'Info', 'Unknown']),
  category: z.string(),
  filePath: z.string(),
  lineNumber: z.number().optional(),
  ruleId: z.string(),
  metadata: z.record(z.string(), z.unknown()),
  status: z.enum(['Open', 'Investigating', 'Fixed', 'Accepted', 'FalsePositive']),
  createdAt: z.string(),
  updatedAt: z.string().optional(),
  assignedTo: z.string().optional(),
  recommendations: z.array(z.lazy(() => SecurityRecommendationSchema)),
});

export const SecurityRecommendationSchema = z.object({
  id: z.string(),
  findingId: z.string(),
  title: z.string(),
  description: z.string(),
  riskAssessment: z.string(),
  remediationSteps: z.string(),
  codeChanges: z.string(),
  justification: z.string(),
  confidence: z.enum(['High', 'Medium', 'Low']),
  confidenceScore: z.number(),
  confidenceExplanation: z.string(),
  whyNotFixReasons: z.array(z.string()),
  createdAt: z.string(),
  approved: z.boolean(),
  approvedBy: z.string().optional(),
  approvedAt: z.string().optional(),
});

export const ConfidenceScoringDetailsSchema = z.object({
  severityScore: z.number(),
  fixPatternScore: z.number(),
  changeRiskScore: z.number(),
  modelAgreementScore: z.number(),
  overallScore: z.number(),
  explanation: z.string(),
  contributingFactors: z.array(z.string()),
});

export const SarifAnalysisRequestSchema = z.object({
  sarifContent: z.string(),
  repository: z.string().optional(),
  branch: z.string().optional(),
});

export const SbomAnalysisRequestSchema = z.object({
  sbomContent: z.string(),
  format: z.enum(['cyclonedx', 'spdx']).optional(),
});

export const SecurityAnalysisResponseSchema = z.object({
  findings: z.array(SecurityFindingSchema),
  totalFindings: z.number(),
  highSeverityCount: z.number(),
  criticalSeverityCount: z.number(),
  analysisId: z.string(),
});

export const RecommendationRequestSchema = z.object({
  context: z.string().optional(),
  codeSnippet: z.string().optional(),
  language: z.string().optional(),
});

export const DiffRequestSchema = z.object({
  filePath: z.string().optional(),
  originalContent: z.string().optional(),
  modifiedContent: z.string().optional(),
});

export const DiffResponseSchema = z.object({
  diffContent: z.string(),
  hunks: z.array(z.object({
    oldStart: z.number(),
    oldLines: z.number(),
    newStart: z.number(),
    newLines: z.number(),
    lines: z.array(z.string()),
  })),
  isValid: z.boolean(),
  errorMessage: z.string().optional(),
});

export const ApplyRequestSchema = z.object({
  repository: z.string().optional(),
  branch: z.string().optional(),
  commitMessage: z.string().optional(),
  authorName: z.string().optional(),
  authorEmail: z.string().optional(),
});

export const ApplyResponseSchema = z.object({
  success: z.boolean(),
  commitId: z.string().optional(),
  pullRequestUrl: z.string().optional(),
  errorMessage: z.string().optional(),
});

// API Error type
export interface ApiError {
  message: string;
  statusCode: number;
  correlationId?: string;
  details?: Record<string, unknown>;
}

// Security Advisor types
export interface SecurityFinding {
  id: string;
  title: string;
  description: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low' | 'Info' | 'Unknown';
  category: string;
  filePath: string;
  lineNumber?: number;
  ruleId: string;
  metadata: Record<string, unknown>;
  status: 'Open' | 'Investigating' | 'Fixed' | 'Accepted' | 'FalsePositive';
  createdAt: string;
  updatedAt?: string;
  assignedTo?: string;
  recommendations: SecurityRecommendation[];
}

export interface SecurityRecommendation {
  id: string;
  findingId: string;
  title: string;
  description: string;
  riskAssessment: string;
  remediationSteps: string;
  codeChanges: string;
  justification: string;
  confidence: 'High' | 'Medium' | 'Low';
  confidenceScore: number;
  confidenceExplanation: string;
  whyNotFixReasons: string[];
  createdAt: string;
  approved: boolean;
  approvedBy?: string;
  approvedAt?: string;
}

export interface ConfidenceScoringDetails {
  severityScore: number;
  fixPatternScore: number;
  changeRiskScore: number;
  modelAgreementScore: number;
  overallScore: number;
  explanation: string;
  contributingFactors: string[];
}

export interface SarifAnalysisRequest {
  sarifContent: string;
  repository?: string;
  branch?: string;
}

export interface SbomAnalysisRequest {
  sbomContent: string;
  format?: 'cyclonedx' | 'spdx';
}

export interface SecurityAnalysisResponse {
  findings: SecurityFinding[];
  totalFindings: number;
  highSeverityCount: number;
  criticalSeverityCount: number;
  analysisId: string;
}

export interface RecommendationRequest {
  context?: string;
  codeSnippet?: string;
  language?: string;
}

export interface DiffRequest {
  filePath?: string;
  originalContent?: string;
  modifiedContent?: string;
}

export interface DiffResponse {
  diffContent: string;
  hunks: DiffHunk[];
  isValid: boolean;
  errorMessage?: string;
}

export interface DiffHunk {
  oldStart: number;
  oldLines: number;
  newStart: number;
  newLines: number;
  lines: string[];
}

export interface ApplyRequest {
  repository?: string;
  branch?: string;
  commitMessage?: string;
  authorName?: string;
  authorEmail?: string;
}

export interface ApplyResponse {
  success: boolean;
  commitId?: string;
  pullRequestUrl?: string;
  errorMessage?: string;
}

export interface PullRequestCommentRequest {
  organization: string;
  project: string;
  pullRequestId: number;
  repositoryId: string;
  findingIds?: string[];
  previewOnly?: boolean;
}

export interface PullRequestCommentResponse {
  success: boolean;
  commentMarkdown?: string;
  threadId?: number;
  commentUrl?: string;
  errorMessage?: string;
  postedAt?: string;
}

export interface SecurityReviewComment {
  organization: string;
  project: string;
  pullRequestId: number;
  repositoryId: string;
  findings: SecurityFindingComment[];
  overallAssessment: string;
  approvalStatus: string;
  generatedAt: string;
}

export interface SecurityFindingComment {
  findingId: string;
  title: string;
  severity: string;
  confidence: string;
  confidenceScore: number;
  description: string;
  filePath: string;
  lineNumber?: number;
  diffSnippet?: string;
  whyNotFixReasons: string[];
  recommendation: string;
}

// Enhanced PR integration types
export interface InlineCommentRequest {
  organization: string;
  project: string;
  pullRequestId: number;
  repositoryId: string;
  findingId: string;
  previewOnly?: boolean;
}

export interface InlineCommentResponse {
  success: boolean;
  commentMarkdown?: string;
  threadId?: number;
  commentUrl?: string;
  errorMessage?: string;
  postedAt?: string;
}

export interface ThreadResolutionRequest {
  organization: string;
  project: string;
  pullRequestId: number;
  repositoryId: string;
  threadIds?: number[];
}

export interface ThreadResolutionResponse {
  success: boolean;
  resolvedThreads: ResolvedThread[];
  errorMessage?: string;
}

export interface ResolvedThread {
  threadId: number;
  findingId: string;
  resolutionComment: string;
  resolvedAt: string;
}

export interface PrStatusRequest {
  organization: string;
  project: string;
  pullRequestId: number;
  repositoryId: string;
  targetUrl?: string;
}

export interface PrStatusResponse {
  success: boolean;
  status: string;
  description: string;
  statusUrl?: string;
  errorMessage?: string;
}

// Zod schemas for PR integration
export const PullRequestCommentRequestSchema = z.object({
  organization: z.string(),
  project: z.string(),
  pullRequestId: z.number(),
  repositoryId: z.string(),
  findingIds: z.array(z.string()).optional(),
  previewOnly: z.boolean().optional(),
});

export const PullRequestCommentResponseSchema = z.object({
  success: z.boolean(),
  commentMarkdown: z.string().optional(),
  threadId: z.number().optional(),
  commentUrl: z.string().optional(),
  errorMessage: z.string().optional(),
  postedAt: z.string().optional(),
});

export const InlineCommentRequestSchema = z.object({
  organization: z.string(),
  project: z.string(),
  pullRequestId: z.number(),
  repositoryId: z.string(),
  findingId: z.string(),
  previewOnly: z.boolean().optional(),
});

export const InlineCommentResponseSchema = z.object({
  success: z.boolean(),
  commentMarkdown: z.string().optional(),
  threadId: z.number().optional(),
  commentUrl: z.string().optional(),
  errorMessage: z.string().optional(),
  postedAt: z.string().optional(),
});

export const ThreadResolutionRequestSchema = z.object({
  organization: z.string(),
  project: z.string(),
  pullRequestId: z.number(),
  repositoryId: z.string(),
  threadIds: z.array(z.number()).optional(),
});

export const ResolvedThreadSchema = z.object({
  threadId: z.number(),
  findingId: z.string(),
  resolutionComment: z.string(),
  resolvedAt: z.string(),
});

export const ThreadResolutionResponseSchema = z.object({
  success: z.boolean(),
  resolvedThreads: z.array(ResolvedThreadSchema),
  errorMessage: z.string().optional(),
});

export const PrStatusRequestSchema = z.object({
  organization: z.string(),
  project: z.string(),
  pullRequestId: z.number(),
  repositoryId: z.string(),
  targetUrl: z.string().optional(),
});

export const PrStatusResponseSchema = z.object({
  success: z.boolean(),
  status: z.string(),
  description: z.string(),
  statusUrl: z.string().optional(),
  errorMessage: z.string().optional(),
});

// Repository Intelligence types
export interface RepoSnapshot {
  id: string;
  repoKey: string;
  snapshotDate: string;
  branch: string;
  commitId: string;
  fileCount: number;
  totalLines: number;
  metadata: Record<string, unknown>;
}

export interface RepositoryMemory {
  id: string;
  repoKey: string;
  memoryType: string;
  title: string;
  content: string;
  confidence: number;
  source: string;
  sourceType: string;
  lastValidated: string;
  isActive: boolean;
  metadata: Record<string, unknown>;
}

export interface CodeInsight {
  id: string;
  repoKey: string;
  fingerprint: string;
  ruleId: string;
  title: string;
  description: string;
  severity: 'Critical' | 'High' | 'Medium' | 'Low' | 'Info';
  confidence: number;
  filePath?: string;
  lineStart?: number;
  lineEnd?: number;
  evidence: string;
  status: 'Open' | 'AcceptedRisk' | 'Suppressed' | 'Fixed';
  createdAt: string;
  updatedAt?: string;
  metadata: Record<string, unknown>;
}

export interface WorkItemLink {
  id: string;
  repoKey: string;
  insightId: string;
  workItemId?: string;
  workItemType: string;
  title: string;
  description: string;
  acceptanceCriteria: string;
  whyNow?: string;
  status: 'Proposed' | 'Approved' | 'Rejected' | 'Created';
  confidence: number;
  createdAt: string;
  approved: boolean;
  approvedBy?: string;
  approvedAt?: string;
  rejectedReason?: string;
  azureDevOpsUrl?: string;
}

export interface AgentRun {
  id: string;
  repoKey: string;
  runType: string;
  model: string;
  promptVersion: string;
  policyVersion: string;
  status: 'Running' | 'Completed' | 'Failed';
  startedAt: string;
  completedAt?: string;
  inputSummary: string;
  outputSummary: string;
  errorMessage?: string;
}

export interface AgentDecision {
  id: string;
  agentRunId: string;
  decisionType: string;
  targetId: string;
  confidence: number;
  reason: string;
  metadata: Record<string, unknown>;
}

export interface AutomationPolicy {
  repoKey: string;
  autoCreateEnabled: boolean;
  allowedSeverities: ('Critical' | 'High')[];
  minimumConfidence: number;
  maxItemsPerDay: number;
  requireHumanApproval: boolean;
  allowOllamaAutoCreate: boolean;
}

export interface RepoOverview {
  repoSummary: RepoSnapshot;
  insightCounts: Record<string, number>;
  workItemCounts: {
    proposed: number;
    created: number;
  };
  policyState: 'Disabled' | 'Restricted' | 'Enabled';
}

// Zod schemas for Repository Intelligence
export const RepoSnapshotSchema = z.object({
  id: z.string(),
  repoKey: z.string(),
  snapshotDate: z.string(),
  branch: z.string(),
  commitId: z.string(),
  fileCount: z.number(),
  totalLines: z.number(),
  metadata: z.record(z.unknown()),
});

export const RepositoryMemorySchema = z.object({
  id: z.string(),
  repoKey: z.string(),
  memoryType: z.string(),
  title: z.string(),
  content: z.string(),
  confidence: z.number(),
  source: z.string(),
  sourceType: z.string(),
  lastValidated: z.string(),
  isActive: z.boolean(),
  metadata: z.record(z.unknown()),
});

export const CodeInsightSchema = z.object({
  id: z.string(),
  repoKey: z.string(),
  fingerprint: z.string(),
  ruleId: z.string(),
  title: z.string(),
  description: z.string(),
  severity: z.enum(['Critical', 'High', 'Medium', 'Low', 'Info']),
  confidence: z.number(),
  filePath: z.string().optional(),
  lineStart: z.number().optional(),
  lineEnd: z.number().optional(),
  evidence: z.string(),
  status: z.enum(['Open', 'AcceptedRisk', 'Suppressed', 'Fixed']),
  createdAt: z.string(),
  updatedAt: z.string().optional(),
  metadata: z.record(z.unknown()),
});

export const WorkItemLinkSchema = z.object({
  id: z.string(),
  repoKey: z.string(),
  insightId: z.string(),
  workItemId: z.string().optional(),
  workItemType: z.string(),
  title: z.string(),
  description: z.string(),
  acceptanceCriteria: z.string(),
  whyNow: z.string().optional(),
  status: z.enum(['Proposed', 'Approved', 'Rejected', 'Created']),
  confidence: z.number(),
  createdAt: z.string(),
  approved: z.boolean(),
  approvedBy: z.string().optional(),
  approvedAt: z.string().optional(),
  rejectedReason: z.string().optional(),
  azureDevOpsUrl: z.string().optional(),
});

export const AgentRunSchema = z.object({
  id: z.string(),
  repoKey: z.string(),
  runType: z.string(),
  model: z.string(),
  promptVersion: z.string(),
  policyVersion: z.string(),
  status: z.enum(['Running', 'Completed', 'Failed']),
  startedAt: z.string(),
  completedAt: z.string().optional(),
  inputSummary: z.string(),
  outputSummary: z.string(),
  errorMessage: z.string().optional(),
});

export const AgentDecisionSchema = z.object({
  id: z.string(),
  agentRunId: z.string(),
  decisionType: z.string(),
  targetId: z.string(),
  confidence: z.number(),
  reason: z.string(),
  metadata: z.record(z.unknown()),
});

export const AutomationPolicySchema = z.object({
  repoKey: z.string(),
  autoCreateEnabled: z.boolean(),
  allowedSeverities: z.array(z.enum(['Critical', 'High'])),
  minimumConfidence: z.number(),
  maxItemsPerDay: z.number(),
  requireHumanApproval: z.boolean(),
  allowOllamaAutoCreate: z.boolean(),
});

export const RepoOverviewSchema = z.object({
  repoSummary: RepoSnapshotSchema,
  insightCounts: z.record(z.number()),
  workItemCounts: z.object({
    proposed: z.number(),
    created: z.number(),
  }),
  policyState: z.enum(['Disabled', 'Restricted', 'Enabled']),
});