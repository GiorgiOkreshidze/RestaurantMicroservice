using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Web;

namespace Restaurant.Infrastructure.ExternalServices
{
    public class ReportServiceClient(HttpClient httpClient, ILogger<ReportServiceClient> logger) : IReportServiceClient
    {
        public async Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl)
        {
                var endpoint = $"{baseUrl}/reports/send";
                logger.LogInformation("sent to url {Endpoint}", endpoint);
                var response = await httpClient.PostAsync(endpoint, null);
                
                response.EnsureSuccessStatusCode();
                
                var resultMessage = await response.Content.ReadAsStringAsync();
                logger.LogInformation("Successfully sent report email request. Response: {Response}", resultMessage);
                
                return response;
        }
        
        public async Task<(List<ReportResponse> WaiterSummaries, List<LocationSummaryResponse> LocationSummaries)> GetReportsAsync(
            string baseUrl,
            string startDate,
            string endDate,
            string? locationId,
            string? reportType = null)
        {
            var queryParams = BuildQueryParams(startDate, endDate, locationId, reportType: reportType);
                
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
    
            var locationSummaries = new List<LocationSummaryResponse>();
            var waiterSummaries = new List<ReportResponse>();

            // Check if response is an object with Sales and Performance properties
            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;

                // Case 1: Both Sales and Performance in an object
                if (root.ValueKind == JsonValueKind.Object &&
                    (root.TryGetProperty("sales", out var salesElement) ||
                     root.TryGetProperty("performance", out var performanceElement)))
                {
                    if (root.TryGetProperty("sales", out salesElement))
                    {
                        locationSummaries = JsonSerializer.Deserialize<List<LocationSummaryResponse>>(
                            salesElement.GetRawText(), options) ?? new List<LocationSummaryResponse>();
                    }

                    if (root.TryGetProperty("performance", out performanceElement))
                    {
                        waiterSummaries = JsonSerializer.Deserialize<List<ReportResponse>>(
                            performanceElement.GetRawText(), options) ?? new List<ReportResponse>();
                    }
                }
                // Case 2: Direct array of LocationSummaryResponse (sales only)
                else if (string.Equals(reportType, "sales", StringComparison.OrdinalIgnoreCase))
                {
                    locationSummaries = JsonSerializer.Deserialize<List<LocationSummaryResponse>>(content, options) ?? 
                        new List<LocationSummaryResponse>();
                }
                // Case 3: Direct array of ReportResponse (performance only)
                else if (string.Equals(reportType, "performance", StringComparison.OrdinalIgnoreCase))
                {
                    waiterSummaries = JsonSerializer.Deserialize<List<ReportResponse>>(content, options) ?? 
                        new List<ReportResponse>();
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to deserialize report response");
                throw;
            }

            logger.LogInformation("Retrieved {LocationCount} location summaries and {WaiterCount} waiter summaries",
                locationSummaries.Count, waiterSummaries.Count);

            return (waiterSummaries, locationSummaries);
        }

        public async Task<byte[]> DownloadReportAsync(
            string baseUrl,
            string startDate,
            string endDate,
            string? locationId,
            string format,
            string reportType)
        {
            var queryParams = BuildQueryParams(startDate, endDate, locationId, format, reportType);
          
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
            string startDate, 
            string endDate, 
            string? locationId = null,
            string? format = null,
            string? reportType = null)
        {
            var queryParams = HttpUtility.ParseQueryString(string.Empty);
    
            queryParams["startDate"] = startDate;
            queryParams["endDate"] = endDate;
    
            if (!string.IsNullOrEmpty(locationId))
                queryParams["locationId"] = locationId;
        
            if (!string.IsNullOrEmpty(format))
                queryParams["format"] = format.ToLower();
            
            if (!string.IsNullOrEmpty(reportType))
                queryParams["reportType"] = reportType.ToLower();
        
            return queryParams;
        }
    }
}
