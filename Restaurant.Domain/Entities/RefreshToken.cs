using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities
{
        [DynamoDBTable("RefreshTokens")]
        public class RefreshToken
        {
            [DynamoDBHashKey("id")]
            public required string Id { get; set; }

            [DynamoDBGlobalSecondaryIndexHashKey("UserIdIndex")]  // Marks this as GSI partition key
            [DynamoDBProperty("userId")]
            public required string UserId { get; set; }

            [DynamoDBProperty("token")]
            [DynamoDBGlobalSecondaryIndexHashKey("TokenIndex")]
            public required string Token { get; set; }

            [DynamoDBProperty("expiresAt")]
            public required string ExpiresAt { get; set; }

            [DynamoDBProperty("isRevoked")]
            public bool IsRevoked { get; set; }

            [DynamoDBProperty("createdAt")]
            public required string CreatedAt { get; set; }
        }
}
