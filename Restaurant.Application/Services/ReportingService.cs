using Microsoft.Extensions.Logging;
using Restaurant.Application.DTOs.Reports;
using Microsoft.Extensions.Options;
using Restaurant.Application.Interfaces;
using Restaurant.Infrastructure.ExternalServices;
using AutoMapper;
using FluentValidation;
using Restaurant.Application.Exceptions;
using Restaurant.Infrastructure.ExternalServices;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services
{
    public class ReportingService(
        IReportServiceClient reportServiceClient,
        ILogger<ReportingService> logger,
        IOptions<ReportSettings> options,
        IValidator<ReportRequest> reportRequestValidator,
        IValidator<ReportDownloadRequest> reportDownloadRequestValidator,
        ILocationRepository locationRepository) : IReportingService
    {
        public async Task SendReportEmailAsync()
        {
            logger.LogInformation("Initiating report email sending process");
            await reportServiceClient.SendReportEmailAsync(options.Value.BaseUrl);
        }
        
        public async Task<IEnumerable<ReportResponse>> GetReportsAsync(ReportRequest request)
        {
            await ValidateRequestAsync(request, reportRequestValidator);
            await ValidateLocationIdAsync(request.LocationId);
            
            logger.LogInformation("Retrieving reports with filters. StartDate: {StartDate}, EndDate: {EndDate}, LocationId: {LocationId}",
                request.StartDate, request.EndDate, request.LocationId);
                
            var reports = await reportServiceClient.GetReportsAsync(options.Value.BaseUrl, request.StartDate, request.EndDate, request.LocationId);
            
            logger.LogInformation("Successfully retrieved {Count} reports", reports?.Count() ?? 0);
            return reports ?? [];
        }
        
        public async Task<byte[]> DownloadReportAsync(ReportDownloadRequest request)
        {
            await ValidateRequestAsync(request, reportDownloadRequestValidator);
            await ValidateLocationIdAsync(request.LocationId);
            ValidateReportFormat(request.Format);
            
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
        
        private static async Task ValidateRequestAsync<T>(T request, IValidator<T> validator)
        {
            var validationResult = await validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new BadRequestException("Invalid Request", validationResult);
            }
        }
        
        private async Task ValidateLocationIdAsync(string? locationId)
        {
            if (locationId is not null && !await locationRepository.LocationExistsAsync(locationId))
            {
                throw new NotFoundException($"Location with ID {locationId} not found.");
            }
        }
        
        private static void ValidateReportFormat(string format)
        {
            var validFormats = new[] { "excel", "pdf", "csv" };
            if (!validFormats.Contains(format.ToLower()))
            {
                throw new BadRequestException("Invalid format. Supported formats are 'excel', 'pdf', or 'csv'");
            }
        }
    }
}
