using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("GetCars")]
    public async Task<IActionResult> GetAllCars()
    {
        var cars = await _carService.GetAllCars();
        return Ok(cars.Select(CarResponseDto.From));
    }

    [Authorize]
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableCars(DateOnly startDate, DateOnly endDate)
    {
        if (startDate >= endDate)
            return BadRequest("End date must be after start date");

        var availableCars = await _carService.GetAvailableCarsAsync(startDate, endDate);
        return Ok(availableCars.Select(CarResponseDto.From));
    }

    [HttpGet("{carId}/estimate")]
    public async Task<IActionResult> GetEstimatedPrice(int carId, DateOnly startDate, DateOnly endDate)
    {
        var totalPrice = await _carService.GetEstimatedPriceAsync(carId, startDate, endDate);

        if (totalPrice == null)
            return BadRequest("Invalid request or car not found");

        return Ok(totalPrice);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("AddCar")]
    public async Task<IActionResult> AddCar(CarDto car)
    {
        await _carService.AddCar(car);
        return Ok("Car added");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("UpdateCar/{id}")]
    public async Task<IActionResult> UpdateCar(int id, CarDto car)
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
