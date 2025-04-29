using Microsoft.Extensions.Logging;

namespace Restaurant.Infrastructure.ExternalServices
{
    public class ReportServiceClient(HttpClient httpClient, ILogger<ReportServiceClient> logger) : IReportServiceClient
    {
        public async Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl)
        {
            logger.LogInformation("Sending request to reporting service to send report email");
                var endpoint = $"{baseUrl}/send-report";
                var response = await httpClient.PostAsync(endpoint, null);
                
                response.EnsureSuccessStatusCode();
                
                var resultMessage = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent report email request. Response: {Response}", resultMessage);
                
                return response;
        }
    }
}
