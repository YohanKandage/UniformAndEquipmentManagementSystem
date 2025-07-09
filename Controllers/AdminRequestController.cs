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

            // Show both pending requests and requests processed by this admin
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            if (currentUser != null)
            {
                query = query.Where(r => 
                    r.Status == RequestStatus.ApprovedByPropertyManager || // Pending requests
                    (r.ProcessedById == currentUser.Id && (r.Status == RequestStatus.ApprovedByAdmin || r.Status == RequestStatus.RejectedByAdmin)) // Requests processed by this admin
                );
            }
            else
            {
                query = query.Where(r => r.Status == RequestStatus.ApprovedByPropertyManager);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string status, string adminComment, decimal? cost)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.ApprovedByPropertyManager)
            {
                TempData["Error"] = "This request is not ready for admin processing.";
                return RedirectToAction(nameof(Index));
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (admin == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
            {
                if (statusEnum != RequestStatus.ApprovedByAdmin && statusEnum != RequestStatus.RejectedByAdmin)
                {
                    TempData["Error"] = "Invalid status for admin processing.";
                    return RedirectToAction(nameof(Index));
                }

                request.Status = statusEnum;
            }
            else
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            request.AdminComment = adminComment;
            request.Cost = cost;
            request.ProcessedDate = DateTime.Now;
            request.ProcessedById = admin.Id;

            if (statusEnum == RequestStatus.ApprovedByAdmin)
            {
                request.Item.Quantity--;
                if (request.Item.Quantity <= 0)
                {
                    request.Item.Status = "Unavailable";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Request has been {statusEnum.ToString().ToLower()} successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Process(int id)
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

            // Ensure only Property Manager approved requests can be processed
            if (request.Status != RequestStatus.ApprovedByPropertyManager)
            {
                TempData["Error"] = "This request is not ready for admin processing.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
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

        [HttpGet]
        public async Task<IActionResult> ViewAssignedItems(int requestId)
        {
            var request = await _context.Requests
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            // Get all item assignments for this employee
            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Supplier)
                .Include(ia => ia.Employee)
                .Where(ia => ia.EmployeeId == request.EmployeeId && ia.Status == "Assigned")
                .OrderByDescending(ia => ia.AssignedDate)
                .ToListAsync();

            ViewBag.Employee = request.Employee;
            ViewBag.RequestId = requestId;

            return View(assignedItems);
        }

        [HttpGet]
        public async Task<IActionResult> ViewItemSummary(int requestId)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                    .ThenInclude(i => i.Department)
                .Include(r => r.Item)
                    .ThenInclude(i => i.Supplier)
                .Include(r => r.Employee)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
            {
                return NotFound();
            }

            // Get similar items for price comparison
            var similarItems = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.ItemType == request.Item.ItemType && 
                           i.DepartmentId == request.Item.DepartmentId &&
                           i.Id != request.Item.Id)
                .Take(5)
                .ToListAsync();

            ViewBag.SimilarItems = similarItems;
            ViewBag.RequestId = requestId;

            return View(request);
        }
    }
} 