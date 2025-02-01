using Microsoft.EntityFrameworkCore;

namespace API_V2._0.Data
{
    public class UPMDbContext:DbContext
    {
        public UPMDbContext(DbContextOptions<UPMDbContext> options) : base(options)
        {
        }
        //Dbset
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<Education> Educations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // User - UserProfile (1-to-1 relationship)
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(up => up.User)
                .HasForeignKey<UserProfile>(up => up.UserId);

            // UserProfile - Skills (1-to-Many relationship)
            modelBuilder.Entity<UserProfile>()
                .HasMany(up => up.Skills)
                .WithOne(s => s.UserProfile)
                .HasForeignKey(s => s.ProfileId);

            // UserProfile - WorkExperiences (1-to-Many relationship)
            modelBuilder.Entity<UserProfile>()
                .HasMany(up => up.WorkExperiences)
                .WithOne(we => we.UserProfile)
                .HasForeignKey(we => we.ProfileId);

            // UserProfile - Educations (1-to-Many relationship)
            modelBuilder.Entity<UserProfile>()
                .HasMany(up => up.Educations)
                .WithOne(e => e.UserProfile)
                .HasForeignKey(e => e.ProfileId);
        }
    }
}
