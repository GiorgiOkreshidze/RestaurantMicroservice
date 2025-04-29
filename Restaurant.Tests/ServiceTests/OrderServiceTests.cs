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

        _orderService = new OrderService(_reservationRepositoryMock.Object, _dishRepositoryMock.Object,
            _orderRepositoryMock.Object);
    }

    [Test]
    public async Task AddDishToOrderAsync_InputsAreValid_AddDishToOrder()
    {
        // Arrange
        var reservationId = "valid-reservation";
        var dishId = "valid-dish";
        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-10-01",
            GuestsNumber = "2",
            LocationId = "locationId",
            LocationAddress = "Location Address",
            PreOrder = "Mint strawberry milkshake",
            Status = "Confirmed",
            TableId = "tableId",
            TableCapacity = "4",
            TableNumber = "1",
            TimeFrom = "10:00",
            TimeTo = "11:00",
            TimeSlot = "10:00-11:00",
            CreatedAt = "2023-09-01T10:00:00Z",
        };
        var dish = new Dish
        {
            Id = dishId,
            Price = "10",
            Name = "someName",
            Weight = "50g",
            ImageUrl = "http://testimage.com",
            Quantity = 1
        };

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);

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
    public void AddDishToOrderAsync_ReservationDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var reservationId = "nonexistent-reservation";
        var dishId = "valid-dish";

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync((Reservation)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _orderService.AddDishToOrderAsync(reservationId, dishId));

        Assert.That(ex.Message, Is.EqualTo($"The Reservation with the key '{reservationId}' was not found."));
    }

    [Test]
    public async Task AddDishToOrderAsync_DishDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var reservationId = "valid-reservation";
        var dishId = "nonexistent-dish";
        var reservation = new Reservation
        {
            Id = reservationId,
            Date = "2023-10-01",
            GuestsNumber = "2",
            LocationId = "locationId",
            LocationAddress = "Location Address",
            PreOrder = "Mint strawberry milkshake",
            Status = "Confirmed",
            TableId = "tableId",
            TableCapacity = "4",
            TableNumber = "1",
            TimeFrom = "10:00",
            TimeTo = "11:00",
            TimeSlot = "10:00-11:00",
            CreatedAt = "2023-09-01T10:00:00Z",
        };

        _reservationRepositoryMock
            .Setup(r => r.GetReservationByIdAsync(reservationId))
            .ReturnsAsync(reservation);

        _dishRepositoryMock
            .Setup(d => d.GetDishByIdAsync(dishId))
            .ReturnsAsync((Dish)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<NotFoundException>(() =>
            _orderService.AddDishToOrderAsync(reservationId, dishId));

        Assert.That(ex?.Message, Is.EqualTo("The Dish with the key 'nonexistent-dish' was not found."));
    }
}