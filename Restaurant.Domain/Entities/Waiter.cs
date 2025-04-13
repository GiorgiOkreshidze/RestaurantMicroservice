using Amazon.DynamoDBv2.DataModel;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("EmployeeInfo")]
public class EmployeeInfo
{
    [DynamoDBHashKey("email")]
    public required string Email { get; set; }

    [DynamoDBProperty("locationId")]
    public required string LocationId { get; set; }
}