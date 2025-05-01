using Amazon;
using Amazon.Runtime;
using Amazon.S3;

namespace Restaurant.Infrastructure.AWS;

public static class S3Factory
{
    public static IAmazonS3 CreateS3Client(AWSCredentials credentials)
    {
        var creds = credentials.GetCredentials();
        Console.WriteLine($"Creating S3 client with AccessKeyId: {creds.AccessKey[..5]}...");

        var config = new AmazonS3Config
        {
            RegionEndpoint = RegionEndpoint.EUWest2
        };

        var sessionCredentials = new SessionAWSCredentials(
            creds.AccessKey,
            creds.SecretKey,
            creds.Token);

        return new AmazonS3Client(sessionCredentials, config);
    }
}