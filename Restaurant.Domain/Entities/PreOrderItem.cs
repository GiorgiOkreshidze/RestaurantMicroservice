using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Restaurant.Domain.Entities;

// This class represents an item within a pre-order
[DynamoDBTable("PreOrders")]
public class PreOrderItem
{
    [DynamoDBHashKey("userId")]
    public required string UserId { get; set; }

    // SortKey that creates the hierarchical relationship
    [DynamoDBRangeKey("sk")]
    public required string SortKey { get; set; }

    // Helper properties to work with the composite sort key
    [DynamoDBIgnore]
    public string? PreOrderId
    {
        get
        {
            var parts = SortKey?.Split('#');
            return parts?.Length > 1 ? parts[1] : null;
        }
    }

    [DynamoDBIgnore]
    public string? ItemId
    {
        get
        {
            var parts = SortKey?.Split('#');
            return parts?.Length > 3 ? parts[3] : null;
        }
    }

    public void SetSortKey(string preOrderId, string itemId)
    {
        SortKey = $"PreOrder#{preOrderId}#Item#{itemId}";
    }

    [DynamoDBProperty("dishId")]
    public required string DishId { get; set; }

    // Item details
    [DynamoDBProperty("dishName")]
    public required string DishName { get; set; }

    [DynamoDBProperty("quantity")]
    public int Quantity { get; set; }

    [DynamoDBProperty("price")]
    public decimal Price { get; set; }

    [DynamoDBProperty("dishImageUrl")]
    public required string DishImageUrl { get; set; }

    [DynamoDBProperty("notes")]
    public string Notes { get; set; } = string.Empty;
}
