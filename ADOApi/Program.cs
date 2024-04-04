using ADOApi.Interfaces;
using ADOApi.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            services.AddControllers();
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            });
            services.AddHttpClient();
            services.AddScoped<Interfaces.IAuthenticationService, Services.AuthenticationService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IWorkItemService, WorkItemService>();

            services.AddScoped<AzureDevOpsService>(provider =>
            {
                var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient();
                var configuration = provider.GetRequiredService<IConfiguration>();
                var organization = configuration["AzureDevOps:Organization"];
                var personalAccessToken = configuration["AzureDevOps:PersonalAccessToken"];
                var adminToken = configuration["AzureDevOps:AdminPat"];
                var authService = provider.GetRequiredService<Interfaces.IAuthenticationService>();
                var queryService = provider.GetRequiredService<IQueryService>();
                var workItemService = provider.GetRequiredService<IWorkItemService>();

                return new AzureDevOpsService(
                    httpClient,
                    organization,
                    personalAccessToken,
                    adminToken,
                    authService,
                    queryService,
                    workItemService);
            });

            // Add Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "ADO API", Version = "v1" });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADO API V1");
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