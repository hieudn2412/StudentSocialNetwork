namespace StudentSocialNetwork.Api.Application.DTOs.Messages;

public class MessageAttachmentInputDto
{
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
