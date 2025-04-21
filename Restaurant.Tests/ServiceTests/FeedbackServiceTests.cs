using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.DTOs.Reservations;
using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;


namespace Restaurant.Tests.ServiceTests
{
    public class FeedbackServiceTests
    {
        private Mock<IFeedbackRepository> _feedbackRepositoryMock = null!;
        private Mock<IUserRepository> _userRepositoryMock = null!;
        private Mock<IFeedbackFactory> _feedbackFactoryMock = null!;
        private Mock<IReservationRepository> _reservationRepositoryMock = null!;
        private IMapper _mapper = null!;
        private IFeedbackService _feedbackService = null!;
        private List<Feedback> _feedbacks = null!;
        private readonly string _locationId = "3a88c365-970b-4a7a-a206-bc5282b9b25f";

        [SetUp]
        public void SetUp()
        {
            _feedbackRepositoryMock = new Mock<IFeedbackRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _feedbackFactoryMock = new Mock<IFeedbackFactory>();
            _reservationRepositoryMock = new Mock<IReservationRepository>();
            
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Feedback, FeedbackDto>().ReverseMap();
                cfg.CreateMap<User, UserDto>().ReverseMap();
                cfg.CreateMap<Reservation, ReservationDto>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _feedbackService = new FeedbackService(
                _feedbackRepositoryMock.Object,
                _reservationRepositoryMock.Object,
                _userRepositoryMock.Object,
                _feedbackFactoryMock.Object,
                _mapper);

            _feedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Id = "1",
                    LocationId = _locationId,
                    Type = "SERVICE_QUALITY",
                    TypeDate = "SERVICE_QUALITY#2025-04-24T12:00:00Z",
                    Rate = 5,
                    Comment = "Great place!",
                    UserName = "John Doe",
                    UserAvatarUrl = "https://example.com/avatar1.jpg",
                    Date = "2025-04-24T12:00:00Z",
                    ReservationId = "res-123",
                    LocationIdType = $"{_locationId}#SERVICE_QUALITY",
                    ReservationIdType = "res-123#SERVICE_QUALITY"
                },
                new Feedback
                {
                    Id = "2",
                    LocationId = _locationId,
                    Type = "CUISINE_EXPERIENCE",
                    TypeDate = "CUISINE_EXPERIENCE#2025-04-23T12:00:00Z",
                    Rate = 4,
                    Comment = "Delicious food!",
                    UserName = "Jane Smith",
                    UserAvatarUrl = "https://example.com/avatar2.jpg",
                    Date = "2025-04-23T12:00:00Z",
                    ReservationId = "res-456",
                    LocationIdType = $"{_locationId}#CUISINE_EXPERIENCE",
                    ReservationIdType = "res-456#CUISINE_EXPERIENCE"
                }
            };
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithValidId_ReturnsFeedbacksWithMetaData()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Page = 1,
                PageSize = 10,
                SortProperty = "date",
                SortDirection = "desc"
            };

            var feedbacks = new List<Feedback> { _feedbacks[0] };
            string nextToken = "next-token";

            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()))
                .ReturnsAsync((feedbacks, nextToken));

            // Act
            var result = await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo(feedbacks[0].Id));
            Assert.That(result.Content[0].Rate, Is.EqualTo(feedbacks[0].Rate));
            Assert.That(result.Content[0].Comment, Is.EqualTo(feedbacks[0].Comment));
            Assert.That(result.Content[0].UserName, Is.EqualTo(feedbacks[0].UserName));
            Assert.That(result.Content[0].Date, Is.EqualTo(feedbacks[0].Date));
            Assert.That(result.Content[0].Type, Is.EqualTo(feedbacks[0].Type));
            Assert.That(result.Content[0].LocationId, Is.EqualTo(feedbacks[0].LocationId));
            Assert.That(result.Token, Is.EqualTo(nextToken));
            Assert.That(result.Sort.Property, Is.EqualTo(queryParams.SortProperty));
            Assert.That(result.Sort.Direction, Is.EqualTo(queryParams.SortDirection.ToUpper()));
            Assert.That(result.Sort.Ascending, Is.EqualTo(queryParams.SortDirection.ToLower() == "asc"));
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithFeedbackType_ConvertsFeedbackTypeCorrectly()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Type = "SERVICE_QUALITY",
                Page = 1,
                PageSize = 10,
                SortProperty = "date",
                SortDirection = "desc"
            };

            var expectedEnumType = FeedbackType.ServiceQuality;

            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(_locationId, It.Is<FeedbackQueryParameters>(p =>
                p.EnumType == expectedEnumType)))
                .ReturnsAsync((new List<Feedback>(), null));

            // Act
            await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            _feedbackRepositoryMock.Verify(repo => repo.GetFeedbacksAsync(_locationId,
                It.Is<FeedbackQueryParameters>(p => p.EnumType == expectedEnumType)), Times.Once);
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithPage1_UsesCorrectPaginationLogic()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Page = 1,
                PageSize = 2,
                SortProperty = "date",
                SortDirection = "desc"
            };

            string nextToken = "next-token";
            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()))
                .ReturnsAsync((_feedbacks, nextToken));

            // Act
            var result = await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(2));
            Assert.That(result.Token, Is.EqualTo(nextToken));
            _feedbackRepositoryMock.Verify(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()), Times.Once);
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithPageGreaterThan1_FollowsPaginationTokens()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Page = 2,
                PageSize = 2,
                SortProperty = "date",
                SortDirection = "desc"
            };

            var firstPageFeedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Id = "1",
                    LocationId = _locationId,
                    Type = "SERVICE_QUALITY",
                    TypeDate = "SERVICE_QUALITY#2025-04-24T12:00:00Z",
                    Rate = 5,
                    Comment = "Great place!",
                    UserName = "John Doe",
                    UserAvatarUrl = "https://example.com/avatar1.jpg",
                    Date = "2025-04-24T12:00:00Z",
                    LocationIdType = $"{_locationId}#SERVICE_QUALITY",
                    ReservationIdType = "res-123#SERVICE_QUALITY"
                },
                new Feedback
                {
                    Id = "2",
                    LocationId = _locationId,
                    Type = "CUISINE_EXPERIENCE",
                    TypeDate = "CUISINE_EXPERIENCE#2025-04-23T12:00:00Z",
                    Rate = 4,
                    Comment = "Delicious food!",
                    UserName = "Jane Smith",
                    UserAvatarUrl = "https://example.com/avatar2.jpg",
                    Date = "2025-04-23T12:00:00Z",
                    LocationIdType = $"{_locationId}#CUISINE_EXPERIENCE",
                    ReservationIdType = "res-456#CUISINE_EXPERIENCE"
                }
            };

            var secondPageFeedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Id = "3",
                    LocationId = _locationId,
                    Type = "SERVICE_QUALITY",
                    TypeDate = "SERVICE_QUALITY#2025-04-20T12:00:00Z",
                    Rate = 3,
                    Comment = "Average service",
                    UserName = "Sam Smith",
                    UserAvatarUrl = "https://example.com/avatar3.jpg",
                    Date = "2025-04-20T12:00:00Z",
                    LocationIdType = $"{_locationId}#SERVICE_QUALITY",
                    ReservationIdType = "res-789#SERVICE_QUALITY"
                },
                new Feedback
                {
                    Id = "4",
                    LocationId = _locationId,
                    Type = "CUISINE_EXPERIENCE",
                    TypeDate = "CUISINE_EXPERIENCE#2025-04-19T12:00:00Z",
                    Rate = 2,
                    Comment = "Food was okay",
                    UserName = "Alex Johnson",
                    UserAvatarUrl = "https://example.com/avatar4.jpg",
                    Date = "2025-04-19T12:00:00Z",
                    LocationIdType = $"{_locationId}#CUISINE_EXPERIENCE",
                    ReservationIdType = "res-101#CUISINE_EXPERIENCE"
                }
            };

            string firstToken = "token-page-1";
            string secondToken = "token-page-2";

            int callCount = 0;
            _feedbackRepositoryMock
                .Setup(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()))
                .ReturnsAsync((string id, FeedbackQueryParameters parameters) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return (firstPageFeedbacks, firstToken);
                    }

                    // For subsequent calls, verify token is being used correctly
                    Assert.That(parameters.NextPageToken, Is.EqualTo(firstToken));
                    return (secondPageFeedbacks, secondToken);
                });

            // Act
            var result = await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(2));
            Assert.That(result.Content[0].Id, Is.EqualTo("3"));
            Assert.That(result.Content[1].Id, Is.EqualTo("4"));
            Assert.That(result.Token, Is.EqualTo(secondToken));
            _feedbackRepositoryMock.Verify(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()), Times.Exactly(2));
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithNoResults_ReturnsEmptyContent()
        {
            // Arrange
            string nonExistentLocationId = "non-existent-id";
            var queryParams = new FeedbackQueryParameters
            {
                Page = 1,
                PageSize = 10,
                SortProperty = "date",
                SortDirection = "desc"
            };

            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(nonExistentLocationId, It.IsAny<FeedbackQueryParameters>()))
                .ReturnsAsync((new List<Feedback>(), null));

            // Act
            var result = await _feedbackService.GetFeedbacksByLocationIdAsync(nonExistentLocationId, queryParams);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Is.Empty);
            Assert.That(result.Token, Is.Null);
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithSortByRating_UsesSortPropertyCorrectly()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Page = 1,
                PageSize = 10,
                SortProperty = "rating",
                SortDirection = "asc"
            };

            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(_locationId, It.Is<FeedbackQueryParameters>(p =>
                p.SortProperty == "rating" && p.SortDirection == "asc")))
                .ReturnsAsync((new List<Feedback>(), null));

            // Act
            await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            _feedbackRepositoryMock.Verify(repo => repo.GetFeedbacksAsync(_locationId,
                It.Is<FeedbackQueryParameters>(p => p.SortProperty == "rating" && p.SortDirection == "asc")), Times.Once);
        }

        [Test]
        public async Task GetFeedbacksByLocationIdAsync_WithNullTokenFromRepository_ReturnsNullToken()
        {
            // Arrange
            var queryParams = new FeedbackQueryParameters
            {
                Page = 1,
                PageSize = 10,
                SortProperty = "date",
                SortDirection = "desc"
            };

            var feedbacks = new List<Feedback>
            {
                new Feedback
                {
                    Id = "1",
                    LocationId = _locationId,
                    Type = "SERVICE_QUALITY",
                    TypeDate = "SERVICE_QUALITY#2025-04-24T12:00:00Z",
                    Rate = 5,
                    Comment = "Great place!",
                    UserName = "John Doe",
                    UserAvatarUrl = "https://example.com/avatar1.jpg",
                    Date = "2025-04-24T12:00:00Z",
                    LocationIdType = $"{_locationId}#SERVICE_QUALITY",
                    ReservationIdType = "res-123#SERVICE_QUALITY"
                }
            };

            _feedbackRepositoryMock.Setup(repo => repo.GetFeedbacksAsync(_locationId, It.IsAny<FeedbackQueryParameters>()))
                .ReturnsAsync((feedbacks, null));

            // Act
            var result = await _feedbackService.GetFeedbacksByLocationIdAsync(_locationId, queryParams);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Is.Not.Empty);
            Assert.That(result.Token, Is.Null);
        }

        [Test]
        public async Task AddFeedbackAsync_ValidData_UpsertsFeedbacks()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                ServiceRating = "5",
                CuisineRating = "4"
                // ...other properties...
            };
            
            var reservation = new Reservation
            {
                Id = "res-1",
                Date = "date",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                TableId = "table-1",
                LocationAddress = "address",
                UserEmail = "user@example.com",
                GuestsNumber = "1",
                LocationId = "loc 1",
                PreOrder = "not implemented",
                Status = "In Progress",
                TableCapacity = "3",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Different user
            };
            
            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                ImgUrl = "some url",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            
            var feedback = new FeedbackDto
            {
                Id = "feedback-1",
                Rate = 5,
                Comment = "Great service!",
                UserName = "John Doe",
                UserAvatarUrl = "https://example.com/avatar.jpg",
                Date = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                Type = "SERVICE_QUALITY",
                LocationId = reservation.LocationId,
                ReservationId = reservation.Id
            };
            
            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync("res-123"))
                .ReturnsAsync(reservation);
            _userRepositoryMock.Setup(u => u.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _feedbackFactoryMock.Setup(f => f.CreateFeedbacksAsync(request, It.IsAny<UserDto>(), It.IsAny<ReservationDto>()))
                .ReturnsAsync(new List<FeedbackDto> { feedback }.ToArray());

            // Act
            await _feedbackService.AddFeedbackAsync(request, "user-123");

            // Assert
            _feedbackRepositoryMock.Verify(f => f.UpsertFeedbackByReservationAndTypeAsync(It.Is<Feedback>(
                fb => fb.Id == "feedback-1")), Times.Once);
        }

        [Test]
        public void AddFeedbackAsync_ReservationNotFound_ThrowsNotFoundException()
        {
            // Arrange
            var request = new CreateFeedbackRequest { ReservationId = "invalid-res" };
            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync("invalid-res"))
                .ReturnsAsync((Reservation)null);

            // Act & Assert
            Assert.ThrowsAsync<NotFoundException>(() => _feedbackService.AddFeedbackAsync(request, "user-123"));
        }

        [Test]
        public void AddFeedbackAsync_UserNotRegistered_ThrowsUnauthorizedException()
        {
            // Arrange
            var reservation = new Reservation
            {
                Id = "res-1",
                Date = "date",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                TableId = "table-1",
                LocationAddress = "address",
                UserEmail = "otheruser@example.com",
                GuestsNumber = "1",
                LocationId = "loc 1",
                PreOrder = "not implemented",
                Status = "In Progress",
                TableCapacity = "3",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Different user
            };
            
            var request = new CreateFeedbackRequest { ReservationId = "res-123" };
            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync("res-123"))
                .ReturnsAsync(reservation);
            _userRepositoryMock.Setup(u => u.GetUserByIdAsync("user-123"))
                .ReturnsAsync((User)null);

            // Act & Assert
            Assert.ThrowsAsync<UnauthorizedException>(() => _feedbackService.AddFeedbackAsync(request, "user-123"));
        }

        [Test]
        public void AddFeedbackAsync_InvalidRating_ThrowsConflictException()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                CuisineRating = "6"
                // ...other properties...
            };
            
            var reservation = new Reservation
            {
                Id = "res-1",
                Date = "date",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                TableId = "table-1",
                LocationAddress = "address",
                UserEmail = "user@example.com",
                GuestsNumber = "1",
                LocationId = "loc 1",
                PreOrder = "not implemented",
                Status = "In Progress",
                TableCapacity = "3",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), // Different user
            };
            
            var user = new User
            {
                Id = "user-123",
                Email = "user@example.com",
                FirstName = "John",
                LastName = "Doe",
                ImgUrl = "some url",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
            
            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync("res-123"))
                .ReturnsAsync(reservation);
            _userRepositoryMock.Setup(u => u.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            // Act & Assert
            Assert.ThrowsAsync<ConflictException>(() => _feedbackService.AddFeedbackAsync(request, "user-123"));
        }
    }
}
