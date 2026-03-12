namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class SendMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public long? ReplyToMessageId { get; set; }
    public List<MessageAttachmentInputDto> Attachments { get; set; } = new();
}
