using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public DashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Contains("Admin"))
            {
                return RedirectToAction("AdminDashboard");
            }
            else if (roles.Contains("Manager"))
            {
                return RedirectToAction("ManagerDashboard");
            }
            else
            {
                return RedirectToAction("EmployeeDashboard");
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Manager")]
        public IActionResult ManagerDashboard()
        {
            return View();
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EmployeeDashboard()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get employee's assigned items
            var assignedItems = await _context.Items
                .Include(i => i.Department)
                .Where(i => i.AssignedToId == employee.Id)
                .ToListAsync();

            // Get request statistics
            var requests = await _context.Requests
                .Where(r => r.EmployeeId == employee.Id)
                .ToListAsync();

            var requestStats = new
            {
                Pending = requests.Count(r => r.Status == "Pending"),
                Approved = requests.Count(r => r.Status == "Approved"),
                Cancelled = requests.Count(r => r.Status == "Cancelled")
            };

            ViewBag.Employee = employee;
            ViewBag.AssignedItems = assignedItems;
            ViewBag.RequestStats = requestStats;

            return View(employee);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Profile()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }
    }
} 