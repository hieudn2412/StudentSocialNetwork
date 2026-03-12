namespace StudentSocialNetwork.Api.Application.DTOs.Conversations;

public class ConversationSummaryDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? LastMessageId { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public IReadOnlyCollection<ConversationMemberDto> Members { get; set; } = Array.Empty<ConversationMemberDto>();
}
