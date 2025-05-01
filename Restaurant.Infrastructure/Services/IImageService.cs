namespace Restaurant.Infrastructure.Services;

public interface IImageService
{
    Task<string> UploadUserImageAsync(string base64EncodedImage, string userId);
}