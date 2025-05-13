using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Employee")]
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var requests = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.ProcessedBy)
                .Where(r => r.EmployeeId == employee.Id)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        public async Task<IActionResult> Create()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get available items for the employee's department
            var availableItems = await _context.Items
                .Where(i => i.DepartmentId == employee.DepartmentId && i.Status == "Available")
                .ToListAsync();

            ViewBag.AvailableItems = availableItems;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,Reason")] Request request)
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                request.EmployeeId = employee.Id;
                request.RequestDate = DateTime.Now;
                request.Status = "Pending";

                _context.Add(request);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            var availableItems = await _context.Items
                .Where(i => i.DepartmentId == employee.DepartmentId && i.Status == "Available")
                .ToListAsync();

            ViewBag.AvailableItems = availableItems;
            return View(request);
        }

        public async Task<IActionResult> AssignedItems()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var assignedItems = await _context.Items
                .Include(i => i.Department)
                .Where(i => i.AssignedToId == employee.Id)
                .ToListAsync();

            return View(assignedItems);
        }
    }
} 