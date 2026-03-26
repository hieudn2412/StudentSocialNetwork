using Microsoft.AspNetCore.Http;

namespace StudentSocialNetwork.Api.Application.Interfaces.Services;

public interface IImageStorageService
{
    Task<string?> UploadPostImageAsync(IFormFile? file, CancellationToken cancellationToken = default);
}
