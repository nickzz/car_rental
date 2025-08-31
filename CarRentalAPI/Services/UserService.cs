using Microsoft.EntityFrameworkCore;

public class UserService {
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context) {
        _context = context;
    }

    public async Task<User> GetProfile(int userId) {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<IEnumerable<User>> GetAllUsers() {
        return await _context.Users.ToListAsync();
    }

    public async Task UpdateProfile(int userId, RegisterDto dto) {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.DOB = dto.DateOfBirth;
        user.ICNumber = dto.NRIC;
        user.Email = dto.Email;
        user.Address = dto.Address;
        user.PhoneNumber = dto.PhoneNumber;

        await _context.SaveChangesAsync();
    }
}
