using System.ComponentModel.DataAnnotations;

public class RefreshToken
{
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required]
    [StringLength(500)]
    public string? Token { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
}