
namespace Restaurant.Domain.Entities.Enums
{
    public static class FeedbackTypeExtensions
    {
        public static string ToDynamoDBType(this FeedbackType feedbackType)
        {
            return feedbackType switch
            {
                FeedbackType.ServiceQuality => "SERVICE_QUALITY",
                FeedbackType.CuisineExperience => "CUISINE_EXPERIENCE",
                _ => throw new ArgumentException("Unknown feedback type")
            };
        }

        public static FeedbackType ToFeedbackType(this string type)
        {
            return type switch
            {
                "SERVICE_QUALITY" => FeedbackType.ServiceQuality,
                "CUISINE_EXPERIENCE" => FeedbackType.CuisineExperience,
                _ => throw new ArgumentException(
                    $"Invalid feedback type: {type}. Should be either 'SERVICE_QUALITY' or 'CUISINE_EXPERIENCE'.", nameof(type))
            };
        }
    }
}
