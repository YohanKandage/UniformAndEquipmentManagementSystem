using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Services;
using System.Data;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
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
            return View();
        }

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
                    ItemName = r.Item.ItemName,
                    Reason = r.Reason,
                    RequestDate = r.RequestDate,
                    Status = r.Status
                })
                .ToListAsync();

            ViewBag.Statuses = new List<string> { "Pending", "Approved", "Cancelled" };
            ViewBag.EmployeeName = employeeName;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(requests);
        }

        [HttpPost]
        public async Task<IActionResult> ExportDepartmentWiseEmployeeReport(int? departmentId)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
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

            var excelBytes = _excelService.ExportToExcel(dataTable, "Department Wise Employee Report");
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "DepartmentWiseEmployeeReport.xlsx");
        }

        [HttpPost]
        public async Task<IActionResult> ExportEmployeeRequestReport(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Requests
                .Include(r => r.Employee)
                .Include(r => r.Item)
                .AsQueryable();

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
                    ItemName = r.Item.ItemName,
                    Reason = r.Reason,
                    RequestDate = r.RequestDate,
                    Status = r.Status
                })
                .ToListAsync();

            var dataTable = new DataTable();
            dataTable.Columns.Add("Request ID");
            dataTable.Columns.Add("Employee Name");
            dataTable.Columns.Add("Item Name");
            dataTable.Columns.Add("Reason");
            dataTable.Columns.Add("Request Date");
            dataTable.Columns.Add("Status");

            foreach (var request in requests)
            {
                dataTable.Rows.Add(
                    request.RequestId,
                    request.EmployeeName,
                    request.ItemName,
                    request.Reason,
                    request.RequestDate,
                    request.Status
                );
            }

            var excelBytes = _excelService.ExportToExcel(dataTable, "Employee Request Report");
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "EmployeeRequestReport.xlsx");
        }
    }
} 