using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Infrastructure.Repositories;

public class PreOrderRepository(IDynamoDBContext context) : IPreOrderRepository
{
    public async Task<List<PreOrder>> GetPreOrdersAsync(string userId, bool includeCancelled = false)
    {
        var items = await QueryPreOrderItemsAsync(userId);
        if (items.Count == 0)
            return [];

        var preOrderMap = ProcessPreOrderItems(items);

        if (!includeCancelled)
        {
            return preOrderMap.Values
                .Where(p => p.Status != "Cancelled")
                .ToList();
        }
        
        return preOrderMap.Values.ToList();
    }

    public async Task<PreOrder?> GetPreOrderByIdAsync(string userId, string preOrderId)
    {
        var items = await QueryPreOrderItemsAsync(userId, preOrderId);
        if (items.Count == 0)
            return null;

        var preOrderMap = ProcessPreOrderItems(items);
        return preOrderMap.Values.FirstOrDefault();
    }

    public async Task<PreOrder> CreatePreOrderAsync(PreOrder preOrder)
    {
        if (string.IsNullOrEmpty(preOrder.PreOrderId))
            preOrder.SetSortKey(Guid.NewGuid().ToString("N"));
        
        if (preOrder.CreateDate == default)
            preOrder.CreateDate = DateTime.UtcNow;
        
        await context.SaveAsync(preOrder);
        foreach (var item in preOrder.Items)
        {
            item.UserId = preOrder.UserId;
            item.SetSortKey(preOrder.PreOrderId!, Guid.NewGuid().ToString("N"));
            await context.SaveAsync(item);
        }

        return preOrder;
    }

    public async Task UpdatePreOrderAsync(PreOrder preOrder)
    {
        var existingPreOrder = await GetPreOrderByIdAsync(preOrder.UserId, preOrder.PreOrderId!);
        if (existingPreOrder == null)
            throw new InvalidOperationException($"Pre-order with ID {preOrder.PreOrderId} not found");

        await context.SaveAsync(preOrder);

        foreach (var item in existingPreOrder.Items)
        {
            await context.DeleteAsync(item);
        }
        
        foreach (var item in preOrder.Items)
        {
            item.UserId = preOrder.UserId;
            item.SetSortKey(preOrder.PreOrderId!, Guid.NewGuid().ToString("N"));
            await context.SaveAsync(item);
        }
    }

    private async Task<List<Document>> QueryPreOrderItemsAsync(string userId, string? preOrderId = null)
    {
        var prefix = preOrderId is null ? "PreOrder#" : $"PreOrder#{preOrderId}";
        var queryConfig = new QueryOperationConfig
        {
            KeyExpression = new Expression
            {
                ExpressionStatement = "userId = :userId and begins_with(sk, :prefix)",
                ExpressionAttributeValues = 
                {
                    { ":userId", userId },
                    { ":prefix", prefix }
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
            Address = GetStringValue(item, "address"),
            CreateDate = GetDateValue(item, "createDate"),
            ReservationDate = GetStringValue(item, "reservationDate"),
            TimeSlot = GetStringValue(item, "timeSlot"),
            TotalPrice = GetDecimalValue(item, "totalPrice"),
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
    private string GetStringValue(Document item, string key) =>
        item.ContainsKey(key) ? item[key].AsString() : string.Empty;

    private int GetIntValue(Document item, string key) =>
        item.ContainsKey(key) ? (int)item[key].AsDecimal() : 0;

    private decimal GetDecimalValue(Document item, string key) =>
        item.ContainsKey(key) ? item[key].AsDecimal() : 0;

    private DateTime GetDateValue(Document item, string key) =>
        item.ContainsKey(key) ? DateTime.Parse(item[key].AsString()) : DateTime.MinValue;
}
