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

        [Authorize(Roles = "Admin,PropertyManager")]
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
                query = query.Where(r => r.Status == status);
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
                    Status = r.Status,
                    ProcessedDate = r.ProcessedDate,
                    Remarks = r.Remarks
                })
                .ToListAsync();

            ViewBag.Statuses = new List<string> { "Pending", "Approved", "Cancelled" };
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

        #endregion

        #region Request Status Reports

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
        [Authorize(Roles = "Admin,PropertyManager")]
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
                query = query.Where(r => r.Status == status);
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
                    Status = r.Status,
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