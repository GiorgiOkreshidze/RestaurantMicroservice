using System.Globalization;
using FluentValidation;
using Restaurant.Application.DTOs.Tables;

namespace Restaurant.Application.Validations.Reservations
{
    public class FilterValidator : AbstractValidator<FilterParameters>
    {
        public FilterValidator()
        {
            RuleFor(x => x.LocationId)
            .NotEmpty().WithMessage("LocationId is required");

            RuleFor(x => x.Date)
           .NotEmpty().WithMessage("Date is required")
           .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Date must be in format yyyy-MM-dd")
           .Must(BeValidDate).WithMessage("Invalid date format")
           .Must(BeNotInPast).WithMessage("Reservation date cannot be in the past");

            RuleFor(x => x.Guests)
            .InclusiveBetween(1, 10).WithMessage("Guests must be between 1 and 10");

            When(x => !string.IsNullOrEmpty(x.Time), () => {
                RuleFor(x => x.Time)
                    .Matches(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$").WithMessage("Time must be in format HH:MM")
                    .Must((query, time) => BeValidTimeNotInPast(query.Date, time)).WithMessage("Reservation time cannot be in the past");
            });
        }

        private bool BeValidDate(string date)
        {
            return DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _);
        }

        private bool BeNotInPast(string date)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate >= DateTime.UtcNow.Date;
            }
            return true;
        }

        private bool BeValidTimeNotInPast(string date, string time)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var parsedDate) &&
                TimeSpan.TryParseExact(time, "hh\\:mm", CultureInfo.InvariantCulture, out var parsedTime))
            {
                var reservationDateTime = parsedDate.Add(parsedTime);
                return reservationDateTime >= DateTime.UtcNow;
            }
            return true;
        }
    }
}
