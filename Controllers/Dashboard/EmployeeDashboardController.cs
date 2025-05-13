using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmployeeDashboardController> _logger;

        public EmployeeDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmployeeDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found in EmployeeDashboard");
                    return RedirectToAction("Login", "Account");
                }

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Employee"))
                {
                    _logger.LogWarning($"User {user.Email} does not have Employee role. Roles: {string.Join(", ", userRoles)}");
                    return RedirectToAction("AccessDenied", "Account");
                }

                // Get employee data
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    _logger.LogWarning($"No employee record found for user {user.Email}");
                    // Create a basic view model with user info
                    var viewModel = new Employee
                    {
                        FirstName = user.FirstName ?? "User",
                        LastName = user.LastName ?? "",
                        Email = user.Email,
                        Department = new Department { Name = user.Department ?? "Not Assigned" },
                        IsActive = true,
                        Position = "Employee",
                        EmployeeId = user.EmployeeId.ToString()
                    };
                    return View(viewModel);
                }

                _logger.LogInformation($"Successfully loaded dashboard for employee {employee.FirstName} {employee.LastName}");
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee dashboard");
                return RedirectToAction("Error", "Home");
            }
        }
    }
} 