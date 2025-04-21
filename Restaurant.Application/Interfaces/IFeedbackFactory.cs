using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Users;

namespace Restaurant.Application.Interfaces;

public interface IFeedbackFactory
{
    Task<FeedbackDto[]> CreateFeedbacksAsync(CreateFeedbackRequest request, UserDto user, ReservationDto reservation);
}