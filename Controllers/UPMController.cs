using API_V2._0.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API_V2._0.Controllers
{
    [Route("api")]
    [ApiController]
    public class UPMController : ControllerBase
    {
        private readonly UPMDbContext _context;

        public UPMController(UPMDbContext context)
        {
            _context = context;
        }

        // Signup
        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest("User Already Exists");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = request.Email,
                    Password = HashPassword(request.Password),  // Hash the password for security
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var userProfile = new UserProfile
                {
                    ProfileId = Guid.NewGuid(),
                    UserId = user.UserId,
                    Name = request.Name,
                    DOB = request.DOB,
                    Place = request.Place,
                    IsStudent = request.IsStudent,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { user.UserId, user.Email, userProfile.ProfileId, userProfile.Name });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Add Additional Information
        [HttpPost("add-info")]
        public async Task<IActionResult> AddInfo([FromBody] skillWExpRequest request, [FromQuery] Guid profileId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var skill = new Skill
                {
                    SkillId = Guid.NewGuid(),
                    SkillName = request.SkillName,
                    ProfileId = profileId
                };
                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();

                var education = new Education
                {
                    EducationId = Guid.NewGuid(),
                    Degree = request.Degree,
                    FieldOfStudy = request.FieldOfStudy,
                    InstitutionName = request.InstitutionName,
                    StartYear = request.StartYear,
                    EndYear = request.EndYear,
                    ProfileId = profileId
                };
                _context.Educations.Add(education);
                await _context.SaveChangesAsync();

                var workExperience = new WorkExperience
                {
                    WorkExperienceId = Guid.NewGuid(),
                    Role = request.Role,
                    YearExperience = request.YearExperience,
                    ProfileId = profileId
                };
                _context.WorkExperiences.Add(workExperience);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok("Info added successfully");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !VerifyPassword(request.Password, user.Password))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            return Ok(new { message = "Login successful", userId = user.UserId });
        }

        // Send Reset Code
        [HttpPost("send-reset-code")]
        public async Task<IActionResult> SendResetCode([FromBody] ResetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found");
            }

            var resetCode = GenerateResetCode();
            var isSent = await SendEmailAsync(request.Email, resetCode);
            if (!isSent)
                return StatusCode(500, "Failed to send email");

            return Ok(new { Message = "Reset code sent successfully", Code = resetCode });
        }

        // Get All Users
        [HttpGet("get-all-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p.Skills)
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p.Educations)
                .Include(u => u.UserProfile)
                    .ThenInclude(p => p.WorkExperiences)
                .ToListAsync();

            if (users.Count == 0)
            {
                return NotFound("No users found");
            }

            var result = users.Select(user => new
            {
                user.UserId,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                user.UpdatedAt,
                Profile = new
                {
                    user.UserProfile.ProfileId,
                    user.UserProfile.Name,
                    user.UserProfile.DOB,
                    user.UserProfile.Place,
                    user.UserProfile.IsStudent,
                    Skills = user.UserProfile.Skills.Select(s => new { s.SkillId, s.SkillName }),
                    Education = user.UserProfile.Educations.Select(e => new { e.EducationId, e.Degree, e.FieldOfStudy, e.InstitutionName, e.StartYear, e.EndYear }),
                    WorkExperiences = user.UserProfile.WorkExperiences.Select(w => new { w.WorkExperienceId, w.Role, w.YearExperience })
                }
            });

            return Ok(result);
        }

        // Utility Functions
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            return HashPassword(enteredPassword) == storedHash;
        }

        private static string GenerateResetCode()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int code = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 900000 + 100000;
            return code.ToString();
        }

        private async Task<bool> SendEmailAsync(string email, string resetCode)
        {
            try
            {
                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("your-email@gmail.com", "your-app-password"),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("your-email@gmail.com"),
                    Subject = "Password Reset Code",
                    Body = $"Your password reset code is: {resetCode}",
                    IsBodyHtml = false
                };

                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}




// DTOs
public class ResetRequest
    {
        public string Email { get; set; }
    }




    //DTO
    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
    public class SignUpRequest
    {
        // User fields
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; } = true; // Default to true
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // UserProfile fields
        public string Name { get; set; }
        public DateOnly DOB { get; set; }
        public string Place { get; set; }
        public bool IsStudent { get; set; } = true;

    }

    public class skillWExpRequest
    {
        public string SkillName { get; set; }
        public string Degree { get; set; }
        public string FieldOfStudy { get; set; }
        public string InstitutionName { get; set; }
        public string StartYear { get; set; }
        public string EndYear { get; set; }
        public string Role { get; set; }
        public string YearExperience { get; set; }
    }
