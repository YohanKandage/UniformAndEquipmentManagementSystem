using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using UniformAndEquipmentManagementSystem.Services;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,PropertyManager")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfService _pdfService;

        public EmployeeController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            IPdfService pdfService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _pdfService = pdfService;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees.Include(e => e.Department).ToListAsync();
            return View(employees);
        }

        public IActionResult Create()
        {
            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee, IFormFile? ImageFile)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        ModelState.AddModelError(string.Empty, modelError.ErrorMessage);
                    }
                    return View(employee);
                }

                // Set default ImagePath if no image is uploaded
                employee.ImagePath = null;

                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "employees");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    employee.ImagePath = "/images/employees/" + uniqueFileName;
                }

                // Create the employee record
                _context.Add(employee);
                await _context.SaveChangesAsync();

                // Create the associated user account
                var user = new ApplicationUser
                {
                    UserName = employee.Email,
                    Email = employee.Email,
                    FirstName = employee.FirstName,
                    LastName = employee.LastName,
                    Department = employee.Department?.Name,
                    EmployeeId = employee.Id,
                    EmailConfirmed = true // Auto-confirm email for simplicity
                };

                // Use the provided password from the form
                var result = await _userManager.CreateAsync(user, employee.Password);
                if (result.Succeeded)
                {
                    // Assign the role
                    await _userManager.AddToRoleAsync(user, employee.Role);
                    TempData["Success"] = $"Employee created successfully!";
                }
                else
                {
                    // If user creation fails, delete the employee record
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                    
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                    return View(employee);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the employee: " + ex.Message);
                return View(employee);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile? ImageFile)
        {
            if (id != employee.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                return View(employee);
            }

            try
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "employees");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid() + "_" + ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageFile.CopyToAsync(fileStream);
                    }

                    employee.ImagePath = "/images/employees/" + uniqueFileName;
                }

                _context.Update(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Employees.Any(e => e.Id == employee.Id))
                    return NotFound();
                else
                    throw;
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee != null)
            {
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        public async Task<IActionResult> DownloadPdf(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
            {
                return NotFound();
            }

            var memoryStream = new MemoryStream();
            _pdfService.GenerateDocument(doc =>
            {
                // Add title
                var title = new Paragraph($"Employee Details - {employee.FirstName} {employee.LastName}")
                    .SetFontSize(20)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(20);
                doc.Add(title);

                // Personal Information Section
                var personalInfoTitle = new Paragraph("Personal Information")
                    .SetFontSize(16)
                    .SetMarginBottom(10);
                doc.Add(personalInfoTitle);

                var personalInfoTable = new Table(2).UseAllAvailableWidth();
                personalInfoTable.AddCell("Employee ID").AddCell(employee.EmployeeId);
                personalInfoTable.AddCell("Full Name").AddCell($"{employee.FirstName} {employee.LastName}");
                personalInfoTable.AddCell("Gender").AddCell(employee.Gender);
                personalInfoTable.AddCell("Date of Birth").AddCell(employee.DateOfBirth.ToString("d"));
                personalInfoTable.AddCell("NIC").AddCell(employee.NIC);
                doc.Add(personalInfoTable);
                doc.Add(new Paragraph().SetMarginBottom(20));

                // Contact Information Section
                var contactInfoTitle = new Paragraph("Contact Information")
                    .SetFontSize(16)
                    .SetMarginBottom(10);
                doc.Add(contactInfoTitle);

                var contactInfoTable = new Table(2).UseAllAvailableWidth();
                contactInfoTable.AddCell("Email").AddCell(employee.Email);
                contactInfoTable.AddCell("Phone").AddCell(employee.Phone);
                contactInfoTable.AddCell("Address").AddCell(employee.Address);
                doc.Add(contactInfoTable);
                doc.Add(new Paragraph().SetMarginBottom(20));

                // Employment Information Section
                var employmentInfoTitle = new Paragraph("Employment Information")
                    .SetFontSize(16)
                    .SetMarginBottom(10);
                doc.Add(employmentInfoTitle);

                var employmentInfoTable = new Table(2).UseAllAvailableWidth();
                employmentInfoTable.AddCell("Department").AddCell(employee.Department?.Name ?? "Not Assigned");
                employmentInfoTable.AddCell("Position").AddCell(employee.Position);
                employmentInfoTable.AddCell("Join Date").AddCell(employee.JoinDate.ToString("d"));
                employmentInfoTable.AddCell("Status").AddCell(employee.IsActive ? "Active" : "Inactive");
                doc.Add(employmentInfoTable);
            });

            return File(
                memoryStream.ToArray(),
                "application/pdf",
                $"Employee_{employee.EmployeeId}_{DateTime.Now:yyyyMMdd}.pdf"
            );
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            ViewBag.EmployeeEmail = employee.Email;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByEmailAsync(employee.Email);
            if (user == null)
            {
                return NotFound();
            }

            // Remove the old password
            var removePasswordResult = await _userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                foreach (var error in removePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.EmployeeName = $"{employee.FirstName} {employee.LastName}";
                ViewBag.EmployeeEmail = employee.Email;
                return View();
            }

            // Add the new password
            var addPasswordResult = await _userManager.AddPasswordAsync(user, newPassword);
            if (addPasswordResult.Succeeded)
            {
                TempData["Success"] = "Password has been reset successfully.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in addPasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewBag.EmployeeName = $"{employee.FirstName} {employee.LastName}";
            ViewBag.EmployeeEmail = employee.Email;
            return View();
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Email == user.Email);

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Employee model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var employee = await _context.Employees.FindAsync(model.Id);
            if (employee == null)
            {
                return NotFound();
            }

            // Only update allowed fields
            employee.FirstName = model.FirstName;
            employee.LastName = model.LastName;
            employee.Phone = model.Phone;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EmployeeExists(model.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }
    }
} 