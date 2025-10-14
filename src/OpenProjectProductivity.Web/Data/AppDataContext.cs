using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Models;
using OpenProjectProductivity.Web.Models;

namespace OpenProductivity.Web.Data
{
    public class OpenProjectContext : IdentityDbContext<AuthUser>
    {
        public OpenProjectContext(DbContextOptions<OpenProjectContext> options)
            : base(options) { }

        // Domain entities
        public DbSet<User> Users => Set<User>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<WorkPackage> WorkPackages => Set<WorkPackage>();
        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Important for Identity

            // WorkPackage -> Assignee (domain user)
            modelBuilder.Entity<WorkPackage>()
                .HasOne(w => w.Assignee)
                .WithMany(u => u.AssignedWorkPackages)
                .HasForeignKey(w => w.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            // WorkPackage -> Project
            modelBuilder.Entity<WorkPackage>()
                .HasOne(w => w.Project)
                .WithMany(p => p.WorkPackages)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Activity -> WorkPackage
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.WorkPackage)
                .WithMany(w => w.Activities)
                .HasForeignKey(a => a.WorkPackageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Activity -> User (domain user)
            modelBuilder.Entity<Activity>()
                .HasOne(a => a.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
