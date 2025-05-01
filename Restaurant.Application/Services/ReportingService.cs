using Microsoft.Extensions.Logging;
using Restaurant.Application.DTOs.Reports;
using Microsoft.Extensions.Options;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.ExternalServices;
using AutoMapper;
using Restaurant.Infrastructure.ExternalServices;

namespace Restaurant.Application.Services
{
    public class ReportingService(IReportServiceClient reportServiceClient, ILogger<ReportingService> logger, IOptions<ReportSettings> options, IMapper mapper) : IReportingService
    {
        public async Task SendReportEmailAsync()
        {
            logger.LogInformation("Initiating report email sending process");
            await reportServiceClient.SendReportEmailAsync(options.Value.BaseUrl);
        }
        
        public async Task<IEnumerable<ReportResponse>> GetReportsAsync(ReportRequest request)
        {
            logger.LogInformation("Retrieving reports with filters. StartDate: {StartDate}, EndDate: {EndDate}, LocationId: {LocationId}",
                request.StartDate, request.EndDate, request.LocationId);
                
            var reports = await reportServiceClient.GetReportsAsync(options.Value.BaseUrl, request.StartDate, request.EndDate, request.LocationId);
            
            logger.LogInformation("Successfully retrieved {Count} reports", reports?.Count() ?? 0);
            return reports;
        }
        
        public async Task<byte[]> DownloadReportAsync(ReportDownloadRequest request)
        {
            logger.LogInformation("Downloading report. Format: {Format}, StartDate: {StartDate}, EndDate: {EndDate}, LocationId: {LocationId}",
                request.Format, request.StartDate, request.EndDate, request.LocationId);
            
            var fileBytes = await reportServiceClient.DownloadReportAsync(
                options.Value.BaseUrl, 
                request.StartDate, 
                request.EndDate, 
                request.LocationId, 
                request.Format);
            
            logger.LogInformation("Report download completed. Size: {Size} bytes", fileBytes?.Length ?? 0);
            return fileBytes;
        }
    }
}
