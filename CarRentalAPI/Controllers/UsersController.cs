using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(ApplicationDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(User user)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
        {
            return BadRequest("Email already exists.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto, [FromServices] TokenService tokenService)
    {
        var sw = Stopwatch.StartNew();
        if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            return BadRequest("Email and password are required.");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized("Invalid email or password");

        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

        if (!isPasswordValid)
            return Unauthorized("Invalid email or password");
            
        var token = tokenService.CreateToken(user);

        sw.Stop();
        _logger.LogInformation($"GetData took {sw.ElapsedMilliseconds} ms");
        return Ok(new { token, role = user.Role });
    }


    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _context.Users.ToListAsync();
        return Ok(users);
    }
}
