using Restaurant.Infrastructure.AWS;

namespace Restaurant.API.BackgroundServices;

public class AwsCredentialsRefresher(AwsCredentialsFactory credentialsFactory, ILogger<AwsCredentialsRefresher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Refreshing AWS credentials at: {Time}", DateTimeOffset.Now);
                var (stsClient, credentials) = await credentialsFactory.CreateCredentialsAsync();
                
                logger.LogInformation("AWS credentials refreshed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing AWS credentials.");
            }

            await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
        }
    }
}