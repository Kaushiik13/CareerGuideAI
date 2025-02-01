using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Skill
{
    [Key]
    public Guid SkillId { get; set; }

    [Required]
    public string SkillName { get; set; }

    [ForeignKey("UserProfile")]
    public Guid ProfileId { get; set; }

    // Navigation Property
    public UserProfile UserProfile { get; set; }
}
