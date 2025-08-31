using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
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
    public async Task<IActionResult> Register(RegisterDto dto) {
        var result = await _authService.Register(dto);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto) {
        var result = await _authService.Login(dto);
        if (result == null) return Unauthorized("Invalid credentials");
        return Ok(new { token = result.Value.Token, role = result.Value.Role });
    }

}
