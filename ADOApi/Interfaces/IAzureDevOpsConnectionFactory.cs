using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;

namespace ADOApi.Interfaces
{
    public interface IAzureDevOpsConnectionFactory
    {
        Task<VssConnection> CreateConnectionAsync(CancellationToken ct = default);
    }
}