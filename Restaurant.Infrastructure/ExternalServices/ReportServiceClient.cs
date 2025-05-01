using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;

namespace Restaurant.Infrastructure.ExternalServices
{
    public class ReportServiceClient(HttpClient httpClient, ILogger<ReportServiceClient> logger) : IReportServiceClient
    {
        public async Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl)
        {
            logger.LogInformation("Sending request to reporting service to send report email");
                var endpoint = $"{baseUrl}/send";
                var response = await httpClient.PostAsync(endpoint, null);
                
                response.EnsureSuccessStatusCode();
                
                var resultMessage = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent report email request. Response: {Response}", resultMessage);
                
                return response;
        }
        
        public async Task<IEnumerable<ReportResponse>> GetReportsAsync(string baseUrl, DateTime startDate, DateTime endDate, string? locationId)
        {
            var queryParams = BuildQueryParams(startDate, endDate, locationId);
                
            var endpoint = $"{baseUrl}/reports";
            if (queryParams.Count > 0)
            {
                endpoint += $"?{queryParams}";
            }
            
            var response = await httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            logger.LogInformation("Successfully retrieved report data from external service");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var reports = JsonSerializer.Deserialize<IEnumerable<ReportResponse>>(content, options);
            //write log for reports
            logger.LogInformation("Retrieved reports: {Reports}", JsonSerializer.Serialize(reports));
            return reports;
        }

        public async Task<byte[]> DownloadReportAsync(string baseUrl, DateTime startDate, DateTime endDate, string? locationId, string format)
        {
            var queryParams = BuildQueryParams(startDate, endDate, locationId, format);
          
            var endpoint = $"{baseUrl}/reports/download";
            if (queryParams.Count > 0)
            {
                endpoint += $"?{queryParams}";
            }
            
            logger.LogInformation("Sending download request to {Endpoint}", endpoint);
            var response = await httpClient.GetAsync(endpoint);
            
            response.EnsureSuccessStatusCode();
            
            var fileContent = await response.Content.ReadAsByteArrayAsync();
            logger.LogInformation("Successfully downloaded report in {Format} format, size: {Size} bytes", 
                format, fileContent.Length);
                
            return fileContent;
        }
        
        private static System.Collections.Specialized.NameValueCollection BuildQueryParams(
            DateTime startDate, 
            DateTime endDate, 
            string? locationId = null,
            string? format = null)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
    
            queryParams["startDate"] = startDate.ToString("yyyy-MM-dd");
            queryParams["endDate"] = endDate.ToString("yyyy-MM-dd");
    
            if (!string.IsNullOrEmpty(locationId))
                queryParams["locationId"] = locationId;
        
            if (!string.IsNullOrEmpty(format))
                queryParams["format"] = format.ToLower();
        
            return queryParams;
        }
    }
}
