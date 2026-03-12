namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class MessageAttachmentDto
{
    public long Id { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
