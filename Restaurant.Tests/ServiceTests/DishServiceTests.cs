using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Dishes;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests.ServiceTests
{
    public class DishServiceTests
    {
        private Mock<IDishRepository> _dishRepositoryMock = null!;
        private Mock<ILocationRepository> _locationRepositoryMock = null!;
        private IDishService _dishService = null!;
        private IMapper _mapper = null!;
        private List<Dish> _dishes;

        [SetUp]
        public void SetUp()
        {
            _dishRepositoryMock = new Mock<IDishRepository>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Dish, DishDto>().ReverseMap();
                cfg.CreateMap<Location, LocationDto>().ReverseMap();
            });

            _mapper = config.CreateMapper();

            _dishService = new DishService(_dishRepositoryMock.Object, _locationRepositoryMock.Object, _mapper);
            
            _dishes = new()
            {
                new Dish
                {
                    Id = "dish-1",
                    Name = "Dish 1",
                    Price = "9.99",
                    ImageUrl = "http://example.com/dish1.jpg",
                    Weight = "200g",
                    IsPopular = true,
                    LocationId = "valid-location-id"
                },
                new Dish
                {
                    Id = "dish-2",
                    Name = "Dish 2",
                    Price = "12.99",
                    ImageUrl = "http://example.com/dish2.jpg",
                    Weight = "300g",
                    IsPopular = true,
                    LocationId = "valid-location-id"
                }
            };
        }

        [Test]
        public async Task GetSpecialtyDishesByLocationAsync_NoDishesFound_ReturnsEmptyList()
        {
            // Arrange
            var locationId = "valid-location-id";

            var location = new Location
            {
                Id = locationId,
                Address = "Test Address",
                AverageOccupancy = 75.5,
                Rating = 4.5,
                TotalCapacity = 100,
                ImageUrl = "http://testimage.com",
                Description = "Test Description"
            };

            _locationRepositoryMock.Setup(repo => repo.GetLocationByIdAsync(locationId))
                                   .ReturnsAsync(location);

            _dishRepositoryMock.Setup(repo => repo.GetSpecialtyDishesByLocationAsync(locationId))
                               .ReturnsAsync(new List<Dish>());

            // Act
            var result = await _dishService.GetSpecialtyDishesByLocationAsync(locationId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetSpecialtyDishesByLocationAsync_DishesFound_ReturnsMappedDishList()
        {
            // Arrange
            var locationId = "valid-location-id";

            var location = new Location
            {
                Id = locationId,
                Address = "Test Address",
                AverageOccupancy = 75.5,
                Rating = 4.5,
                TotalCapacity = 100,
                ImageUrl = "http://testimage.com",
                Description = "Test Description"
            };

            _locationRepositoryMock.Setup(repo => repo.GetLocationByIdAsync(locationId))
                                   .ReturnsAsync(location); // Simulate location found

            _dishRepositoryMock.Setup(repo => repo.GetSpecialtyDishesByLocationAsync(locationId))
                               .ReturnsAsync(_dishes);

            // Act
            var result = (await _dishService.GetSpecialtyDishesByLocationAsync(locationId)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo("dish-1"));
            Assert.That(result[0].Name, Is.EqualTo("Dish 1"));
            Assert.That(result[0].Price, Is.EqualTo("9.99"));
            Assert.That(result[0].ImageUrl, Is.EqualTo("http://example.com/dish1.jpg"));
            Assert.That(result[0].Weight, Is.EqualTo("200g"));

            Assert.That(result[1].Id, Is.EqualTo("dish-2"));
            Assert.That(result[1].Name, Is.EqualTo("Dish 2"));
            Assert.That(result[1].Price, Is.EqualTo("12.99"));
            Assert.That(result[1].ImageUrl, Is.EqualTo("http://example.com/dish2.jpg"));
            Assert.That(result[1].Weight, Is.EqualTo("300g"));
        }

        [Test] 
        public async Task GetPopularDishesAsync_DishesFound_ReturnsMappedDishList()
        {
            // Arrange
            var expectedDishesDto = new List<DishDto>
            {
                new()
                {
                    Id = "dish-1",
                    Name = "Dish 1",
                    Price = "9.99",
                    ImageUrl = "http://example.com/dish1.jpg",
                    Weight = "200g",
                },
                new()
                {
                    Id = "dish-2",
                    Name = "Dish 2",
                    Price = "12.99",
                    ImageUrl = "http://example.com/dish2.jpg",
                    Weight = "300g",
                }
            };

            _dishRepositoryMock.Setup(repo => repo.GetPopularDishesAsync())
                               .ReturnsAsync(_dishes);

            // Act
            var actual = (await _dishService.GetPopularDishesAsync()).ToList();

            // Assert
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual, Has.Count.EqualTo(2));
            Assert.That(actual[0].Id, Is.EqualTo(expectedDishesDto[0].Id));
            Assert.That(actual[0].Name, Is.EqualTo(expectedDishesDto[0].Name));
            Assert.That(actual[0].Price, Is.EqualTo(expectedDishesDto[0].Price));
            Assert.That(actual[0].ImageUrl, Is.EqualTo(expectedDishesDto[0].ImageUrl));
            Assert.That(actual[0].Weight, Is.EqualTo(expectedDishesDto[0].Weight));

            Assert.That(actual[1].Id, Is.EqualTo(expectedDishesDto[1].Id));
            Assert.That(actual[1].Name, Is.EqualTo(expectedDishesDto[1].Name));
            Assert.That(actual[1].Price, Is.EqualTo(expectedDishesDto[1].Price));
            Assert.That(actual[1].ImageUrl, Is.EqualTo(expectedDishesDto[1].ImageUrl));
            Assert.That(actual[1].Weight, Is.EqualTo(expectedDishesDto[1].Weight));
        }
    }
}
