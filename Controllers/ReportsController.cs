using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Services;
using System.Data;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Layout.Borders;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,PropertyManager,StockManager")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IExcelService _excelService;
        private readonly IPdfService _pdfService;

        public ReportsController(ApplicationDbContext context, IExcelService excelService, IPdfService pdfService)
        {
            _context = context;
            _excelService = excelService;
            _pdfService = pdfService;
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
                        query = query.Where(i => i.Quantity > 0);
                        break;
                    case "assigned":
                        // Items that have active assignments
                        var assignedItemIds = await _context.ItemAssignments
                            .Where(ia => ia.Status == "Assigned")
                            .Select(ia => ia.ItemId)
                            .ToListAsync();
                        query = query.Where(i => assignedItemIds.Contains(i.Id));
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

            var items = await query.ToListAsync();

            // Get assignment information for each item
            var itemAssignments = await _context.ItemAssignments
                .Include(ia => ia.Employee)
                .Where(ia => ia.Status == "Assigned")
                .ToListAsync();

            var result = items.Select(i => {
                var assignment = itemAssignments.FirstOrDefault(ia => ia.ItemId == i.Id);
                return new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    Quantity = i.Quantity,
                    ThresholdQuantity = i.ThresholdQuantity,
                    AssignedTo = assignment?.Employee != null ? $"{assignment.Employee.FirstName} {assignment.Employee.LastName}" : "Not Assigned",
                    AssignedDate = assignment?.AssignedDate,
                    Status = assignment != null ? "Assigned" : (i.Quantity > 0 ? "Available" : "Out of Stock")
                };
            }).ToList();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.ItemTypes = new List<string> { "Uniform", "Equipment" };
            ViewBag.Statuses = new List<string> { "Available", "Assigned", "OutOfStock" };
            ViewBag.ItemType = itemType;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Status = status;
            ViewBag.MinQuantity = minQuantity;
            ViewBag.MaxQuantity = maxQuantity;

            return View(result);
        }

        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> LowStockReport()
        {
            var items = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.Quantity <= i.ThresholdQuantity)
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    CurrentQuantity = i.Quantity,
                    Threshold = i.ThresholdQuantity,
                    Status = i.Quantity == 0 ? "Out of Stock" : "Low Stock"
                })
                .OrderBy(i => i.CurrentQuantity)
                .ToListAsync();

            ViewBag.TotalItems = items.Count;
            ViewBag.OutOfStockItems = items.Count(i => i.CurrentQuantity == 0);
            ViewBag.LowStockItems = items.Count(i => i.CurrentQuantity > 0);
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
        [Authorize(Roles = "Admin,PropertyManager,StockManager")]
        public async Task<IActionResult> ExportEmployeeRequestReportPdf(string employeeName, string status, DateTime? startDate, DateTime? endDate)
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

            // Get current user for signature
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var preparedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "N/A";

            var pdfBytes = _pdfService.GenerateDocument(doc =>
            {
                // Header with FMI Logo and Company Information
                var headerTable = new Table(3).UseAllAvailableWidth();
                headerTable.SetMarginBottom(30);
                
                // FMI Logo Section (Blue background with FMI text)
                var logoCell = new Cell().Add(
                    new Paragraph("FMI").AddStyle(_pdfService.GetHeaderTitleStyle())
                );
                logoCell.SetWidth(120);
                logoCell.SetHeight(80);
                logoCell.SetBackgroundColor(ColorConstants.BLUE);
                logoCell.SetBorder(null);
                logoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Company Information Section
                var companyInfoCell = new Cell().Add(
                    new Paragraph("Facilities Management Integrated (Pvt) Ltd.").AddStyle(_pdfService.GetHeaderSubtitleStyle())
                ).Add(
                    new Paragraph("Telephone: (+94) 11 59 40740").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Address: No. 490, Oceanica Towers, Colombo 03").AddStyle(_pdfService.GetCompanyInfoStyle())
                );
                companyInfoCell.SetBorder(null);
                companyInfoCell.SetTextAlignment(TextAlignment.LEFT);
                companyInfoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Document Number Section
                var documentNumberCell = new Cell().Add(
                    new Paragraph($"Document #: {DateTime.Now:yyyyMMddHHmmss}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Date: {DateTime.Now:MMM dd, yyyy}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Time: {DateTime.Now:HH:mm}").AddStyle(_pdfService.GetDocumentNumberStyle())
                );
                documentNumberCell.SetBorder(null);
                documentNumberCell.SetTextAlignment(TextAlignment.RIGHT);
                documentNumberCell.SetVerticalAlignment(VerticalAlignment.TOP);
                
                headerTable.AddCell(logoCell);
                headerTable.AddCell(companyInfoCell);
                headerTable.AddCell(documentNumberCell);
                doc.Add(headerTable);

                // Filter Information Section
                var filterTable = new Table(2).UseAllAvailableWidth();
                filterTable.SetMarginBottom(20);
                filterTable.SetBorder(null);
                
                AddTableRow(filterTable, "Start Date:", startDate?.ToString("MMM dd, yyyy") ?? "ALL");
                AddTableRow(filterTable, "Status:", !string.IsNullOrEmpty(status) ? status : "ALL");
                AddTableRow(filterTable, "End Date:", endDate?.ToString("MMM dd, yyyy") ?? "ALL");
                
                doc.Add(filterTable);

                // Document Title
                doc.Add(new Paragraph("Request Report").AddStyle(_pdfService.GetTitleStyle()));
                
                // Decorative line
                var line = new Paragraph("").SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 2)).SetMarginBottom(20);
                doc.Add(line);

                // Request Details Table
                if (requests.Any())
                {
                    var requestTable = new Table(6).UseAllAvailableWidth();
                    requestTable.SetMarginBottom(25);
                    
                    // Add headers
                    var headers = new[] { "Request ID", "Employee", "Department", "Item", "Request Date", "Status" };
                    foreach (var header in headers)
                    {
                        var headerCell = new Cell().Add(new Paragraph(header).AddStyle(_pdfService.GetTableHeaderStyle()));
                        headerCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                        requestTable.AddCell(headerCell);
                    }
                    
                    // Add data rows
                    foreach (var request in requests.Take(50)) // Limit to 50 rows for PDF
                    {
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.RequestId.ToString()).AddStyle(_pdfService.GetTableContentStyle())));
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.EmployeeName).AddStyle(_pdfService.GetTableContentStyle())));
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.Department).AddStyle(_pdfService.GetTableContentStyle())));
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.ItemName).AddStyle(_pdfService.GetTableContentStyle())));
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.RequestDate.ToString("MMM dd, yyyy")).AddStyle(_pdfService.GetTableContentStyle())));
                        requestTable.AddCell(new Cell().Add(new Paragraph(request.Status).AddStyle(_pdfService.GetTableContentStyle())));
                    }
                    
                    doc.Add(requestTable);
                    
                    if (requests.Count > 50)
                    {
                        doc.Add(new Paragraph($"Note: Showing first 50 of {requests.Count} requests").AddStyle(_pdfService.GetNormalStyle()));
                    }
                }
                else
                {
                    doc.Add(new Paragraph("No requests found matching the specified criteria.").AddStyle(_pdfService.GetNormalStyle()));
                }

                // Signature Footer with Dotted Lines
                doc.Add(new Paragraph("").SetMarginTop(40));
                
                var signatureTable = new Table(3).UseAllAvailableWidth();
                signatureTable.SetMarginTop(20);
                
                // Prepared By
                var preparedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Prepared By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                preparedByCell.SetBorder(null);
                preparedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Authorized By
                var authorizedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Authorized By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                authorizedByCell.SetBorder(null);
                authorizedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Issued Date
                var issuedDateCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Issued Date").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                issuedDateCell.SetBorder(null);
                issuedDateCell.SetTextAlignment(TextAlignment.CENTER);
                
                signatureTable.AddCell(preparedByCell);
                signatureTable.AddCell(authorizedByCell);
                signatureTable.AddCell(issuedDateCell);
                
                doc.Add(signatureTable);
            });

            return File(pdfBytes, "application/pdf", $"EmployeeRequestReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        private void AddTableRow(Table table, string label, string value)
        {
            var labelCell = new Cell().Add(new Paragraph(label).AddStyle(_pdfService.GetTableHeaderStyle()));
            labelCell.SetBorder(null);
            labelCell.SetPadding(10);
            labelCell.SetWidth(150);
            labelCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
            
            var valueCell = new Cell().Add(new Paragraph(value).AddStyle(_pdfService.GetTableContentStyle()));
            valueCell.SetBorder(null);
            valueCell.SetPadding(10);
            valueCell.SetBackgroundColor(ColorConstants.WHITE);
            
            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> ExportInventoryReport(string itemType, int? departmentId, string status)
        {
            var query = _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
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
                        query = query.Where(i => i.Quantity > 0);
                        break;
                    case "assigned":
                        // Items that have active assignments
                        var assignedItemIds = await _context.ItemAssignments
                            .Where(ia => ia.Status == "Assigned")
                            .Select(ia => ia.ItemId)
                            .ToListAsync();
                        query = query.Where(i => assignedItemIds.Contains(i.Id));
                        break;
                    case "outofstock":
                        query = query.Where(i => i.Quantity == 0);
                        break;
                }
            }

            var items = await query.ToListAsync();

            // Get assignment information for each item
            var itemAssignments = await _context.ItemAssignments
                .Include(ia => ia.Employee)
                .Where(ia => ia.Status == "Assigned")
                .ToListAsync();

            var result = items.Select(i => {
                var assignment = itemAssignments.FirstOrDefault(ia => ia.ItemId == i.Id);
                return new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    Quantity = i.Quantity,
                    AssignedTo = assignment?.Employee != null ? $"{assignment.Employee.FirstName} {assignment.Employee.LastName}" : "Not Assigned",
                    AssignedDate = assignment?.AssignedDate,
                    Status = assignment != null ? "Assigned" : (i.Quantity > 0 ? "Available" : "Out of Stock")
                };
            }).ToList();

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

            foreach (var item in result)
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
        public async Task<IActionResult> ExportInventoryReportPdf(string itemType, int? departmentId, string status)
        {
            var query = _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
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
                        query = query.Where(i => i.Quantity > 0);
                        break;
                    case "assigned":
                        // Items that have active assignments
                        var assignedItemIds = await _context.ItemAssignments
                            .Where(ia => ia.Status == "Assigned")
                            .Select(ia => ia.ItemId)
                            .ToListAsync();
                        query = query.Where(i => assignedItemIds.Contains(i.Id));
                        break;
                    case "outofstock":
                        query = query.Where(i => i.Quantity == 0);
                        break;
                }
            }

            var items = await query.ToListAsync();

            // Get assignment information for each item
            var itemAssignments = await _context.ItemAssignments
                .Include(ia => ia.Employee)
                .Where(ia => ia.Status == "Assigned")
                .ToListAsync();

            var result = items.Select(i => {
                var assignment = itemAssignments.FirstOrDefault(ia => ia.ItemId == i.Id);
                return new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    Quantity = i.Quantity,
                    AssignedTo = assignment?.Employee != null ? $"{assignment.Employee.FirstName} {assignment.Employee.LastName}" : "Not Assigned",
                    AssignedDate = assignment?.AssignedDate,
                    Status = assignment != null ? "Assigned" : (i.Quantity > 0 ? "Available" : "Out of Stock")
                };
            }).ToList();

            // Get current user for signature
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);
            var preparedBy = currentUser != null ? $"{currentUser.FirstName} {currentUser.LastName}" : "N/A";

            // Get department name for filter display
            var departmentName = "ALL";
            if (departmentId.HasValue)
            {
                var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId.Value);
                departmentName = department?.Name ?? "ALL";
            }

            var pdfBytes = _pdfService.GenerateDocument(doc =>
            {
                // Header with FMI Logo and Company Information
                var headerTable = new Table(3).UseAllAvailableWidth();
                headerTable.SetMarginBottom(30);
                
                // FMI Logo Section (Blue background with FMI text)
                var logoCell = new Cell().Add(
                    new Paragraph("FMI").AddStyle(_pdfService.GetHeaderTitleStyle())
                );
                logoCell.SetWidth(120);
                logoCell.SetHeight(80);
                logoCell.SetBackgroundColor(ColorConstants.BLUE);
                logoCell.SetBorder(null);
                logoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Company Information Section
                var companyInfoCell = new Cell().Add(
                    new Paragraph("Facilities Management Integrated (Pvt) Ltd.").AddStyle(_pdfService.GetHeaderSubtitleStyle())
                ).Add(
                    new Paragraph("Telephone: (+94) 11 59 40740").AddStyle(_pdfService.GetCompanyInfoStyle())
                ).Add(
                    new Paragraph("Address: No. 490, Oceanica Towers, Colombo 03").AddStyle(_pdfService.GetCompanyInfoStyle())
                );
                companyInfoCell.SetBorder(null);
                companyInfoCell.SetTextAlignment(TextAlignment.LEFT);
                companyInfoCell.SetVerticalAlignment(VerticalAlignment.MIDDLE);
                
                // Document Number Section
                var documentNumberCell = new Cell().Add(
                    new Paragraph($"Document #: {DateTime.Now:yyyyMMddHHmmss}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Date: {DateTime.Now:MMM dd, yyyy}").AddStyle(_pdfService.GetDocumentNumberStyle())
                ).Add(
                    new Paragraph($"Time: {DateTime.Now:HH:mm}").AddStyle(_pdfService.GetDocumentNumberStyle())
                );
                documentNumberCell.SetBorder(null);
                documentNumberCell.SetTextAlignment(TextAlignment.RIGHT);
                documentNumberCell.SetVerticalAlignment(VerticalAlignment.TOP);
                
                headerTable.AddCell(logoCell);
                headerTable.AddCell(companyInfoCell);
                headerTable.AddCell(documentNumberCell);
                doc.Add(headerTable);

                // Filter Information Section
                var filterTable = new Table(2).UseAllAvailableWidth();
                filterTable.SetMarginBottom(20);
                filterTable.SetBorder(null);
                
                AddTableRow(filterTable, "Item Type:", !string.IsNullOrEmpty(itemType) ? itemType : "ALL");
                AddTableRow(filterTable, "Department:", departmentName);
                AddTableRow(filterTable, "Status:", !string.IsNullOrEmpty(status) ? status : "ALL");
                
                doc.Add(filterTable);

                // Document Title
                doc.Add(new Paragraph("Inventory Report").AddStyle(_pdfService.GetTitleStyle()));
                
                // Decorative line
                var line = new Paragraph("").SetBorderBottom(new SolidBorder(ColorConstants.BLUE, 2)).SetMarginBottom(20);
                doc.Add(line);

                // Inventory Details Table
                if (result.Any())
                {
                    var inventoryTable = new Table(6).UseAllAvailableWidth();
                    inventoryTable.SetMarginBottom(25);
                    
                    // Add headers
                    var headers = new[] { "Item ID", "Item Name", "Type", "Department", "Quantity", "Status" };
                    foreach (var header in headers)
                    {
                        var headerCell = new Cell().Add(new Paragraph(header).AddStyle(_pdfService.GetTableHeaderStyle()));
                        headerCell.SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                        inventoryTable.AddCell(headerCell);
                    }
                    
                    // Add data rows
                    foreach (var item in result.Take(50)) // Limit to 50 rows for PDF
                    {
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.ItemId.ToString()).AddStyle(_pdfService.GetTableContentStyle())));
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.ItemName).AddStyle(_pdfService.GetTableContentStyle())));
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.ItemType).AddStyle(_pdfService.GetTableContentStyle())));
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.Department).AddStyle(_pdfService.GetTableContentStyle())));
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.Quantity.ToString()).AddStyle(_pdfService.GetTableContentStyle())));
                        inventoryTable.AddCell(new Cell().Add(new Paragraph(item.Status).AddStyle(_pdfService.GetTableContentStyle())));
                    }
                    
                    doc.Add(inventoryTable);
                    
                    if (result.Count > 50)
                    {
                        doc.Add(new Paragraph($"Note: Showing first 50 of {result.Count} items").AddStyle(_pdfService.GetNormalStyle()));
                    }
                }
                else
                {
                    doc.Add(new Paragraph("No inventory items found matching the specified criteria.").AddStyle(_pdfService.GetNormalStyle()));
                }

                // Signature Footer with Dotted Lines
                doc.Add(new Paragraph("").SetMarginTop(40));
                
                var signatureTable = new Table(3).UseAllAvailableWidth();
                signatureTable.SetMarginTop(20);
                
                // Prepared By
                var preparedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Prepared By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                preparedByCell.SetBorder(null);
                preparedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Authorized By
                var authorizedByCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Authorized By").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                authorizedByCell.SetBorder(null);
                authorizedByCell.SetTextAlignment(TextAlignment.CENTER);
                
                // Issued Date
                var issuedDateCell = new Cell().Add(
                    new Paragraph("").SetBorderBottom(new DottedBorder(ColorConstants.BLACK, 1)).SetHeight(30)
                ).Add(
                    new Paragraph("Issued Date").AddStyle(_pdfService.GetSignatureLabelStyle())
                );
                issuedDateCell.SetBorder(null);
                issuedDateCell.SetTextAlignment(TextAlignment.CENTER);
                
                signatureTable.AddCell(preparedByCell);
                signatureTable.AddCell(authorizedByCell);
                signatureTable.AddCell(issuedDateCell);
                
                doc.Add(signatureTable);
            });

            return File(pdfBytes, "application/pdf", $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        [HttpPost]
        [Authorize(Roles = "Admin,StockManager,PropertyManager")]
        public async Task<IActionResult> ExportLowStockReport()
        {
            var items = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.Quantity <= i.ThresholdQuantity)
                .Select(i => new
                {
                    ItemId = i.Id,
                    ItemName = i.ItemName,
                    ItemType = i.ItemType,
                    Department = i.Department.Name,
                    Supplier = i.Supplier.CompanyName,
                    CurrentQuantity = i.Quantity,
                    Threshold = i.ThresholdQuantity,
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

            var title = $"Low Stock Report - Generated on {DateTime.Now:MMM dd, yyyy HH:mm}";
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