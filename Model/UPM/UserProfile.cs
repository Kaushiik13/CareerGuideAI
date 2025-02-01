using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class UserProfile
{
    [Key]
    public Guid ProfileId { get; set; } // Primary Key

    [Required]
    [ForeignKey("User")]
    public Guid UserId { get; set; }

    [Required]
    public string Name { get; set; }

    [Required]
    public DateOnly DOB { get; set; }

    public bool IsStudent { get; set; } = true;

    [Required]
    public string Place { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    public User User { get; set; }
    public ICollection<Skill> Skills { get; set; }
    public ICollection<WorkExperience> WorkExperiences { get; set; }
    public ICollection<Education> Educations { get; set; }
}
