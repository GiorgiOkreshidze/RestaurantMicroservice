using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.Interfaces;

namespace Restaurant.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportingController(IReportingService reportingService) : ControllerBase
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
    }
}
