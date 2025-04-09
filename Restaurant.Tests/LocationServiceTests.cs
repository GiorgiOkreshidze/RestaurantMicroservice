using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Tests
{
    public class LocationServiceTests
    {
        private Mock<ILocationRepository> _locationRepositoryMock;
        private ILocationService _locationService;
        private IMapper _mapper;

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

        // [Test]
        // public async Task GetLocationByIdAsync_ReturnsLocationDto()
        // {
        //     // Arrange
        //     var locationId = "test-id";
        //     var location = new Location { Id = locationId, Address = "Test Address" };
        //     _locationRepositoryMock.Setup(repo => repo.GetLocationByIdAsync(locationId)).ReturnsAsync(location);
        //
        //     // Act
        //     var result = await _locationService.GetLocationByIdAsync(locationId);
        //
        //     // Assert
        //     Assert.IsNotNull(result);
        //     Assert.AreEqual(locationId, result.Id);
        // }

        [Test]
        public async Task GetAllLocationsAsync_ReturnsListOfLocationDto()
        {
            // Arrange
            var locations = new List<Location>
            {
                new() { Id = "test-id-1", Address = "Test Address 1" },
                new() { Id = "test-id-2", Address = "Test Address 2" }
            };
            _locationRepositoryMock.Setup(repo => repo.GetAllLocationsAsync()).ReturnsAsync(locations);

            // Act
            var result = await _locationService.GetAllLocationsAsync();

            // Assert
            Assert.That(result.ToList(), Has.Count.EqualTo(2));
        }
    }
}