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
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-1",
                    PreOrderId = "order-1",
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    ReservationDate = "2023-05-15",
                    Items = new List<PreOrderItem>
                    {
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-1#Item#1",
                            DishId = "dish-1",
                            DishName = "Pasta Carbonara",
                            Quantity = 2,
                            Price = 15.99m,
                            DishImageUrl = "https://example.com/images/pasta.jpg"
                        },
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-1#Item#2",
                            DishId = "dish-2",
                            DishName = "Caesar Salad",
                            Quantity = 1,
                            Price = 14.01m,
                            DishImageUrl = "https://example.com/images/salad.jpg"
                        }
                    }
                },
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-2",
                    PreOrderId = "order-2",
                    ReservationId = "res-2",
                    Status = "PENDING",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    TotalPrice = 32.50m,
                    Address = "456 Oak Ave",
                    TimeSlot = "18:30 - 19:30",
                    ReservationDate = "2023-05-16",
                    Items = new List<PreOrderItem>
                    {
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-2#Item#1",
                            DishId = "dish-3",
                            DishName = "Margherita Pizza",
                            Quantity = 1,
                            Price = 18.50m,
                            DishImageUrl = "https://example.com/images/pizza.jpg"
                        },
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-2#Item#2",
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
            Assert.That(firstPreOrder.DishItems[0].DishPrice, Is.EqualTo("15.99"));
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
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-3",
                    ReservationId = "res-3",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-17",
                    TotalPrice = 0m,
                    Address = "789 Pine St",
                    TimeSlot = "19:00 - 20:00",
                    Items = new List<PreOrderItem>()  // Empty items list
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
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-4",
                    ReservationId = string.Empty,
                    Status = string.Empty,
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-18",
                    TotalPrice = 25.99m,
                    Address = string.Empty,
                    TimeSlot = string.Empty,
                    Items = new List<PreOrderItem>
                    {
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-4#Item#1",
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
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.GetUserCart(null!));

            // Verify repository was not called
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(
                It.IsAny<string>(), false), Times.Never);
        }

        [Test]
        public void GetUserCart_WithEmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
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
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#complex-id-123-456", // Custom complex ID
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-15",
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>()
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
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "InvalidPrefix-order-1", // Missing PreOrder# prefix
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                    ReservationDate = "2023-05-15",
                    TotalPrice = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>()
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
        public void UpsertPreOrder_WithNullUserId_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest()
            {
                ReservationId = "res-1",
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.UpsertPreOrder(null!, request));

            Assert.That(exception!.Message, Does.Contain("User ID cannot be null or empty"));
        }

        [Test]
        public void UpsertPreOrder_WithEmptyUserId_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.UpsertPreOrder(string.Empty, request));

            Assert.That(exception!.Message, Does.Contain("User ID cannot be null or empty"));
        }

        [Test]
        public void UpsertPreOrder_WithNonExistentPreOrderId_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = "non-existent-id",
                ReservationId = "res-1",
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrderByIdAsync(_userId, "non-existent-id"))
                .ReturnsAsync((PreOrder?)null);
            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);
            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new Dish
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "10.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Pre-order with ID non-existent-id does not exist"));
        }

        [Test]
        public void UpsertPreOrder_WithNullReservationId_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = null!,
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Reservation ID cannot be null or empty"));
        }

        [Test]
        public void UpsertPreOrder_WithEmptyDishItems_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                DishItems = new List<DishItemDto>()
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Order must contain at least one dish item"));
        }

        [Test]
        public void UpsertPreOrder_WithNullDishItems_ThrowsArgumentException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                DishItems = null!
            };

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Order must contain at least one dish item"));
        }

        [Test]
        public void UpsertPreOrder_WithExistingPreOrderWithinCutoffTime_ThrowsInvalidOperationException()
        {
            // Arrange
            var futureTime = DateTime.Now.AddMinutes(20).ToString("HH:mm"); // Within 30 min cutoff
            var today = DateTime.Now.ToString("yyyy-MM-dd");

            var existingPreOrder = new PreOrder
            {
                UserId = _userId,
                PreOrderId = "existing-id",
                SortKey = "PreOrder#existing-id",
                TimeSlot = futureTime,
                ReservationDate = today,
                ReservationId = "res-1",
                Status = "Draft",
                Address = "123 Main St",
            };

            var request = new UpsertPreOrderRequest
            {
                Id = "existing-id",
                ReservationId = "res-1",
                TimeSlot = futureTime,
                ReservationDate = today,
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrderByIdAsync(_userId, "existing-id"))
                .ReturnsAsync(existingPreOrder);
            
            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new Dish
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "10.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });
            
            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Cannot modify pre-order within 30 minutes of reservation time"));
        }

        [Test]
        public void UpsertPreOrder_WithNonExistentReservation_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "non-existent-res",
                DishItems = new List<DishItemDto> { new()
                    {
                        DishId = "dish-1",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    }
                }
            };

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("non-existent-res"))
                .ReturnsAsync(false);

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("Reservation with ID non-existent-res does not exist"));
        }

        [Test]
        public void UpsertPreOrder_WithNonExistentDishId_ThrowsInvalidOperationException()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                DishItems = new List<DishItemDto>
                {
                    new()
                    {
                        DishId = "valid-dish",
                        DishQuantity = 1,
                        DishPrice = "$10.99",
                        DishImageUrl = "image1.jpg",
                        DishName = "Pasta"
                    },
                    new()
                    {
                        DishId = "invalid-dish",
                        DishQuantity = 2,
                        DishPrice = "$12.99",
                        DishImageUrl = "image2.jpg",
                        DishName = "Salad"
                    }
                }
            };

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                    {
                        Id = "valid-dish",
                        Name = "Valid Dish",
                        Price = "10.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.UpsertPreOrder(_userId, request));

            Assert.That(exception!.Message, Does.Contain("The following dish IDs do not exist: invalid-dish"));
        }

        [Test]
        public async Task UpsertPreOrder_CreateNewPreOrder_CreatesAndReturnsCart()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                Status = "Draft",
                TimeSlot = "14:00 - 15:00",
                Address = "123 Main St",
                ReservationDate = "2023-10-15",
                DishItems = new List<DishItemDto>
                {
                    new() { DishId = "dish-1", DishName = "Pasta", DishQuantity = 2, DishPrice = "$12.99", DishImageUrl = "image1.jpg" }
                }
            };

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "12.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            // Capture the created preOrder
            _preOrderRepositoryMock.Setup(repo => repo.CreatePreOrderAsync(It.IsAny<PreOrder>()))
                .Callback<PreOrder>(po => _ = po)
                .ReturnsAsync((PreOrder)null!);

            // Setup for GetUserCart
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { new()
                    {
                        PreOrderId = "new-order",
                        UserId = _userId,
                        SortKey = "PreOrder#new-order",
                        ReservationId = "res-1",
                        Status = "Draft",
                        TimeSlot = "14:00 - 15:00",
                        ReservationDate = "2023-10-15",
                        Address = "123 Main St",
                    }
                });

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);

            // Verify repository methods were called with correct parameters
            _preOrderRepositoryMock.Verify(repo => repo.CreatePreOrderAsync(It.Is<PreOrder>(p => 
                p.UserId == _userId && 
                p.ReservationId == "res-1" && 
                p.Status == "Draft" && 
                p.TimeSlot == "14:00 - 15:00" &&
                p.Items.Count == 1
            )), Times.Once);
            
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId, false), Times.Once);
        }

        [Test]
        public async Task UpsertPreOrder_UpdateExistingPreOrder_UpdatesAndReturnsCart()
        {
            // Arrange
            var existingId = "existing-id";
            var existingPreOrder = new PreOrder
            {
                UserId = _userId,
                PreOrderId = existingId,
                SortKey = "PreOrder#existing-id",
                ReservationId = "res-old",
                Status = "Draft",
                TimeSlot = "16:00 - 17:00",
                ReservationDate = "9999-12-15", // Far in future to avoid cutoff time issue
                CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                Items = new List<PreOrderItem>(),
                Address = "123 Old St"
            };

            var request = new UpsertPreOrderRequest
            {
                Id = existingId,
                ReservationId = "res-1",
                Status = "Draft",
                TimeSlot = "14:00 - 15:00",
                Address = "123 Main St",
                ReservationDate = "9999-12-15",
                DishItems = new List<DishItemDto>
                {
                    new() { DishId = "dish-1", DishName = "Pasta", DishQuantity = 2, DishPrice = "$12.99", DishImageUrl = "image1.jpg" }
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrderByIdAsync(_userId, existingId))
                .ReturnsAsync(existingPreOrder);

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "12.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            // Capture the updated preOrder
            _preOrderRepositoryMock.Setup(repo => repo.UpdatePreOrderAsync(It.IsAny<PreOrder>()))
                .Callback<PreOrder>(po => _ = po)
                .Returns(Task.CompletedTask);

            // Setup for GetUserCart to return the updated order
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { existingPreOrder });

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);

            // Verify repository methods were called
            _preOrderRepositoryMock.Verify(repo => repo.UpdatePreOrderAsync(It.Is<PreOrder>(p => 
                p.UserId == _userId && 
                p.PreOrderId == existingId && 
                p.ReservationId == "res-1" &&
                p.TimeSlot == "14:00 - 15:00" &&
                p.Items.Count == 1
            )), Times.Once);
            
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId, false), Times.Once);
        }

        [Test]
        public async Task UpsertPreOrder_WithStatusSubmitted_SendsConfirmationEmail()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                Id = "existing-id",
                ReservationId = "res-1",
                Status = "Submitted", // This should trigger email sending
                TimeSlot = "14:00 - 15:00",
                Address = "123 Main St",
                ReservationDate = "9999-12-15",
                DishItems = new List<DishItemDto>
                {
                    new() { DishId = "dish-1", DishName = "Pasta", DishQuantity = 2, DishPrice = "$12.99", DishImageUrl = "image1.jpg" }
                }
            };

            var existingPreOrder = new PreOrder
            {
                UserId = _userId,
                PreOrderId = "existing-id",
                SortKey = "PreOrder#existing-id",
                ReservationId = "res-1",
                Status = "Draft",
                TimeSlot = "14:00 - 15:00",
                ReservationDate = "9999-12-15",
                CreateDate = DateTime.Parse("2023-05-15T12:00:00Z", CultureInfo.InvariantCulture),
                Items = new List<PreOrderItem>(),
                Address = "123 Main St"
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrderByIdAsync(_userId, "existing-id"))
                .ReturnsAsync(existingPreOrder);

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "12.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            _preOrderRepositoryMock.Setup(repo => repo.UpdatePreOrderAsync(It.IsAny<PreOrder>()))
                .Returns(Task.CompletedTask);

            // Setup for email sending verification
            _emailServiceMock.Setup(service => service.SendPreOrderConfirmationEmailAsync(_userId, It.IsAny<PreOrder>()))
                .Returns(Task.CompletedTask);

            // Setup for GetUserCart
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { existingPreOrder });

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);

            // Verify email was sent
            _emailServiceMock.Verify(service => service.SendPreOrderConfirmationEmailAsync(_userId, It.IsAny<PreOrder>()), Times.Once);
        }

        [Test]
        public async Task UpsertPreOrder_WithStatusDraft_DoesNotSendEmail()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                Status = "Draft", // This should NOT trigger email sending
                TimeSlot = "14:00 - 15:00",
                Address = "123 Main St",
                ReservationDate = "2023-12-15",
                DishItems = new List<DishItemDto>
                {
                    new() { DishId = "dish-1", DishName = "Pasta", DishQuantity = 2, DishPrice = "$12.99", DishImageUrl = "image1.jpg" }
                }
            };

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                    {
                        Id = "dish-1",
                        Name = "Pasta",
                        Price = "12.99",
                        Weight = "1kg",
                        ImageUrl = "image1.jpg",
                    }
                });

            _preOrderRepositoryMock.Setup(repo => repo.CreatePreOrderAsync(It.IsAny<PreOrder>()))
                .ReturnsAsync((PreOrder)null!);

            // Setup for GetUserCart
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { new()
                    {
                        PreOrderId = "new-order",
                        UserId = _userId,
                        SortKey = "PreOrder#new-order",
                        ReservationId = "res-1",
                        Status = "Draft",
                        TimeSlot = "14:00 - 15:00",
                        ReservationDate = "2023-12-15",
                        Address = "123 Main St",
                    }
                });

            // Act
            var result = await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);

            // Verify email was NOT sent
            _emailServiceMock.Verify(service => service.SendPreOrderConfirmationEmailAsync(It.IsAny<string>(), It.IsAny<PreOrder>()), Times.Never);
        }

        [Test]
        public async Task UpsertPreOrder_CalculatesTotalPriceCorrectly()
        {
            // Arrange
            var request = new UpsertPreOrderRequest
            {
                ReservationId = "res-1",
                Status = "Draft",
                TimeSlot = "14:00 - 15:00",
                Address = "123 Main St",
                ReservationDate = "2023-12-15",
                DishItems = new List<DishItemDto>
                {
                    new() { DishId = "dish-1", DishName = "Pasta", DishQuantity = 2, DishPrice = "$12.99", DishImageUrl = "image1.jpg" },
                    new() { DishId = "dish-2", DishName = "Salad", DishQuantity = 1, DishPrice = "$8.50", DishImageUrl = "image2.jpg" }
                }
            };

            _reservationRepositoryMock.Setup(repo => repo.ReservationExistsAsync("res-1"))
                .ReturnsAsync(true);

            _dishRepositoryMock.Setup(repo => repo.GetDishesByIdsAsync(It.IsAny<List<string>>()))
                .ReturnsAsync(new List<Dish> { new()
                {
                    Id = "dish-1",
                    Name = "Pasta",
                    Price = "12.99",
                    Weight = "1kg",
                    ImageUrl = "image1.jpg",
                }, new()
                    {
                        Id = "dish-2",
                        Name = "Salad",
                        Price = "8.50",
                        Weight = "500g",
                        ImageUrl = "image2.jpg",
                    }
                });

            // Capture the created preOrder to verify total price
            PreOrder? capturedPreOrder = null;
            _preOrderRepositoryMock.Setup(repo => repo.CreatePreOrderAsync(It.IsAny<PreOrder>()))
                .Callback<PreOrder>(po => capturedPreOrder = po)
                .ReturnsAsync((PreOrder)null!);

            // Setup for GetUserCart
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId, false))
                .ReturnsAsync(new List<PreOrder> { new()
                    {
                        PreOrderId = "new-order",
                        UserId = _userId,
                        SortKey = "PreOrder#new-order",
                        ReservationId = "res-1",
                        Status = "Draft",
                        TimeSlot = "14:00 - 15:00",
                        ReservationDate = "2023-12-15",
                        Address = "123 Main St",
                    }
                });

            // Act
            await _preOrderService.UpsertPreOrder(_userId, request);

            // Assert - Verify total price calculation: (12.99 * 2) + (8.50 * 1) = 34.48
            Assert.That(capturedPreOrder, Is.Not.Null);
            Assert.That(capturedPreOrder!.TotalPrice, Is.EqualTo(34.48m));
        }

        #endregion
    }
}

