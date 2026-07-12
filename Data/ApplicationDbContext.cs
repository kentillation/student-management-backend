using Microsoft.EntityFrameworkCore;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure RefreshToken
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure User
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50);
                entity.Property(e => e.LastName).HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Seed roles with deterministic IDs (match migrations)
            var adminRoleId = "bf4f4642-3251-40cf-98b1-aeff812658b1";
            var teacherRoleId = "07094013-e51f-47ff-af6e-9257ad663277";
            var studentRoleId = "7f7b6805-0ec9-4dff-a539-0251c8efcefc";

            modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().HasData(
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "39994b62-d8cd-455d-9abc-fd4384a47cf0"
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = teacherRoleId,
                    Name = "Teacher",
                    NormalizedName = "TEACHER",
                    ConcurrencyStamp = "8c448de7-b1ca-42d8-b8d2-1f953e26b8a4"
                },
                new Microsoft.AspNetCore.Identity.IdentityRole
                {
                    Id = studentRoleId,
                    Name = "Student",
                    NormalizedName = "STUDENT",
                    ConcurrencyStamp = "9a9686a9-aa2c-4aa0-9197-d4440a31a01a"
                }
            );

            // Seed Student data
            modelBuilder.Entity<Student>().HasData(
                new Student
                {
                    Id = 1,
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    Phone = "123-456-7890",
                    Course = "Computer Science",
                    EnrollmentDate = new DateTime(2026, 1, 12, 22, 52, 35, 685, DateTimeKind.Local).AddTicks(5787),
                    GPA = 3.8
                },
                new Student
                {
                    Id = 2,
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    Phone = "098-765-4321",
                    Course = "Mathematics",
                    EnrollmentDate = new DateTime(2026, 4, 12, 22, 52, 35, 689, DateTimeKind.Local).AddTicks(9364),
                    GPA = 3.5
                }
            );
        }
    }
}