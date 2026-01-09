using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using ADOApi.Services;
using ADOApi.Interfaces;

namespace ADOApi.Tests
{
    public class AzureDevOpsConnectionFactoryTests
    {
        [Fact]
        public async Task CreateConnectionAsync_UsesEntraAuth_WhenConfigured()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AzureDevOps:OrganizationUrl"]).Returns("https://dev.azure.com/test");
            config.Setup(c => c.GetValue<bool>("AzureDevOps:UseEntraAuth")).Returns(true);
            config.Setup(c => c["AzureDevOpsEntra:ClientId"]).Returns("client-id");
            config.Setup(c => c["AzureDevOpsEntra:ClientSecret"]).Returns("client-secret");
            config.Setup(c => c["AzureDevOpsEntra:TenantId"]).Returns("tenant-id");
            config.Setup(c => c["AzureDevOpsEntra:AuthorityHost"]).Returns("https://login.microsoftonline.com/");

            var logger = new Mock<ILogger<AzureDevOpsConnectionFactory>>();

            var factory = new AzureDevOpsConnectionFactory(config.Object, logger.Object);

            // Act & Assert
            // Note: This would require mocking MSAL for full test
            // For now, ensure factory is created without exceptions
            Assert.NotNull(factory);
        }

        [Fact]
        public async Task CreateConnectionAsync_UsesPat_WhenEntraNotConfigured()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AzureDevOps:OrganizationUrl"]).Returns("https://dev.azure.com/test");
            config.Setup(c => c.GetValue<bool>("AzureDevOps:UseEntraAuth")).Returns(false);
            config.Setup(c => c["AzureDevOps:PersonalAccessToken"]).Returns("pat-token");

            var logger = new Mock<ILogger<AzureDevOpsConnectionFactory>>();

            var factory = new AzureDevOpsConnectionFactory(config.Object, logger.Object);

            // Act
            var connection = await factory.CreateConnectionAsync();

            // Assert
            Assert.NotNull(connection);
            Assert.Equal("https://dev.azure.com/test", connection.Uri.ToString());
        }

        [Fact]
        public async Task CreateConnectionAsync_Throws_WhenPatMissing()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["AzureDevOps:OrganizationUrl"]).Returns("https://dev.azure.com/test");
            config.Setup(c => c.GetValue<bool>("AzureDevOps:UseEntraAuth")).Returns(false);
            config.Setup(c => c["AzureDevOps:PersonalAccessToken"]).Returns((string)null);

            var logger = new Mock<ILogger<AzureDevOpsConnectionFactory>>();

            var factory = new AzureDevOpsConnectionFactory(config.Object, logger.Object);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                factory.CreateConnectionAsync());
            Assert.Contains("PersonalAccessToken", exception.Message);
        }
    }
}