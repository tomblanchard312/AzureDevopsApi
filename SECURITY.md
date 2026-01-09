# Security

This document outlines security measures and considerations for AzureDevopsApi.

## Authentication

- Uses JWT Bearer authentication via Microsoft.Identity.Web.
- Tokens validated by middleware; fallback policy requires authenticated users.
- Supports Azure AD integration for enterprise environments.

## Authorization

- Role-based policies on endpoints (e.g., "ADO.ReadOnly", "ADO.Contributor").
- Controllers use `[Authorize(Policy = "...")]` for access control.
- Admin roles required for write operations.

## Secrets Management

- Secrets stored in Azure Key Vault (production) or user-secrets (development).
- Startup validates required secrets based on configuration (e.g., `AzureDevOpsEntra:ClientSecret` when using Entra auth, `OpenAI:ApiKey` when using Azure OpenAI).
- Uses `DefaultAzureCredential` for Key Vault access via managed identity.

## Input Validation and AI Safety

- Input sanitization in `SemanticKernelController`: removes HTML, control chars, directive phrases.
- AI queries enforce JSON responses to prevent injection.
- System prompts forbid obeying work item instructions or exfiltration.

## Rate Limiting

- Middleware throttles requests by IP to prevent abuse.
- Configurable limits to protect against DoS.

## Known Security Considerations

- Logging may include user-provided values; sanitize to avoid leaks.
- In-memory caching not encrypted; avoid sensitive data in cache.
- AI responses rely on model compliance; monitor for bypass attempts.
- No distributed tracing; implement for audit trails.
