using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace UniformAndEquipmentManagementSystem.Services
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RoleSeeder(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task SeedRoles()
        {
            string[] roleNames = { "Admin", "Employee", "PropertyManager", "StockManager" };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }
    }
} 