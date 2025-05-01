using Moq;
using NUnit.Framework;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests;

public class OrderServiceTests
{
    private Mock<IOrderRepository> _orderRepositoryMock = null!;
    private Mock<IDishRepository> _dishRepositoryMock = null!;
    private Mock<IReservationRepository> _reservationRepositoryMock = null!;
    private IOrderService _orderService = null!;

    [SetUp]
    public void SetUp()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _dishRepositoryMock = new Mock<IDishRepository>();
        _reservationRepositoryMock = new Mock<IReservationRepository>();

        _orderService = new OrderService(
            _dishRepositoryMock.Object, 
            _orderRepositoryMock.Object,
            _reservationRepositoryMock.Object);
    }

    [Test]
    public async Task AddDishToOrderAsync_InputsAreValid_AddDishToOrder()
    {
        // Arrange
        var reservationId = "valid-reservation";
        var dishId = "valid-dish";
        var dish = new Dish
        {
            Id = dishId,
            Price = "10",
            Name = "someName",
            Weight = "50g",
            ImageUrl = "http://testimage.com",
            Quantity = 1
        };

        _dishRepositoryMock
            .Setup(d => d.GetDishByIdAsync(dishId))
            .ReturnsAsync(dish);

        _orderRepositoryMock
            .Setup(o => o.GetOrderByReservationIdAsync(reservationId))
            .ReturnsAsync((Order)null);

        _orderRepositoryMock
            .Setup(o => o.SaveAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.AddDishToOrderAsync(reservationId, dishId);

        // Assert
        _orderRepositoryMock.Verify(o => o.SaveAsync(It.Is<Order>(order =>
            order.ReservationId == reservationId &&
            order.Dishes.Count == 1 &&
            order.Dishes[0].Id == dishId &&
            order.Dishes[0].Quantity == 1 &&
            order.TotalPrice == 10.0m
        )), Times.Once);
    }

    [Test]
    public async Task AddDishToOrderAsync_DishDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var reservationId = "valid-reservation";
        var dishId = "nonexistent-dish";

        _dishRepositoryMock
            .Setup(d => d.GetDishByIdAsync(dishId))
            .ReturnsAsync((Dish)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _orderService.AddDishToOrderAsync(reservationId, dishId));

        Assert.That(ex?.Message, Is.EqualTo("The Dish with the key 'nonexistent-dish' was not found."));
    }

    [Test]
    public async Task DeleteDishFromOrderAsync_ValidInputs_RemovesDishFromOrder()
    {
        // Arrange
        var reservationId = "valid-reservation";
        var dishId = "valid-dish";
        var order = new Order
        {
            Id = "order-id",
            ReservationId = reservationId,
            Dishes = new List<Dish>
            {
                new()
                {
                    Id = dishId,
                    Price = "10",
                    Name = "someName",
                    Weight = "50g",
                    ImageUrl = "http://testimage.com",
                    Quantity = 1
                }
            },
            TotalPrice = 20.0m,
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        _orderRepositoryMock
            .Setup(o => o.GetOrderByReservationIdAsync(reservationId))
            .ReturnsAsync(order);

        _dishRepositoryMock
            .Setup(d => d.GetDishByIdAsync(dishId))
            .ReturnsAsync(new Dish { 
                Id = dishId,
                Price = "10",
                Name = "someName",
                Weight = "50g",
                ImageUrl = "http://testimage.com",
                Quantity = 1 });

        _orderRepositoryMock
            .Setup(o => o.SaveAsync(It.IsAny<Order>()))
            .Returns(Task.CompletedTask);

        // Act
        await _orderService.DeleteDishFromOrderAsync(reservationId, dishId);

        // Assert
        _orderRepositoryMock.Verify(o => o.SaveAsync(It.Is<Order>(order =>
                order.ReservationId == reservationId &&
                order.Dishes.Count == 0 && // dish was removed
                order.TotalPrice == 0.0m // total updated accordingly
        )), Times.Once);
    }
    
    [Test]
    public async Task GetDishesInOrderAsync_ReservationExists_OrderWithDishes_ReturnsDishes()
    {
        // Arrange
        var reservationId = "res-123";
        var reservation = new Reservation
        {
            Id = reservationId,
            Date = DateTime.UtcNow.ToString("o"),
            GuestsNumber = "1",
            LocationId = "loc-123",
            LocationAddress = "123 Main St",
            PreOrder = "something",
            Status = "Confirmed",
            TableId = "table-123",
            TableCapacity = "4",
            TableNumber = "1",
            TimeFrom = "10:00",
            TimeTo = "12:00",
            TimeSlot = "10:00-12:00",
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };
        var order = new Order
        {
            ReservationId = reservationId,
            Dishes = new List<Dish>
            {
                new()
                {
                    Id = "dish-1",
                    Name = "Pizza",
                    Price = "10",
                    Weight = "500g",
                    ImageUrl = "",
                    IsPopular = true
                }
            },
            Id = "ORDER-123",
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        _orderRepositoryMock.Setup(o => o.GetOrderByReservationIdAsync(reservationId))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.GetDishesInOrderAsync(reservationId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("dish-1"));
    }
    
    [Test]
    public async Task GetDishesInOrderAsync_ReservationExists_OrderIsNull_ReturnsEmptyList()
    {
        // Arrange
        var reservationId = "res-456";
        var reservation = new Reservation
        {
            Id = reservationId,
            Date = DateTime.UtcNow.ToString("o"),
            GuestsNumber = "1",
            LocationId = "loc-123",
            LocationAddress = "123 Main St",
            PreOrder = "something",
            Status = "Confirmed",
            TableId = "table-123",
            TableCapacity = "4",
            TableNumber = "1",
            TimeFrom = "10:00",
            TimeTo = "12:00",
            TimeSlot = "10:00-12:00",
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        _orderRepositoryMock.Setup(o => o.GetOrderByReservationIdAsync(reservationId))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetDishesInOrderAsync(reservationId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
    
    [Test]
    public void GetDishesInOrderAsync_ReservationDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var reservationId = "res-789";

        _reservationRepositoryMock.Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _orderService.GetDishesInOrderAsync(reservationId));

        Assert.That(ex!.Message, Is.EqualTo($"The Reservation with the key '{reservationId}' was not found."));
    }
}