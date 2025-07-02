using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "PropertyManager")]
    public class PropertyManagerRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PropertyManagerRequestController(ApplicationDbContext context)
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

            // Show both pending requests and requests processed by this property manager
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            if (currentUser != null)
            {
                query = query.Where(r => 
                    r.Status == RequestStatus.Pending || // Pending requests
                    (r.ProcessedById == currentUser.Id && (r.Status == RequestStatus.ApprovedByPropertyManager || r.Status == RequestStatus.RejectedByPropertyManager)) // Requests processed by this property manager
                );
            }
            else
            {
                query = query.Where(r => r.Status == RequestStatus.Pending);
            }

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
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }
            }

            var requests = await query.OrderByDescending(r => r.RequestDate).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            
            // Pass filter values to view
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            
            return View(requests);
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