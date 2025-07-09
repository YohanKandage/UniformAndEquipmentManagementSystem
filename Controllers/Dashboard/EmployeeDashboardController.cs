using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize]
    public class EmployeeDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmployeeDashboardController> _logger;

        public EmployeeDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<EmployeeDashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found in EmployeeDashboard");
                    return RedirectToAction("Login", "Account");
                }

                // Get user roles
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("Employee"))
                {
                    _logger.LogWarning($"User {user.Email} does not have Employee role. Roles: {string.Join(", ", userRoles)}");
                    return RedirectToAction("AccessDenied", "Account");
                }

                // Get employee data
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    _logger.LogWarning($"No employee record found for user {user.Email}");
                    // Create a basic view model with user info
                    var viewModel = new Employee
                    {
                        FirstName = user.FirstName ?? "User",
                        LastName = user.LastName ?? "",
                        Email = user.Email,
                        Department = new Department { Name = user.Department?.Name ?? "Not Assigned" },
                        IsActive = true,
                        Position = "Employee",
                        EmployeeId = user.EmployeeId.ToString()
                    };
                    return View(viewModel);
                }

                // Get real-time dashboard data
                var dashboardData = await GetDashboardData(employee.Id);

                ViewBag.DashboardData = dashboardData;
                _logger.LogInformation($"Successfully loaded dashboard for employee {employee.FirstName} {employee.LastName}");
                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employee dashboard");
                return RedirectToAction("Error", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { error = "User not found" });
                }

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return Json(new { error = "Employee not found" });
                }

                var dashboardData = await GetDashboardData(employee.Id);
                return Json(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { error = "Error loading dashboard data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentActivities()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { error = "User not found" });
                }

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    return Json(new { error = "Employee not found" });
                }

                var recentActivities = await GetRecentActivitiesData(employee.Id);
                return Json(recentActivities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return Json(new { error = "Error loading recent activities" });
            }
        }

        private async Task<object> GetDashboardData(int employeeId)
        {
            // Get assigned items count
            var assignedItemsCount = await _context.ItemAssignments
                .Where(ia => ia.EmployeeId == employeeId && ia.Status == "Assigned")
                .CountAsync();

            // Get pending requests count
            var pendingRequestsCount = await _context.Requests
                .Where(r => r.EmployeeId == employeeId && r.Status == RequestStatus.Pending)
                .CountAsync();

            // Get approved requests count
            var approvedRequestsCount = await _context.Requests
                .Where(r => r.EmployeeId == employeeId && 
                           (r.Status == RequestStatus.ApprovedByPropertyManager || 
                            r.Status == RequestStatus.ApprovedByAdmin || 
                            r.Status == RequestStatus.ReleasedByStockManager))
                .CountAsync();

            // Get rejected/cancelled requests count
            var rejectedRequestsCount = await _context.Requests
                .Where(r => r.EmployeeId == employeeId && 
                           (r.Status == RequestStatus.RejectedByPropertyManager || 
                            r.Status == RequestStatus.RejectedByAdmin))
                .CountAsync();

            // Get uniforms and equipment breakdown
            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                .Where(ia => ia.EmployeeId == employeeId && ia.Status == "Assigned")
                .ToListAsync();

            var uniformsCount = assignedItems.Count(ia => ia.Item.ItemType == "Uniform");
            var equipmentCount = assignedItems.Count(ia => ia.Item.ItemType == "Equipment");

            // Get this month's stats
            var thisMonth = DateTime.Now.Month;
            var thisYear = DateTime.Now.Year;
            var thisMonthAssignments = await _context.ItemAssignments
                .Where(ia => ia.EmployeeId == employeeId && 
                            ia.AssignedDate.Month == thisMonth && 
                            ia.AssignedDate.Year == thisYear)
                .CountAsync();

            return new
            {
                assignedItemsCount,
                pendingRequestsCount,
                approvedRequestsCount,
                rejectedRequestsCount,
                uniformsCount,
                equipmentCount,
                thisMonthAssignments,
                lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private async Task<object> GetRecentActivitiesData(int employeeId)
        {
            // Get recent requests
            var recentRequests = await _context.Requests
                .Include(r => r.Item)
                .Where(r => r.EmployeeId == employeeId)
                .OrderByDescending(r => r.RequestDate)
                .Take(5)
                .Select(r => new
                {
                    type = "Request",
                    title = $"Request for {r.Item.ItemName}",
                    description = r.Reason,
                    time = r.RequestDate,
                    status = r.Status.ToString(),
                    statusClass = GetStatusClass(r.Status)
                })
                .ToListAsync();

            // Get recent assignments
            var recentAssignments = await _context.ItemAssignments
                .Include(ia => ia.Item)
                .Where(ia => ia.EmployeeId == employeeId)
                .OrderByDescending(ia => ia.AssignedDate)
                .Take(5)
                .Select(ia => new
                {
                    type = "Assignment",
                    title = $"{ia.Item.ItemName} Assigned",
                    description = $"Item assigned on {ia.AssignedDate:MMM dd, yyyy}",
                    time = ia.AssignedDate,
                    status = ia.Status,
                    statusClass = ia.Status == "Assigned" ? "approved" : "pending"
                })
                .ToListAsync();

            // Combine and sort by time
            var allActivities = recentRequests.Concat(recentAssignments)
                .OrderByDescending(a => a.time)
                .Take(10)
                .Select(a => new
                {
                    a.type,
                    a.title,
                    a.description,
                    timeAgo = GetTimeAgo(a.time),
                    a.status,
                    a.statusClass
                })
                .ToList();

            return new
            {
                activities = allActivities,
                lastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        private string GetStatusClass(RequestStatus status)
        {
            return status switch
            {
                RequestStatus.Pending => "pending",
                RequestStatus.ApprovedByPropertyManager => "approved",
                RequestStatus.ApprovedByAdmin => "approved",
                RequestStatus.ReleasedByStockManager => "completed",
                RequestStatus.RejectedByPropertyManager => "negative",
                RequestStatus.RejectedByAdmin => "negative",
                _ => "pending"
            };
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalDays >= 1)
            {
                var days = (int)timeSpan.TotalDays;
                return days == 1 ? "1 day ago" : $"{days} days ago";
            }
            else if (timeSpan.TotalHours >= 1)
            {
                var hours = (int)timeSpan.TotalHours;
                return hours == 1 ? "1 hour ago" : $"{hours} hours ago";
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                var minutes = (int)timeSpan.TotalMinutes;
                return minutes == 1 ? "1 minute ago" : $"{minutes} minutes ago";
            }
            else
            {
                return "Just now";
            }
        }
    }
} 