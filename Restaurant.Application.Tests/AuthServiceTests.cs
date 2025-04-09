using Moq;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Restaurant.Application.DTOs;
using Restaurant.Application.Interfaces;
using Restaurant.Application.Services;
using System.Threading.Tasks;

namespace Restaurant.Application.Tests
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<UserManager<IdentityUser>> _userManagerMock;
        private Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private Mock<IConfiguration> _configurationMock;
        private IAuthService _authService;

        [SetUp]
        public void SetUp()
        {
            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                _userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<IdentityUser>>(), null, null, null, null);
            _configurationMock = new Mock<IConfiguration>();
            _authService = new AuthService(_userManagerMock.Object, _signInManagerMock.Object, _configurationMock.Object);
        }

        [Test]
        public async Task RegisterAsync_ShouldReturnTrue_WhenRegistrationIsSuccessful()
        {
            // Arrange
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Password123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public async Task RegisterAsync_ShouldReturnFalse_WhenRegistrationFails()
        {
            // Arrange
            var registerDto = new RegisterDto { Email = "test@example.com", Password = "Password123" };
            _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Error" }));

            // Act
            var result = await _authService.RegisterAsync(registerDto);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public async Task LoginAsync_ShouldReturnToken_WhenLoginIsSuccessful()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123" };
            _signInManagerMock.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(new IdentityUser { Id = "1", Email = "test@example.com" });
            _configurationMock.Setup(x => x["Jwt:Key"]).Returns("YourSuperSecretKeyHere");
            _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("YourIssuer");
            _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("YourAudience");
            _configurationMock.Setup(x => x["Jwt:ExpireMinutes"]).Returns("60");

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task LoginAsync_ShouldReturnNull_WhenLoginFails()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "test@example.com", Password = "Password123" };
            _signInManagerMock.Setup(x => x.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), false, false)).ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.IsNull(result);
        }
    }
}
