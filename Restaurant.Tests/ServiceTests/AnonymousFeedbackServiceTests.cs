using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Feedbacks;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Entities.Enums;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests
{
    public class AnonymousFeedbackServiceTests
    {
        private Mock<ITokenService> _tokenServiceMock = null!;
        private Mock<IReservationRepository> _reservationRepositoryMock = null!;
        private Mock<IFeedbackService> _feedbackServiceMock = null!;
        private IAnonymousFeedbackService _anonymousFeedbackService = null!;

        [SetUp]
        public void SetUp()
        {
            _tokenServiceMock = new Mock<ITokenService>();
            _reservationRepositoryMock = new Mock<IReservationRepository>();
            _feedbackServiceMock = new Mock<IFeedbackService>();

            _anonymousFeedbackService = new AnonymousFeedbackService(
                _tokenServiceMock.Object,
                _reservationRepositoryMock.Object,
                _feedbackServiceMock.Object);
        }

        [Test]
        public async Task ValidateTokenAndGetReservationId_ValidToken_ReturnsReservationId()
        {
            // Arrange
            string token = "valid-token";
            string reservationId = "res-123";
            
            var reservation = new Reservation
            {
                Id = reservationId,
                Status = ReservationStatus.Finished.ToString(),
                LocationId = "loc-1",
                Date = "2023-05-01",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                GuestsNumber = "4",
                LocationAddress = "123 Main St",
                PreOrder = "Not implemented",
                TableId = "table-1",
                TableCapacity = "6",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.ValidateAnonymousFeedbackToken(token, out reservationId))
                .Returns(true);

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
                .ReturnsAsync(reservation);

            // Act
            var result = await _anonymousFeedbackService.ValidateTokenAndGetReservationId(token);

            // Assert
            Assert.That(result, Is.EqualTo(reservationId));
            _tokenServiceMock.Verify(t => t.ValidateAnonymousFeedbackToken(token, out It.Ref<string>.IsAny), Times.Once);
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(reservationId), Times.Once);
        }

        [Test]
        public void ValidateTokenAndGetReservationId_InvalidToken_ThrowsInvalidOperationException()
        {
            // Arrange
            string token = "invalid-token";
            string? reservationId = null;

            _tokenServiceMock.Setup(t => t.ValidateAnonymousFeedbackToken(token, out reservationId))
                .Returns(false);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _anonymousFeedbackService.ValidateTokenAndGetReservationId(token));

            Assert.That(ex.Message, Is.EqualTo("Invalid or expired feedback token."));
            _tokenServiceMock.Verify(t => t.ValidateAnonymousFeedbackToken(token, out It.Ref<string>.IsAny), Times.Once);
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void ValidateTokenAndGetReservationId_ReservationNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            string token = "valid-token";
            string reservationId = "res-123";

            _tokenServiceMock.Setup(t => t.ValidateAnonymousFeedbackToken(token, out reservationId))
                .Returns(true);

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
                .ReturnsAsync((Reservation)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _anonymousFeedbackService.ValidateTokenAndGetReservationId(token));

            Assert.That(ex.Message, Is.EqualTo("Reservation not found or not completed."));
            _tokenServiceMock.Verify(t => t.ValidateAnonymousFeedbackToken(token, out It.Ref<string>.IsAny), Times.Once);
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(reservationId), Times.Once);
        }

        [Test]
        public void ValidateTokenAndGetReservationId_ReservationNotFinished_ThrowsInvalidOperationException()
        {
            // Arrange
            string token = "valid-token";
            string reservationId = "res-123";
            
            var reservation = new Reservation
            {
                Id = reservationId,
                Status = ReservationStatus.InProgress.ToString(), // Not finished
                LocationId = "loc-1",
                Date = "2023-05-01",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                GuestsNumber = "4",
                LocationAddress = "123 Main St",
                PreOrder = "Not implemented",
                TableId = "table-1",
                TableCapacity = "6",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _tokenServiceMock.Setup(t => t.ValidateAnonymousFeedbackToken(token, out reservationId))
                .Returns(true);

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
                .ReturnsAsync(reservation);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _anonymousFeedbackService.ValidateTokenAndGetReservationId(token));

            Assert.That(ex.Message, Is.EqualTo("Reservation not found or not completed."));
            _tokenServiceMock.Verify(t => t.ValidateAnonymousFeedbackToken(token, out It.Ref<string>.IsAny), Times.Once);
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(reservationId), Times.Once);
        }

        [Test]
        public async Task SubmitAnonymousFeedback_ValidRequest_CallsAddAnonymousFeedback()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                ServiceRating = "5",
                CuisineRating = "4",
                ServiceComment = "Great service!",
                CuisineComment = "Food was excellent!"
            };
            
            var reservation = new Reservation
            {
                Id = "res-123",
                Status = ReservationStatus.Finished.ToString(),
                LocationId = "loc-1",
                Date = "2023-05-01",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                TableId = "table-1",
                LocationAddress = "123 Main St",
                UserEmail = "user@example.com",
                GuestsNumber = "4",
                PreOrder = "Not implemented",
                TableCapacity = "6",
                TableNumber = "T5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);

            _feedbackServiceMock.Setup(f => f.AddAnonymousFeedbackAsync(It.IsAny<CreateFeedbackRequest>(), It.IsAny<Reservation>()))
                .Returns(Task.CompletedTask);

            // Act
            await _anonymousFeedbackService.SubmitAnonymousFeedback(request);

            // Assert
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(request.ReservationId), Times.Once);
            _feedbackServiceMock.Verify(f => f.AddAnonymousFeedbackAsync(
                It.Is<CreateFeedbackRequest>(req => 
                    req.ReservationId == request.ReservationId && 
                    req.ServiceRating == request.ServiceRating && 
                    req.CuisineRating == request.CuisineRating &&
                    req.ServiceComment == request.ServiceComment &&
                    req.CuisineComment == request.CuisineComment), 
                It.Is<Reservation>(res => res.Id == reservation.Id)), 
                Times.Once);
        }

        [Test]
        public void SubmitAnonymousFeedback_ReservationNotFound_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                ServiceRating = "5",
                CuisineRating = "4"
            };

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync((Reservation)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _anonymousFeedbackService.SubmitAnonymousFeedback(request));

            Assert.That(ex.Message, Is.EqualTo("Reservation not found or not completed."));
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(request.ReservationId), Times.Once);
            _feedbackServiceMock.Verify(f => f.AddAnonymousFeedbackAsync(
                It.IsAny<CreateFeedbackRequest>(), It.IsAny<Reservation>()), Times.Never);
        }

        [Test]
        public void SubmitAnonymousFeedback_ReservationNotFinished_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                ServiceRating = "5",
                CuisineRating = "4"
            };
            
            var reservation = new Reservation
            {
                Id = "res-123",
                Status = ReservationStatus.InProgress.ToString(), // Not finished
                LocationId = "loc-1",
                Date = "2023-05-01",
                GuestsNumber = "4",
                LocationAddress = "123 Main St",
                PreOrder = "Not implemented",
                TableId = "table-1",
                TableCapacity = "6",
                TableNumber = "5",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(
                () => _anonymousFeedbackService.SubmitAnonymousFeedback(request));

            Assert.That(ex.Message, Is.EqualTo("Reservation not found or not completed."));
            _reservationRepositoryMock.Verify(r => r.GetReservationByIdAsync(request.ReservationId), Times.Once);
            _feedbackServiceMock.Verify(f => f.AddAnonymousFeedbackAsync(
                It.IsAny<CreateFeedbackRequest>(), It.IsAny<Reservation>()), Times.Never);
        }

        [Test]
        public async Task SubmitAnonymousFeedback_PreservesAllRequestProperties()
        {
            // Arrange
            var request = new CreateFeedbackRequest
            {
                ReservationId = "res-123",
                ServiceRating = "5",
                CuisineRating = "4",
                ServiceComment = "Excellent service",
                CuisineComment = "Delicious food"
            };
            
            var reservation = new Reservation
            {
                Id = "res-123",
                Status = ReservationStatus.Finished.ToString(),
                LocationId = "loc-1",
                Date = "2023-05-01",
                TimeFrom = "13:30",
                TimeTo = "15:00",
                GuestsNumber = "4",
                LocationAddress = "123 Main St",
                PreOrder = "Not Implemented",
                TableId = "table-1",
                TableCapacity = "6",
                TableNumber = "5",
                TimeSlot = "13:30 - 15:00",
                CreatedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            CreateFeedbackRequest capturedRequest = null;

            _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);

            _feedbackServiceMock.Setup(f => f.AddAnonymousFeedbackAsync(It.IsAny<CreateFeedbackRequest>(), It.IsAny<Reservation>()))
                .Callback<CreateFeedbackRequest, Reservation>((req, _) => capturedRequest = req)
                .Returns(Task.CompletedTask);

            // Act
            await _anonymousFeedbackService.SubmitAnonymousFeedback(request);

            // Assert
            Assert.That(capturedRequest, Is.Not.Null);
            Assert.That(capturedRequest.ReservationId, Is.EqualTo(request.ReservationId));
            Assert.That(capturedRequest.ServiceRating, Is.EqualTo(request.ServiceRating));
            Assert.That(capturedRequest.CuisineRating, Is.EqualTo(request.CuisineRating));
            Assert.That(capturedRequest.ServiceComment, Is.EqualTo(request.ServiceComment));
            Assert.That(capturedRequest.CuisineComment, Is.EqualTo(request.CuisineComment));
        }
    }
}