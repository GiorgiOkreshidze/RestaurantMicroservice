
namespace Restaurant.Infrastructure.ExternalServices
{
    public interface IReportServiceClient
    {
        Task<HttpResponseMessage> SendReportEmailAsync(string baseUrl);
        
        Task<IEnumerable<ReportResponse>> GetReportsAsync(string baseUrl, DateTime startDate, DateTime endDate, string? locationId);
        
        Task<byte[]> DownloadReportAsync(string baseUrl, DateTime startDate, DateTime endDate, string? locationId, string format);
    }
}
