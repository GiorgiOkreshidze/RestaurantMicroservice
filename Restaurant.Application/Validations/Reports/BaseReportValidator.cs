using System.Globalization;
using System.Text.RegularExpressions;
using FluentValidation;
using Restaurant.Application.DTOs.Reports;

namespace Restaurant.Application.Validations.Reports;

public abstract class BaseReportValidator<T> : AbstractValidator<T> where T : ReportRequest
{
   
    private const string DateFormat = "yyyy-MM-dd";

    protected BaseReportValidator()
    {
        // Basic date format and presence validations
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required")
            .Must(IsValidIsoFormat).WithMessage("Start date must be in ISO format (YYYY-MM-DD)")
            .Must(IsNotInFuture).WithMessage("StartDate cannot be in the future");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required")
            .Must(IsValidIsoFormat).WithMessage("End date must be in ISO format (YYYY-MM-DD)")
            .Must(IsNotInFuture).WithMessage("EndDate cannot be in the future");

        // Date relationship validations
        When(BothDatesAreValid, () => {
            RuleFor(x => x)
                .Must(DatesAreInOrder)
                .WithMessage("EndDate must be after StartDate")
                .Must(DatesAreNotSameDay)
                .WithMessage("StartDate and EndDate cannot be the same day")
                .Must(DateRangeIsWithinMaximum)
                .WithMessage("Date range cannot exceed 50 years");
        });
    }

    // Basic date validations
    private static bool IsValidIsoFormat(string dateStr)
    {
        var regex = new Regex(@"^\d{4}-\d{2}-\d{2}$");
        if (!regex.IsMatch(dateStr))
            return false;
        
        return DateTime.TryParseExact(dateStr, DateFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static bool IsNotInFuture(string dateStr)
    {
        if (!ParseDate(dateStr, out var date))
            return true; // Skip this validation if date format is invalid
            
        return date.Date <= DateTime.UtcNow.Date;
    }
    
    private static bool BothDatesAreValid(T request)
    {
        return IsValidIsoFormat(request.StartDate) && IsValidIsoFormat(request.EndDate);
    }

    private static bool DatesAreInOrder(T request)
    {
        ParseDate(request.StartDate, out var startDate);
        ParseDate(request.EndDate, out var endDate);
        return endDate > startDate;
    }

    private static bool DatesAreNotSameDay(T request)
    {
        ParseDate(request.StartDate, out var startDate);
        ParseDate(request.EndDate, out var endDate);
        return startDate.Date != endDate.Date;
    }

    private static bool DateRangeIsWithinMaximum(T request)
    {
        ParseDate(request.StartDate, out var startDate);
        ParseDate(request.EndDate, out var endDate);
        const int maxYears = 50;
        return (endDate - startDate).TotalDays <= maxYears * 365.25;
    }

    private static bool ParseDate(string dateStr, out DateTime result)
    {
        return DateTime.TryParseExact(dateStr, DateFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }
}