using FluentValidation;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Validations.Reports;

public class ReportRequestValidator : BaseReportValidator<ReportRequest>
{
    public ReportRequestValidator() : base()
    {
        // Add ReportType validation
        When(x => !string.IsNullOrEmpty(x.ReportType), () => {
            RuleFor(x => x.ReportType)
                .Must(type => string.Equals(type, "sales", StringComparison.OrdinalIgnoreCase) 
                               || string.Equals(type, "performance", StringComparison.OrdinalIgnoreCase))
                .WithMessage("ReportType must be either 'sales' or 'performance'");
        });
    }
}
