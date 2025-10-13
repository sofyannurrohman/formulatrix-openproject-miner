using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Models;

namespace OpenProductivity.Web.Data
{
    public class OpenProjectContext : DbContext
    {
        public OpenProjectContext(DbContextOptions<OpenProjectContext> options)
            : base(options) { }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<User> Users => Set<User>();
        public DbSet<WorkPackage> WorkPackages => Set<WorkPackage>();
        public DbSet<Activity> Activities => Set<Activity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // WorkPackage -> Assignee
            modelBuilder.Entity<WorkPackage>()
                .HasOne(w => w.Assignee)
                .WithMany(u => u.AssignedWorkPackages)
                .HasForeignKey(w => w.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull); // avoid FK issues if user deleted

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

            // Activity -> User (optional)
            modelBuilder.Entity<Activity>()
    .HasOne(a => a.User)
    .WithMany(u => u.Activities)   // assuming User has ICollection<Activity> Activities
    .HasForeignKey(a => a.UserId)
    .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
