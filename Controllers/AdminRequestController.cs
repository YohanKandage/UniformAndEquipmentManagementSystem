using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string employeeName, int? departmentId, string status)
        {
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(employeeName))
            {
                query = query.Where(r => r.Employee.FirstName.Contains(employeeName) || r.Employee.LastName.Contains(employeeName));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(r => r.Employee.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            
            // Pass filter values to view
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            
            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string status, string adminComment)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (admin == null)
            {
                return NotFound();
            }

            request.Status = status;
            request.AdminComment = adminComment;
            request.ProcessedDate = DateTime.Now;
            request.ProcessedById = admin.Id;

            if (status == "Approved")
            {
                // Update item quantity
                request.Item.Quantity--;
                if (request.Item.Quantity <= 0)
                {
                    request.Item.Status = "Unavailable";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Request has been {status.ToLower()} successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }
    }
} 