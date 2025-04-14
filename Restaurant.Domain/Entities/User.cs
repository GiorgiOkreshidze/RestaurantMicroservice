using Amazon.DynamoDBv2.DataModel;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.Domain.Entities
{
    [DynamoDBTable("Users")]
    public class User
    {
        [DynamoDBHashKey("id")]
        public string? Id { get; set; }

        [DynamoDBGlobalSecondaryIndexHashKey("GSI1")]
        [DynamoDBProperty("email")]
        public required string Email { get; set; }

        [DynamoDBProperty("passwordHash")]
        public string? PasswordHash { get; set; }

        [DynamoDBProperty("firstName")]
        public required string FirstName { get; set; }

        [DynamoDBProperty("lastName")]
        public required string LastName { get; set; }

        [DynamoDBProperty("imgUrl")]
        public required string ImgUrl { get; set; }

        [DynamoDBProperty("createdAt")]
        public required string CreatedAt { get; set; }

        [DynamoDBIgnore]
        public Role Role { get; set; } = Role.Customer;

        [DynamoDBProperty("role")]
        public string RoleString
        {
            get => Role.ToString();
            set => Role = Enum.TryParse<Role>(value, out var role) ? role : Role.Customer;
        }

        [DynamoDBProperty("locationId")]
        public string? LocationId { get; set; }
    }
}
