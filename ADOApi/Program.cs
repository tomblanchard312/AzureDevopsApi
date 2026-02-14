using ADOApi.Interfaces;
using ADOApi.Services;
using ADOApi.Exceptions;
using ADOApi.Utilities;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using System.Collections.Generic;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Azure.AI.OpenAI;
using IAuthService = ADOApi.Interfaces.IAuthenticationService;
using AuthService = ADOApi.Services.AuthenticationService;
using Microsoft.EntityFrameworkCore;
using ADOApi.Data;

using System;
using System.Net.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure configuration sources
builder.Configuration.AddUserSecrets(typeof(Program).Assembly, optional: true);

// Read KeyVault uri from current configuration
var kvUri = builder.Configuration["KeyVault:VaultUri"];
if (!string.IsNullOrEmpty(kvUri))
{
    try
    {
        var credential = new Azure.Identity.DefaultAzureCredential();
        builder.Configuration.AddAzureKeyVault(new Uri(kvUri), credential);
    }
    catch (Exception)
    {
        // Swallow here; startup will validate secrets and fail fast if necessary
    }
}

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// Configure authentication conditionally based on environment
if (builder.Environment.IsDevelopment())
{
    // In development, allow anonymous access for easier testing
    // WARNING: All authorization policies are bypassed in development mode.
    // Ensure this application is never deployed with ASPNETCORE_ENVIRONMENT=Development.
    builder.Services.AddAuthentication();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("ADO.ReadOnly", policy => policy.RequireAssertion(_ => true));
        options.AddPolicy("ADO.Contributor", policy => policy.RequireAssertion(_ => true));
        options.AddPolicy("ADO.Admin", policy => policy.RequireAssertion(_ => true));
    });
}
else
{
    // Configure Microsoft Identity Web (Azure AD) JWT Bearer authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    // Configure role claim mapping
    builder.Services.Configure<JwtBearerOptions>(
        JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters.RoleClaimType = "roles";
        });

    // Authorization: require auth by default and add role-based policies
    builder.Services.AddAuthorization(options =>
    {
        // Require authenticated user for all endpoints unless [AllowAnonymous]
        options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        options.AddPolicy("ADO.ReadOnly", policy => policy.RequireRole("ADO.ReadOnly"));
        options.AddPolicy("ADO.Contributor", policy => policy.RequireRole("ADO.Contributor"));
        options.AddPolicy("ADO.Admin", policy => policy.RequireRole("ADO.Admin"));
    });
}

builder.Services.AddControllers();

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Configure Azure DevOps clients
// Use Task.Run to ensure the async call runs on a thread pool thread,
// avoiding potential deadlocks from blocking on async code.
builder.Services.AddScoped<VssConnection>(sp =>
{
    var factory = sp.GetRequiredService<IAzureDevOpsConnectionFactory>();
    return Task.Run(() => factory.CreateConnectionAsync()).GetAwaiter().GetResult();
});

builder.Services.AddScoped<WorkItemTrackingHttpClient>(sp =>
{
    var connection = sp.GetRequiredService<VssConnection>();
    return connection.GetClient<WorkItemTrackingHttpClient>();
});

builder.Services.AddScoped<GitHttpClient>(sp =>
{
    var connection = sp.GetRequiredService<VssConnection>();
    return connection.GetClient<GitHttpClient>();
});

builder.Services.AddScoped<ProjectHttpClient>(sp =>
{
    var connection = sp.GetRequiredService<VssConnection>();
    return connection.GetClient<ProjectHttpClient>();
});

// Register services
builder.Services.AddScoped<IWorkItemService, WorkItemService>();
builder.Services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<ICachingService, CachingService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IRepositoryService, RepositoryService>();
builder.Services.AddScoped<IDocsGenerationService, DocsGenerationService>();
builder.Services.AddScoped<ISecurityAdvisorService, SecurityAdvisorService>();
builder.Services.AddScoped<ISecurityGovernanceService, SecurityGovernanceService>();
builder.Services.AddScoped<IPullRequestCommentService, PullRequestCommentService>();

// Register repository memory services
builder.Services.AddScoped<IRepoMemoryService, RepoMemoryService>();
builder.Services.AddScoped<IInsightService, InsightService>();
builder.Services.AddScoped<IWorkItemLinkService, WorkItemLinkService>();
builder.Services.AddScoped<IAgentRunService, AgentRunService>();
builder.Services.AddScoped<IFingerprintService, FingerprintService>();

// Configure database provider switching (SQLite default, Azure SQL optional)
var databaseProvider = builder.Configuration["Database:Provider"] ?? "SQLite";
switch (databaseProvider.ToUpperInvariant())
{
    case "SQLSERVER":
        builder.Services.AddDbContext<SecurityAdvisorDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("SecurityAdvisorDb") ??
                throw new InvalidOperationException("SQL Server connection string 'SecurityAdvisorDb' is required when using SQL Server provider"),
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null)));
        break;
    case "SQLITE":
    default:
        builder.Services.AddDbContext<SecurityAdvisorDbContext>(options =>
            options.UseSqlite(
                builder.Configuration.GetConnectionString("SecurityAdvisorDb") ??
                "Data Source=securityadvisor.db"));
        break;
}

// Register Security Advisor repository
builder.Services.AddScoped<ISecurityAdvisorRepository, SecurityAdvisorRepository>();

// Register Azure DevOps connection factory
builder.Services.AddSingleton<IAzureDevOpsConnectionFactory, AzureDevOpsConnectionFactory>();

// Configure HttpClient for Ollama
builder.Services.AddHttpClient("Ollama", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register LLM clients
builder.Services.AddScoped<AzureOpenAiClient>();
builder.Services.AddScoped<OllamaClient>();

// Register ILLMClient with factory
builder.Services.AddScoped<ILLMClient>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var provider = config["LLM:Provider"] ?? "AzureOpenAI";

    return provider switch
    {
        "Ollama" => sp.GetRequiredService<OllamaClient>(),
        _ => sp.GetRequiredService<AzureOpenAiClient>()
    };
});

// Add Swagger with OAuth2 support (Authorize button)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ADO API", Version = "v1" });

    // OAuth2 / OpenID Connect configuration for Azure AD
    var authority = builder.Configuration["AzureAd:Instance"] ?? "https://login.microsoftonline.com/";
    var tenant = builder.Configuration["AzureAd:TenantId"] ?? "common";
    var tokenUrl = new Uri($"{authority}{tenant}/oauth2/v2.0/token");
    var authorizationUrl = new Uri($"{authority}{tenant}/oauth2/v2.0/authorize");

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = authorizationUrl,
                TokenUrl = tokenUrl,
                Scopes = new Dictionary<string, string>
                {
                    { builder.Configuration["AzureAd:Scope"] ?? builder.Configuration["AzureAd:Audience"] ?? "api://default/.default", "Access API" }
                }
            }
        }
    });

    c.OperationFilter<ADOApi.Utilities.SwaggerAuthorizeOperationFilter>();
});

// Add logging
builder.Services.AddLogging();
// Audit logger
builder.Services.AddSingleton<IAuditLogger, AuditLogger>();

// Register background services
builder.Services.AddHostedService<RiskAcceptanceExpiryService>();

// Resilience policies for Azure DevOps calls
builder.Services.AddSingleton<ResiliencePolicies>();
// Semantic chat adapter
builder.Services.AddScoped<ADOApi.Interfaces.ISemanticChatService, ADOApi.Services.SemanticChatService>();

// Register chat services
builder.Services.AddScoped<ADOApi.Services.Chat.RepoChatAgentService>();
builder.Services.AddScoped<ADOApi.Services.Chat.RepoChatContextBuilder>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:3000" };
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure database is created on startup (after host build to avoid premature service provider)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SecurityAdvisorDbContext>();
    dbContext.Database.EnsureCreated();
}

// Fail-fast startup check for required secrets only in Production
if (app.Environment.IsProduction())
{
    var missing = new System.Collections.Generic.List<string>();
    var provider = app.Configuration["LLM:Provider"] ?? "AzureOpenAI";

    if (provider == "Ollama")
    {
        if (string.IsNullOrEmpty(app.Configuration["Ollama:Model"])) missing.Add("Ollama:Model");
    }
    else // AzureOpenAI
    {
        if (string.IsNullOrEmpty(app.Configuration["OpenAI:ApiKey"])) missing.Add("OpenAI:ApiKey");
        if (string.IsNullOrEmpty(app.Configuration["OpenAI:Endpoint"])) missing.Add("OpenAI:Endpoint");
        if (string.IsNullOrEmpty(app.Configuration["OpenAI:Deployment"])) missing.Add("OpenAI:Deployment");
    }

    if (string.IsNullOrEmpty(app.Configuration["AzureDevOps:PersonalAccessToken"])) missing.Add("AzureDevOps:PersonalAccessToken");
    if (missing.Count > 0)
    {
        app.Logger.LogCritical("Missing required secrets/configuration: {Missing}", string.Join(',', missing));
        throw new InvalidOperationException($"Missing required secrets/configuration: {string.Join(',', missing)}");
    }
}

// Add global exception handling (must be first to catch all exceptions)
app.UseMiddleware<GlobalExceptionHandler>();

if (app.Environment.IsDevelopment())
{
    app.Logger.LogWarning("Running in Development mode: all authorization policies are bypassed");
}

// Correlation and actor capture for audit
app.UseMiddleware<ADOApi.Middleware.AuditCorrelationMiddleware>();

// Add rate limiting
app.UseMiddleware<RateLimitingMiddleware>();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADO API V1");
    c.RoutePrefix = string.Empty;
    // Configure OAuth2 client for Swagger UI
    var clientId = app.Configuration["AzureAd:ClientId"];
    if (!string.IsNullOrEmpty(clientId))
    {
        c.OAuthClientId(clientId);
        c.OAuthUsePkce();
    }
});

app.UseRouting();
app.UseCors("DefaultCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute(
    name: "default",
    pattern: "{version:apiVersion=1.0}/api/{controller}/{action}/{id?}");

app.Run();