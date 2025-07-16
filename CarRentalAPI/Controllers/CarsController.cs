using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class CarsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CarsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCars()
    {
        var cars = await _context.Cars.Include(c => c.Owner).ToListAsync();
        return Ok(cars);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCar(int id)
    {
        var car = await _context.Cars.FindAsync(id);
        if (car == null) return NotFound();
        return Ok(car);
    }

    [HttpPost]
    public async Task<IActionResult> AddCar(Car car)
    {
        _context.Cars.Add(car);
        await _context.SaveChangesAsync();
        return Ok(car);
    }

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
