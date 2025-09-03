using Microsoft.EntityFrameworkCore;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<string> Register(RegisterDto dto)
    {
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
            Role = UserRole.Admin
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return "Registered successfully";
    }

    public async Task<(string Token, string Role)?> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
            return null;

        var token = JwtHelper.GenerateToken(user, _config);
        return (token, user.Role.ToString());
    }
}
