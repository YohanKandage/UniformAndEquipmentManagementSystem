using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "StockManager")]
    public class StockManagerRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StockManagerRequestController(ApplicationDbContext context)
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

            // Show both pending requests and requests processed by this stock manager
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            if (currentUser != null)
            {
                query = query.Where(r => 
                    r.Status == RequestStatus.ApprovedByAdmin || // Pending requests
                    (r.ProcessedById == currentUser.Id && r.Status == RequestStatus.ReleasedByStockManager) // Requests processed by this stock manager
                );
            }
            else
            {
                query = query.Where(r => r.Status == RequestStatus.ApprovedByAdmin);
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

            // Ensure only Admin approved requests can be processed by Stock Manager
            if (request.Status != RequestStatus.ApprovedByAdmin)
            {
                TempData["Error"] = "This request is not ready for stock manager processing.";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string status, string stockManagerComment)
        {
            // Add logging to debug the issue
            System.Diagnostics.Debug.WriteLine($"ProcessRequest called with id: {id}, status: {status}");
            
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Ensure only Admin approved requests can be processed
            if (request.Status != RequestStatus.ApprovedByAdmin)
            {
                TempData["Error"] = "This request is not ready for stock manager processing.";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            var stockManager = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name || u.UserName == User.Identity.Name);

            if (stockManager == null)
            {
                TempData["Error"] = $"Stock manager not found. User: {User.Identity?.Name}";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
            {
                // Only allow Stock Manager to release
                if (statusEnum != RequestStatus.ReleasedByStockManager)
                {
                    TempData["Error"] = "Invalid status for stock manager processing.";
                    return RedirectToAction("Index", "StockManagerRequest");
                }

                request.Status = statusEnum;
            }
            else
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            request.Remarks = stockManagerComment;
            request.ProcessedDate = DateTime.Now;
            request.ProcessedById = stockManager.Id;

            // Update the assignment when released
            if (statusEnum == RequestStatus.ReleasedByStockManager && request.Item != null)
            {
                // Check if there's available quantity (inventory was already reduced when admin approved)
                if (request.Item.Quantity < 0)
                {
                    TempData["Error"] = "Item is out of stock. Cannot release request.";
                    return RedirectToAction("Index", "StockManagerRequest");
                }

                // Find and update the existing assignment record
                var assignment = await _context.ItemAssignments
                    .FirstOrDefaultAsync(ia => ia.RequestId == request.Id);

                if (assignment != null)
                {
                    assignment.Status = "Assigned";
                    assignment.Remarks = stockManagerComment;
                    assignment.Cost = request.Cost;
                }
                else
                {
                    // Create a new assignment record if one doesn't exist
                    assignment = new ItemAssignment
                    {
                        ItemId = request.ItemId,
                        EmployeeId = request.EmployeeId,
                        RequestId = request.Id,
                        AssignedDate = DateTime.Now,
                        Status = "Assigned",
                        Remarks = stockManagerComment,
                        Cost = request.Cost
                    };
                    _context.ItemAssignments.Add(assignment);
                }

                // Note: Inventory was already reduced when admin approved the request
                // No need to reduce it again here
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Request has been {statusEnum.ToString().ToLower()} successfully.";
                return RedirectToAction("Index", "StockManagerRequest");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving changes: {ex.Message}");
                TempData["Error"] = "An error occurred while processing the request.";
                return RedirectToAction("Index", "StockManagerRequest");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Release(int id, string? stockManagerComment = null)
        {
            // Add debugging
            System.Diagnostics.Debug.WriteLine($"Release action called with id: {id}, comment: {stockManagerComment}");
            
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            // Ensure only Admin approved requests can be processed
            if (request.Status != RequestStatus.ApprovedByAdmin)
            {
                TempData["Error"] = "This request is not ready for stock manager processing.";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            var stockManager = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name || u.UserName == User.Identity.Name);

            if (stockManager == null)
            {
                TempData["Error"] = $"Stock manager not found. User: {User.Identity?.Name}";
                return RedirectToAction("Index", "StockManagerRequest");
            }

            // Update the request status
            request.Status = RequestStatus.ReleasedByStockManager;
            request.Remarks = stockManagerComment;
            request.ProcessedDate = DateTime.Now;
            request.ProcessedById = stockManager.Id;

            // Update the assignment when released
            if (request.Item != null)
            {
                // Check if there's available quantity (inventory was already reduced when admin approved)
                if (request.Item.Quantity < 0)
                {
                    TempData["Error"] = "Item is out of stock. Cannot release request.";
                    return RedirectToAction("Index", "StockManagerRequest");
                }

                // Find and update the existing assignment record
                var assignment = await _context.ItemAssignments
                    .FirstOrDefaultAsync(ia => ia.RequestId == request.Id);

                if (assignment != null)
                {
                    assignment.Status = "Assigned";
                    assignment.Remarks = stockManagerComment;
                    assignment.Cost = request.Cost;
                }
                else
                {
                    // Create a new assignment record if one doesn't exist
                    assignment = new ItemAssignment
                    {
                        ItemId = request.ItemId,
                        EmployeeId = request.EmployeeId,
                        RequestId = request.Id,
                        AssignedDate = DateTime.Now,
                        Status = "Assigned",
                        Remarks = stockManagerComment,
                        Cost = request.Cost
                    };
                    _context.ItemAssignments.Add(assignment);
                }

                // Note: Inventory was already reduced when admin approved the request
                // No need to reduce it again here
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Request has been released successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing the request.";
            }

            return RedirectToAction("Index", "StockManagerRequest");
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