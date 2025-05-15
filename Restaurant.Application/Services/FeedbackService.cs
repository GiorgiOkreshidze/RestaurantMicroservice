using System.ComponentModel;
using AutoMapper;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class FeedbackService(
    IFeedbackRepository feedbackRepository, 
    IReservationRepository reservationRepository, 
    IUserRepository userRepository,
    IFeedbackFactory feedbackFactory,
    ILocationRepository locationRepository,
    IMapper mapper) : IFeedbackService
{
    public async Task<FeedbacksWithMetaData> GetFeedbacksByLocationIdAsync(string id, FeedbackQueryParameters queryParams)
    {
        // First verify the location exists
        var locationExists = await locationRepository.LocationExistsAsync(id);
        if (!locationExists)
        {
            throw new NotFoundException("Location", id);
        }

        // Handle feedback type parameter safely
        if (!string.IsNullOrEmpty(queryParams.Type))
        {
            try
            {
                queryParams.EnumType = queryParams.Type.ToFeedbackType();
            }
            catch (ArgumentException ex)
            {
                throw new NotFoundException(ex.Message);
            }
        }

        var (feedbacks, token) = await GetPaginatedFeedbacks(id, queryParams);
        var Response = BuildResponseBody(feedbacks, queryParams, token);

        return Response;
    }

    public async Task AddFeedbackAsync(CreateFeedbackRequest feedbackRequest, string userId)
    {
        var reservation = await reservationRepository.GetReservationByIdAsync(feedbackRequest.ReservationId);
        if (reservation is null) throw new NotFoundException("Reservation not found");
        
        var user = await userRepository.GetUserByIdAsync(userId);
        if (user is null) throw new UnauthorizedException("User is not registered");

        var userDto = mapper.Map<UserDto>(user);
        var reservationDto = mapper.Map<ReservationDto>(reservation);

        ValidateReservation(reservation, feedbackRequest);
        ValidateUser(reservation, user);

        var feedbackDtos = await feedbackFactory.CreateFeedbacksAsync(feedbackRequest, userDto, reservationDto);

        foreach (var feedbackDto in feedbackDtos)
        {
            var feedbackEntity = mapper.Map<Feedback>(feedbackDto);
            await feedbackRepository.UpsertFeedbackByReservationAndTypeAsync(feedbackEntity);
        }
    }

    public async Task AddAnonymousFeedbackAsync(CreateFeedbackRequest feedbackRequest, Reservation reservation)
    {
        var reservationDto = mapper.Map<ReservationDto>(reservation);
        
        ValidateReservation(reservation, feedbackRequest);
        
        // Create anonymous user info
        var anonymousUser = new UserDto
        {
           FirstName = "Anonymous",
           LastName = "Customer",
           Email = "anonymous@example.com",
           ImgUrl = ""
        };
        
        var feedbackDtos = await feedbackFactory
            .CreateFeedbacksAsync(feedbackRequest, anonymousUser, reservationDto);
        foreach (var feedbackDto in feedbackDtos)
        {
            feedbackDto.IsAnonymous = true;
            var feedbackEntity = mapper.Map<Feedback>(feedbackDto);
            await feedbackRepository.UpsertFeedbackByReservationAndTypeAsync(feedbackEntity);
        }
    }

    private void ValidateReservation(Reservation reservation, CreateFeedbackRequest feedbackRequest)
    {
        if (reservation.Status != Utils.GetEnumDescription(ReservationStatus.InProgress) &&
            reservation.Status != Utils.GetEnumDescription(ReservationStatus.Finished))
            throw new ConflictException("Reservation should be in status 'In Progress' or 'Finished'");

        if (!string.IsNullOrEmpty(feedbackRequest.CuisineRating) &&
            int.TryParse(feedbackRequest.CuisineRating, out var cuisineRating) &&
            (cuisineRating < 0 || cuisineRating > 5))
        {
            throw new ConflictException("Cuisine rating must be between 0 and 5");
        }

        if (!string.IsNullOrEmpty(feedbackRequest.ServiceRating) &&
            int.TryParse(feedbackRequest.ServiceRating, out var serviceRating) &&
            (serviceRating < 0 || serviceRating > 5))
        {
            throw new ConflictException("Service rating must be between 0 and 5");
        }
    }

    private void ValidateUser(Reservation reservation, User user)
    {
        if (reservation.UserEmail != user.Email)
            throw new UnauthorizedException("You are not authorized to add feedback for this reservation");
    }

    private async Task<(List<Feedback>, string?)> GetPaginatedFeedbacks(
        string id,
        FeedbackQueryParameters queryParams)
    {
        // Page 1 is simply a direct query
        if (queryParams.Page <= 1)
        {
            // Clear any token if we're requesting page 1 explicitly
            queryParams.NextPageToken = null;
            return await feedbackRepository.GetFeedbacksAsync(id, queryParams);
        }

        // For pages > 1, we need to follow the tokens
        string? currentToken = null;
        List<Feedback> currentPageFeedbacks = [];

        // Start with page 1
        var (firstPageFeedbacks, nextToken) = await feedbackRepository.GetFeedbacksAsync(id, queryParams);

        // If there's no first page or no next token, we can't get to page 2+
        if (firstPageFeedbacks.Count == 0 || nextToken == null)
        {
            return ([], null);
        }

        currentToken = nextToken;

        // Follow the tokens to get to the requested page
        for (int i = 1; i < queryParams.Page; i++)
        {
            // Update the token for the next query
            queryParams.NextPageToken = currentToken;

            // Get the next page
            var (pageFeedbacks, pageToken) = await feedbackRepository.GetFeedbacksAsync(id, queryParams);

            // If we got no results or no further token, we've reached the end
            if (pageFeedbacks.Count == 0)
            {
                return ([], null);
            }

            currentPageFeedbacks = pageFeedbacks;
            currentToken = pageToken;

            // If there's no next token, we've reached the last page
            if (currentToken == null)
            {
                break;
            }
        }

        return (currentPageFeedbacks, currentToken);
    }

    private FeedbacksWithMetaData BuildResponseBody(
        List<Feedback> feedbacks,
        FeedbackQueryParameters queryParams,
        string? token
    )
    {
        return new FeedbacksWithMetaData
        {
            Content = feedbacks.Select(f => new FeedbackDto
            {
                Id = f.Id,
                Rate = f.Rate,
                Comment = f.Comment,
                UserName = f.UserName,
                UserAvatarUrl = f.UserAvatarUrl,
                Date = f.Date,
                Type = f.Type,
                LocationId = f.LocationId,
                ReservationId = f.ReservationId,
                IsAnonymous = f.IsAnonymous 
            }).ToList(),
            Sort = new FeedbacksSortMetaData
            {
                Direction = queryParams.SortDirection.ToUpper(),
                NullHandling = "string",
                Ascending = queryParams.SortDirection.ToLower() == "asc",
                Property = queryParams.SortProperty,
                IgnoreCase = true
            },
            Token = token
        };
    }
}