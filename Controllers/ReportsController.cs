using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Services;
using System.Data;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,PropertyManager,StockManager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExcelService _excelService;

        public ReportsController(ApplicationDbContext context, IExcelService excelService)
        {
            _context = context;
            _excelService = excelService;
        }

        public IActionResult Index()
        {
            // Redirect users to appropriate reports based on their role
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("DepartmentWiseEmployeeReport");
            }
            else if (User.IsInRole("PropertyManager"))
            {
                return RedirectToAction("EmployeeRequestReport");
            }
            else if (User.IsInRole("StockManager"))
            {
                return RedirectToAction("InventoryReport");
            }
            else
            {
                // For users without report access, redirect to dashboard
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #region Employee Reports

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DepartmentWiseEmployeeReport(string departmentName, string email, string role)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(departmentName))
            {
                query = query.Where(e => e.Department.Name.Contains(departmentName));
            }

            if (!string.IsNullOrEmpty(email))
            {
                query = query.Where(e => e.Email.Contains(email));
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(e => e.Role == role);
            }

            var employees = await query
                .Select(e => new
                {
                    DepartmentName = e.Department.Name,
                    UserName = e.UserName,
                    Email = e.Email,
                    Role = e.Role,
                    Gender = e.Gender,
                    Phone = e.Phone
                })
                .ToListAsync();

            ViewBag.Departments = await _context.Departments.Select(d => d.Name).ToListAsync();
            ViewBag.Roles = new List<string> { "Admin", "StockManager", "PropertyManager", "Employee" };
            ViewBag.DepartmentName = departmentName;
            ViewBag.Email = email;
            ViewBag.Role = role;

            return View(employees);
        }

        [Authorize(Roles = "Admin,PropertyManager,StockManager")]
        public async Task<IActionResult> EmployeeRequestReport(string employeeName, string status, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(employeeName))
            {
                query = query.Where(r => r.Employee.FirstName.Contains(employeeName) || r.Employee.LastName.Contains(employeeName));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            var requests = await query
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    Reason = r.Reason,
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    ProcessedDate = r.ProcessedDate,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            ViewBag.Statuses = Enum.GetValues(typeof(RequestStatus)).Cast<RequestStatus>().Select(s => s.ToString()).ToList();
            ViewBag.EmployeeName = employeeName;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(requests);
        }

        #endregion

        #region Property Manager Reports

        #endregion

        #region Inventory Reports

        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> InventoryReport(string itemType, int? departmentId, string status, int? minQuantity, int? maxQuantity)
        {
            var query = _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Include(i => i.AssignedTo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(i => i.ItemType == itemType);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(i => i.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "available":
                        query = query.Where(i => i.AssignedToId == null && i.Quantity > 0);
                        break;
                    case "assigned":
                        query = query.Where(i => i.AssignedToId != null);
                        break;
                    case "outofstock":
                        query = query.Where(i => i.Quantity == 0);
                        break;
                }
            }

            if (minQuantity.HasValue)
            {
                query = query.Where(i => i.Quantity >= minQuantity.Value);
            }

            if (maxQuantity.HasValue)
            {
                query = query.Where(i => i.Quantity <= maxQuantity.Value);
            }

            var items = await query
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    Quantity = i.Quantity,
                    AssignedTo = i.AssignedTo != null ? $"{i.AssignedTo.FirstName} {i.AssignedTo.LastName}" : "Not Assigned",
                    AssignedDate = i.AssignedDate,
                    Status = i.AssignedToId != null ? "Assigned" : (i.Quantity > 0 ? "Available" : "Out of Stock")
                })
                .ToListAsync();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.ItemTypes = new List<string> { "Uniform", "Equipment" };
            ViewBag.Statuses = new List<string> { "Available", "Assigned", "OutOfStock" };
            ViewBag.ItemType = itemType;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            ViewBag.MinQuantity = minQuantity;
            ViewBag.MaxQuantity = maxQuantity;

            return View(items);
        }

        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> LowStockReport(int? threshold = 10)
        {
            var items = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.Quantity <= threshold)
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    CurrentQuantity = i.Quantity,
                    Threshold = threshold.Value,
                    Status = i.Quantity == 0 ? "Out of Stock" : "Low Stock"
                })
                .OrderBy(i => i.CurrentQuantity)
                .ToListAsync();

            ViewBag.Threshold = threshold;
            return View(items);
        }

        #endregion

        #region Supplier Reports

        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> SupplierReport(string category, bool? isApproved)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.SupplierCategory == category);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(s => s.ApprovalState == (isApproved.Value ? "Approved" : "Pending"));
            }

            var suppliers = await query
                .Select(s => new
                {
                    SupplierId = s.Id,
                    CompanyName = s.CompanyName,
                    ContactPerson = s.ContactPerson,
                    Email = s.ContactPersonEmail,
                    Phone = s.ContactNo,
                    Category = s.SupplierCategory,
                    IsApproved = s.ApprovalState == "Approved",
                    ApprovalDate = s.ApprovalState == "Approved" ? s.ContractStartDate : (DateTime?)null
                })
                .ToListAsync();

            ViewBag.Categories = new List<string> { "Uniform", "Equipment", "General" };
            ViewBag.Category = category;
            ViewBag.IsApproved = isApproved;

            return View(suppliers);
        }

        [Authorize(Roles = "StockManager")]
        public async Task<IActionResult> ReleasesReport(string employeeName, int? departmentId, string itemType, 
            DateTime? startDate, DateTime? endDate, decimal? minCost, decimal? maxCost, string releasedBy)
        {
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .Where(r => r.Status == RequestStatus.ReleasedByStockManager)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(employeeName))
            {
                query = query.Where(r => r.Employee.FirstName.Contains(employeeName) || r.Employee.LastName.Contains(employeeName));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(r => r.Employee.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(r => r.Item.ItemType == itemType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            if (minCost.HasValue)
            {
                query = query.Where(r => r.Cost >= minCost.Value);
            }

            if (maxCost.HasValue)
            {
                query = query.Where(r => r.Cost <= maxCost.Value);
            }

            if (!string.IsNullOrEmpty(releasedBy))
            {
                query = query.Where(r => r.ProcessedBy.UserName.Contains(releasedBy));
            }

            var releases = await query
                .OrderByDescending(r => r.ProcessedDate)
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    RequestDate = r.RequestDate,
                    ReleaseDate = r.ProcessedDate,
                    Cost = r.Cost,
                    ReleasedBy = r.ProcessedBy.UserName,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            // Get statistics
            var totalReleases = releases.Count;
            var totalCost = releases.Where(r => r.Cost.HasValue).Sum(r => r.Cost.Value);
            var averageCost = releases.Where(r => r.Cost.HasValue).Any() ? 
                releases.Where(r => r.Cost.HasValue).Average(r => r.Cost.Value) : 0;
            var todayReleases = releases.Count(r => r.ReleaseDate?.Date == DateTime.Today);
            var thisMonthReleases = releases.Count(r => r.ReleaseDate?.Month == DateTime.Now.Month && 
                                                       r.ReleaseDate?.Year == DateTime.Now.Year);

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.ItemTypes = new List<string> { "Uniform", "Equipment" };
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.ItemType = itemType;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.MinCost = minCost;
            ViewBag.MaxCost = maxCost;
            ViewBag.ReleasedBy = releasedBy;
            ViewBag.TotalReleases = totalReleases;
            ViewBag.TotalCost = totalCost;
            ViewBag.AverageCost = averageCost;
            ViewBag.TodayReleases = todayReleases;
            ViewBag.ThisMonthReleases = thisMonthReleases;

            return View(releases);
        }

        [HttpPost]
        [Authorize(Roles = "StockManager")]
        public async Task<IActionResult> ExportReleasesReport(string employeeName, int? departmentId, string itemType, 
            DateTime? startDate, DateTime? endDate, decimal? minCost, decimal? maxCost, string releasedBy)
        {
            try
            {
                var query = _context.Requests
                    .Include(r => r.Item)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.ProcessedBy)
                    .Where(r => r.Status == RequestStatus.ReleasedByStockManager)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(employeeName))
                {
                    query = query.Where(r => r.Employee.FirstName.Contains(employeeName) || r.Employee.LastName.Contains(employeeName));
                }

                if (departmentId.HasValue)
                {
                    query = query.Where(r => r.Employee.DepartmentId == departmentId);
                }

                if (!string.IsNullOrEmpty(itemType))
                {
                    query = query.Where(r => r.Item.ItemType == itemType);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                }

                if (minCost.HasValue)
                {
                    query = query.Where(r => r.Cost >= minCost.Value);
                }

                if (maxCost.HasValue)
                {
                    query = query.Where(r => r.Cost <= maxCost.Value);
                }

                if (!string.IsNullOrEmpty(releasedBy))
                {
                    query = query.Where(r => r.ProcessedBy.UserName.Contains(releasedBy));
                }

                var releases = await query
                    .OrderByDescending(r => r.ProcessedDate)
                    .Select(r => new
                    {
                        RequestId = r.Id,
                        EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                        Department = r.Employee.Department.Name,
                        ItemName = r.Item.ItemName,
                        ItemType = r.Item.ItemType,
                        RequestDate = r.RequestDate,
                        ReleaseDate = r.ProcessedDate,
                        Cost = r.Cost,
                        ReleasedBy = r.ProcessedBy.UserName,
                        Remarks = r.Remarks ?? ""
                    })
                    .ToListAsync();

                var excelData = releases.Select(r => new
                {
                    RequestID = r.RequestId,
                    EmployeeName = r.EmployeeName,
                    Department = r.Department,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    RequestDate = r.RequestDate.ToString("MMM dd, yyyy"),
                    ReleaseDate = r.ReleaseDate?.ToString("MMM dd, yyyy HH:mm") ?? "N/A",
                    Cost = r.Cost?.ToString("F2") ?? "No Cost",
                    ReleasedBy = r.ReleasedBy,
                    Remarks = r.Remarks
                }).ToList();

                var fileName = $"ReleasesReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var excelBytes = _excelService.ExportToExcel(excelData, "Releases Report");

                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the Excel report.";
                return RedirectToAction("ReleasesReport");
            }
        }

        #endregion

        #region Request Status Reports

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate, decimal? minCost, decimal? maxCost)
        {
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .Where(r => r.ProcessedById == currentUser.Id && 
                           (r.Status == RequestStatus.ApprovedByAdmin || r.Status == RequestStatus.RejectedByAdmin))
                .AsQueryable();

            // Apply filters
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

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            if (minCost.HasValue)
            {
                query = query.Where(r => r.Cost >= minCost.Value);
            }

            if (maxCost.HasValue)
            {
                query = query.Where(r => r.Cost <= maxCost.Value);
            }

            var requests = await query
                .OrderByDescending(r => r.ProcessedDate)
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    RequestDate = r.RequestDate,
                    ProcessedDate = r.ProcessedDate,
                    Status = r.Status.ToString(),
                    Cost = r.Cost,
                    AdminComment = r.AdminComment,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            // Get statistics
            var totalProcessed = requests.Count;
            var approvedCount = requests.Count(r => r.Status == "ApprovedByAdmin");
            var rejectedCount = requests.Count(r => r.Status == "RejectedByAdmin");
            var totalCost = requests.Where(r => r.Cost.HasValue).Sum(r => r.Cost.Value);
            var averageCost = requests.Where(r => r.Cost.HasValue).Any() ? 
                requests.Where(r => r.Cost.HasValue).Average(r => r.Cost.Value) : 0;
            var todayProcessed = requests.Count(r => r.ProcessedDate?.Date == DateTime.Today);
            var thisMonthProcessed = requests.Count(r => r.ProcessedDate?.Month == DateTime.Now.Month && 
                                                       r.ProcessedDate?.Year == DateTime.Now.Year);

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Statuses = new List<string> { "ApprovedByAdmin", "RejectedByAdmin" };
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.MinCost = minCost;
            ViewBag.MaxCost = maxCost;
            ViewBag.TotalProcessed = totalProcessed;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.RejectedCount = rejectedCount;
            ViewBag.TotalCost = totalCost;
            ViewBag.AverageCost = averageCost;
            ViewBag.TodayProcessed = todayProcessed;
            ViewBag.ThisMonthProcessed = thisMonthProcessed;

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportAdminProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate, decimal? minCost, decimal? maxCost)
        {
            try
            {
                var currentUserEmail = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                
                var query = _context.Requests
                    .Include(r => r.Item)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.ProcessedBy)
                    .Where(r => r.ProcessedById == currentUser.Id && 
                               (r.Status == RequestStatus.ApprovedByAdmin || r.Status == RequestStatus.RejectedByAdmin))
                    .AsQueryable();

                // Apply filters
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

                if (startDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                }

                if (minCost.HasValue)
                {
                    query = query.Where(r => r.Cost >= minCost.Value);
                }

                if (maxCost.HasValue)
                {
                    query = query.Where(r => r.Cost <= maxCost.Value);
                }

                var requests = await query
                    .OrderByDescending(r => r.ProcessedDate)
                    .Select(r => new
                    {
                        RequestId = r.Id,
                        EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                        Department = r.Employee.Department.Name,
                        ItemName = r.Item.ItemName,
                        ItemType = r.Item.ItemType,
                        RequestDate = r.RequestDate,
                        ProcessedDate = r.ProcessedDate,
                        Status = r.Status.ToString(),
                        Cost = r.Cost,
                        AdminComment = r.AdminComment ?? "",
                        Remarks = r.Remarks ?? ""
                    })
                    .ToListAsync();

                var excelData = requests.Select(r => new
                {
                    RequestID = r.RequestId,
                    EmployeeName = r.EmployeeName,
                    Department = r.Department,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    RequestDate = r.RequestDate.ToString("MMM dd, yyyy"),
                    ProcessedDate = r.ProcessedDate?.ToString("MMM dd, yyyy HH:mm") ?? "N/A",
                    Status = r.Status,
                    Cost = r.Cost?.ToString("F2") ?? "No Cost",
                    AdminComment = r.AdminComment,
                    Remarks = r.Remarks
                }).ToList();

                var fileName = $"AdminProcessedRequestsReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var excelBytes = _excelService.ExportToExcel(excelData, "Admin Processed Requests Report");

                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the Excel report.";
                return RedirectToAction("AdminProcessedRequestsReport");
            }
        }

        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> PropertyManagerProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate)
        {
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .Where(r => r.ProcessedById == currentUser.Id && 
                           (r.Status == RequestStatus.ApprovedByPropertyManager || r.Status == RequestStatus.RejectedByPropertyManager))
                .AsQueryable();

            // Apply filters
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

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            var requests = await query
                .OrderByDescending(r => r.ProcessedDate)
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    RequestDate = r.RequestDate,
                    ProcessedDate = r.ProcessedDate,
                    Status = r.Status.ToString(),
                    Remarks = r.Remarks
                })
                .ToListAsync();

            // Get statistics
            var totalProcessed = requests.Count;
            var approvedCount = requests.Count(r => r.Status == "ApprovedByPropertyManager");
            var rejectedCount = requests.Count(r => r.Status == "RejectedByPropertyManager");
            var todayProcessed = requests.Count(r => r.ProcessedDate?.Date == DateTime.Today);
            var thisMonthProcessed = requests.Count(r => r.ProcessedDate?.Month == DateTime.Now.Month && 
                                                       r.ProcessedDate?.Year == DateTime.Now.Year);

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Statuses = new List<string> { "ApprovedByPropertyManager", "RejectedByPropertyManager" };
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalProcessed = totalProcessed;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.RejectedCount = rejectedCount;
            ViewBag.TodayProcessed = todayProcessed;
            ViewBag.ThisMonthProcessed = thisMonthProcessed;

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "PropertyManager")]
        public async Task<IActionResult> ExportPropertyManagerProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var currentUserEmail = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                
                var query = _context.Requests
                    .Include(r => r.Item)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.ProcessedBy)
                    .Where(r => r.ProcessedById == currentUser.Id && 
                               (r.Status == RequestStatus.ApprovedByPropertyManager || r.Status == RequestStatus.RejectedByPropertyManager))
                    .AsQueryable();

                // Apply filters
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

                if (startDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                }

                var requests = await query
                    .OrderByDescending(r => r.ProcessedDate)
                    .Select(r => new
                    {
                        RequestId = r.Id,
                        EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                        Department = r.Employee.Department.Name,
                        ItemName = r.Item.ItemName,
                        ItemType = r.Item.ItemType,
                        RequestDate = r.RequestDate,
                        ProcessedDate = r.ProcessedDate,
                        Status = r.Status.ToString(),
                        Remarks = r.Remarks ?? ""
                    })
                    .ToListAsync();

                var excelData = requests.Select(r => new
                {
                    RequestID = r.RequestId,
                    EmployeeName = r.EmployeeName,
                    Department = r.Department,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    RequestDate = r.RequestDate.ToString("MMM dd, yyyy"),
                    ProcessedDate = r.ProcessedDate?.ToString("MMM dd, yyyy HH:mm") ?? "N/A",
                    Status = r.Status,
                    Remarks = r.Remarks
                }).ToList();

                var fileName = $"PropertyManagerProcessedRequestsReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var excelBytes = _excelService.ExportToExcel(excelData, "Property Manager Processed Requests Report");

                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the Excel report.";
                return RedirectToAction("PropertyManagerProcessedRequestsReport");
            }
        }

        [Authorize(Roles = "StockManager")]
        public async Task<IActionResult> StockManagerProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate)
        {
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            
            var query = _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .Where(r => r.ProcessedById == currentUser.Id && r.Status == RequestStatus.ReleasedByStockManager)
                .AsQueryable();

            // Apply filters
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

            if (startDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
            }

            var requests = await query
                .OrderByDescending(r => r.ProcessedDate)
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    RequestDate = r.RequestDate,
                    ProcessedDate = r.ProcessedDate,
                    Cost = r.Cost,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            // Get statistics
            var totalReleased = requests.Count;
            var totalCost = requests.Where(r => r.Cost.HasValue).Sum(r => r.Cost.Value);
            var averageCost = requests.Where(r => r.Cost.HasValue).Any() ? 
                requests.Where(r => r.Cost.HasValue).Average(r => r.Cost.Value) : 0;
            var todayReleased = requests.Count(r => r.ProcessedDate?.Date == DateTime.Today);
            var thisMonthReleased = requests.Count(r => r.ProcessedDate?.Month == DateTime.Now.Month && 
                                                      r.ProcessedDate?.Year == DateTime.Now.Year);

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.EmployeeName = employeeName;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.TotalReleased = totalReleased;
            ViewBag.TotalCost = totalCost;
            ViewBag.AverageCost = averageCost;
            ViewBag.TodayReleased = todayReleased;
            ViewBag.ThisMonthReleased = thisMonthReleased;

            return View(requests);
        }

        [HttpPost]
        [Authorize(Roles = "StockManager")]
        public async Task<IActionResult> ExportStockManagerProcessedRequestsReport(string employeeName, int? departmentId, string status, 
            DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var currentUserEmail = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
                
                var query = _context.Requests
                    .Include(r => r.Item)
                    .Include(r => r.Employee)
                        .ThenInclude(e => e.Department)
                    .Include(r => r.ProcessedBy)
                    .Where(r => r.ProcessedById == currentUser.Id && r.Status == RequestStatus.ReleasedByStockManager)
                    .AsQueryable();

                // Apply filters
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

                if (startDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(r => r.ProcessedDate <= endDate.Value.AddDays(1).AddSeconds(-1));
                }

                var requests = await query
                    .OrderByDescending(r => r.ProcessedDate)
                    .Select(r => new
                    {
                        RequestId = r.Id,
                        EmployeeName = r.Employee.FirstName + " " + r.Employee.LastName,
                        Department = r.Employee.Department.Name,
                        ItemName = r.Item.ItemName,
                        ItemType = r.Item.ItemType,
                        RequestDate = r.RequestDate,
                        ProcessedDate = r.ProcessedDate,
                        Cost = r.Cost,
                        Remarks = r.Remarks ?? ""
                    })
                    .ToListAsync();

                var excelData = requests.Select(r => new
                {
                    RequestID = r.RequestId,
                    EmployeeName = r.EmployeeName,
                    Department = r.Department,
                    ItemName = r.ItemName,
                    ItemType = r.ItemType,
                    RequestDate = r.RequestDate.ToString("MMM dd, yyyy"),
                    ProcessedDate = r.ProcessedDate?.ToString("MMM dd, yyyy HH:mm") ?? "N/A",
                    Cost = r.Cost?.ToString("F2") ?? "No Cost",
                    Remarks = r.Remarks
                }).ToList();

                var fileName = $"StockManagerProcessedRequestsReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                var excelBytes = _excelService.ExportToExcel(excelData, "Stock Manager Processed Requests Report");

                return File(excelBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating the Excel report.";
                return RedirectToAction("StockManagerProcessedRequestsReport");
            }
        }

        #endregion

        #region Export Methods

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExportDepartmentWiseEmployeeReport(string departmentName, string email, string role)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.Department)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(departmentName))
                {
                    query = query.Where(e => e.Department.Name.Contains(departmentName));
                }

                if (!string.IsNullOrEmpty(email))
                {
                    query = query.Where(e => e.Email.Contains(email));
                }

                if (!string.IsNullOrEmpty(role))
                {
                    query = query.Where(e => e.Role == role);
                }

                var employees = await query
                    .Select(e => new
                    {
                        DepartmentName = e.Department.Name,
                        UserName = e.UserName,
                        Email = e.Email,
                        Role = e.Role,
                        Gender = e.Gender,
                        Phone = e.Phone
                    })
                    .ToListAsync();

                var dataTable = new DataTable();
                dataTable.Columns.Add("Department Name");
                dataTable.Columns.Add("Username");
                dataTable.Columns.Add("Email");
                dataTable.Columns.Add("Role");
                dataTable.Columns.Add("Gender");
                dataTable.Columns.Add("Phone");

                foreach (var employee in employees)
                {
                    dataTable.Rows.Add(
                        employee.DepartmentName,
                        employee.UserName,
                        employee.Email,
                        employee.Role,
                        employee.Gender,
                        employee.Phone
                    );
                }

                var title = $"Department Wise Employee Report - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
                var excelBytes = _excelService.ExportToExcel(dataTable, "Department Wise Employee Report", title);
                
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DepartmentWiseEmployeeReport.xlsx");
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"Excel export error: {ex.Message}");
                return BadRequest($"Error generating Excel file: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin,PropertyManager,StockManager")]
        public async Task<IActionResult> ExportEmployeeRequestReport(string employeeName, string status, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Item)
                .AsQueryable();

            if (!string.IsNullOrEmpty(employeeName))
            {
                query = query.Where(r => r.Employee.FirstName.Contains(employeeName) || r.Employee.LastName.Contains(employeeName));
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.RequestDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.RequestDate <= endDate.Value);
            }

            var requests = await query
                .Select(r => new
                {
                    RequestId = r.Id,
                    EmployeeName = $"{r.Employee.FirstName} {r.Employee.LastName}",
                    Department = r.Employee.Department.Name,
                    ItemName = r.Item.ItemName,
                    ItemType = r.Item.ItemType,
                    Reason = r.Reason,
                    RequestDate = r.RequestDate,
                    Status = r.Status.ToString(),
                    ProcessedDate = r.ProcessedDate,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Request ID");
            dataTable.Columns.Add("Employee Name");
            dataTable.Columns.Add("Department");
            dataTable.Columns.Add("Item Name");
            dataTable.Columns.Add("Item Type");
            dataTable.Columns.Add("Reason");
            dataTable.Columns.Add("Request Date");
            dataTable.Columns.Add("Status");
            dataTable.Columns.Add("Processed Date");
            dataTable.Columns.Add("Remarks");

            foreach (var request in requests)
            {
                dataTable.Rows.Add(
                    request.RequestId,
                    request.EmployeeName,
                    request.Department,
                    request.ItemName,
                    request.ItemType,
                    request.Reason,
                    request.RequestDate,
                    request.Status,
                    request.ProcessedDate,
                    request.Remarks
                );
            }

            var title = $"Employee Request Report - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
            var excelBytes = _excelService.ExportToExcel(dataTable, "Employee Request Report", title);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeRequestReport.xlsx");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> ExportInventoryReport(string itemType, int? departmentId, string status)
        {
            var query = _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Include(i => i.AssignedTo)
                .AsQueryable();

            if (!string.IsNullOrEmpty(itemType))
            {
                query = query.Where(i => i.ItemType == itemType);
            }

            if (departmentId.HasValue)
            {
                query = query.Where(i => i.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<RequestStatus>(status, out var statusEnum))
                {
                    query = query.Where(i => i.AssignedToId == null && i.Quantity > 0);
                }
            }

            var items = await query
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    Quantity = i.Quantity,
                    AssignedTo = i.AssignedTo != null ? $"{i.AssignedTo.FirstName} {i.AssignedTo.LastName}" : "Not Assigned",
                    AssignedDate = i.AssignedDate,
                    Status = i.AssignedToId != null ? "Assigned" : (i.Quantity > 0 ? "Available" : "Out of Stock")
                })
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Item ID");
            dataTable.Columns.Add("Item Name");
            dataTable.Columns.Add("Item Type");
            dataTable.Columns.Add("Department");
            dataTable.Columns.Add("Supplier");
            dataTable.Columns.Add("Quantity");
            dataTable.Columns.Add("Assigned To");
            dataTable.Columns.Add("Assigned Date");
            dataTable.Columns.Add("Status");

            foreach (var item in items)
            {
                dataTable.Rows.Add(
                    item.ItemId,
                    item.ItemName,
                    item.ItemType,
                    item.Department,
                    item.Supplier,
                    item.Quantity,
                    item.AssignedTo,
                    item.AssignedDate,
                    item.Status
                );
            }

            var title = $"Inventory Report - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
            var excelBytes = _excelService.ExportToExcel(dataTable, "Inventory Report", title);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "InventoryReport.xlsx");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> ExportLowStockReport(int? threshold = 10)
        {
            var items = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.Quantity <= threshold)
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    CurrentQuantity = i.Quantity,
                    Threshold = threshold.Value,
                    Status = i.Quantity == 0 ? "Out of Stock" : "Low Stock"
                })
                .OrderBy(i => i.CurrentQuantity)
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Item ID");
            dataTable.Columns.Add("Item Name");
            dataTable.Columns.Add("Item Type");
            dataTable.Columns.Add("Department");
            dataTable.Columns.Add("Supplier");
            dataTable.Columns.Add("Current Quantity");
            dataTable.Columns.Add("Threshold");
            dataTable.Columns.Add("Status");

            foreach (var item in items)
            {
                dataTable.Rows.Add(
                    item.ItemId,
                    item.ItemName,
                    item.ItemType,
                    item.Department,
                    item.Supplier,
                    item.CurrentQuantity,
                    item.Threshold,
                    item.Status
                );
            }

            var title = $"Low Stock Report (Threshold: {threshold}) - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
            var excelBytes = _excelService.ExportToExcel(dataTable, "Low Stock Report", title);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "LowStockReport.xlsx");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> ExportSupplierReport(string category, bool? isApproved)
        {
            var query = _context.Suppliers.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(s => s.SupplierCategory == category);
            }

            if (isApproved.HasValue)
            {
                query = query.Where(s => s.ApprovalState == (isApproved.Value ? "Approved" : "Pending"));
            }

            var suppliers = await query
                .Select(s => new
                {
                    SupplierId = s.Id,
                    CompanyName = s.CompanyName,
                    ContactPerson = s.ContactPerson,
                    Email = s.ContactPersonEmail,
                    Phone = s.ContactNo,
                    Category = s.SupplierCategory,
                    IsApproved = s.ApprovalState == "Approved",
                    ApprovalDate = s.ApprovalState == "Approved" ? s.ContractStartDate : (DateTime?)null
                })
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Supplier ID");
            dataTable.Columns.Add("Company Name");
            dataTable.Columns.Add("Contact Person");
            dataTable.Columns.Add("Email");
            dataTable.Columns.Add("Phone");
            dataTable.Columns.Add("Category");
            dataTable.Columns.Add("Is Approved");
            dataTable.Columns.Add("Approval Date");

            foreach (var supplier in suppliers)
            {
                dataTable.Rows.Add(
                    supplier.SupplierId,
                    supplier.CompanyName,
                    supplier.ContactPerson,
                    supplier.Email,
                    supplier.Phone,
                    supplier.Category,
                    supplier.IsApproved ? "Yes" : "No",
                    supplier.ApprovalDate
                );
            }

            var title = $"Supplier Report - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
            var excelBytes = _excelService.ExportToExcel(dataTable, "Supplier Report", title);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SupplierReport.xlsx");
        }

        #endregion
    }
} 