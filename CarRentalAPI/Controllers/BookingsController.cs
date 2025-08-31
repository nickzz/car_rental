using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{

    private readonly RentalService _rentalService;

    public BookingsController(RentalService rentalService)
    {
        _rentalService = rentalService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SubmitRental(BookingDto dto)
    {
        foreach (var claim in User.Claims)
        {
            Console.WriteLine($"{claim.Type}: {claim.Value}");
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        await _rentalService.SubmitApplication(dto, userId);
        return Ok("Application submitted");
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<IActionResult> GetMyApplications()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
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
    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _rentalService.ApproveApplication(id);
        return Ok("Approved");
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        await _rentalService.RejectApplication(id);
        return Ok("Rejected");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/message")]
    public async Task<IActionResult> SendMessage(int id, MessageDto dto)
    {
        await _rentalService.SendMessage(id, dto.Message);
        return Ok("Message sent");
    }

}
