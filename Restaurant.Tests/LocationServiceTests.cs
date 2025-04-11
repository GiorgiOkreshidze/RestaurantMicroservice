using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs.Locations;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests
{
    public class LocationServiceTests
    {
        private Mock<ILocationRepository> _locationRepositoryMock = null!;
        private ILocationService _locationService = null!;
        private IMapper _mapper = null!;

        [SetUp]
        public void SetUp()
        {
            _locationRepositoryMock = new Mock<ILocationRepository>();

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Location, LocationDto>().ReverseMap();
            });
            _mapper = config.CreateMapper();

            _locationService = new LocationService(_locationRepositoryMock.Object, _mapper);
        }

        [Test]
        public async Task GetLocationByIdAsync_ValidId_ReturnsLocationDto()
        {
            // Arrange
            var locationId = "test-id-1";
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

            _locationRepositoryMock
                .Setup(repo => repo.GetLocationByIdAsync(locationId))
                .ReturnsAsync(location);

            // Act
            var result = await _locationService.GetLocationByIdAsync(locationId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(location.Id));
            Assert.That(result.Address, Is.EqualTo(location.Address));
            Assert.That(result.AverageOccupancy, Is.EqualTo(location.AverageOccupancy));
            Assert.That(result.Rating, Is.EqualTo(location.Rating));
            Assert.That(result.TotalCapacity, Is.EqualTo(location.TotalCapacity));
            Assert.That(result.ImageUrl, Is.EqualTo(location.ImageUrl));
            Assert.That(result.Description, Is.EqualTo(location.Description));
        }

        [Test]
        public async Task GetLocationByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var invalidLocationId = "invalid-id";

            _locationRepositoryMock
                .Setup(repo => repo.GetLocationByIdAsync(invalidLocationId))
                .ReturnsAsync((Location?)null);

            // Act
            var result = await _locationService.GetLocationByIdAsync(invalidLocationId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetAllLocationsAsync_ReturnsListOfLocationDto()
        {
            // Arrange
            var locations = new List<Location>
            {
                new() { Id = "test-id-1", Address = "Test Address 1", AverageOccupancy = 75.5, Rating = 4.5, TotalCapacity = 100, ImageUrl = "http://testimage.com", Description = "Test Description" },
                new() {Id = "test-id-2", Address = "Test Address 2", AverageOccupancy = 75.5, Rating = 4.5, TotalCapacity = 100, ImageUrl = "http://testimage.com", Description = "Test Description"}
            };
            _locationRepositoryMock.Setup(repo => repo.GetAllLocationsAsync()).ReturnsAsync(locations);

            // Act
            var result = await _locationService.GetAllLocationsAsync();

            // Assert
            Assert.That(result.ToList(), Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetAllLocationsAsync_ReturnsSelectOptionDtos_WhenLocationsExist()
        {
            // Arrange
            var mockedLocations = new List<Location>
            {
                new Location {Id = "1", Address = "Location 1", AverageOccupancy = 75.5, Rating = 4.5, TotalCapacity = 100, ImageUrl = "http://testimage.com", Description = "Test Description"},
                new Location {Id = "2", Address = "Location 2", AverageOccupancy = 75.5, Rating = 4.5, TotalCapacity = 100, ImageUrl = "http://testimage.com", Description = "Test Description"}
            };

            _locationRepositoryMock
                .Setup(repo => repo.GetAllLocationsAsync())
                .ReturnsAsync(mockedLocations);

            // Act
            var result = await _locationService.GetAllLocationsForDropDownAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ToList(), Has.Count.EqualTo(2));
            Assert.That(result, Has.Exactly(1).Matches<LocationSelectOptionDto>(dto => dto.Id == "1" && dto.Address == "Location 1"));
            Assert.That(result, Has.Exactly(1).Matches<LocationSelectOptionDto>(dto => dto.Id == "2" && dto.Address == "Location 2"));

            // Verify the repository method was called once
            _locationRepositoryMock.Verify(repo => repo.GetAllLocationsAsync(), Times.Once);
        }

        [Test]
        public async Task GetAllLocationsAsync_ReturnsEmptyList_WhenNoLocationsExist()
        {
            // Arrange
            _locationRepositoryMock
                 .Setup(repo => repo.GetAllLocationsAsync())
                .ReturnsAsync(new List<Location>());

            // Act
            var result = await _locationService.GetAllLocationsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);

            // Verify the repository method was called once
            _locationRepositoryMock.Verify(repo => repo.GetAllLocationsAsync(), Times.Once);
        }
    }
}