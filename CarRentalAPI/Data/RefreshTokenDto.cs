using System.ComponentModel.DataAnnotations;

namespace CarRentalAPI.Data
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token is required")]
        public string RefreshToken { get; set; }
    }
}