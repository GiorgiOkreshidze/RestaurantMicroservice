using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.PerOrders;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Profiles;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Restaurant.Tests.ServiceTests
{
    public class PreOrderServiceTests
    {
        private Mock<IPreOrderRepository> _preOrderRepositoryMock = null!;
        private IMapper _mapper = null!;
        private IPreOrderService _preOrderService = null!;
        private List<PreOrder> _preOrders = null!;
        private readonly string _userId = "user-123";

        [SetUp]
        public void SetUp()
        {
            // Configure repository mock
            _preOrderRepositoryMock = new Mock<IPreOrderRepository>();

            // Configure real AutoMapper with actual profiles
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new CartProfile());
            });

            // Create a real mapper instance
            _mapper = mapperConfig.CreateMapper();

            // Initialize the service with real mapper
            _preOrderService = new PreOrderService(_preOrderRepositoryMock.Object, _mapper);

            // Create test data
            _preOrders = new List<PreOrder>
            {
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-1",
                    ReservationId = "res-1",
                    Status = "SUBMITTED",
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z"),
                    TotalAmount = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
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
                            DishImageUrl = "https://example.com/images/pasta.jpg",
                            Notes = "Extra cheese"
                        },
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-1#Item#2",
                            DishId = "dish-2",
                            DishName = "Caesar Salad",
                            Quantity = 1,
                            Price = 14.01m,
                            DishImageUrl = "https://example.com/images/salad.jpg",
                            Notes = ""
                        }
                    }
                },
                new PreOrder
                {
                    UserId = _userId,
                    SortKey = "PreOrder#order-2",
                    ReservationId = "res-2",
                    Status = "PENDING",
                    CreateDate = DateTime.Parse("2023-05-16T18:30:00Z"),
                    TotalAmount = 32.50m,
                    Address = "456 Oak Ave",
                    TimeSlot = "18:30 - 19:30",
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
                            DishImageUrl = "https://example.com/images/pizza.jpg",
                            Notes = "Well done"
                        },
                        new PreOrderItem
                        {
                            UserId = _userId,
                            SortKey = "PreOrder#order-2#Item#2",
                            DishId = "dish-4",
                            DishName = "Tiramisu",
                            Quantity = 2,
                            Price = 7.00m,
                            DishImageUrl = "https://example.com/images/tiramisu.jpg",
                            Notes = ""
                        }
                    }
                }
            };
        }

        [Test]
        public async Task GetUserCart_WithValidUserId_ReturnsCartWithPreOrders()
        {
            // Arrange
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
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
            Assert.That(firstPreOrder.Date, Is.EqualTo(DateTime.Parse("2023-05-15T12:00:00Z")));
            Assert.That(firstPreOrder.TotalAmount, Is.EqualTo(45.99m));

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
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithEmptyCart_ReturnsEmptyCartIndicator()
        {
            // Arrange
            var emptyList = new List<PreOrder>();
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.Content, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithNullResultFromRepository_ReturnsEmptyCartIndicator()
        {
            // Arrange
            List<PreOrder>? nullList = null;
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ReturnsAsync(nullList);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.True);
            Assert.That(result.Content, Is.Empty);

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
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
                    CreateDate = DateTime.Parse("2023-05-17T19:00:00Z"),
                    TotalAmount = 0m,
                    Address = "789 Pine St",
                    TimeSlot = "19:00 - 20:00",
                    Items = new List<PreOrderItem>()  // Empty items list
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
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
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
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
                    CreateDate = DateTime.Parse("2023-05-18T20:00:00Z"),
                    TotalAmount = 25.99m,
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

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
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
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
        }

        [Test]
        public async Task GetUserCart_WithRepositoryException_PropagatesException()
        {
            // Arrange
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _preOrderService.GetUserCart(_userId));

            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Is.EqualTo("Database connection failed"));

            // Verify repository was called once with correct userId
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
        }

        [Test]
        public void GetUserCart_WithNullUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.GetUserCart(null!));

            // Verify repository was not called
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public void GetUserCart_WithEmptyUserId_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(async () =>
                await _preOrderService.GetUserCart(string.Empty));

            // Verify repository was not called
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetUserCart_WithSinglePreOrder_ReturnsCorrectCart()
        {
            // Arrange
            var singlePreOrder = new List<PreOrder> { _preOrders[0] };
            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ReturnsAsync(singlePreOrder);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("order-1"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
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
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z"),
                    TotalAmount = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>()
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ReturnsAsync(preOrderWithCustomSortKey);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            Assert.That(result.Content[0].Id, Is.EqualTo("complex-id-123-456"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
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
                    CreateDate = DateTime.Parse("2023-05-15T12:00:00Z"),
                    TotalAmount = 45.99m,
                    Address = "123 Main St",
                    TimeSlot = "12:00 - 13:00",
                    Items = new List<PreOrderItem>()
                }
            };

            _preOrderRepositoryMock.Setup(repo => repo.GetPreOrdersAsync(_userId))
                .ReturnsAsync(preOrderWithBadSortKey);

            // Act
            var result = await _preOrderService.GetUserCart(_userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Content, Has.Count.EqualTo(1));
            // The ID should be the entire key since the prefix doesn't match
            Assert.That(result.Content[0].Id, Is.EqualTo("InvalidPrefix-order-1"));

            // Verify repository was called once
            _preOrderRepositoryMock.Verify(repo => repo.GetPreOrdersAsync(_userId), Times.Once);
        }
    }
}
