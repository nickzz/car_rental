using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CarsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // [Authorize]
    [HttpGet("GetAllCars")]
    public async Task<IActionResult> GetAllCars()
    {
        var cars = await _context.Cars.Include(c => c.Owner).ToListAsync();
        return Ok(cars);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();
        return Ok(car);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost("AddCar")]
    public async Task<IActionResult> AddCar(Car car)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
        return Ok(car);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCar(int id, Car updatedCar)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        car.Model = updatedCar.Model;
        car.Description = updatedCar.Description;
        car.PricePerDay = updatedCar.PricePerDay;
        car.IsAvailable = updatedCar.IsAvailable;

        await _context.SaveChangesAsync();
        return Ok(car);
    }

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();

        _context.Cars.Remove(car);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
