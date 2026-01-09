using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Xunit;
using Microsoft.Extensions.Configuration;
using Moq;
using ADOApi.Interfaces;
using ADOApi.Models;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using System.Collections.Generic;

public class IntegrationTests : IClassFixture<WebApplicationFactory<ADOApi.Program>>
{
    private readonly WebApplicationFactory<ADOApi.Program> _factory;

    public IntegrationTests(WebApplicationFactory<ADOApi.Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // Configure low rate limits for tests
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                var settings = new System.Collections.Generic.Dictionary<string, string>
                {
                    { "RateLimiting:DefaultMaxRequests", "2" },
                    { "RateLimiting:DefaultWindowSeconds", "60" }
                };
                cfg.AddInMemoryCollection(settings);
            });

            builder.ConfigureServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
            });
        });
    }

    [Fact]
    public async Task Unauthenticated_Returns_401_For_Protected_Endpoint()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/project/projects");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task InsufficientRole_Returns_403_For_Write_Endpoint()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-User", "tester");
        client.DefaultRequestHeaders.Add("Test-Roles", "ADO.ReadOnly");

        var payload = new { NewBranchName = "feature/test", SourceBranch = "main" };
        var res = await client.PostAsJsonAsync("/api/repository/branches/myProject/00000000-0000-0000-0000-000000000000", payload);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task RateLimit_Triggers_429_After_Exceeded()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Test-User", "ratelimit-user");

        // Send requests exceeding the configured limit (2)
        var r1 = await client.GetAsync("/api/project/projects");
        var r2 = await client.GetAsync("/api/project/projects");
        var r3 = await client.GetAsync("/api/project/projects");

        // third request should be throttled
        Assert.Equal((HttpStatusCode)429, r3.StatusCode);
    }

    [Fact]
    public async Task DocsPreview_Returns_Documentation_Content()
    {
        var mockRepoService = new Mock<IRepositoryService>();
        var mockChatService = new Mock<ISemanticChatService>();
        var mockAuditLogger = new Mock<IAuditLogger>();

        mockRepoService.Setup(r => r.GetRepositoryStructureAsync(It.IsAny<string>(), It.IsAny<string>(), null))
            .ReturnsAsync(new RepositoryStructure { Path = "", IsDirectory = true, Children = new List<RepositoryStructure>() });

        mockChatService.Setup(c => c.GetChatResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("# README\nGenerated content");

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => mockRepoService.Object);
                services.AddScoped(_ => mockChatService.Object);
                services.AddScoped(_ => mockAuditLogger.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("Test-User", "tester");
        client.DefaultRequestHeaders.Add("Test-Roles", "ADO.ReadOnly");

        var request = new DocsPreviewRequest
        {
            Project = "testProject",
            RepositoryId = "testRepo",
            FilesToGenerate = new List<string> { "README.md" }
        };

        var response = await client.PostAsJsonAsync("/api/docs/preview", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DocsPreviewResponse>();
        Assert.NotNull(result);
        Assert.Single(result.GeneratedFiles);
        Assert.Equal("README.md", result.GeneratedFiles[0].FileName);
        Assert.Contains("Generated content", result.GeneratedFiles[0].Content);
    }

    [Fact]
    public async Task DocsApply_Creates_Markdown_Files_Only()
    {
        var mockRepoService = new Mock<IRepositoryService>();
        var mockChatService = new Mock<ISemanticChatService>();
        var mockAuditLogger = new Mock<IAuditLogger>();

        mockRepoService.Setup(r => r.GetBranchAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GitRef { ObjectId = "commit123" });

        mockRepoService.Setup(r => r.CreateFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new GitPush { Commits = new List<GitCommitRef> { new GitCommitRef { CommitId = "newCommit" } } });

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => mockRepoService.Object);
                services.AddScoped(_ => mockChatService.Object);
                services.AddScoped(_ => mockAuditLogger.Object);
            });
        }).CreateClient();

        client.DefaultRequestHeaders.Add("Test-User", "tester");
        client.DefaultRequestHeaders.Add("Test-Roles", "ADO.Contributor");

        var request = new DocsApplyRequest
        {
            Project = "testProject",
            RepositoryId = "testRepo",
            FilesToApply = new List<GeneratedFile>
            {
                new GeneratedFile { FileName = "README.md", Content = "Test content" }
            },
            Branch = "master",
            CommitMessage = "Test commit"
        };

        var response = await client.PostAsJsonAsync("/api/docs/apply", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<DocsApplyResponse>();
        Assert.NotNull(result);
        Assert.Contains("README.md", result.FilesWritten);
        Assert.Equal("newCommit", result.CommitId);
    }

    [Fact]
    public async Task DocsApply_Unauthorized_Returns_403()
    {
        var mockChatService = new Mock<ISemanticChatService>();
        var mockAuditLogger = new Mock<IAuditLogger>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => mockChatService.Object);
                services.AddScoped(_ => mockAuditLogger.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Add("Test-User", "tester");
        client.DefaultRequestHeaders.Add("Test-Roles", "ADO.ReadOnly"); // Not write

        var request = new DocsApplyRequest
        {
            Project = "testProject",
            RepositoryId = "testRepo",
            FilesToApply = new List<GeneratedFile>
            {
                new GeneratedFile { FileName = "README.md", Content = "Test" }
            }
        };

        var response = await client.PostAsJsonAsync("/api/docs/apply", request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DocsApply_NonMdFiles_Fails()
    {
        var mockChatService = new Mock<ISemanticChatService>();
        var mockAuditLogger = new Mock<IAuditLogger>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped(_ => mockChatService.Object);
                services.AddScoped(_ => mockAuditLogger.Object);
            });
        }).CreateClient();
        client.DefaultRequestHeaders.Add("Test-User", "tester");
        client.DefaultRequestHeaders.Add("Test-Roles", "ADO.Contributor");

        var request = new DocsApplyRequest
        {
            Project = "testProject",
            RepositoryId = "testRepo",
            FilesToApply = new List<GeneratedFile>
            {
                new GeneratedFile { FileName = "README.md", Content = "Test" },
                new GeneratedFile { FileName = "script.py", Content = "print('hack')" } // Invalid
            }
        };

        var response = await client.PostAsJsonAsync("/api/docs/apply", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not allowed", content);
    }
}
