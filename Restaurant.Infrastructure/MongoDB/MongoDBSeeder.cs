using MongoDB.Driver;
using Restaurant.Domain.Entities;

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
        var locationCollection = _database.GetCollection<Location>("Locations");
        
        // Check if collection is empty
        if (!await locationCollection.Find(_ => true).AnyAsync())
        {
            // Seed initial data
            var locations = new List<Location>
            {
                new Location
                {
                    Id = "3a88c365-970b-4a7a-a206-bc5282b9b25f",
                    Address = "14 Baratashvili Street",
                    AverageOccupancy = 99,
                    Description = "A cozy retreat in the mountains, perfect for remote work.",
                    ImageUrl = "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Locations/compresed/ruben-ramirez-uk-unsplash.jpg",
                    Rating = 4.7,
                    TotalCapacity = 4
                },
                new Location
                {
                    Id = "e1fcb3b4-bf68-4bcb-b9ba-eac917dafac7",
                    Address = "9 Abashidze Street",
                    AverageOccupancy = 78,
                    Description = "A beachfront lounge with stunning views and great ambiance.",
                    ImageUrl = "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Locations/compresed/kayleigh-harrington-yhn4okt6ci0-unsplash+1.jpg",
                    Rating = 4.8,
                    TotalCapacity = 4
                },
                new Location
                {
                    Id = "8c4fc44e-c1a5-42eb-9912-55aeb5111a99",
                    Address = "48 Rustaveli Avenue",
                    AverageOccupancy = 12,
                    Description = "A spacious coworking space with modern amenities.",
                    ImageUrl = "https://team2-demo-bucket.s3.eu-west-2.amazonaws.com/Images/Locations/compresed/kaja-sariwating-6XMIzhk91_Y-unsplash+1.jpg",
                    Rating = 4.5,
                    TotalCapacity = 4
                }
            };

            await locationCollection.InsertManyAsync(locations);
        }
    }
}