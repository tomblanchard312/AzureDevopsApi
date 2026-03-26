# API Catalog

Auto-generated from controller attributes in ADOApi/Controllers on 2026-03-26.

## Inference Rules For Likely Auth Policy

1. Method-level Authorize attribute overrides class-level auth.
2. Else class-level Authorize attribute is used.
3. Else fallback is Authenticated user (prod fallback).

Production fallback policy in `ADOApi/Program.cs` requires authenticated users by default.

## Endpoint Catalog

| Method | Route | Controller | Likely Auth Policy |
|---|---|---|---|
| POST | /api/chat/repo | RepoChatController | ADO.ReadOnly (class-level) |
| POST | /api/Docs/apply | DocsController | Authenticated user (prod fallback) |
| POST | /api/Docs/preview | DocsController | Authenticated user (prod fallback) |
| GET | /api/Health/azuredevops | HealthController | ADO.Admin (class-level) |
| GET | /api/Project/iterations | ProjectController | Authenticated user (prod fallback) |
| GET | /api/Project/projects | ProjectController | Authenticated user (prod fallback) |
| GET | /api/Repository/branches/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| POST | /api/Repository/branches/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/branches/{project}/{repositoryId}/{branchName} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/commits/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/commits/{project}/{repositoryId}/{commitId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/content/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/directory/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| DELETE | /api/Repository/files/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| POST | /api/Repository/files/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| PUT | /api/Repository/files/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/repositories/{project} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/Repository/structure/{project}/{repositoryId} | RepositoryController | Authenticated user (prod fallback) |
| GET | /api/SecurityAdvisor/analysis/{analysisId}/metadata | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| POST | /api/SecurityAdvisor/analysis/{prId}/rerun | SecurityAdvisorController | ADO.Contributor (method-level) |
| POST | /api/SecurityAdvisor/analyze/sarif | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| POST | /api/SecurityAdvisor/analyze/sbom | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| POST | /api/SecurityAdvisor/apply/{recommendationId} | SecurityAdvisorController | ADO.Contributor (method-level) |
| GET | /api/SecurityAdvisor/config/versions | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| POST | /api/SecurityAdvisor/diff/{recommendationId} | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| GET | /api/SecurityAdvisor/findings | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| GET | /api/SecurityAdvisor/findings/filtered | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| GET | /api/SecurityAdvisor/governance/metrics | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| GET | /api/SecurityAdvisor/governance/noise-policies | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| POST | /api/SecurityAdvisor/governance/noise-policy | SecurityAdvisorController | ADO.Admin (method-level) |
| POST | /api/SecurityAdvisor/governance/override | SecurityAdvisorController | ADO.Contributor (method-level) |
| POST | /api/SecurityAdvisor/governance/override/{overrideId}/approve | SecurityAdvisorController | ADO.Admin (method-level) |
| GET | /api/SecurityAdvisor/governance/overrides | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| POST | /api/SecurityAdvisor/governance/risk-acceptance | SecurityAdvisorController | ADO.Contributor (method-level) |
| GET | /api/SecurityAdvisor/governance/risk-acceptances | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| POST | /api/SecurityAdvisor/pr/inline-comment | SecurityAdvisorController | ADOContributor (method-level) |
| POST | /api/SecurityAdvisor/pr/resolve-threads | SecurityAdvisorController | ADOContributor (method-level) |
| POST | /api/SecurityAdvisor/pr/status | SecurityAdvisorController | ADOContributor (method-level) |
| POST | /api/SecurityAdvisor/pull-request/comment | SecurityAdvisorController | ADO.Contributor (method-level) |
| PUT | /api/SecurityAdvisor/pull-request/comment/{threadId} | SecurityAdvisorController | ADO.Contributor (method-level) |
| POST | /api/SecurityAdvisor/pull-request/comment/preview | SecurityAdvisorController | ADO.ReadOnly (method-level) |
| POST | /api/SecurityAdvisor/recommendations/{findingId} | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| GET | /api/SecurityAdvisor/risk-acceptance/expiring | SecurityAdvisorController | ADO.ReadOnly (class-level) |
| POST | /api/SemanticKernel/query | SemanticKernelController | Authenticated user (prod fallback) |
| POST | /api/Token/personalaccesstoken | TokenController | Authenticated user (prod fallback) |
| GET | /api/Token/personalaccesstokens | TokenController | Authenticated user (prod fallback) |
| POST | /api/WorkItem | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/{workItemId} | WorkItemController | Authenticated user (prod fallback) |
| PUT | /api/WorkItem/{workItemId} | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/{workItemId}/related | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/{workItemId}/relations | WorkItemController | Authenticated user (prod fallback) |
| POST | /api/WorkItem/{workItemId}/relations | WorkItemController | Authenticated user (prod fallback) |
| DELETE | /api/WorkItem/{workItemId}/relations/{targetWorkItemId} | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/assigned/{project}/{userIdentifier} | WorkItemController | Authenticated user (prod fallback) |
| POST | /api/WorkItem/filter | WorkItemController | Authenticated user (prod fallback) |
| POST | /api/WorkItem/from-template/{templateId} | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/project/{project} | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/recent | WorkItemController | Authenticated user (prod fallback) |
| POST | /api/WorkItem/templates | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/templates/{project} | WorkItemController | Authenticated user (prod fallback) |
| DELETE | /api/WorkItem/templates/{templateId} | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/workitembyid | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/workitemforproject | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/workitemsassignedtouser | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/workitemsbytype | WorkItemController | Authenticated user (prod fallback) |
| GET | /api/WorkItem/workitemtypes | WorkItemController | Authenticated user (prod fallback) |

## Regeneration

Run from repo root:

powershell -ExecutionPolicy Bypass -File .\scripts\Generate-ApiCatalog.ps1
