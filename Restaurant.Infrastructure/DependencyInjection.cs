using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Restaurant.Infrastructure.AWS;
using Restaurant.Infrastructure.Interfaces;
using Restaurant.Infrastructure.Repositories;

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

        services.AddScoped<ILocationRepository, LocationRepository>();

        return services;
    }
}