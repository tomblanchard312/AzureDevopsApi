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

// API Error type
export interface ApiError {
  message: string;
  statusCode: number;
  correlationId?: string;
  details?: Record<string, unknown>;
}