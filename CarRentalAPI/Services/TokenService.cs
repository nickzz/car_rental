using Microsoft.EntityFrameworkCore;
using CarRentalAPI.Exceptions;
using System.Security.Claims;
using CarRentalAPI.Models;

public class TokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<TokenService> _logger;

    public TokenService(
        ApplicationDbContext context,
        IConfiguration config,
        ILogger<TokenService> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Generate both access and refresh tokens for a user
    /// </summary>
    public async Task<TokenResponse> GenerateTokens(User user)
    {
        var accessToken = JwtHelper.GenerateAccessToken(user, _config);
        var refreshToken = JwtHelper.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
            CreatedAt = DateTime.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenEntity);

        // Clean up old expired tokens for this user
        await CleanupExpiredTokens(user.Id);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated new tokens for user {UserId} ({Email})", user.Id, user.Email);

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60")),
            RefreshTokenExpiresAt = refreshTokenEntity.ExpiresAt,
            Role = user.Role.ToString(),
            UserId = user.Id,
            Email = user.Email
        };
    }

    /// <summary>
    /// Refresh tokens using a valid refresh token
    /// </summary>
    public async Task<TokenResponse> RefreshTokens(string refreshToken, string accessToken)
    {
        // Validate the access token (even if expired)
        var principal = JwtHelper.GetPrincipalFromExpiredToken(accessToken, _config);
        if (principal == null)
            throw new UnauthorizedException("Invalid access token");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedException("Invalid token claims");

        var userId = int.Parse(userIdClaim);

        // Find and validate the refresh token
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (storedToken == null)
            throw new UnauthorizedException("Invalid refresh token");

        if (storedToken.IsRevoked)
            throw new UnauthorizedException("Refresh token has been revoked");

        if (storedToken.IsExpired)
            throw new UnauthorizedException("Refresh token has expired");

        // Revoke the old refresh token (one-time use)
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;

        // Generate new tokens
        var newTokens = await GenerateTokens(storedToken.User!);

        _logger.LogInformation("Refreshed tokens for user {UserId}", userId);

        return newTokens;
    }

    /// <summary>
    /// Revoke a specific refresh token (logout from one device)
    /// </summary>
    public async Task RevokeToken(string refreshToken, int userId)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken && rt.UserId == userId);

        if (token == null)
            throw new NotFoundException("Refresh token not found");

        if (token.IsRevoked)
            throw new BadRequestException("Token is already revoked");

        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked refresh token for user {UserId}", userId);
    }

    /// <summary>
    /// Revoke all refresh tokens for a user (logout from all devices)
    /// </summary>
    public async Task RevokeAllUserTokens(int userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        if (!tokens.Any())
        {
            _logger.LogWarning("No active tokens found for user {UserId}", userId);
            return;
        }

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Revoked {Count} refresh tokens for user {UserId}", tokens.Count, userId);
    }

    /// <summary>
    /// Remove expired and revoked tokens from database
    /// </summary>
    private async Task CleanupExpiredTokens(int userId)
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && 
                        (rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow))
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            _logger.LogInformation("Cleaned up {Count} expired tokens for user {UserId}", 
                expiredTokens.Count, userId);
        }
    }

    /// <summary>
    /// Get all active tokens for a user (for debugging/admin purposes)
    /// </summary>
    public async Task<List<RefreshToken>> GetActiveTokensForUser(int userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked && !rt.IsExpired)
            .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Clean up all expired tokens in the system (run periodically)
    /// </summary>
    public async Task CleanupAllExpiredTokens()
    {
        var expiredTokens = await _context.RefreshTokens
            .Where(rt => rt.IsRevoked || rt.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expiredTokens.Any())
        {
            _context.RefreshTokens.RemoveRange(expiredTokens);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Cleaned up {Count} expired tokens from the system", expiredTokens.Count);
        }
    }
}


































// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Microsoft.IdentityModel.Tokens;

// public class TokenService
// {
//     private readonly IConfiguration _config;

//     public TokenService(IConfiguration config)
//     {
//         _config = config;
//     }

//     public string CreateToken(User user)
//     {
//         var claims = new[]
//         {
//             new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//             new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
//             new Claim(ClaimTypes.Role, user.Role ?? string.Empty)
//         };

//         var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in configuration") ));
//         var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//         var token = new JwtSecurityToken(
//             issuer: _config["Jwt:Issuer"],
//             audience: _config["Jwt:Audience"],
//             claims: claims,
//             expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiresInMinutes"] ?? "60")),
//             signingCredentials: creds
//         );

//         return new JwtSecurityTokenHandler().WriteToken(token);
//     }
// }
