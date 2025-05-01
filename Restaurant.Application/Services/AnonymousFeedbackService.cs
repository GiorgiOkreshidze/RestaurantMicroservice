using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class AnonymousFeedbackService(
    ITokenService tokenService,
    IReservationRepository reservationRepository,
    IFeedbackService feedbackService) : IAnonymousFeedbackService
{
    public async Task<string> ValidateTokenAndGetReservationId(string token)
    {
        if (!tokenService.ValidateAnonymousFeedbackToken(token, out var reservationId))
        {
            throw new InvalidOperationException("Invalid or expired feedback token.");
        }

        var reservation = await reservationRepository.GetReservationByIdAsync(reservationId);
        if (reservation == null || reservation.Status != ReservationStatus.Finished.ToString())
        {
            throw new InvalidOperationException("Reservation not found or not completed.");
        }

        return reservationId;
    }

    public async Task SubmitAnonymousFeedback(CreateFeedbackRequest request)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(request.ReservationId);
        if (reservation == null || reservation.Status != ReservationStatus.Finished.ToString())
        {
            throw new InvalidOperationException("Reservation not found or not completed.");
        }

        var feedbackRequest = new CreateFeedbackRequest
        {
            ReservationId = request.ReservationId,
            CuisineRating = request.CuisineRating,
            CuisineComment = request.CuisineComment,
            ServiceRating = request.ServiceRating,
            ServiceComment = request.ServiceComment,
        };

        await feedbackService.AddAnonymousFeedbackAsync(feedbackRequest, reservation);
    }
}