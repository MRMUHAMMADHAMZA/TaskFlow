using Microsoft.EntityFrameworkCore;
using TaskManagementApp.Models;

namespace TaskManagementApp.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\MSSQLLocalDB;Database=TaskManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── USER ──
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID)
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.FullName)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(e => e.Email)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.HasIndex(e => e.Email)
                      .IsUnique();
                entity.Property(e => e.Role)
                      .HasDefaultValue("User");
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");
            });

            // ── PROJECT ──
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID)
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.ProjectName)
                      .IsRequired()
                      .HasMaxLength(150);
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Projects)
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── TASK ──
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID)
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.Title)
                      .IsRequired()
                      .HasMaxLength(150);
                entity.Property(e => e.Priority)
                      .HasDefaultValue("Medium");
                entity.Property(e => e.Status)
                      .HasDefaultValue("To Do");
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Project)
                      .WithMany(p => p.Tasks)
                      .HasForeignKey(e => e.ProjectID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(e => e.AssignedTo)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── COMMENT ──
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID)
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.Content)
                      .IsRequired()
                      .HasMaxLength(1000);
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Task)
                      .WithMany(t => t.Comments)
                      .HasForeignKey(e => e.TaskID)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── ACTIVITY LOG ──
            modelBuilder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.ID);
                entity.Property(e => e.ID)
                      .ValueGeneratedOnAdd();
                entity.Property(e => e.Action)
                      .IsRequired()
                      .HasMaxLength(255);
                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserID)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}