using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class PreOrderRepository(IDynamoDBContext context) : IPreOrderRepository
{
    public async Task<List<PreOrder>> GetPreOrdersAsync(string userId)
    {
        var queryConfig = new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "userId = :userId and begins_with(sk, :prefix)",
                ExpressionAttributeValues = 
                {
                    { ":userId", userId },
                    { ":prefix", "PreOrder#" }
                }
            }
        };

        var table = context.GetTargetTable<PreOrder>();
        var search = table.Query(queryConfig);
        var allItems = await search.GetRemainingAsync();

        var preOrderMap = new Dictionary<string, PreOrder>();

        foreach ( var item in allItems)
        {
            var sk = item["sk"].AsString();

            if (!sk.Contains("#Item#"))
            {
                var preOrderId = sk.Replace("PreOrder#", "");

                var preOrder = new PreOrder
                {
                    UserId = item["userId"].AsString(),
                    SortKey = sk,
                    ReservationId = item.ContainsKey("reservationId") ? item["reservationId"].AsString() : String.Empty,
                    Status = item.ContainsKey("status") ? item["status"].AsString() : String.Empty,
                    CreateDate = item.ContainsKey("createDate") ? DateTime.Parse(item["createDate"].AsString()) : DateTime.MinValue,
                    TotalAmount = item.ContainsKey("totalAmount") ? item["totalAmount"].AsDecimal() : 0,
                    Items = new List<PreOrderItem>(),
                    Address = item.ContainsKey("address") ? item["address"].AsString() : String.Empty,
                    TimeSlot = item.ContainsKey("timeSlot") ? item["timeSlot"].AsString() : String.Empty
                };

                preOrderMap[preOrderId] = preOrder;
            }
            else
            {
                var parts = sk.Split('#');
                var preOrderId = parts[1];

                if (preOrderMap.ContainsKey(preOrderId))
                {
                    var preOrderItem = new PreOrderItem
                    {
                        UserId = item["userId"].AsString(),
                        SortKey = sk,
                        DishId = item.ContainsKey("dishId") ? item["dishId"].AsString() : string.Empty,
                        DishName = item.ContainsKey("dishName") ? item["dishName"].AsString() : string.Empty,
                        Quantity = item.ContainsKey("quantity") ? (int)item["quantity"].AsDecimal() : 0,
                        Price = item.ContainsKey("price") ? item["price"].AsDecimal() : 0,
                        DishImageUrl = item.ContainsKey("dishImageUrl") ? item["dishImageUrl"].AsString() : string.Empty,
                        Notes = item.ContainsKey("notes") ? item["notes"].AsString() : string.Empty
                    };

                    preOrderMap[preOrderId].Items.Add(preOrderItem);
                }
            }
        }

        return preOrderMap.Values.ToList();
    }
}
