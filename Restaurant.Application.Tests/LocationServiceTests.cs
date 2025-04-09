using AutoMapper;
using Moq;
using NUnit.Framework;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Restaurant.Application.Tests
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
                cfg.AddProfile<LocationProfile>();
            });

            _mapper = config.CreateMapper();
            _locationService = new LocationService(_locationRepositoryMock.Object, _mapper);
        }

        [Test]
        public async Task GetAllLocationsAsync_ShouldReturnLocations()
        {
            // Arrange
            var locations = new List<Location>
            {
                new Location { Id = "1", Address = "123 River Street", AverageOccupancy = 4.7, Rating = 4.7, TotalCapacity = 15, ImageUrl = "https://example.com/images/riverside.jpg", Description = "Authentic Italian cuisine with riverside views" },
                new Location { Id = "2", Address = "45 High Street", AverageOccupancy = 4.5, Rating = 4.7, TotalCapacity = 15, ImageUrl = "https://example.com/images/bistro.jpg", Description = "Elegant French bistro in the heart of the city" }
            };

            _locationRepositoryMock.Setup(repo => repo.GetAllLocationsAsync()).ReturnsAsync(locations);

            // Act
            var result = await _locationService.GetAllLocationsAsync();

            // Assert
            Assert.AreEqual(2, result.Count());
        }
    }
}