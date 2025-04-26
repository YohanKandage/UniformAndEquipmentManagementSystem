using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
        public IActionResult EmployeeDashboard()
        {
            return View();
        }
    }
} 