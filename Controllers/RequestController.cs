using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Employee,PropertyManager,StockManager")]
    public class RequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RequestController> _logger;

        public RequestController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            ILogger<RequestController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string employeeName, string department, string status, string searchString)
        {
            var user = await _userManager.GetUserAsync(User);
            var userEmail = user?.Email;

            if (User.IsInRole("PropertyManager"))
            {
                var query = _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.Item)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(employeeName))
                {
                    query = query.Where(r => 
                        (r.Employee.FirstName + " " + r.Employee.LastName).Contains(employeeName));
                }

                if (!string.IsNullOrEmpty(department))
                {
                    query = query.Where(r => r.Employee.Department.Name == department);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                    {
                        query = query.Where(r => r.Status == statusEnum);
                    }
                }

                if (!string.IsNullOrEmpty(searchString))
                {
                    query = query.Where(r =>
                        r.Employee.FirstName.Contains(searchString) ||
                        r.Employee.LastName.Contains(searchString) ||
                        r.Employee.Department.Name.Contains(searchString) ||
                        r.Item.ItemName.Contains(searchString) ||
                        r.Reason.Contains(searchString));
                }

                var requests = await query.ToListAsync();

                // Get all departments
                var departments = await _context.Departments
                    .OrderBy(d => d.Name)
                    .ToListAsync();

                // Pass filter values and departments to view
                ViewBag.EmployeeName = employeeName;
                ViewBag.Department = department;
                ViewBag.Status = status;
                ViewBag.SearchString = searchString;
                ViewBag.Departments = departments;

                return View("AllRequests", requests);
            }
            else
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (employee == null)
                {
                    return NotFound();
                }

                var requests = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.Item)
                    .Where(r => r.EmployeeId == employee.Id)
                    .ToListAsync();

                return View(requests);
            }
        }

        public async Task<IActionResult> Create()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get department-specific items count for initial display
            var uniformCount = await _context.Items
                .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                               i.ItemType == "Uniform" && 
                               i.Status == "Available");

            var equipmentCount = await _context.Items
                .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                               i.ItemType == "Equipment" && 
                               i.Status == "Available");

            ViewBag.UniformCount = uniformCount;
            ViewBag.EquipmentCount = equipmentCount;
            ViewBag.DepartmentName = employee.Department?.Name;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByType(string type)
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var items = await _context.Items
                .Where(i => i.DepartmentId == employee.DepartmentId && 
                           i.ItemType == type && 
                           i.Status == "Available")
                .Select(i => new {
                    id = i.Id,
                    itemName = i.ItemName,
                    imagePath = i.ImagePath ?? "/images/default-item.png",
                    price = i.Price,
                    details = $"Department: {employee.Department.Name} | Material: {i.Material} | Price: Rs. {i.Price:F2} | Available: {i.Quantity}" + 
                             (i.Price > 1000 ? "\n\nNote: Please handover the previous fault items within 7 days to proceed the request" : "")
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,Reason")] Request request, string Size, List<IFormFile> ProofImages)
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (employee == null)
                {
                    return NotFound();
                }

                // Log the incoming request data
                Console.WriteLine($"Debug - Request Data:");
                Console.WriteLine($"ItemId: {request.ItemId}");
                Console.WriteLine($"Reason: {request.Reason}");
                Console.WriteLine($"Size: {Size}");
                Console.WriteLine($"EmployeeId: {employee.Id}");

                // Validate that the requested item belongs to the employee's department
                var selectedItem = await _context.Items
                    .FirstOrDefaultAsync(i => i.Id == request.ItemId && i.DepartmentId == employee.DepartmentId);

                if (selectedItem == null)
                {
                    ModelState.AddModelError("ItemId", "The selected item is not available for your department");
                    return View(request);
                }

                // Log the selected item
                Console.WriteLine($"Debug - Selected Item:");
                Console.WriteLine($"ItemType: {selectedItem.ItemType}");
                Console.WriteLine($"ItemName: {selectedItem.ItemName}");
                Console.WriteLine($"DepartmentId: {selectedItem.DepartmentId}");

                // Validate size for uniform requests
                if (selectedItem.ItemType == "Uniform" && string.IsNullOrEmpty(Size))
                {
                    ModelState.AddModelError("Size", "Size is required for uniform requests");
                    return View(request);
                }

                // Validate quantity
                if (selectedItem.Quantity <= 0)
                {
                    ModelState.AddModelError("ItemId", "This item is currently out of stock");
                    return View(request);
                }

                // Set required fields
                request.EmployeeId = employee.Id;
                request.RequestDate = DateTime.Now;
                request.Status = RequestStatus.Pending;

                // Add size and department to the reason if it's a uniform request
                if (selectedItem.ItemType == "Uniform")
                {
                    request.Reason = $"Department: {employee.Department.Name}\nSize: {Size}\n\nReason: {request.Reason}";
                }
                else
                {
                    request.Reason = $"Department: {employee.Department.Name}\n\nReason: {request.Reason}";
                }

                // Handle proof images
                if (ProofImages != null && ProofImages.Count > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "proofs");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    for (int i = 0; i < Math.Min(ProofImages.Count, 3); i++)
                    {
                        var file = ProofImages[i];
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            var relativePath = "/images/proofs/" + uniqueFileName;
                            if (i == 0) request.ProofImage1 = relativePath;
                            if (i == 1) request.ProofImage2 = relativePath;
                            if (i == 2) request.ProofImage3 = relativePath;
                        }
                    }
                }

                // Log the final request object
                Console.WriteLine($"Debug - Final Request Object:");
                Console.WriteLine($"EmployeeId: {request.EmployeeId}");
                Console.WriteLine($"ItemId: {request.ItemId}");
                Console.WriteLine($"RequestDate: {request.RequestDate}");
                Console.WriteLine($"Status: {request.Status}");
                Console.WriteLine($"Reason: {request.Reason}");

                _context.Add(request);
                var result = await _context.SaveChangesAsync();
                Console.WriteLine($"Debug - SaveChanges Result: {result}");

                if (result > 0)
                {
                    TempData["Success"] = "Request submitted successfully!";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "Failed to save the request. Please try again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating request: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while submitting your request. Please try again.");
            }
            return View(request);
        }

        public async Task<IActionResult> AssignedItems()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Where(ia => ia.EmployeeId == employee.Id && ia.Status == "Assigned")
                .ToListAsync();

            return View(assignedItems);
        }

        public async Task<IActionResult> MyInventory()
        {
            var userEmail = User.Identity?.Name;
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == userEmail);

            if (employee == null)
            {
                return NotFound();
            }

            // Get approved requests for this employee
            var assignedItems = await _context.Requests
                .Include(r => r.Item)
                .Where(r => r.EmployeeId == employee.Id && r.Status == RequestStatus.ApprovedByPropertyManager)
                .OrderByDescending(r => r.ProcessedDate)
                .ToListAsync();

            return View(assignedItems);
        }

        // GET: Request/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Item)
                .Include(r => r.ProcessedBy)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        // GET: Request/Process/5
        [Authorize(Roles = "PropertyManager,StockManager")]
        public async Task<IActionResult> Process(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var request = await _context.Requests
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            return View(request);
        }

        // POST: Request/Process/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "PropertyManager,StockManager")]
        public async Task<IActionResult> Process(int id, string status, string remarks)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["Error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            var userEmail = User.Identity?.Name;
            var processedBy = await _userManager.FindByEmailAsync(userEmail);

            if (processedBy == null)
            {
                return NotFound();
            }

            if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
            {
                request.Status = statusEnum;
            }
            else
            {
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction(nameof(Index));
            }

            request.Remarks = remarks;
            request.ProcessedById = processedBy.Id;
            request.ProcessedDate = DateTime.Now;

            if (statusEnum == RequestStatus.ApprovedByPropertyManager && request.Item != null)
            {
                // Update item quantity
                request.Item.Quantity--;
                if (request.Item.Quantity == 0)
                {
                    request.Item.Status = "Out of Stock";
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Request has been {statusEnum.ToString().ToLower()} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request {RequestId}", id);
                TempData["Error"] = "An error occurred while processing the request.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 