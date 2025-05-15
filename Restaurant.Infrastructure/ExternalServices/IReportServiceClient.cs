
namespace Restaurant.Infrastructure.ExternalServices;

public interface IReportServiceClient
{
    Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl);
        
    Task<(List<WaiterSummaryResponse> WaiterSummaries, List<LocationSummaryResponse> LocationSummaries)> GetReportsAsync(
        string baseUrl, string startDate, string endDate, string? locationId, string? reportType = null);
        
    Task<byte[]> DownloadReportAsync(
        string baseUrl, string startDate, string endDate, string? locationId, string format, string reportType);
}