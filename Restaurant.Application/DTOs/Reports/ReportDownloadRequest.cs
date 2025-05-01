using System;

namespace Restaurant.Application.DTOs.Reports
{
    public class ReportDownloadRequest : ReportRequest
    {
        public string Format { get; set; } = "Excel";
    }
}
