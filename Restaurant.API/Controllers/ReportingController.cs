using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs.Reports;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities.Enums;

namespace Restaurant.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportingController(IReportingService reportingService, ILogger<ReportingController> logger) : ControllerBase
    {
        /// <summary>
        /// Triggers sending a report email with an Excel file attachment.
        /// </summary>
        /// <returns>A success message upon successfully sending the report.</returns>
        /// <response code="200">Report email sent successfully</response>
        /// <response code="500">If report sending fails due to external service issues</response>
        [HttpPost("send-report")]
        public async Task<IActionResult> CreateClientReservations()
        {
            await reportingService.SendReportEmailAsync();
            return Ok(new { message = "Report email sent successfully." });
        }
        
        /// <summary>
        /// Gets report data based on the provided filters.
        /// </summary>
        /// <param name="request">Filter criteria for the report</param>
        /// <returns>Report data based on the specified filters</returns>
        /// <response code="200">Returns the report data</response>
        /// <response code="400">If the request parameters are invalid</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReports([FromQuery] ReportRequest request)
        { 
            var reports = await reportingService.GetReportsAsync(request);
            return Ok(reports);
        }
        
        /// <summary>
        /// Downloads a report in the specified format based on the provided filters.
        /// </summary>
        /// <param name="request">Filter criteria and format for the report download</param>
        /// <returns>A file containing the report data in the requested format</returns>
        /// <response code="200">Returns the report file</response>
        /// <response code="400">If the request parameters are invalid</response>
        /// <response code="401">If the request is not authorized</response>
        /// <response code="415">If the requested format is not supported</response>
        [HttpGet("downloads")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DownloadReport([FromQuery] ReportDownloadRequest request)
        {   
            try
            {
                var fileBytes = await reportingService.DownloadReportAsync(request);
                
                string contentType = GetContentType(request.Format);
                string fileName = $"Report_{request.StartDate:yyyyMMdd}_to_{request.EndDate:yyyyMMdd}.{GetFileExtension(request.Format)}";
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading report");
                return StatusCode(500, new { message = "Failed to download report", error = ex.Message });
            }
        }
        
        private string GetContentType(string format)
        {
            return format.ToLower() switch
            {
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                "csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }
        
        private string GetFileExtension(string format)
        {
            return format.ToLower() switch
            {
                "excel" => "xlsx",
                "pdf" => "pdf",
                "csv" => "csv",
                _ => "txt"
            };
        }
    }
}
