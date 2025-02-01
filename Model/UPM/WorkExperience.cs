using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class WorkExperience
{
    [Key]
    public Guid WorkExperienceId { get; set; }

    [ForeignKey("UserProfile")]
    public Guid ProfileId { get; set; }

    [Required]
    public string Role { get; set; }

    [Required]
    public string YearExperience { get; set; }

    // Navigation Property
    public UserProfile UserProfile { get; set; }
}
