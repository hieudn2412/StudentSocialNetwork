using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.DTOs.Conversations;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Controllers;

/// <summary>
/// Provides conversation and membership management endpoints.
/// </summary>
[Authorize]
[ApiController]
[Route("api/conversations")]
public class ConversationsController : ControllerBase
{
    private readonly IConversationService _conversationService;

    public ConversationsController(IConversationService conversationService)
    {
        _conversationService = conversationService;
    }

    /// <summary>
    /// Returns all active conversations for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ConversationSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConversationSummaryDto>>>> GetConversations(CancellationToken cancellationToken)
    {
        var conversations = await _conversationService.GetUserConversationsAsync(GetCurrentUserId(), cancellationToken);
        return Ok(ApiResponse.Ok(conversations));
    }

    /// <summary>
    /// Creates a private (1:1) conversation.
    /// </summary>
    [HttpPost("private")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConversationSummaryDto>>> CreatePrivateConversation(
        [FromBody] CreatePrivateConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.CreatePrivateConversationAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(conversation, "Private conversation created."));
    }

    /// <summary>
    /// Creates a group conversation.
    /// </summary>
    [HttpPost("group")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConversationSummaryDto>>> CreateGroupConversation(
        [FromBody] CreateGroupConversationRequestDto request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.CreateGroupConversationAsync(GetCurrentUserId(), request, cancellationToken);
        return Ok(ApiResponse.Ok(conversation, "Group conversation created."));
    }

    /// <summary>
    /// Adds a member to a group conversation.
    /// </summary>
    [HttpPost("{conversationId:int}/members")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConversationSummaryDto>>> AddMember(
        int conversationId,
        [FromBody] AddConversationMemberRequestDto request,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.AddMemberAsync(GetCurrentUserId(), conversationId, request, cancellationToken);
        return Ok(ApiResponse.Ok(conversation, "Member added."));
    }

    /// <summary>
    /// Removes a member from a conversation.
    /// </summary>
    [HttpDelete("{conversationId:int}/members/{memberUserId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConversationSummaryDto>>> RemoveMember(
        int conversationId,
        int memberUserId,
        CancellationToken cancellationToken)
    {
        var conversation = await _conversationService.RemoveMemberAsync(GetCurrentUserId(), conversationId, memberUserId, cancellationToken);
        return Ok(ApiResponse.Ok(conversation, "Member removed."));
    }

    /// <summary>
    /// Current user leaves a conversation.
    /// </summary>
    [HttpPost("{conversationId:int}/leave")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> LeaveConversation(int conversationId, CancellationToken cancellationToken)
    {
        await _conversationService.LeaveConversationAsync(GetCurrentUserId(), conversationId, cancellationToken);
        return Ok(ApiResponse.Ok<object>(new { }, "You left the conversation."));
    }

    private int GetCurrentUserId()
    {
        var subjectClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!int.TryParse(subjectClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        }

        return userId;
    }
}
