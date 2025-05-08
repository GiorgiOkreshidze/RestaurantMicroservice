using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson.Serialization.Attributes;

namespace Restaurant.Domain.Entities
{
        public class RefreshToken
        {
            [BsonId]
            public required string Id { get; set; }

            public required string UserId { get; set; }

            public required string Token { get; set; }

            public required string ExpiresAt { get; set; }

            public bool IsRevoked { get; set; }

            public required string CreatedAt { get; set; }
        }
}
