using MongoDB.Driver;
using Restaurant.Domain.Entities;
using System.Linq.Expressions;

namespace Restaurant.Infrastructure;

public class MongoDbSeeder
{
    private readonly IMongoDatabase _database;

    public MongoDbSeeder(IMongoDatabase database)
    {
        _database = database;
    }
    
    public async Task SeedAsync()
    {
        await CreateIndexesAsync();
        await SeedCollectionAsync<Dish>("Dishes", GetDishSeedData());
        await SeedCollectionAsync<EmployeeInfo>("EmployeeInfo", GetEmployeeInfoSeedData());
    }
    
    private async Task CreateIndexesAsync()
    {
        var dishCollection = _database.GetCollection<Dish>("Dishes");
        await CreateIndexAsync(dishCollection, d => d.LocationId, "LocationId_Index");
        await CreateIndexAsync(dishCollection, d => d.DishType, "DishType_Index");
    }
    
    private async Task CreateIndexAsync<T>(IMongoCollection<T> collection, 
        Expression<Func<T, object>> field, string indexName)
    {
        var indexKeysDefinition = Builders<T>.IndexKeys.Ascending(field);
        var indexOptions = new CreateIndexOptions { Name = indexName };
        var indexModel = new CreateIndexModel<T>(indexKeysDefinition, indexOptions);
        await collection.Indexes.CreateOneAsync(indexModel);
    }
    
    private async Task SeedCollectionAsync<T>(string collectionName, List<T> seedData) where T : class
    {
        var collection = _database.GetCollection<T>(collectionName);
        
        // Check if collection is empty
        if (!await collection.Find(_ => true).AnyAsync())
        {
            // Seed initial data
            await collection.InsertManyAsync(seedData);
        }
    }
   
    private List<Dish> GetDishSeedData()
    {
        return new List<Dish>
        {
            CreateDish("df5d475c-7de0-409c-95e7-1b389e921948", 
                "Roasted Sweet Potato & Lentil Salad", 
                10, 
                "MainCourses", 
                "3a88c365-970b-4a7a-a206-bc5282b9b25f", 
                true,
                "Warm, roasted sweet potatoes paired with hearty lentils in a flavorful salad.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(4).png",
                "620 kcal", "55-60 g", "35-45 g", "20-25 g", "Calcium, Vitamin A, Vitamin B12", "430 g"),
            
            CreateDish("a77b72ad-0537-4354-9986-547b9937be78", 
                "Avocado and Egg Toast", 
                9, 
                "Appetizers", 
                "3a88c365-970b-4a7a-a206-bc5282b9b25f", 
                true,
                "Creamy avocado and a perfectly cooked egg on crisp toast.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(6).png",
                "503 kcal", "30-35 g", "33-35 g", "18-30 g", "Vitamins A, C, E, and B12", "180 g"),
            
            CreateDish("204aa910-7095-4ff3-a9d4-379e7d44cedb", 
                "Chocolate Mousse with Berries", 
                9, 
                "Desserts", 
                "e1fcb3b4-bf68-4bcb-b9ba-eac917dafac7", 
                false,
                "Rich, velvety chocolate mousse topped with juicy, fresh berries.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(5).png",
                "503 kcal", "30-35 g", "33-35 g", "18-30 g", "Vitamin C, Vitamin A, calcium and iron", "250 g",
                "On Stop"),
            
            CreateDish("ed3f1f9b-7412-42ea-84aa-04c9b85ab67a", 
                "Asparagus salad", 
                17, 
                "Appetizers", 
                "e1fcb3b4-bf68-4bcb-b9ba-eac917dafac7", 
                false,
                "Fresh, crisp asparagus tossed in a light, tangy salad.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(3).png",
                "420 kcal", "40-45 g", "15-20 g", "35-40 g", "Omega-3, Vitamin D, Vitamin B12", "430 g",
                "On Stop"),
            
            CreateDish("03915567-4385-4455-91f5-9e6c8cc695fd", 
                "Pineapple Tart with Vanilla Souffle", 
                6, 
                "Desserts", 
                "8c4fc44e-c1a5-42eb-9912-55aeb5111a99", 
                true,
                "Sweet, tropical pineapple tart topped with light, fluffy vanilla souffle.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(1).png",
                "480 kcal", "15-20 g", "28-32 g", "35-40 g", "Vitamin B6, Vitamin B12, Vitamin D", "130 g"),
            
            CreateDish("eb8bc656-cbb8-4a78-a81a-c59f74ac00a0", 
                "Fresh Strawberry Mint Salad", 
                15, 
                "MainCourses", 
                "e1fcb3b4-bf68-4bcb-b9ba-eac917dafac7", 
                true,
                "A refreshing mix of juicy strawberries and aromatic mint.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture.png",
                "400 kcal", "30-35 g", "20-35 g", "18-30 g", "Vitamin C, Vitamin K, and folate.", "430 g"),
            
            CreateDish("75cf91de-a9b4-4fdf-9533-c7c38500de4c", 
                "Spring Salad", 
                14, 
                "MainCourses", 
                "8c4fc44e-c1a5-42eb-9912-55aeb5111a99", 
                true,
                "Light, vibrant mix of fresh greens and seasonal veggies.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(7).png",
                "503 kcal", "30-35 g", "33-35 g", "18-30 g", "Vitamins A, C, K, and folate", "430 g",
                "On Stop"),
            
            CreateDish("39f71b8f-987f-4cde-bb51-4ea0757932af", 
                "Avocado Pine Nut Bowl", 
                15, 
                "MainCourses", 
                "8c4fc44e-c1a5-42eb-9912-55aeb5111a99", 
                true,
                "Creamy avocado mixed with crunchy pine nuts in a savory bowl.",
                "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Dishes/compresed/Dish+picture+(2).png",
                "550 kcal", "35-40 g", "38-42 g", "25-30 g", "Vitamin B12, Calcium, Zinc", "430 g")
        };
    }

    private Dish CreateDish(string id, string name, decimal price, string dishType, string locationId, bool isPopular,
        string description = null, string imageUrl = null, string calories = null, string carbohydrates = null, 
        string fats = null, string proteins = null, string vitamins = null, string weight = null, string state = "Active")
    {
        return new Dish
        {
            Id = id,
            Name = name,
            Price = price,
            DishType = dishType,
            LocationId = locationId,
            IsPopular = isPopular,
            Description = description,
            ImageUrl = imageUrl,
            Calories = calories,
            Carbohydrates = carbohydrates,
            Fats = fats,
            Proteins = proteins,
            Vitamins = vitamins,
            Weight = weight,
            State = state
        };
    }
    
    private List<EmployeeInfo> GetEmployeeInfoSeedData()
    {
        return new List<EmployeeInfo>
        {
            CreateEmployeeInfo("laydyGaga98@example.com", "8c4fc44e-c1a5-42eb-9912-55aeb5111a99"),
            CreateEmployeeInfo("johnyDepp007@example.com", "8c4fc44e-c1a5-42eb-9912-55aeb5111a99"),
            CreateEmployeeInfo("kylieJenner69@example.com", "e1fcb3b4-bf68-4bcb-b9ba-eac917dafac7"),
            CreateEmployeeInfo("johnCena46@example.com", "3a88c365-970b-4a7a-a206-bc5282b9b25f")
        };
    }

    private EmployeeInfo CreateEmployeeInfo(string email, string locationId)
    {
        return new EmployeeInfo
        {
            Email = email,
            LocationId = locationId,
            Role = "Waiter",
        };
    }
}
