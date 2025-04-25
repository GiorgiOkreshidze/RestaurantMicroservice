using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities
{
    [DynamoDBTable("Feedbacks")]
    public class Feedback
    {
        [DynamoDBProperty("id")]
        public required string Id { get; set; }

        [DynamoDBHashKey("locationId")]
        [DynamoDBGlobalSecondaryIndexHashKey(IndexNames = new[] { "DateIndex", "RatingIndex" })]
        public required string LocationId { get; set; }

        [DynamoDBProperty("type")]
        [DynamoDBGlobalSecondaryIndexRangeKey("ReservationByTypeIndex")]
        public required string Type { get; set; }

        [DynamoDBRangeKey("type#date")]
        public required string TypeDate { get; set; }

        [DynamoDBProperty("rate")]
        [DynamoDBGlobalSecondaryIndexRangeKey(IndexNames = new[] { "RatingIndex", "RatingByTypeIndex" })]
        public required int Rate { get; set; }

        [DynamoDBProperty("comment")]
        public required string Comment { get; set; }

        [DynamoDBProperty("userName")]
        public required string UserName { get; set; }

        [DynamoDBProperty("userAvatarUrl")]
        public required string UserAvatarUrl { get; set; }

        [DynamoDBProperty("date")]
        [DynamoDBGlobalSecondaryIndexRangeKey(IndexNames = new[] { "DateIndex", "DateByTypeIndex" })]
        public required string Date { get; set; }

        [DynamoDBProperty("reservationId")]
        public string ReservationId { get; set; } = null!;

        // GSIs - Composite attributes
        [DynamoDBGlobalSecondaryIndexHashKey(IndexNames = new[] { "RatingByTypeIndex", "DateByTypeIndex" })]
        [DynamoDBProperty("locationId#type")]
        public required string LocationIdType { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey("ReservationTypeIndex")]
        [DynamoDBProperty("reservationId#type")]
        public required string ReservationIdType { get; set; }

        // Helper method to set composite keys based on individual properties
        public void SetCompositeKeys()
        {
            TypeDate = $"{Type}#{Date}";
            LocationIdType = $"{LocationId}#{Type}";
            ReservationIdType = $"{ReservationId}#{Type}";
        }
    }
}
