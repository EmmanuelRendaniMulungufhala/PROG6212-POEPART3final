using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ClaimModel> Claims => Set<ClaimModel>();
        public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();
        public DbSet<ClaimStatusHistory> ClaimStatusHistory => Set<ClaimStatusHistory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
                entity.Property(u => u.EmployeeId).HasMaxLength(20);
                entity.Property(u => u.Department).HasMaxLength(100);
                entity.Property(u => u.DateJoined).IsRequired();
                entity.Property(u => u.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(u => u.Role).IsRequired().HasConversion<string>();
            });

            // Configure ClaimModel
            builder.Entity<ClaimModel>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Month).IsRequired();
                entity.Property(c => c.HoursWorked).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(c => c.HourlyRate).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(c => c.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(c => c.SubmissionDate).IsRequired();
                entity.Property(c => c.Status).IsRequired().HasConversion<string>();

                // Relationships
                entity.HasOne(c => c.Lecturer)
                      .WithMany(u => u.Claims)
                      .HasForeignKey(c => c.LecturerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(c => c.SupportingDocuments)
                      .WithOne(d => d.Claim!)
                      .HasForeignKey(d => d.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.StatusHistory)
                      .WithOne(h => h.Claim!)
                      .HasForeignKey(h => h.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Indexes
                entity.HasIndex(c => c.LecturerId);
                entity.HasIndex(c => c.Status);
                entity.HasIndex(c => c.SubmissionDate);
                entity.HasIndex(c => c.Month);
            });

            // Configure SupportingDocument
            builder.Entity<SupportingDocument>(entity =>
            {
                entity.HasKey(d => d.Id);
                entity.Property(d => d.OriginalFileName).IsRequired().HasMaxLength(255);
                entity.Property(d => d.FileName).IsRequired().HasMaxLength(255);
                entity.Property(d => d.FileSize).IsRequired();
                entity.Property(d => d.ContentType).IsRequired().HasMaxLength(100);
                entity.Property(d => d.UploadDate).IsRequired();
                entity.Property(d => d.UploadedBy).IsRequired().HasMaxLength(100);

                entity.HasIndex(d => d.ClaimId);
            });

            // Configure ClaimStatusHistory
            builder.Entity<ClaimStatusHistory>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.OldStatus).IsRequired().HasConversion<string>();
                entity.Property(h => h.NewStatus).IsRequired().HasConversion<string>();
                entity.Property(h => h.ChangedDate).IsRequired();
                entity.Property(h => h.ChangedBy).HasMaxLength(100);

                entity.HasIndex(h => h.ClaimId);
                entity.HasIndex(h => h.ChangedDate);
            });
        }
    }
}