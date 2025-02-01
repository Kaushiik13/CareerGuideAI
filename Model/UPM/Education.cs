using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Education
{
    [Key]
    public Guid EducationId { get; set; }

    [Required]
    public string Degree { get; set; }

    [Required]
    public string FieldOfStudy { get; set; }

    [Required]
    public string InstitutionName { get; set; }

    [Required]
    public string StartYear { get; set; }

    [Required]
    public string EndYear { get; set; }

    [ForeignKey("UserProfile")]
    public Guid ProfileId { get; set; }

    // Navigation Property
    public UserProfile UserProfile { get; set; }
}
