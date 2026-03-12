using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers.Api;

[Route("api/conversations")]
public class ConversationsApiController : ProxyApiControllerBase
{
    public ConversationsApiController(IBackendApiProxy backendApiProxy)
        : base(backendApiProxy)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetConversations(CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(HttpMethod.Get, "api/conversations", GetBearerToken(), cancellationToken: cancellationToken);
        return AsProxyResponse(proxyResult);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGroupConversation([FromBody] CreateGroupConversationRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            "api/conversations/group",
            GetBearerToken(),
            request,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("private")]
    public async Task<IActionResult> CreatePrivateConversation([FromBody] CreatePrivateConversationRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            "api/conversations/private",
            GetBearerToken(),
            request,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("{conversationId:int}/members")]
    public async Task<IActionResult> AddMember(int conversationId, [FromBody] AddConversationMemberRequest request, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            $"api/conversations/{conversationId}/members",
            GetBearerToken(),
            request,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpDelete("{conversationId:int}/members/{memberUserId:int}")]
    public async Task<IActionResult> RemoveMember(int conversationId, int memberUserId, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Delete,
            $"api/conversations/{conversationId}/members/{memberUserId}",
            GetBearerToken(),
            cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("{conversationId:int}/leave")]
    public async Task<IActionResult> LeaveConversation(int conversationId, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            $"api/conversations/{conversationId}/leave",
            GetBearerToken(),
            cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    public sealed class CreatePrivateConversationRequest
    {
        public int OtherUserId { get; set; }
    }

    public sealed class CreateGroupConversationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public List<int> MemberIds { get; set; } = new();
    }

    public sealed class AddConversationMemberRequest
    {
        public int UserId { get; set; }
        public string Role { get; set; } = "Member";
    }
}
