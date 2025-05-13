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
                .WithMany()
                .HasForeignKey(i => i.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Item>()
                .HasOne(i => i.Supplier)
                .WithMany()
                .HasForeignKey(i => i.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
    }
} 