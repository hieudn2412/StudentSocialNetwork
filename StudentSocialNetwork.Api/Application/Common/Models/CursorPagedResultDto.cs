namespace StudentSocialNetwork.Api.Application.Common.Models;

public class CursorPagedResultDto<T>
{
    public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
    public string? NextCursor { get; set; }
    public int Limit { get; set; }
}
