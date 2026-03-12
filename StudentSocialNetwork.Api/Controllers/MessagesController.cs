using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Exceptions;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Messages;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

/// <summary>
/// Provides message query and interaction endpoints.
/// </summary>
[Authorize]
[ApiController]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    /// <summary>
    /// Returns conversation messages in descending created time order with cursor pagination.
    /// </summary>
    [HttpGet("api/messages/{conversationId:int}")]
    [ProducesResponseType(typeof(ApiResponse<CursorPagedResultDto<MessageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CursorPagedResultDto<MessageDto>>>> GetMessages(
        int conversationId,
        [FromQuery] string? cursor,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        var page = await _messageService.GetMessagesAsync(GetCurrentUserId(), conversationId, cursor, limit, cancellationToken);
        return Ok(ApiResponse.Ok(page));
    }

    /// <summary>
    /// Sends a new message to a conversation.
    /// </summary>
    [HttpPost("api/conversations/{conversationId:int}/messages")]
    [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(
        int conversationId,
        [FromBody] SendMessageRequestDto request,
        CancellationToken cancellationToken)
    {
        var message = await _messageService.SendMessageAsync(GetCurrentUserId(), conversationId, request, cancellationToken);
        return Ok(ApiResponse.Ok(message, "Message sent."));
    }

    /// <summary>
    /// Adds a reaction to a message.
    /// </summary>
    [HttpPost("api/messages/{messageId:long}/reactions")]
    [ProducesResponseType(typeof(ApiResponse<MessageReactionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<MessageReactionDto>>> AddReaction(
        long messageId,
        [FromBody] AddMessageReactionRequestDto request,
        CancellationToken cancellationToken)
    {
        var reaction = await _messageService.AddReactionAsync(GetCurrentUserId(), messageId, request, cancellationToken);
        return Ok(ApiResponse.Ok(reaction, "Reaction added."));
    }

    /// <summary>
    /// Removes the current user's reaction from a message.
    /// </summary>
    [HttpDelete("api/messages/{messageId:long}/reactions")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> RemoveReaction(
        long messageId,
        [FromQuery] string reactionType,
        CancellationToken cancellationToken)
    {
        await _messageService.RemoveReactionAsync(GetCurrentUserId(), messageId, reactionType, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Reaction removed."));
    }

    /// <summary>
    /// Marks all messages in a conversation as read for the current user.
    /// </summary>
    [HttpPost("api/messages/{conversationId:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> MarkConversationAsRead(int conversationId, CancellationToken cancellationToken)
    {
        await _messageService.MarkConversationAsReadAsync(GetCurrentUserId(), conversationId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Conversation marked as read."));
    }

    /// <summary>
    /// Returns all pinned messages in a conversation.
    /// </summary>
    [HttpGet("api/conversations/{conversationId:int}/pinned-messages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PinnedMessageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PinnedMessageDto>>>> GetPinnedMessages(int conversationId, CancellationToken cancellationToken)
    {
        var pinned = await _messageService.GetPinnedMessagesAsync(GetCurrentUserId(), conversationId, cancellationToken);
        return Ok(ApiResponse.Ok(pinned));
    }

    /// <summary>
    /// Pins a message in a conversation.
    /// </summary>
    [HttpPost("api/conversations/{conversationId:int}/messages/{messageId:long}/pin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> PinMessage(int conversationId, long messageId, CancellationToken cancellationToken)
    {
        await _messageService.PinMessageAsync(GetCurrentUserId(), conversationId, messageId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Message pinned."));
    }

    /// <summary>
    /// Unpins a message in a conversation.
    /// </summary>
    [HttpDelete("api/conversations/{conversationId:int}/messages/{messageId:long}/pin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> UnpinMessage(int conversationId, long messageId, CancellationToken cancellationToken)
    {
        await _messageService.UnpinMessageAsync(GetCurrentUserId(), conversationId, messageId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "Message unpinned."));
    }

    private int GetCurrentUserId()
    {
        var subjectClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(subjectClaim, out var userId))
        {
            throw new UnauthorizedException("Authenticated user id is missing.");
        }

        return userId;
    }
}
