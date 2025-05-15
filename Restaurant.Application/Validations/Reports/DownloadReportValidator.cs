using FluentValidation;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Validations.Reports;

public class DownloadReportValidator : BaseReportValidator<ReportDownloadRequest>
{
    private static readonly string[] ValidReportTypes = ["sales", "performance"];
    
    public DownloadReportValidator() : base()
    {
        RuleFor(r => r.ReportType)
            .Must(rt => ValidReportTypes.Contains(rt, StringComparer.OrdinalIgnoreCase))
            .WithMessage("ReportType must be either 'sales' or 'performance'");
    }
}
