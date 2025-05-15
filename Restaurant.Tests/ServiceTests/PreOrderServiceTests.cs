using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Profiles;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using System.Globalization;
using Restaurant.Application.DTOs.PerOrders.Request;
using Restaurant.Application.Exceptions;

namespace Restaurant.Tests.ServiceTests
{
    public class PreOrderServiceTests
    {
        private Mock<IPreOrderRepository> _preOrderRepositoryMock = null!;
        private IMapper _mapper = null!;
        private IPreOrderService _preOrderService = null!;
        private List<PreOrder> _preOrders = null!;
        private Mock<IReservationRepository> _reservationRepositoryMock = null!;
        private Mock<IDishRepository> _dishRepositoryMock = null!;
        private Mock<IEmailService> _emailServiceMock = null!;
        private readonly string _userId = "user-123";

        [SetUp]
        public void SetUp()
        {
            // Configure repository mocks
            _preOrderRepositoryMock = new Mock<IPreOrderRepository>();
            _reservationRepositoryMock = new Mock<IReservationRepository>();
            _dishRepositoryMock = new Mock<IDishRepository>();
            _emailServiceMock = new Mock<IEmailService>();

            // Configure AutoMapper with actual profiles
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new CartProfile());
            });

            _mapper = mapperConfig.CreateMapper();

            // Initialize the service
            _preOrderService = new PreOrderService(
                _preOrderRepositoryMock.Object,
                _mapper,
                _reservationRepositoryMock.Object,
                _dishRepositoryMock.Object,
                _emailServiceMock.Object);

            // Create test data
            _preOrders = new List<PreOrder>
            {
                new()
                {
                    UserId = _userId,
                    Id = "order-1",
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    ReservationDate = "2023-05-15",
                    Items = new List<PreOrderItem>
                    {
                        new()
                        {
                            DishId = "dish-1",
                            DishName = "Pasta Carbonara",
                            Quantity = 2,
                            Price = 15.99m,
                            DishImageUrl = "https://example.com/images/pasta.jpg",
                            Id = Guid.NewGuid().ToString()
                        },
                        new()
                        {
                            DishId = "dish-2",
                            DishName = "Caesar Salad",
                            Quantity = 1,
                            Price = 14.01m,
                            DishImageUrl = "https://example.com/images/salad.jpg",
                            Id = Guid.NewGuid().ToString()
                        }
                    }
                },
                new()
                {
                    UserId = _userId,
                    Id = "order-2",
                    ReservationId = "res-2",
                    Status = "PENDING",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    TotalPrice = 32.50m,
                    Address = "456 Oak Ave",
                    TimeSlot = "18:30 - 19:30",
                    ReservationDate = "2023-05-16",
                    Items = new List<PreOrderItem>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            DishId = "dish-3",
                            DishName = "Margherita Pizza",
                            Quantity = 1,
                            Price = 18.50m,
                            DishImageUrl = "https://example.com/images/pizza.jpg",
                        },
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            DishId = "dish-4",
                            DishName = "Tiramisu",
                            Quantity = 2,
                            Price = 7.00m,
                            DishImageUrl = "https://example.com/images/tiramisu.jpg"
                        }
                    }
                }
            };
        }

        [Test]
        public async Task GetUserCart_WithValidUserId_ReturnsCartWithPreOrders()
        {
            // Arrange
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId,
                    false))!
                .ReturnsAsync((List<PreOrder>?)_preOrders); // Explicitly cast to nullable type

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(2));

            // Verify first pre-order
            var firstPreOrder = result.Content[0];
            Assert.That(firstPreOrder.Id, Is.EqualTo("order-1"));
            Assert.That(firstPreOrder.ReservationId, Is.EqualTo("res-1"));
            Assert.That(firstPreOrder.Status, Is.EqualTo("SUBMITTED"));
            Assert.That(firstPreOrder.Address, Is.EqualTo("123 Main St"));
            Assert.That(firstPreOrder.TimeSlot, Is.EqualTo("12:00 - 13:00"));
            Assert.That(firstPreOrder.ReservationDate, Is.EqualTo("2023-05-15"));
            Assert.That(firstPreOrder.TotalPrice, Is.EqualTo(45.99m));

            // Verify first pre-order items
            Assert.That(firstPreOrder.DishItems, Has.Count.EqualTo(2));
            Assert.That(firstPreOrder.DishItems[0].DishId, Is.EqualTo("dish-1"));
            Assert.That(firstPreOrder.DishItems[0].DishName, Is.EqualTo("Pasta Carbonara"));
            Assert.That(firstPreOrder.DishItems[0].DishQuantity, Is.EqualTo(2));
            Assert.That(firstPreOrder.DishItems[0].DishPrice, Is.EqualTo(15.99m));
            Assert.That(firstPreOrder.DishItems[0].DishImageUrl, Is.EqualTo("https://example.com/images/pasta.jpg"));

            // Verify second pre-order
            var secondPreOrder = result.Content[1];
            Assert.That(secondPreOrder.Id, Is.EqualTo("order-2"));
            Assert.That(secondPreOrder.Status, Is.EqualTo("PENDING"));

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithEmptyCart_ReturnsEmptyCartIndicator()
        {
            // Arrange
            var emptyList = new List<PreOrder>();
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.Content, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithNullResultFromRepository_ReturnsEmptyCartIndicator()
        {
            // Arrange
            List<PreOrder>? nullList = null;
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId,
                    false))!
                .ReturnsAsync(nullList);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.Content, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithPreOrdersWithoutItems_MapsDataCorrectly()
        {
            // Arrange
            var preOrdersWithoutItems = new List<PreOrder>
            {
                new()
                {
                    UserId = _userId,
                    ReservationId = "res-3",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z",
                        CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-17",
                    TotalPrice = 0m,
                    Address = "789 Pine St",
                    TimeSlot = "19:00 - 20:00",
                    Items = new List<PreOrderItem>(),
                    Id = "order-3"
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ReturnsAsync(preOrdersWithoutItems);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("order-3"));
            Assert.That(result.Content[0].DishItems, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithPreOrdersWithNullFields_HandlesNullsGracefully()
        {
            // Arrange
            var preOrderWithNulls = new List<PreOrder>
            {
                new()
                {
                    UserId = _userId,
                    Id = "order-4",
                    ReservationId = string.Empty,
                    Status = string.Empty,
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-18",
                    TotalPrice = 25.99m,
                    Address = string.Empty,
                    TimeSlot = string.Empty,
                    Items = new List<PreOrderItem>
                    {
                        new()
                        {
                            Id = Guid.NewGuid().ToString(),
                            DishId = string.Empty,
                            DishName = string.Empty,
                            Quantity = 1,
                            Price = 25.99m,
                            DishImageUrl = string.Empty,
                            Notes = string.Empty
                        }
                    }
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ReturnsAsync(preOrderWithNulls);

            // Act - This should not throw exceptions
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("order-4"));
            Assert.That(result.Content[0].ReservationId, Is.Empty);
            Assert.That(result.Content[0].Status, Is.Empty);
            Assert.That(result.Content[0].Address, Is.Empty);
            Assert.That(result.Content[0].TimeSlot, Is.Empty);

            Assert.That(result.Content[0].DishItems, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].DishItems[0].DishId, Is.Empty);
            Assert.That(result.Content[0].DishItems[0].DishName, Is.Empty);
            Assert.That(result.Content[0].DishItems[0].DishImageUrl, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }

        [Test]
        public Task GetUserCart_WithRepositoryException_PropagatesException()
        {
            // Arrange
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.GetUserCart(_userId));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Is.EqualTo("Database connection failed"));

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
            return Task.CompletedTask;
        }

        [Test]
        public void GetUserCart_WithNullUserId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(async () =>
                await _preOrderService.GetUserCart(null!));

            // Verify repository was not called
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                It.IsAny<string>(), false), Times.Never);
        }

        [Test]
        public void GetUserCart_WithEmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<BadRequestException>(async () =>
                await _preOrderService.GetUserCart(string.Empty));

            // Verify repository was not called
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                It.IsAny<string>(), false), Times.Never);
        }

        [Test]
        public async Task GetUserCart_WithSinglePreOrder_ReturnsCorrectCart()
        {
            // Arrange
            var singlePreOrder = new List<PreOrder> { _preOrders[0] };
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ReturnsAsync(singlePreOrder);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("order-1"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_VerifyPreOrderIdExtraction_ExtractsCorrectly()
        {
            // Arrange
            var preOrderWithCustomSortKey = new List<PreOrder>
            {
                new()
                {
                    UserId = _userId,
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z",
                        CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-15",
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>(),
                    Id = "complex-id-123-456"
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ReturnsAsync(preOrderWithCustomSortKey);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("complex-id-123-456"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithMalformedSortKey_HandlesGracefully()
        {
            // Arrange
            var preOrderWithBadSortKey = new List<PreOrder>
            {
                new()
                {
                    UserId = _userId,
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z",
                        CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-15",
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>(),
                    Id = "InvalidPrefix-order-1",
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(
                    _userId, false))
                .ReturnsAsync(preOrderWithBadSortKey);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            // The ID should be the entire key since the prefix doesn't match
            Assert.That(result.Content[0].Id, Is.EqualTo("InvalidPrefix-order-1"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                _userId, false), Times.Once);
        }
        
        #region UpsertPreOrder Tests
        
         [Test] 
         public async Task UpsertPreOrder_WhenCreatingNewPreOrder_ShouldCreatePreOrderAndReturnUpdatedCart() 
         {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 },
                    new() { DishId = "dish2", DishQuantity = 1 }
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "0",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var dishes = new List<Dish>
            {
                new() { Id = "dish1", Name = "Pizza", Price = 10.99M, Weight = "500g", ImageUrl = "pizza.jpg" },
                new() { Id = "dish2", Name = "Pasta", Price = 8.50M, Weight = "400g", ImageUrl = "pasta.jpg" }
            };

            var createdPreOrder = new PreOrder
            {
                Id = "newpreorder123",
                UserId = _userId,
                ReservationId = "reservation123",
                Status = "Submitted",
                TimeSlot = reservation.TimeSlot,
                Address = reservation.LocationAddress,
                ReservationDate = reservation.Date,
                CreateDate = DateTime.UtcNow,
                TotalPrice = 30.48M,
                Items = new List<PreOrderItem>
                {
                    new() { Id = "item1", DishId = "dish1", DishName = "Pizza", Quantity = 2, Price = 10.99M, DishStatus = "New", DishImageUrl = "pizza.jpg" },
                    new() { Id = "item2", DishId = "dish2", DishName = "Pasta", Quantity = 1, Price = 8.50M, DishStatus = "New", DishImageUrl = "pasta.jpg" }
                }
            };

            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByReservationIdAsync(request.ReservationId))
                .ReturnsAsync((PreOrder)null);
            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);
            _dishRepositoryMock.Setup(x => x.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(dishes);
            
            // Use ReturnsAsync with a function to capture and use the PreOrder that is created
            PreOrder capturedPreOrder = null;
            _preOrderRepositoryMock.Setup(x => x.CreatePreOrderAsync(It.IsAny<PreOrder>()))
                .Callback<PreOrder>(po => capturedPreOrder = po)
                .Returns(Task.FromResult<PreOrder>(null))  // Changed to use Returns with Task.FromResult
                .Callback<PreOrder>(po => capturedPreOrder = po);
                
            // Setup GetPreOrderOnlyByIdAsync to return the captured PreOrder for any ID
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderOnlyByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => capturedPreOrder);
                
            // Setup GetPreOrderByIdAsync to return the captured PreOrder
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByIdAsync(_userId, It.IsAny<string>()))
                .ReturnsAsync((string userId, string id) => capturedPreOrder);
                
            // Setup GetPreOrdersAsync to return a list containing the captured PreOrder
            _preOrderRepositoryMock.Setup(x => x.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(() => new List<PreOrder> { capturedPreOrder });

            _reservationRepositoryMock.Setup(x => x.UpsertReservationAsync(It.IsAny<Reservation>()))
                .ReturnsAsync((Reservation r) => r);

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            _preOrderRepositoryMock.Verify(x => x.CreatePreOrderAsync(It.IsAny<PreOrder>()), Times.Once);
            _preOrderRepositoryMock.Verify(x => x.UpdatePreOrderAsync(It.IsAny<PreOrder>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendPreOrderConfirmationEmailAsync(_userId, It.IsAny<PreOrder>()), Times.Once);
        }

        [Test]
        public async Task UpsertPreOrder_WhenUpdatingExistingPreOrder_ShouldUpdatePreOrderAndReturnUpdatedCart()
        {
            // Arrange
            var preOrderId = "preorder123";
            var request = new UpsertPreOrderRequest
            {
                Id = preOrderId,
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 3 }
                }
            };

            var existingPreOrder = new PreOrder
            {
                Id = preOrderId,
                UserId = _userId,
                ReservationId = "reservation123",
                Status = "Submitted",
                TimeSlot = "18:00 - 20:00",
                ReservationDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                CreateDate = DateTime.UtcNow.AddHours(-1),
                Address = "123 Main St",
                TotalPrice = 20.99M,
                Items = new List<PreOrderItem>
                {
                    new() { Id = "item1", DishId = "dish1", DishName = "Pizza", Quantity = 2, Price = 10.99M, DishStatus = "New", DishImageUrl = "pizza.jpg" }
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "2",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var dishes = new List<Dish>
            {
                new() { Id = "dish1", Name = "Pizza", Price = 10.99M, Weight = "500g", ImageUrl = "pizza.jpg" }
            };

            // Create an updated version of the PreOrder that will be returned by the repository after update
            var updatedPreOrder = new PreOrder
            {
                Id = preOrderId,
                UserId = _userId,
                ReservationId = "reservation123",
                Status = "Submitted",
                TimeSlot = "18:00 - 20:00",
                ReservationDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                CreateDate = existingPreOrder.CreateDate,
                Address = "123 Main St",
                TotalPrice = 32.97M, // 3 * 10.99
                Items = new List<PreOrderItem>
                {
                    new() { Id = "item1", DishId = "dish1", DishName = "Pizza", Quantity = 3, Price = 10.99M, DishStatus = "New", DishImageUrl = "pizza.jpg" }
                }
            };

            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByIdAsync(_userId, preOrderId))
                .ReturnsAsync(existingPreOrder);
            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);
            _dishRepositoryMock.Setup(x => x.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(dishes);
            
            // Setup to return the updated PreOrder after updating
            PreOrder capturedPreOrder = null;
            _preOrderRepositoryMock.Setup(x => x.UpdatePreOrderAsync(It.IsAny<PreOrder>()))
                .Callback<PreOrder>(po => capturedPreOrder = po)
                .Returns(Task.FromResult<PreOrder>(null))  // Changed to use Returns with Task.FromResult
                .Callback<PreOrder>(po => capturedPreOrder = po);
            
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderOnlyByIdAsync(preOrderId))
                .ReturnsAsync(updatedPreOrder);
            
            // Return a non-empty list containing the updated PreOrder
            _preOrderRepositoryMock.Setup(x => x.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { updatedPreOrder });
            
            _reservationRepositoryMock.Setup(x => x.UpsertReservationAsync(It.IsAny<Reservation>()))
                .ReturnsAsync((Reservation r) => r);

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            _preOrderRepositoryMock.Verify(x => x.UpdatePreOrderAsync(It.IsAny<PreOrder>()), Times.Once);
            _preOrderRepositoryMock.Verify(x => x.CreatePreOrderAsync(It.IsAny<PreOrder>()), Times.Never);
            _emailServiceMock.Verify(x => x.SendPreOrderConfirmationEmailAsync(_userId, It.IsAny<PreOrder>()), Times.Once);
            
            // Additional assertions to verify the content of the returned cart
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo(preOrderId));
            Assert.That(result.Content[0].TotalPrice, Is.EqualTo(32.97M));
        }

        [Test]
        public void UpsertPreOrder_WithInvalidReservationId_ShouldThrowNotFoundException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = null,
                ReservationId = "invalid_reservation",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 }
                }
            };

            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync((Reservation)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(() => _preOrderService.UpsertPreOrder(_userId, request));
            Assert.That(exception.Message, Contains.Substring($"Reservation with ID {request.ReservationId} does not exist"));
        }

        [Test]
        public void UpsertPreOrder_WithExistingPreOrderForReservation_ShouldThrowConflictException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = null, // New order
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 }
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "0",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var existingPreOrder = new PreOrder
            {
                Id = "preorder123",
                UserId = _userId,
                ReservationId = "reservation123",
                Status = "Submitted",
                TimeSlot = "18:00 - 20:00",
                ReservationDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                CreateDate = DateTime.UtcNow,
                Address = "123 Main St",
                TotalPrice = 20.99M,
                Items = new List<PreOrderItem>()
            };

            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByReservationIdAsync(request.ReservationId))
                .ReturnsAsync(existingPreOrder);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ConflictException>(() => _preOrderService.UpsertPreOrder(_userId, request));
            Assert.That(exception.Message, Contains.Substring($"A preorder with ID {existingPreOrder.Id} already exists for reservation ID {request.ReservationId}"));
        }

        [Test]
        public void UpsertPreOrder_WithInvalidDishId_ShouldThrowNotFoundException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = null,
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 },
                    new() { DishId = "invalid_dish", DishQuantity = 1 }
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "0",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var dishes = new List<Dish>
            {
                new() { Id = "dish1", Name = "Pizza", Price = 10.99M, Weight = "500g", ImageUrl = "pizza.jpg" }
            };

            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByReservationIdAsync(request.ReservationId))
                .ReturnsAsync((PreOrder)null);
            _dishRepositoryMock.Setup(x => x.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(dishes);

            // Act & Assert
            var exception = Assert.ThrowsAsync<NotFoundException>(() => _preOrderService.UpsertPreOrder(_userId, request));
            Assert.That(exception.Message, Contains.Substring("The following dish IDs do not exist: invalid_dish"));
        }

        [Test]
        public void UpsertPreOrder_WithInvalidDishQuantity_ShouldThrowBadRequestException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = null,
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 },
                    new() { DishId = "dish2", DishQuantity = 0 } // Invalid quantity
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "0",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var dishes = new List<Dish>
            {
                new() { Id = "dish1", Name = "Pizza", Price = 10.99M, Weight = "500g", ImageUrl = "pizza.jpg" },
                new() { Id = "dish2", Name = "Pasta", Price = 8.50M, Weight = "400g", ImageUrl = "pasta.jpg" }
            };

            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);
            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByReservationIdAsync(request.ReservationId))
                .ReturnsAsync((PreOrder)null);
            _dishRepositoryMock.Setup(x => x.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(dishes);

            // Act & Assert
            var exception = Assert.ThrowsAsync<BadRequestException>(() => _preOrderService.UpsertPreOrder(_userId, request));
            Assert.That(exception.Message, Contains.Substring("Dish quantities must be greater than zero"));
        }

        [Test]
        public void UpsertPreOrder_WithinCutoffTime_ShouldThrowBadRequestException()
        {
            // Arrange
            string preOrderId = "preorder123";
            var request = new UpsertPreOrderRequest
            {
                Id = preOrderId,
                ReservationId = "reservation123",
                Status = "Submitted",
                DishItems = new List<DishItemRequest>
                {
                    new() { DishId = "dish1", DishQuantity = 2 }
                }
            };

            var reservation = new Reservation
            {
                Id = "reservation123",
                TimeSlot = "18:00 - 20:00",
                LocationAddress = "123 Main St",
                Date = DateTime.Now.ToString("yyyy-MM-dd"), // Today
                GuestsNumber = "2",
                LocationId = "location1",
                PreOrder = "0",
                Status = "Active",
                TableId = "table1",
                TableCapacity = "4",
                TableNumber = "5",
                TimeFrom = "18:00",
                TimeTo = "20:00",
                CreatedAt = DateTime.Now.ToString()
            };

            var existingPreOrder = new PreOrder
            {
                Id = preOrderId,
                UserId = _userId,
                ReservationId = "reservation123",
                Status = "Submitted",
                TimeSlot = $"{DateTime.Now.AddMinutes(20):HH:mm} - 20:00", // Within 30 min cutoff
                ReservationDate = DateTime.Now.ToString("yyyy-MM-dd"),
                CreateDate = DateTime.UtcNow.AddHours(-1),
                Address = "123 Main St",
                TotalPrice = 20.99M,
                Items = new List<PreOrderItem>()
            };

            _preOrderRepositoryMock.Setup(x => x.GetPreOrderByIdAsync(_userId, preOrderId))
                .ReturnsAsync(existingPreOrder);
            _reservationRepositoryMock.Setup(x => x.GetReservationByIdAsync(request.ReservationId))
                .ReturnsAsync(reservation);

            // Act & Assert
            var exception = Assert.ThrowsAsync<BadRequestException>(() => _preOrderService.UpsertPreOrder(_userId, request));
            Assert.That(exception.Message, Contains.Substring("PreOrder can only be modified before 30 minutes of start time"));
        }

        #endregion
    }
}

