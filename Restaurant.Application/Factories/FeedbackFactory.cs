using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Interfaces;

namespace Restaurant.Application.Factories;

public class FeedbackFactory : IFeedbackFactory
{
    public async Task<FeedbackDto[]> CreateFeedbacksAsync(CreateFeedbackRequest request, UserDto user,
        ReservationDto reservation)
    {
        var feedbacks = new List<FeedbackDto>();
        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        if (!string.IsNullOrEmpty(request.CuisineRating))
        {
            feedbacks.Add(new FeedbackDto
            {
                Id = Guid.NewGuid().ToString(),
                Rate = Convert.ToInt32(request.CuisineRating),
                Comment = request.CuisineComment ?? string.Empty,
                UserName = user.GetFullName(),
                UserAvatarUrl = user.ImgUrl,
                Date = currentDate,
                Type = "CUISINE_EXPERIENCE",
                LocationId = reservation.LocationId,
                ReservationId = reservation.Id
            });
        }

        if (!string.IsNullOrEmpty(request.ServiceRating))
        {
            feedbacks.Add(new FeedbackDto
            {
                Id = Guid.NewGuid().ToString(),
                Rate = Convert.ToInt32(request.ServiceRating),
                Comment = request.ServiceComment ?? string.Empty,
                UserName = user.GetFullName(),
                UserAvatarUrl = user.ImgUrl,
                Date = currentDate,
                Type = "SERVICE_QUALITY",
                LocationId = reservation.LocationId,
                ReservationId = reservation.Id
            });
        }

        return feedbacks.ToArray();
    }
}