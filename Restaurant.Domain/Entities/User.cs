using Amazon.DynamoDBv2.DataModel;
using MongoDB.Bson.Serialization.Attributes;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Domain.Entities
{
    public class User
    {
        [BsonId]
        public string Id { get; set; }

        public required string Email { get; set; }

        public string? PasswordHash { get; set; }

        public required string FirstName { get; set; }

        public required string LastName { get; set; }

        public required string ImgUrl { get; set; }

        public required string CreatedAt { get; set; }

        [BsonIgnore]
        public Role Role { get; set; } = Role.Customer;

        [BsonElement("Role")]
        public string RoleString
        {
            get => Role.ToString();
            set => Role = Enum.TryParse<Role>(value, out var role) ? role : Role.Customer;
        }
        
        public string? LocationId { get; set; }
    }
}
