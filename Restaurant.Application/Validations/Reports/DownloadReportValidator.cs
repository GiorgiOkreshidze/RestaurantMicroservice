using FluentValidation;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Validations.Reports;

public class DownloadReportValidator : BaseReportValidator<ReportDownloadRequest>
{
    public DownloadReportValidator() : base() { }
}
