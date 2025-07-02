using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
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
            else if (roles.Contains("PropertyManager"))
            {
                return RedirectToAction("Index", "PropertyManager");
            }
            else if (roles.Contains("StockManager"))
            {
                return RedirectToAction("StockManagerDashboard");
            }
            else if (roles.Contains("Employee"))
            {
                return RedirectToAction("EmployeeDashboard");
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                // Get total employees count
                var totalEmployees = await _context.Employees.CountAsync();

                // Get total requests count
                var totalRequests = await _context.Requests.CountAsync();

                // Get pending requests count
                var pendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending);

                // Get low stock items (items with quantity less than 10)
                var lowStockItems = await _context.Items.CountAsync(i => i.Quantity < 10);

                // Get department-wise employee count
                var departmentStats = await _context.Departments
                    .Select(d => new
                    {
                        DepartmentName = d.Name,
                        EmployeeCount = _context.Employees.Count(e => e.DepartmentId == d.Id)
                    })
                    .ToListAsync();

                // Get uniform statistics
                var uniformStats = await _context.Items
                    .Where(i => i.ItemType == "Uniform")
                    .GroupBy(i => i.Status ?? "Unknown")
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get equipment statistics
                var equipmentStats = await _context.Items
                    .Where(i => i.ItemType == "Equipment")
                    .GroupBy(i => i.Status ?? "Unknown")
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get recent activities (last 5 requests)
                var recentActivities = await _context.Requests
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.Item)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new
                    {
                        Title = $"New {r.Item.ItemType} Request - {r.Employee.Department.Name}",
                        Description = $"{r.Employee.FirstName} {r.Employee.LastName} requested {r.Item.ItemName}",
                        Time = r.RequestDate,
                        Status = r.Status.ToString()
                    })
                    .ToListAsync();

                // Get request status distribution for chart
                var requestStatusDistribution = await _context.Requests
                    .GroupBy(r => r.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.TotalRequests = totalRequests;
                ViewBag.PendingRequests = pendingRequests;
                ViewBag.LowStockItems = lowStockItems;
                ViewBag.DepartmentStats = departmentStats;
                ViewBag.UniformStats = uniformStats;
                ViewBag.EquipmentStats = equipmentStats;
                ViewBag.RecentActivities = recentActivities;
                ViewBag.RequestStatusDistribution = requestStatusDistribution;

                return View();
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "Error occurred while loading admin dashboard");
                
                // Return error view or handle gracefully
                return View("Error");
            }
        }

        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> PropertyManagerDashboard()
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

                // Get total employees count
                var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive);

                // Get pending requests count
                var pendingRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.Pending);

                // Get total inventory count
                var totalInventory = await _context.Items.CountAsync();

                // Get total suppliers count
                var totalSuppliers = await _context.Suppliers.CountAsync();

                // Get uniform statistics
                var totalUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform");
                var availableUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform" && i.Quantity > 0);
                var assignedUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform" && i.AssignedToId != null);

                // Get equipment statistics
                var totalEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment");
                var availableEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment" && i.Quantity > 0);
                var assignedEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment" && i.AssignedToId != null);

                // Get low stock items (quantity less than 10)
                var lowStockItems = await _context.Items.CountAsync(i => i.Quantity < 10);

                // Get today's requests
                var todayRequests = await _context.Requests
                    .CountAsync(r => r.RequestDate.Date == DateTime.Today);

                // Get this month's requests
                var thisMonthRequests = await _context.Requests
                    .CountAsync(r => r.RequestDate.Month == DateTime.Today.Month && r.RequestDate.Year == DateTime.Today.Year);

                // Get request status distribution
                var requestStatusDistribution = await _context.Requests
                    .GroupBy(r => r.Status)
                    .Select(g => new
                    {
                        Status = g.Key.ToString(),
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get recent requests (last 5)
                var recentRequests = await _context.Requests
                    .Include(r => r.Employee)
                    .Include(r => r.Item)
                    .OrderByDescending(r => r.RequestDate)
                    .Take(5)
                    .Select(r => new
                    {
                        RequestType = r.Item.ItemType,
                        EmployeeName = $"{r.Employee.FirstName} {r.Employee.LastName}",
                        ItemName = r.Item.ItemName,
                        RequestDate = r.RequestDate,
                        Status = r.Status.ToString()
                    })
                    .ToListAsync();

                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.PendingRequests = pendingRequests;
                ViewBag.TotalInventory = totalInventory;
                ViewBag.TotalSuppliers = totalSuppliers;
                ViewBag.TotalUniforms = totalUniforms;
                ViewBag.AvailableUniforms = availableUniforms;
                ViewBag.AssignedUniforms = assignedUniforms;
                ViewBag.TotalEquipment = totalEquipment;
                ViewBag.AvailableEquipment = availableEquipment;
                ViewBag.AssignedEquipment = assignedEquipment;
                ViewBag.LowStockItems = lowStockItems;
                ViewBag.TodayRequests = todayRequests;
                ViewBag.ThisMonthRequests = thisMonthRequests;
                ViewBag.RequestStatusDistribution = requestStatusDistribution;
                ViewBag.RecentRequests = recentRequests;

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading property manager dashboard");
                return View("Error");
            }
        }

        [Authorize(Roles = "StockManager")]
        public async Task<IActionResult> StockManagerDashboard()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                // If no employee record found, create a default one for display purposes
                if (employee == null)
                {
                    // Create a default employee object for StockManager users who might not have Employee records
                    employee = new Employee
                    {
                        FirstName = "Stock",
                        LastName = "Manager",
                        Email = userEmail ?? "stockmanager@company.com",
                        Department = new Department { Name = "Stock Management" }
                    };
                }

                // Get total inventory count
                var totalInventory = await _context.Items.CountAsync();

                // Get low stock items (quantity less than 10)
                var lowStockItems = await _context.Items.CountAsync(i => i.Quantity < 10);

                // Get out of stock items
                var outOfStockItems = await _context.Items.CountAsync(i => i.Quantity == 0);

                // Get total suppliers count
                var totalSuppliers = await _context.Suppliers.CountAsync();

                // Get uniform statistics
                var totalUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform");
                var availableUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform" && i.Quantity > 0);
                var lowStockUniforms = await _context.Items.CountAsync(i => i.ItemType == "Uniform" && i.Quantity < 10 && i.Quantity > 0);

                // Get equipment statistics
                var totalEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment");
                var availableEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment" && i.Quantity > 0);
                var lowStockEquipment = await _context.Items.CountAsync(i => i.ItemType == "Equipment" && i.Quantity < 10 && i.Quantity > 0);

                // Get department-wise inventory count
                var departmentInventory = await _context.Departments
                    .Select(d => new
                    {
                        DepartmentName = d.Name,
                        ItemCount = _context.Items.Count(i => i.DepartmentId == d.Id)
                    })
                    .ToListAsync();

                // Get supplier-wise item count
                var supplierStats = await _context.Suppliers
                    .Select(s => new
                    {
                        SupplierName = s.CompanyName,
                        ItemCount = _context.Items.Count(i => i.SupplierId == s.Id)
                    })
                    .OrderByDescending(s => s.ItemCount)
                    .Take(5)
                    .ToListAsync();

                // Get recent low stock items
                var recentLowStockItems = await _context.Items
                    .Include(i => i.Department)
                    .Include(i => i.Supplier)
                    .Where(i => i.Quantity < 10)
                    .OrderBy(i => i.Quantity)
                    .Take(5)
                    .Select(i => new
                    {
                        ItemName = i.ItemName,
                        ItemType = i.ItemType,
                        Quantity = i.Quantity,
                        Department = i.Department != null ? i.Department.Name : "Unknown",
                        Supplier = i.Supplier != null ? i.Supplier.CompanyName : "Unknown"
                    })
                    .ToListAsync();

                // Get inventory status distribution
                var inventoryStatus = await _context.Items
                    .GroupBy(i => i.Quantity == 0 ? "Out of Stock" : (i.Quantity < 10 ? "Low Stock" : "In Stock"))
                    .Select(g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                    .ToListAsync();

                // Get request statistics for Stock Manager
                var pendingReleaseRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.ApprovedByAdmin);
                var totalReleasedRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.ReleasedByStockManager);
                var todayReleasedRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.ReleasedByStockManager && 
                                                                                   r.ProcessedDate.HasValue && 
                                                                                   r.ProcessedDate.Value.Date == DateTime.Today);
                var thisMonthReleasedRequests = await _context.Requests.CountAsync(r => r.Status == RequestStatus.ReleasedByStockManager && 
                                                                                       r.ProcessedDate.HasValue && 
                                                                                       r.ProcessedDate.Value.Month == DateTime.Now.Month &&
                                                                                       r.ProcessedDate.Value.Year == DateTime.Now.Year);

                ViewBag.TotalInventory = totalInventory;
                ViewBag.LowStockItems = lowStockItems;
                ViewBag.OutOfStockItems = outOfStockItems;
                ViewBag.TotalSuppliers = totalSuppliers;
                ViewBag.TotalUniforms = totalUniforms;
                ViewBag.AvailableUniforms = availableUniforms;
                ViewBag.LowStockUniforms = lowStockUniforms;
                ViewBag.TotalEquipment = totalEquipment;
                ViewBag.AvailableEquipment = availableEquipment;
                ViewBag.LowStockEquipment = lowStockEquipment;
                ViewBag.DepartmentInventory = departmentInventory;
                ViewBag.SupplierStats = supplierStats;
                ViewBag.RecentLowStockItems = recentLowStockItems;
                ViewBag.InventoryStatus = inventoryStatus;
                ViewBag.PendingReleaseRequests = pendingReleaseRequests;
                ViewBag.TotalReleasedRequests = totalReleasedRequests;
                ViewBag.TodayReleasedRequests = todayReleasedRequests;
                ViewBag.ThisMonthReleasedRequests = thisMonthReleasedRequests;

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading stock manager dashboard");
                return View("Error");
            }
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
                Pending = requests.Count(r => r.Status == RequestStatus.Pending),
                Approved = requests.Count(r => r.Status == RequestStatus.ApprovedByPropertyManager),
                Cancelled = requests.Count(r => r.Status == RequestStatus.RejectedByPropertyManager)
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