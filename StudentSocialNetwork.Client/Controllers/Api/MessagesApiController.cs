using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Client.Services;

namespace StudentSocialNetwork.Client.Controllers.Api;

[Route("api/messages")]
public class MessagesApiController : ProxyApiControllerBase
{
    public MessagesApiController(IBackendApiProxy backendApiProxy)
        : base(backendApiProxy)
    {
    }

    [HttpGet("{conversationId:int}")]
    public async Task<IActionResult> GetMessages(int conversationId, [FromQuery] string? cursor, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        }

        if (limit.HasValue)
        {
            query.Add($"limit={limit.Value}");
        }

        var suffix = query.Count == 0 ? string.Empty : $"?{string.Join("&", query)}";
        var path = $"api/messages/{conversationId}{suffix}";

        var proxyResult = await ForwardAsync(HttpMethod.Get, path, GetBearerToken(), cancellationToken: cancellationToken);
        return AsProxyResponse(proxyResult);
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var backendRequest = new
        {
            request.Content,
            request.MessageType,
            request.ReplyToMessageId,
            request.Attachments
        };

        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            $"api/conversations/{request.ConversationId}/messages",
            GetBearerToken(),
            backendRequest,
            cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("reaction")]
    public async Task<IActionResult> SetReaction([FromBody] ReactionRequest request, CancellationToken cancellationToken)
    {
        ProxyResult proxyResult;

        if (request.IsRemove)
        {
            var path = $"api/messages/{request.MessageId}/reactions?reactionType={Uri.EscapeDataString(request.ReactionType)}";
            proxyResult = await ForwardAsync(HttpMethod.Delete, path, GetBearerToken(), cancellationToken: cancellationToken);
        }
        else
        {
            proxyResult = await ForwardAsync(
                HttpMethod.Post,
                $"api/messages/{request.MessageId}/reactions",
                GetBearerToken(),
                new { request.ReactionType },
                cancellationToken);
        }

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("pin")]
    public async Task<IActionResult> SetPin([FromBody] PinRequest request, CancellationToken cancellationToken)
    {
        var path = $"api/conversations/{request.ConversationId}/messages/{request.MessageId}/pin";

        var proxyResult = request.IsPinned
            ? await ForwardAsync(HttpMethod.Post, path, GetBearerToken(), cancellationToken: cancellationToken)
            : await ForwardAsync(HttpMethod.Delete, path, GetBearerToken(), cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpGet("{conversationId:int}/pinned")]
    public async Task<IActionResult> GetPinnedMessages(int conversationId, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Get,
            $"api/conversations/{conversationId}/pinned-messages",
            GetBearerToken(),
            cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    [HttpPost("{conversationId:int}/read")]
    public async Task<IActionResult> MarkAsRead(int conversationId, CancellationToken cancellationToken)
    {
        var proxyResult = await ForwardAsync(
            HttpMethod.Post,
            $"api/messages/{conversationId}/read",
            GetBearerToken(),
            cancellationToken: cancellationToken);

        return AsProxyResponse(proxyResult);
    }

    public sealed class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "Text";
        public long? ReplyToMessageId { get; set; }
        public List<MessageAttachmentInput> Attachments { get; set; } = new();
    }

    public sealed class MessageAttachmentInput
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public sealed class ReactionRequest
    {
        public long MessageId { get; set; }
        public string ReactionType { get; set; } = string.Empty;
        public bool IsRemove { get; set; }
    }

    public sealed class PinRequest
    {
        public int ConversationId { get; set; }
        public long MessageId { get; set; }
        public bool IsPinned { get; set; }
    }
}
