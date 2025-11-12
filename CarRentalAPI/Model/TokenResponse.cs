namespace CarRentalAPI.Models
{
    public class TokenResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string Role { get; set; }
        public int UserId { get; set; }
        public string Email { get; set; }
    }
}