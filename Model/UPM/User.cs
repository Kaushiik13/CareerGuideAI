using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public Guid UserId { get; set; } // Primary Key

    [Required]
    public string Email { get; set; }

    [Required]
    public string Password { get; set; }

    public bool IsActive { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Property
    public UserProfile UserProfile { get; set; }
}
