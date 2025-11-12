using Microsoft.EntityFrameworkCore;
using CarRentalAPI.Exceptions;
using CarRentalAPI.Models;
using CarRentalAPI.Data;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;
    private readonly TokenService _tokenService;

    public AuthService(
        ApplicationDbContext context, 
        IConfiguration config,
        ILogger<AuthService> logger,
        TokenService tokenService)
    {
        _context = context;
        _config = config;
        _logger = logger;
        _tokenService = tokenService;
    }

    public async Task<string> Register(RegisterDto dto)
    {
        // Check if email already exists
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser != null)
            throw new ConflictException("Email is already registered");

        // Check if NRIC already exists
        var existingNric = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ICNumber == dto.NRIC);

        if (existingNric != null)
            throw new ConflictException("NRIC is already registered");

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DOB = dto.DateOfBirth,
            ICNumber = dto.NRIC,
            Email = dto.Email,
            Address = dto.Address,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            PhoneNumber = dto.PhoneNumber,
            Role = UserRole.Customer
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {Email} registered successfully", dto.Email);
        return "Registered successfully";
    }

    public async Task<TokenResponse> Login(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (user == null)
            throw new UnauthorizedException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            throw new UnauthorizedException("Invalid email or password");

        var tokens = await _tokenService.GenerateTokens(user);
        
        _logger.LogInformation("User {Email} logged in successfully", dto.Email);
        return tokens;
    }

    public async Task<TokenResponse> RefreshToken(RefreshTokenDto dto, string accessToken)
    {
        var tokens = await _tokenService.RefreshTokens(dto.RefreshToken, accessToken);
        return tokens;
    }

    public async Task Logout(string refreshToken, int userId)
    {
        await _tokenService.RevokeToken(refreshToken, userId);
        _logger.LogInformation("User {UserId} logged out", userId);
    }

    public async Task LogoutAll(int userId)
    {
        await _tokenService.RevokeAllUserTokens(userId);
        _logger.LogInformation("User {UserId} logged out from all devices", userId);
    }
}