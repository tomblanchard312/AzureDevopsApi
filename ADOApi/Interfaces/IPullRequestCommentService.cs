using System.Threading.Tasks;
using ADOApi.Models;

namespace ADOApi.Interfaces
{
    public interface IPullRequestCommentService
    {
        Task<PullRequestCommentResponse> GenerateAndPostCommentAsync(PullRequestCommentRequest request);
        Task<PullRequestCommentResponse> PreviewCommentAsync(PullRequestCommentRequest request);
        Task<PullRequestCommentResponse> UpdateCommentAsync(int threadId, PullRequestCommentRequest request);
        Task<InlineCommentResponse> PostInlineCommentAsync(InlineCommentRequest request);
        Task<ThreadResolutionResponse> ResolveFixedThreadsAsync(ThreadResolutionRequest request);
        Task<PrStatusResponse> PostPrStatusAsync(PrStatusRequest request);
    }
}