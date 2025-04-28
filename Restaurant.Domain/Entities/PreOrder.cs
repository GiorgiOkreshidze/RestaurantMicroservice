using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities;

[DynamoDBTable("PreOrders")]
public class PreOrder
{
    [DynamoDBHashKey("userId")]
    public required string UserId { get; set; }

    [DynamoDBRangeKey("sk")] public string SortKey { get; set; } = string.Empty;

    // Calculated property to extract the PreOrderId from the SortKey
    [DynamoDBIgnore]
    public string? PreOrderId
    {
        get => SortKey?.Replace("PreOrder#", "");
        init => SortKey = $"PreOrder#{value}";
    }
    
    public void SetSortKey(string preOrderId)
    {
        SortKey = $"PreOrder#{preOrderId}";
    }

    [DynamoDBProperty("reservationId")]
    public required string ReservationId { get; set; }

    [DynamoDBProperty("status")]
    public required string Status { get; set; }

    [DynamoDBProperty("createDate")]
    public DateTime CreateDate { get; set; }
    
    [DynamoDBProperty("completionDate")]
    public DateTime CompletionDate { get; set; }
    
    [DynamoDBProperty("timeSlot")]
    public required string TimeSlot { get; set; }
    
    [DynamoDBProperty("reservationDate")]
    public required string ReservationDate { get; set; }

    [DynamoDBProperty("totalPrice")]
    public decimal TotalPrice { get; set; }

    [DynamoDBProperty("address")]
    public required string Address { get; set; }

    // This won't be stored directly in DynamoDB but will be populated from related items
    [DynamoDBIgnore]
    public List<PreOrderItem> Items { get; set; } = new List<PreOrderItem>();
}
