using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

public static class JwtHelper
{
    /// <summary>
    /// Generate a new JWT access token for a user
    /// </summary>
    public static string GenerateAccessToken(User user, IConfiguration config)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()) // Issued at
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresInMinutes = int.Parse(config["Jwt:ExpiresInMinutes"] ?? "60");

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generate a cryptographically secure random refresh token
    /// </summary>
    public static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Extract claims from an expired token (used for token refresh)
    /// </summary>
    public static ClaimsPrincipal? GetPrincipalFromExpiredToken(string token, IConfiguration config)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
            ValidateLifetime = false // Don't validate expiration - we want to get principal even if expired
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            
            // Verify it's a valid JWT token with correct algorithm
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch (Exception)
        {
            // Token validation failed
            return null;
        }
    }

    /// <summary>
    /// Validate if a token is still valid (not expired)
    /// </summary>
    public static bool IsTokenValid(string token, IConfiguration config)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"])),
            ValidateLifetime = true, // Check if token is expired
            ClockSkew = TimeSpan.Zero // No tolerance for expiration
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get user ID from token without full validation
    /// </summary>
    public static int? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get token expiration time
    /// </summary>
    public static DateTime? GetTokenExpirationTime(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            
            return jwtToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility (Phase 1)
    /// Use GenerateAccessToken instead
    /// </summary>
    [Obsolete("Use GenerateAccessToken instead")]
    public static string GenerateToken(User user, IConfiguration config)
    {
        return GenerateAccessToken(user, config);
    }
}




// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Microsoft.IdentityModel.Tokens;

// public static class JwtHelper {
//     public static string GenerateToken(User user, IConfiguration config) {
//         var claims = new[] {
//             new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
//             new Claim(ClaimTypes.Role, user.Role.ToString())
//         };

//         var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]));
//         var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//         var token = new JwtSecurityToken(
//             claims: claims,
//             expires: DateTime.Now.AddDays(1),
//             signingCredentials: creds);

//         return new JwtSecurityTokenHandler().WriteToken(token);
//     }
// }
