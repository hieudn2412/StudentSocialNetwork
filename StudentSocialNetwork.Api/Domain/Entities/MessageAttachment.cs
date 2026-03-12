namespace StudentSocialNetwork.Api.Domain.Entities;

public class MessageAttachment
{
    public long Id { get; set; }
    public long MessageId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public Message Message { get; set; } = null!;
}
