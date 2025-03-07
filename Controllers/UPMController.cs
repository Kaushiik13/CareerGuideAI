using API_V2._0.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using System.Security.Claims;
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



        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var existingUser = _context.Users.Where(u => u.Email == request.Email);

            if (existingUser.Any())
            {
                return BadRequest("User Exists");   
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    UserId = Guid.NewGuid(),
                    Email = request.Email,
                    Password = request.Password,
                    IsActive = request.IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Add the user to the database
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

                // Add the user profile to the database
                _context.UserProfiles.Add(userProfile);
                await _context.SaveChangesAsync();

                // Commit the transaction
                await transaction.CommitAsync();

                return Ok(new { user.UserId, user.Email, userProfile.ProfileId, userProfile.Name });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("AddInfo")]
        public async Task<IActionResult> AddInfo([FromBody] skillWExpRequest request, Guid ProfileId)
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
                    ProfileId = ProfileId
                };

                _context.Skills.Add(skill);
                await _context.SaveChangesAsync();

                var Education = new Education
                {
                    EducationId = Guid.NewGuid(),
                    Degree = request.Degree,
                    FieldOfStudy = request.FieldOfStudy,
                    InstitutionName = request.InstitutionName,
                    StartYear = request.StartYear,
                    EndYear = request.EndYear,
                    ProfileId = ProfileId
                };
                _context.Educations.Add(Education);
                await _context.SaveChangesAsync();

                var WorkExperience = new WorkExperience
                {
                    WorkExperienceId = Guid.NewGuid(),
                    Role = request.Role,
                    YearExperience = request.YearExperience,
                    ProfileId = ProfileId
                };
                _context.WorkExperiences.Add(WorkExperience);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return (Ok(200));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
            return (Ok(request));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Where(u => u.Email == request.Email && u.Password == request.Password)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var userProfile = await _context.UserProfiles
                .Where(p => p.UserId == user.UserId)
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new { message = "User profile not found" });
            }

            return Ok(new { message = "Login Successful", ProfileId = userProfile.ProfileId });
        }

        [HttpPost("send-reset-code")]
        public async Task<IActionResult> SendResetCode([FromBody] ResetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                return BadRequest("Email is required");

            var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Generate a random code
            var resetCode = GenerateResetCode();

            // Hash the reset code
            var hashedCode = HashResetCode(resetCode);

            // Send the email
            var isSent = await SendEmailAsync(request.Email, resetCode);
            if (!isSent)
                return StatusCode(500, "Failed to send email");

            // Return the hashed code for validation
            return Ok(new { Message = "Reset code sent successfully", HashedCode = hashedCode });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Code) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Email, code, and new password are required.");
            }

            var user = await _context.Users.Where(u => u.Email == request.Email).FirstOrDefaultAsync();
            if (user == null)
            {
                return BadRequest("User not found");
            }

            // Recreate the hashed reset code for validation
            var hashedCode = HashResetCode(request.Code);

            if (hashedCode != request.HashedCode)
            {
                return BadRequest("Invalid reset code.");
            }

            // Update the password
            user.Password = request.NewPassword; // In a real application, hash the password
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok("Password has been successfully reset.");
        }

        private string GenerateResetCode()
        {
            // Securely generate a 6-digit random code
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] randomBytes = new byte[4]; // 4 bytes = 32-bit integer
                rng.GetBytes(randomBytes);

                // Generate a positive 32-bit integer (ensure the result is always positive)
                int code = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 900000 + 100000; // Generate a 6-digit code

                return code.ToString();
            }
        }

        private string HashResetCode(string resetCode)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(resetCode);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        private async Task<bool> SendEmailAsync(string email, string resetCode)
        {
            try
            {
                using var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("akkaushiik@gmail.com", "hnvf tict cfzt yhcu"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("akkaushiik@gmail.com"),
                    Subject = "Password Reset Code",
                    Body = $"Your password reset code is: {resetCode}",
                    IsBodyHtml = false,
                };

                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }

        [HttpGet("googlelogin")]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties { RedirectUri = Url.Action("GoogleResponse") };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            // Authenticate the user using the cookie authentication scheme
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (result.Succeeded)
            {
                // Extract user claims from the authenticated result
                var claims = result.Principal?.Claims.ToList();

                // Extract specific claims like Name, Email, and User ID
                var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var userId = claims?.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                // You can now use this data to generate your own token or store the user in the database
                return Ok(new { Message = "Login Successful", Name = name, Email = email, UserId = userId });
            }
            else
            {
                return BadRequest("Login Failed");
            }
        }

        [HttpDelete("delete-profile/{profileId}")]
        public async Task<IActionResult> DeleteProfile(Guid profileId)
        {
            var userProfile = await _context.UserProfiles
                .Include(p => p.User)
                .Include(p => p.Skills)
                .Include(p => p.Educations)
                .Include(p => p.WorkExperiences)
                .FirstOrDefaultAsync(p => p.ProfileId == profileId);

            if (userProfile == null)
            {
                return NotFound(new { message = "Profile not found." });
            }

            // Remove related data
            if (userProfile.Skills != null)
            {
                _context.Skills.RemoveRange(userProfile.Skills);
            }

            if (userProfile.Educations != null)
            {
                _context.Educations.RemoveRange(userProfile.Educations);
            }

            if (userProfile.WorkExperiences != null)
            {
                _context.WorkExperiences.RemoveRange(userProfile.WorkExperiences);
            }

            // Remove User if exists
            if (userProfile.User != null)
            {
                _context.Users.Remove(userProfile.User);
            }

            // Remove UserProfile
            _context.UserProfiles.Remove(userProfile);

            // Save changes to the database
            await _context.SaveChangesAsync();

            return Ok(new { message = "User and related profile data deleted successfully." });
        }




        [HttpPut("UpdateProfile/{ProfileId}")]
        public async Task<IActionResult> UpdateProfile(Guid ProfileId, [FromBody] UpdateProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userProfile = await _context.UserProfiles.FindAsync(ProfileId);
            if (userProfile == null)
            {
                return NotFound(new { message = "Profile not found." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Update user profile fields
                userProfile.Name = request.Name ?? userProfile.Name;
                userProfile.DOB = request.DOB ?? userProfile.DOB;
                userProfile.Place = request.Place ?? userProfile.Place;
                userProfile.IsStudent = request.IsStudent;
                userProfile.UpdatedAt = DateTime.UtcNow;

                _context.UserProfiles.Update(userProfile);
                await _context.SaveChangesAsync();

                // Update skills (if provided)
                if (!string.IsNullOrEmpty(request.SkillName))
                {
                    var skill = await _context.Skills.FirstOrDefaultAsync(s => s.ProfileId == ProfileId);
                    if (skill != null)
                    {
                        skill.SkillName = request.SkillName;
                        _context.Skills.Update(skill);
                    }
                    else
                    {
                        _context.Skills.Add(new Skill
                        {
                            SkillId = Guid.NewGuid(),
                            SkillName = request.SkillName,
                            ProfileId = ProfileId
                        });
                    }
                }

                // Update education (if provided)
                if (!string.IsNullOrEmpty(request.Degree))
                {
                    var education = await _context.Educations.FirstOrDefaultAsync(e => e.ProfileId == ProfileId);
                    if (education != null)
                    {
                        education.Degree = request.Degree;
                        education.FieldOfStudy = request.FieldOfStudy;
                        education.InstitutionName = request.InstitutionName;
                        education.StartYear = request.StartYear;
                        education.EndYear = request.EndYear;
                        _context.Educations.Update(education);
                    }
                    else
                    {
                        _context.Educations.Add(new Education
                        {
                            EducationId = Guid.NewGuid(),
                            Degree = request.Degree,
                            FieldOfStudy = request.FieldOfStudy,
                            InstitutionName = request.InstitutionName,
                            StartYear = request.StartYear,
                            EndYear = request.EndYear,
                            ProfileId = ProfileId
                        });
                    }
                }

                // Update work experience (if provided)
                if (!string.IsNullOrEmpty(request.Role))
                {
                    var workExperience = await _context.WorkExperiences.FirstOrDefaultAsync(w => w.ProfileId == ProfileId);
                    if (workExperience != null)
                    {
                        workExperience.Role = request.Role;
                        workExperience.YearExperience = request.YearExperience;
                        _context.WorkExperiences.Update(workExperience);
                    }
                    else
                    {
                        _context.WorkExperiences.Add(new WorkExperience
                        {
                            WorkExperienceId = Guid.NewGuid(),
                            Role = request.Role,
                            YearExperience = request.YearExperience,
                            ProfileId = ProfileId
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Profile updated successfully.", ProfileId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("get-profile/{profileId}")]
        public async Task<IActionResult> GetProfile(Guid profileId)
        {
            var userProfile = await _context.UserProfiles
                .Where(p => p.ProfileId == profileId)
                .Include(p => p.User)  // Include User details
                .Include(p => p.Skills) // Include Skills
                .Include(p => p.Educations) // Include Education
                .Include(p => p.WorkExperiences) // Include Work Experience
                .FirstOrDefaultAsync();

            if (userProfile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            var response = new
            {
                ProfileId = userProfile.ProfileId,
                Name = userProfile.Name,
                DOB = userProfile.DOB,
                Place = userProfile.Place,
                IsStudent = userProfile.IsStudent,
                User = new
                {
                    Email = userProfile.User.Email,
                    IsActive = userProfile.User.IsActive,
                },
                Skills = userProfile.Skills.Select(s => new { s.SkillId, s.SkillName }).ToList(),
                Education = userProfile.Educations.Select(e => new
                {
                    e.EducationId,
                    e.Degree,
                    e.FieldOfStudy,
                    e.InstitutionName,
                    e.StartYear,
                    e.EndYear
                }).ToList(),
                WorkExperiences = userProfile.WorkExperiences.Select(w => new
                {
                    w.WorkExperienceId,
                    w.Role,
                    w.YearExperience
                }).ToList()
            };

            return Ok(response);
        }



        //--------------------------------------------------------------DTOs--------------------------------------------------------------------------------
        // DTOs

        public class UpdateProfileRequest
        {
            public string? Name { get; set; }
            public DateOnly? DOB { get; set; }
            public string? Place { get; set; }
            public bool IsStudent { get; set; }
            public string? SkillName { get; set; }
            public string? Degree { get; set; }
            public string? FieldOfStudy { get; set; }
            public string? InstitutionName { get; set; }
            public string? StartYear { get; set; }
            public string? EndYear { get; set; }
            public string? Role { get; set; }
            public string? YearExperience { get; set; }
        }


        public class ResetRequest
        {
            public string Email { get; set; }
        }

        public class ResetPasswordRequest
        {
            public string Email { get; set; }
            public string Code { get; set; }
            public string HashedCode { get; set; }
            public string NewPassword { get; set; }
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

    }
}
