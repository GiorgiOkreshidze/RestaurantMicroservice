namespace Restaurant.Application.DTOs.Aws;

public class AwsSettings
{
    public string SqsQueueName { get; set; } = string.Empty;
    
    public string SqsQueueUrl { get; set; } = string.Empty;
}