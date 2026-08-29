using Microsoft.EntityFrameworkCore;
using TalentRecruitment.API.Models;

namespace TalentRecruitment.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<Recruiter> Recruiters => Set<Recruiter>();
        public DbSet<Applicant> Applicants => Set<Applicant>();
        public DbSet<JobPost> JobPosts => Set<JobPost>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ---------- User ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
                entity.Property(u => u.Role).HasConversion<string>();
                entity.Property(u => u.AuthProvider).HasConversion<string>();
            });

            // ---------- Company ----------
            modelBuilder.Entity<Company>(entity =>
            {
                entity.Property(c => c.Name).IsRequired().HasMaxLength(300);
            });

            // ---------- Recruiter (1:1 with User, N:1 with Company) ----------
            modelBuilder.Entity<Recruiter>(entity =>
            {
                entity.HasOne(r => r.User)
                      .WithOne(u => u.Recruiter)
                      .HasForeignKey<Recruiter>(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Company)
                      .WithMany(c => c.Recruiters)
                      .HasForeignKey(r => r.CompanyId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Applicant (1:1 with User) ----------
            modelBuilder.Entity<Applicant>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithOne(u => u.Applicant)
                      .HasForeignKey<Applicant>(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- JobPost ----------
            modelBuilder.Entity<JobPost>(entity =>
            {
                entity.Property(j => j.Title).IsRequired().HasMaxLength(300);
                entity.Property(j => j.Status).HasConversion<string>();
                entity.Property(j => j.EmploymentType).HasConversion<string>();
                entity.Property(j => j.SalaryMin).HasColumnType("decimal(12,2)");
                entity.Property(j => j.SalaryMax).HasColumnType("decimal(12,2)");

                entity.HasOne(j => j.Company)
                      .WithMany(c => c.JobPosts)
                      .HasForeignKey(j => j.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(j => j.Recruiter)
                      .WithMany(r => r.JobPosts)
                      .HasForeignKey(j => j.RecruiterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- JobApplication ----------
            modelBuilder.Entity<JobApplication>(entity =>
            {
                entity.Property(ja => ja.Status).HasConversion<string>();

                entity.HasOne(ja => ja.JobPost)
                      .WithMany(j => j.JobApplications)
                      .HasForeignKey(ja => ja.JobPostId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ja => ja.Applicant)
                      .WithMany(a => a.JobApplications)
                      .HasForeignKey(ja => ja.ApplicantId)
                      .OnDelete(DeleteBehavior.Restrict);

                // An applicant may only apply once per job post.
                entity.HasIndex(ja => new { ja.JobPostId, ja.ApplicantId }).IsUnique();
            });
        }
    }
}
