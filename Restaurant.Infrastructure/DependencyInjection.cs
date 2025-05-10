using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Infrastructure.AWS;
using Restaurant.Infrastructure.Interfaces;
using Restaurant.Infrastructure.Repositories;
using Restaurant.Infrastructure.Services;
using Amazon.S3;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Restaurant.Infrastructure.MongoDB;

namespace Restaurant.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var credentialsFactory = new AwsCredentialsFactory(configuration);
        // Use .Result to block until the async method completes, Injection should be done in a non-async context.
        var (stsClient, credentials) = credentialsFactory.CreateCredentialsAsync().Result;
        services.AddSingleton(stsClient);
        services.AddSingleton(credentials);
        services.AddSingleton<IAmazonDynamoDB>(_ => DynamoDbFactory.CreateDynamoDbClient(credentials));
        services.AddSingleton<IDynamoDBContext>(sp =>
            DynamoDbFactory.CreateDynamoDbContext(sp.GetRequiredService<IAmazonDynamoDB>()));
        services.AddSingleton<IAmazonSQS>(_ => SqsFactory.CreateSqsClient(credentials));
        services.AddSingleton<IAmazonS3>(_ => S3Factory.CreateS3Client(credentials));
        
        // Register MongoDB
        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING_STANDARD_SRV") ?? 
                                       Environment.GetEnvironmentVariable("CONNECTION_STRING_STANDARD") ?? 
                                       "mongodb://localhost:27017";
            options.DatabaseName = "RestaurantDb";
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return client.GetDatabase(settings.DatabaseName);
        });
        services.AddScoped<MongoDbSeeder>();
        services.AddSingleton<AwsCredentialsFactory>();

        // Register services and repositories
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IDishRepository, DishRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<ITableRepository, TableRepository>();
        services.AddScoped<IWaiterRepository, WaiterRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();
        services.AddScoped<IPreOrderRepository, PreOrderRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
