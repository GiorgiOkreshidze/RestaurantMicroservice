using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class PreOrderRepository(IDynamoDBContext context) : IPreOrderRepository
{
    public async Task<List<PreOrder>> GetPreOrdersAsync(string userId)
    {
        var items = await QueryPreOrderItemsAsync(userId);
        if (items.Count == 0)
            return [];
 
        var preOrderMap = ProcessPreOrderItems(items);
        return preOrderMap.Values.ToList();
    }
 
    private async Task<List<Document>> QueryPreOrderItemsAsync(string userId)
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
        return await search.GetRemainingAsync();
    }
 
    private Dictionary<string, PreOrder> ProcessPreOrderItems(List<Document> items)
    {
        var preOrderMap = new Dictionary<string, PreOrder>();
 
        foreach (var item in items)
        {
            var sk = item["sk"].AsString();
 
            if (sk.Contains("#Item#"))
            {
                ProcessOrderItem(item, sk, preOrderMap);
            }
            else
            {
                ProcessOrder(item, sk, preOrderMap);
            }
        }
 
        return preOrderMap;
    }
 
    private void ProcessOrder(Document item, string sortKey, Dictionary<string, PreOrder> preOrderMap)
    {
        var preOrderId = sortKey.Replace("PreOrder#", "");
 
        var preOrder = new PreOrder
        {
            UserId = item["userId"].AsString(),
            SortKey = sortKey,
            ReservationId = GetStringValue(item, "reservationId"),
            Status = GetStringValue(item, "status"),
            CreateDate = GetDateValue(item, "createDate"),
            TotalAmount = GetDecimalValue(item, "totalAmount"),
            Address = GetStringValue(item, "address"),
            TimeSlot = GetStringValue(item, "timeSlot"),
            Items = []
        };
 
        preOrderMap[preOrderId] = preOrder;
    }
 
    private void ProcessOrderItem(Document item, string sortKey, Dictionary<string, PreOrder> preOrderMap)
    {
        var parts = sortKey.Split('#');
        var preOrderId = parts[1];
 
        if (!preOrderMap.ContainsKey(preOrderId))
            return;
 
        var preOrderItem = new PreOrderItem
        {
            UserId = item["userId"].AsString(),
            SortKey = sortKey,
            DishId = GetStringValue(item, "dishId"),
            DishName = GetStringValue(item, "dishName"),
            Quantity = GetIntValue(item, "quantity"),
            Price = GetDecimalValue(item, "price"),
            DishImageUrl = GetStringValue(item, "dishImageUrl"),
            Notes = GetStringValue(item, "notes")
        };
        
        preOrderMap[preOrderId].Items.Add(preOrderItem);
    }
 
    // Helper methods to safely extract values from Document
    private static string GetStringValue(Document item, string key) =>
        item.ContainsKey(key) ? item[key].AsString() : string.Empty;
 
    private static int GetIntValue(Document item, string key) =>
        item.ContainsKey(key) ? (int)item[key].AsDecimal() : 0;
 
    private static decimal GetDecimalValue(Document item, string key) =>
        item.ContainsKey(key) ? item[key].AsDecimal() : 0;
 
    private static DateTime GetDateValue(Document item, string key) =>
        item.ContainsKey(key) ? DateTime.Parse(item[key].AsString()) : DateTime.MinValue;
}
