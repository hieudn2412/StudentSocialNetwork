using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StudentSocialNetwork.Api.Application.Common.Models;
using StudentSocialNetwork.Api.Application.Interfaces.Services;

namespace StudentSocialNetwork.Api.Infrastructure.Storage;

public class CloudinaryImageStorageService : IImageStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _postsFolder;

    public CloudinaryImageStorageService(IOptions<CloudinaryOptions> options)
    {
        var cloudinaryOptions = options.Value;
        if (string.IsNullOrWhiteSpace(cloudinaryOptions.CloudName) ||
            string.IsNullOrWhiteSpace(cloudinaryOptions.ApiKey) ||
            string.IsNullOrWhiteSpace(cloudinaryOptions.ApiSecret))
        {
            throw new InvalidOperationException("Cloudinary chưa được cấu hình đầy đủ (CloudName/ApiKey/ApiSecret).");
        }

        if (cloudinaryOptions.CloudName.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
            cloudinaryOptions.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
            cloudinaryOptions.ApiSecret.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bạn cần thay giá trị mẫu Cloudinary trong appsettings bằng thông tin thật.");
        }

        _postsFolder = string.IsNullOrWhiteSpace(cloudinaryOptions.PostsFolder)
            ? "student-social/posts"
            : cloudinaryOptions.PostsFolder.Trim('/');

        var account = new Account(
            cloudinaryOptions.CloudName,
            cloudinaryOptions.ApiKey,
            cloudinaryOptions.ApiSecret);

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string?> UploadPostImageAsync(IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Chỉ hỗ trợ file ảnh.");
        }

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = _postsFolder,
            PublicId = Guid.NewGuid().ToString("N"),
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);
        cancellationToken.ThrowIfCancellationRequested();

        if (uploadResult.Error is not null)
        {
            throw new InvalidOperationException($"Upload ảnh lên Cloudinary thất bại: {uploadResult.Error.Message}");
        }

        return uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
    }
}
