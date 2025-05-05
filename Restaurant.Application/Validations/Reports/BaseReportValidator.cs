using FluentValidation;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Validations.Reports;

public abstract class BaseReportValidator<T> : AbstractValidator<T> where T : ReportRequest
{
    protected BaseReportValidator()
    {
        RuleFor(x => x.StartDate).NotNull()
            .NotEmpty().WithMessage("StartDate is required")
            .Must(BeValidDate).WithMessage("StartDate must be in a valid date format");

        RuleFor(x => x.EndDate).NotNull()
            .NotEmpty().WithMessage("EndDate is required")
            .Must(BeValidDate).WithMessage("EndDate must be in a valid date format")
            .GreaterThan(x => x.StartDate).WithMessage("EndDate must be after StartDate");

        RuleFor(x => x)
            .Must(x => x.StartDate.Date != x.EndDate.Date)
            .When(x => x.StartDate != default && x.EndDate != default)
            .WithMessage("StartDate and EndDate cannot be the same day")
            .Must(DateRangeNotExceedMaximum)
            .When(x => x.StartDate != default && x.EndDate != default)
            .WithMessage("Date range cannot exceed 50 years");

        RuleFor(x => x.EndDate)
            .Must(BeNotInFuture)
            .WithMessage("EndDate cannot be in the future");
    }

    private static bool BeValidDate(DateTime date)
    {
        return date != default && date != DateTime.MinValue && date <= DateTime.MaxValue;
    }

    private static bool BeNotInFuture(DateTime date)
    {
        return date.Date <= DateTime.UtcNow.Date;
    }

    private static bool DateRangeNotExceedMaximum(ReportRequest request)
    {
        const int maxYears = 50;
        return (request.EndDate - request.StartDate).TotalDays <= maxYears * 365.25;
    }
}