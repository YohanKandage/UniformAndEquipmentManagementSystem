using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Models;

namespace UniformAndEquipmentManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Item relationships
            builder.Entity<Item>()
                .HasOne(i => i.Department)
                .WithMany(d => d.Items)
                .HasForeignKey(i => i.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .HasOne(i => i.Supplier)
                .WithMany(s => s.Items)
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            // Set default value for Status
            builder.Entity<Item>()
                .Property(i => i.Status)
                .HasDefaultValue("Available");

            // Configure ApplicationUser relationship with Department
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany()
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure RequestStatus enum
            builder.Entity<Request>()
                .Property(r => r.Status)
                .HasConversion<string>()
                .HasDefaultValue(RequestStatus.Pending);
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<ItemAssignment> ItemAssignments { get; set; }
    }
} 