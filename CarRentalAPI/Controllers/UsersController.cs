using Asp.Versioning;
using CarRentalAPI.Data;
using CarRentalAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
// [Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(AuthService authService, ILogger<UsersController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.ErrorResult("Validation failed", errors));
        }

        var result = await _authService.Register(dto);
        return Ok(ApiResponse<string>.SuccessResponse(result, "Registration successful"));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.ErrorResult("Validation failed", errors));
        }

        var tokens = await _authService.Login(dto);
        return Ok(ApiResponse<TokenResponse>.SuccessResponse(tokens, "Login successful"));
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            return BadRequest(ApiResponse.ErrorResult("Validation failed", errors));
        }

        // Get the access token from Authorization header
        var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(ApiResponse.ErrorResult("Access token is required in Authorization header"));
        }

        var tokens = await _authService.RefreshToken(dto, accessToken);
        return Ok(ApiResponse<TokenResponse>.SuccessResponse(tokens, "Token refreshed successfully"));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _authService.Logout(dto.RefreshToken, userId);
        return Ok(ApiResponse.SuccessResult("Logged out successfully"));
    }

    [Authorize]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _authService.LogoutAll(userId);
        return Ok(ApiResponse.SuccessResult("Logged out from all devices successfully"));
    }
}