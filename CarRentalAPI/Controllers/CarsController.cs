using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Asp.Versioning;


[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CarsController : ControllerBase
{
    private readonly CarService _carService;

    public CarsController(CarService carService)
    {
        _carService = carService;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("GetCars")]
    public async Task<IActionResult> GetAllCars()
    {
        var cars = await _carService.GetAllCars();
        return Ok(cars);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableCars(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return BadRequest("End date must be after start date");

        var availableCars = await _carService.GetAvailableCarsAsync(startDate, endDate);
        return Ok(availableCars);
    }

    [HttpGet("{carId}/estimate")]
    public async Task<IActionResult> GetEstimatedPrice(int carId, DateTime startDate, DateTime endDate)
    {
        var totalPrice = await _carService.GetEstimatedPriceAsync(carId, startDate, endDate);

        if (totalPrice == null)
            return BadRequest("Invalid request or car not found");

        return Ok(totalPrice);
    }

    // [Authorize(Roles = "Admin")]
    [HttpPost("AddCar")]
    public async Task<IActionResult> AddCar(Car car)
    {
        await _carService.AddCar(car);
        return Ok("Car added");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("UpdateCar/{id}")]
    public async Task<IActionResult> UpdateCar(int id, Car car)
    {
        await _carService.UpdateCar(id, car);
        return Ok("Car updated");
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("RemoveCar/{id}")]
    public async Task<IActionResult> DeleteCar(int id)
    {
        await _carService.DeleteCar(id);
        return Ok("Car deleted");
    }

}
