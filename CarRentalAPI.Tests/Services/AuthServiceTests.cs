using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CarRentalAPI.Exceptions;

namespace CarRentalAPI.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly Mock<ILogger<AuthService>> _authLoggerMock;
        private readonly Mock<ILogger<TokenService>> _tokenLoggerMock;
        private readonly TokenService _tokenService;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Setup real configuration (not mock)
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                {"Jwt:Key", "ThisIsASecretKeyThatShouldBeLongEnoughForTesting123456"},
                {"Jwt:Issuer", "CarRentalAPI"},
                {"Jwt:Audience", "CarRentalAPIUser"},
                {"Jwt:ExpiresInMinutes", "60"}
            });
            _config = configBuilder.Build();

            // Setup loggers
            _authLoggerMock = new Mock<ILogger<AuthService>>();
            _tokenLoggerMock = new Mock<ILogger<TokenService>>();

            // Create REAL TokenService (no mocking)
            _tokenService = new TokenService(_context, _config, _tokenLoggerMock.Object);

            // Create AuthService with real TokenService
            _authService = new AuthService(
                _context,
                _config,
                _authLoggerMock.Object,
                _tokenService
            );
        }

        [Fact]
        public async Task Register_WithValidData_ShouldCreateUser()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                NRIC = "900101-01-1234",
                Email = "john.doe@example.com",
                Address = "123 Main Street, City",
                PhoneNumber = "0123456789",
                Password = "Test@1234"
            };

            // Act
            var result = await _authService.Register(registerDto);

            // Assert
            result.Should().Be("Registered successfully");
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
            user.Should().NotBeNull();
            user!.FirstName.Should().Be("John");
            user.LastName.Should().Be("Doe");
            user.Role.Should().Be(UserRole.Customer);
            
            // Verify password is hashed
            user.Password.Should().NotBe("Test@1234");
            BCrypt.Net.BCrypt.Verify("Test@1234", user.Password).Should().BeTrue();
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ShouldThrowConflictException()
        {
            // Arrange
            var existingUser = new User
            {
                FirstName = "Existing",
                LastName = "User",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-9999",
                Email = "existing@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword("password"),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                NRIC = "900101-01-1234",
                Email = "existing@example.com", // Duplicate email
                Address = "123 Main Street",
                PhoneNumber = "0123456789",
                Password = "Test@1234"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConflictException>(
                async () => await _authService.Register(registerDto)
            );

            exception.Message.Should().Contain("Email is already registered");
        }

        [Fact]
        public async Task Register_WithDuplicateNRIC_ShouldThrowConflictException()
        {
            // Arrange
            var existingUser = new User
            {
                FirstName = "Existing",
                LastName = "User",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1234",
                Email = "existing@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword("password"),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                FirstName = "John",
                LastName = "Doe",
                DateOfBirth = new DateTime(1990, 1, 1),
                NRIC = "900101-01-1234", // Duplicate NRIC
                Email = "new@example.com",
                Address = "123 Main Street",
                PhoneNumber = "0123456789",
                Password = "Test@1234"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ConflictException>(
                async () => await _authService.Register(registerDto)
            );

            exception.Message.Should().Contain("NRIC is already registered");
        }

        [Fact]
        public async Task Login_WithValidCredentials_ShouldReturnTokenResponse()
        {
            // Arrange
            var password = "Test@1234";
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1234",
                Email = "john@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "john@example.com",
                Password = password
            };

            // Act
            var result = await _authService.Login(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.AccessToken.Should().NotBeNullOrEmpty();
            result.RefreshToken.Should().NotBeNullOrEmpty();
            result.Role.Should().Be("Customer");
            result.UserId.Should().Be(user.Id);
            result.Email.Should().Be(user.Email);
            
            // Verify token was saved to database
            var savedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.UserId == user.Id);
            savedToken.Should().NotBeNull();
            savedToken!.Token.Should().Be(result.RefreshToken);
        }

        [Fact]
        public async Task Login_WithInvalidEmail_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Test@1234"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _authService.Login(loginDto)
            );

            exception.Message.Should().Contain("Invalid email or password");
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ShouldThrowUnauthorizedException()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1234",
                Email = "john@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword("CorrectPassword"),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginDto = new LoginDto
            {
                Email = "john@example.com",
                Password = "WrongPassword"
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<UnauthorizedException>(
                async () => await _authService.Login(loginDto)
            );

            exception.Message.Should().Contain("Invalid email or password");
        }

        [Fact]
        public async Task Logout_WithValidToken_ShouldRevokeToken()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1234",
                Email = "john@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Login to get token
            var loginDto = new LoginDto
            {
                Email = "john@example.com",
                Password = "Test@1234"
            };
            var loginResult = await _authService.Login(loginDto);

            // Act - Logout
            await _authService.Logout(loginResult.RefreshToken, user.Id);

            // Assert
            var revokedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == loginResult.RefreshToken);
            
            revokedToken.Should().NotBeNull();
            revokedToken!.IsRevoked.Should().BeTrue();
            revokedToken.RevokedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task LogoutAll_ShouldRevokeAllUserTokens()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                DOB = new DateTime(1990, 1, 1),
                ICNumber = "900101-01-1234",
                Email = "john@example.com",
                Address = "Address",
                Password = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
                PhoneNumber = "0123456789",
                Role = UserRole.Customer
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create multiple login sessions
            var login1 = await _authService.Login(new LoginDto { Email = "john@example.com", Password = "Test@1234" });
            var login2 = await _authService.Login(new LoginDto { Email = "john@example.com", Password = "Test@1234" });

            // Act - Logout from all devices
            await _authService.LogoutAll(user.Id);

            // Assert
            var allTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id)
                .ToListAsync();
            
            allTokens.Should().AllSatisfy(token =>
            {
                token.IsRevoked.Should().BeTrue();
                token.RevokedAt.Should().NotBeNull();
            });
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}