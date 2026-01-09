# Deployment

This guide covers deploying AzureDevopsApi to Azure App Service.

## Azure App Service Deployment

1. Create an App Service in Azure Portal.
2. Deploy via GitHub Actions or Azure CLI:
   ```bash
   az webapp up --name <app-name> --resource-group <rg> --runtime "dotnet:8"
   ```
3. Set environment variables for non-secret config (e.g., `AzureDevOps__OrganizationUrl`).

## Managed Identity and Key Vault Integration

1. Enable managed identity on the App Service.
2. Grant the identity `Key Vault -> Secret -> Get` access.
3. Set `KeyVault__VaultUri` in app settings.
4. Store secrets in Key Vault (e.g., `OpenAI--ApiKey`).

## Configuration

- Use app settings for non-secrets.
- Secrets via Key Vault.
- Startup validates secrets; fails if missing.

## Monitoring and Logging

- Logs via Azure Application Insights (integrate via NuGet).
- Monitor metrics for requests, errors, and performance.
- Alerts on high error rates.

## Troubleshooting

- **Startup Failures**: Check Key Vault access and secret names.
- **Auth Issues**: Verify managed identity and Key Vault permissions.
- **Performance**: Review async methods and caching.
- **AI Errors**: Check OpenAI endpoint and API key.
- Startup failures: Check Key Vault access and required secrets
