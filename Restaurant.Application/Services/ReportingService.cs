using Microsoft.Extensions.Logging;
using Restaurant.Application.DTOs.Reports;
using Microsoft.Extensions.Options;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.ExternalServices;

namespace Restaurant.Application.Services
{
    public class ReportingService(IReportServiceClient reportServiceClient, ILogger<ReportingService> logger, IOptions<ReportSettings> options) : IReportingService
    {
        public async Task SendReportEmailAsync()
        {
            logger.LogInformation("Initiating report email sending process");
            await reportServiceClient.SendReportEmailAsync(options.Value.BaseUrl);
        }
    }
}
