using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class BookingsController : ControllerBase
{

    private readonly RentalService _rentalService;

    public BookingsController(RentalService rentalService)
    {
        _rentalService = rentalService;
    }

    [Authorize]
    [HttpPost("submit-booking")]
    public async Task<IActionResult> SubmitBooking(BookingDto dto)
    {
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"{claim.Type}: {claim.Value}");
        }

        // var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return BadRequest(new { message = "Invalid or missing user ID in claims" });
        }

        await _rentalService.SubmitApplication(dto, userId);
        return Ok(new { message = "Application submitted" });
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        // var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out int userId))
        {
            return BadRequest(new { message = "Invalid or missing user ID in claims" });
        }

        var apps = await _rentalService.GetUserApplications(userId);
        return Ok(apps);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingApplications()
    {
        var apps = await _rentalService.GetPendingApplications();
        return Ok(apps);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateApplicationStatus(int id, ApplicationStatusDto dto)
    {
        await _rentalService.UpdateApplicationStatus(id, dto.Status, dto.MessageToCustomer);
        return Ok("Application status updated");
    }

    // [Authorize(Roles = "Admin")]
    // [HttpPost("{id}/message")]
    // public async Task<IActionResult> SendMessage(int id, MessageDto dto)
    // {
    //     await _rentalService.SendMessage(id, dto.Message);
    //     return Ok("Message sent");
    // }

}
