using Amazon.S3;
using Amazon.S3.Model;

namespace Restaurant.Infrastructure.Services;

public class ImageService : IImageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName = "team2-demo-bucket";
    
    public ImageService(IAmazonS3 s3Client)
    {
        _s3Client = s3Client;
    }
    
    public async Task<string> UploadUserImageAsync(string base64EncodedImage, string userId)
    {
        if (string.IsNullOrEmpty(base64EncodedImage))
        {
            throw new ArgumentException("Base64 encoded image cannot be null or empty", nameof(base64EncodedImage));
        }
        
        // Remove potential "data:image/jpeg;base64," prefix
        string base64Data = base64EncodedImage;
        if (base64Data.Contains(","))
        {
            base64Data = base64Data.Split(',')[1];
        }
        
        // Convert base64 to byte array
        byte[] imageBytes = Convert.FromBase64String(base64Data);
        
        // Generate a unique key for the S3 object
        string fileName = $"{userId}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
        string s3Key = $"Images/Users/{fileName}";
        
        using (var memoryStream = new MemoryStream(imageBytes))
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = s3Key,
                InputStream = memoryStream,
                ContentType = "image/jpeg"
            };
            
            await _s3Client.PutObjectAsync(putRequest);
        }
        
        return $"https://{_bucketName}.s3.eu-west-2.amazonaws.com/{s3Key}";
    }
}
