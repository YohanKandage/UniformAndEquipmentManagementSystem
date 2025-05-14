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
                .Include(e => e.Department)
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

            // Clear any existing TempData messages
            if (TempData["Success"] != null)
            {
                TempData["Success"] = null;
            }

            return View(requests);
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
                    details = $"Department: {employee.Department.Name} | Material: {i.Material} | Price: ${i.Price:F2} | Available: {i.Quantity}"
                })
                .ToListAsync();

            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ItemId,Reason")] Request request, string Size)
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
                request.Status = "Pending";

                // Add size and department to the reason if it's a uniform request
                if (selectedItem.ItemType == "Uniform")
                {
                    request.EmployeeId = employee.Id;
                    request.RequestDate = DateTime.Now;
                    request.Status = "Pending";

                    // Add size and department to the reason if it's a uniform request
                    if (selectedItem.ItemType == "Uniform")
                    {
                        request.Reason = $"Department: {employee.Department.Name}\nSize: {Size}\n\nReason: {request.Reason}";
                    }
                    else
                    {
                        request.Reason = $"Department: {employee.Department.Name}\n\nReason: {request.Reason}";
                    }

                    try
                    {
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
                }
                else
                {
                    // Log validation errors
                    foreach (var modelStateEntry in ModelState.Values)
                    {
                        foreach (var error in modelStateEntry.Errors)
                        {
                            Console.WriteLine($"Validation Error: {error.ErrorMessage}");
                        }
                    }
                }

                // If we got this far, something failed, redisplay form
                ViewBag.UniformCount = await _context.Items
                    .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                                   i.ItemType == "Uniform" && 
                                   i.Status == "Available");

                ViewBag.EquipmentCount = await _context.Items
                    .CountAsync(i => i.DepartmentId == employee.DepartmentId && 
                                   i.ItemType == "Equipment" && 
                                   i.Status == "Available");

                ViewBag.DepartmentName = employee.Department?.Name;
                return View(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View(request);
            }
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

            var assignedItems = await _context.Items
                .Include(i => i.Department)
                .Where(i => i.AssignedToId == employee.Id)
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
                .Where(r => r.EmployeeId == employee.Id && r.Status == "Approved")
                .OrderByDescending(r => r.ProcessedDate)
                .ToListAsync();

            return View(assignedItems);
        }
    }
} 