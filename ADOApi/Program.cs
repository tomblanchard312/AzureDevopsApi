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
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.Common;
using IAuthService = ADOApi.Interfaces.IAuthenticationService;
using AuthService = ADOApi.Services.AuthenticationService;

using System;
using System.Net.Http;

namespace ADOApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = CreateHostBuilder(args).Build();
            builder.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add memory cache
            services.AddMemoryCache();

            // Add HTTP client factory
            services.AddHttpClient();

            // Add controllers
            services.AddControllers();

            // Add API versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });

            // Configure Azure DevOps clients
            services.AddScoped<VssConnection>(sp =>
            {
                var configuration = sp.GetRequiredService<IConfiguration>();
                var organizationUrl = configuration["AzureDevOps:OrganizationUrl"];
                var pat = configuration["AzureDevOps:PersonalAccessToken"];
                var credentials = new VssBasicCredential(string.Empty, pat);
                return new VssConnection(new Uri(organizationUrl), credentials);
            });

            services.AddScoped<WorkItemTrackingHttpClient>(sp =>
            {
                var connection = sp.GetRequiredService<VssConnection>();
                return connection.GetClient<WorkItemTrackingHttpClient>();
            });

            services.AddScoped<GitHttpClient>(sp =>
            {
                var connection = sp.GetRequiredService<VssConnection>();
                return connection.GetClient<GitHttpClient>();
            });

            services.AddScoped<ProjectHttpClient>(sp =>
            {
                var connection = sp.GetRequiredService<VssConnection>();
                return connection.GetClient<ProjectHttpClient>();
            });

            // Register services
            services.AddScoped<IWorkItemService, WorkItemService>();
            services.AddScoped<IAzureDevOpsService, AzureDevOpsService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<ICachingService, CachingService>();
            services.AddScoped<IWebhookService, WebhookService>();

            // Add Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ADO API", Version = "v1" });
            });

            // Add logging
            services.AddLogging();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Add global exception handling
            app.UseMiddleware<GlobalExceptionHandler>();

            // Add rate limiting
            app.UseMiddleware<RateLimitingMiddleware>();

            // Enable Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADO API V1");
                c.RoutePrefix = string.Empty;
            });

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{version:apiVersion=1.0}/api/{controller}/{action}/{id?}");
            });
        }
    }
}