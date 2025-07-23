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
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPdfService _pdfService;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(
            ApplicationDbContext context, 
            IWebHostEnvironment webHostEnvironment,
            UserManager<ApplicationUser> userManager,
            IPdfService pdfService,
            ILogger<EmployeeController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, string searchField, int? departmentId, string role)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                switch (searchField?.ToLower())
                {
                    case "name":
                        query = query.Where(e => e.FirstName.Contains(searchString) || e.LastName.Contains(searchString));
                        break;
                    case "email":
                        query = query.Where(e => e.Email.Contains(searchString));
                        break;
                    case "phone":
                        query = query.Where(e => e.Phone.Contains(searchString));
                        break;
                    default:
                        query = query.Where(e => e.FirstName.Contains(searchString) || e.LastName.Contains(searchString) || 
                                               e.Email.Contains(searchString) || e.Phone.Contains(searchString));
                        break;
                }
            }

            if (departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(e => e.Role == role);
            }

            var employees = await query.ToListAsync();

            // Pass filter values and lists to view
            ViewBag.SearchString = searchString;
            ViewBag.SearchField = searchField;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Role = role;
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Roles = new List<string> { "Admin", "StockManager", "PropertyManager", "Employee" };

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
                // Age validation - check if employee is at least 18 years old at join date
                var age = employee.JoinDate.Year - employee.DateOfBirth.Year;
                if (employee.JoinDate < employee.DateOfBirth.AddYears(age))
                    age--;

                if (age < 18)
                {
                    ModelState.AddModelError("JoinDate", $"Employee must be at least 18 years old at the time of joining. Current age would be {age} years.");
                    ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                    return View(employee);
                }

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
                    DepartmentId = employee.DepartmentId,
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

                // If employee role, redirect to item assignment
                if (employee.Role == "Employee")
                {
                    TempData["Success"] = $"Employee created successfully! You can now assign items to {employee.FirstName} {employee.LastName}.";
                    return RedirectToAction(nameof(AssignItems), new { id = employee.Id });
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

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);
                
            if (employee == null)
                return NotFound();

            // Get assigned items for this employee
            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Supplier)
                .Where(ia => ia.EmployeeId == id && ia.Status == "Assigned")
                .OrderBy(ia => ia.Item.ItemType)
                .ThenBy(ia => ia.Item.ItemName)
                .ToListAsync();

            ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
            ViewBag.AssignedItems = assignedItems;
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Employee employee, IFormFile? ImageFile)
        {
            if (id != employee.Id)
                return NotFound();

            // Age validation - check if employee is at least 18 years old at join date
            var age = employee.JoinDate.Year - employee.DateOfBirth.Year;
            if (employee.JoinDate < employee.DateOfBirth.AddYears(age))
                age--;

            if (age < 18)
            {
                ModelState.AddModelError("JoinDate", $"Employee must be at least 18 years old at the time of joining. Current age would be {age} years.");
                ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                return View(employee);
            }

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

        // Delete functionality disabled for security
        /*
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
            try
            {
                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == id);
                
                if (employee == null)
                {
                    TempData["Error"] = "Employee not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Check if employee has any active item assignments
                var activeAssignments = await _context.ItemAssignments
                    .Where(ia => ia.EmployeeId == id && ia.Status == "Assigned")
                    .CountAsync();

                if (activeAssignments > 0)
                {
                    TempData["Error"] = $"Cannot delete employee. They have {activeAssignments} active item assignment(s). Please return all assigned items first.";
                    return RedirectToAction(nameof(Index));
                }

                // Find and delete the associated user account first
                var user = await _userManager.FindByEmailAsync(employee.Email);
                if (user != null)
                {
                    var deleteUserResult = await _userManager.DeleteAsync(user);
                    if (!deleteUserResult.Succeeded)
                    {
                        TempData["Error"] = "Failed to delete user account. Employee cannot be deleted.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                // Now delete the employee record
                // Note: ItemAssignments and Requests will be automatically deleted due to cascade delete
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                
                TempData["Success"] = $"Employee '{employee.FirstName} {employee.LastName}' and associated data deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID: {EmployeeId}", id);
                TempData["Error"] = "An error occurred while deleting the employee. Please ensure all related data is properly handled.";
            }

            return RedirectToAction(nameof(Index));
        }
        */

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

            // Get assigned items for this employee
            var assignedItems = await _context.ItemAssignments
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Department)
                .Include(ia => ia.Item)
                    .ThenInclude(i => i.Supplier)
                .Where(ia => ia.EmployeeId == id && ia.Status == "Assigned")
                .OrderBy(ia => ia.Item.ItemType)
                .ThenBy(ia => ia.Item.ItemName)
                .ToListAsync();

            ViewBag.AssignedItems = assignedItems;

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

        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found for profile view");
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for user: {Email}", user.Email);
                    return RedirectToAction("Index", "Dashboard");
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee profile");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> EditProfile()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found for profile edit");
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Email == user.Email);

                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for user: {Email}", user.Email);
                    return RedirectToAction("Index", "Dashboard");
                }

                return View(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee profile for edit");
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public async Task<IActionResult> UpdateProfile(Employee model)
        {
            _logger.LogInformation("UpdateProfile action called");
            _logger.LogInformation("Request method: {Method}", Request.Method);
            _logger.LogInformation("Request path: {Path}", Request.Path);
            
            try
            {
                _logger.LogInformation("Starting profile update for employee ID: {EmployeeId}", model.Id);
                _logger.LogInformation("Model received: {@Model}", model);

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid model state for profile update");
                    foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        _logger.LogWarning("Model error: {ErrorMessage}", error.ErrorMessage);
                    }
                    
                    // Repopulate the model with department information for the view
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        var currentEmployee = await _context.Employees
                            .Include(e => e.Department)
                            .FirstOrDefaultAsync(e => e.Email == currentUser.Email);
                        if (currentEmployee != null)
                        {
                            model.Department = currentEmployee.Department;
                        }
                    }
                    
                    return View("EditProfile", model);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("User not found for profile update");
                    return RedirectToAction("Login", "Account");
                }

                var employee = await _context.Employees
                    .Include(e => e.Department)
                    .FirstOrDefaultAsync(e => e.Id == model.Id);

                if (employee == null)
                {
                    _logger.LogWarning("Employee not found for ID: {Id}", model.Id);
                    return RedirectToAction("Index", "Dashboard");
                }

                // Security: Ensure the employee belongs to the current user
                if (employee.Email != user.Email)
                {
                    _logger.LogWarning("Security violation: User {UserId} attempted to update profile {ProfileId}", user.Id, model.Id);
                    return RedirectToAction("Index", "Dashboard");
                }

                // Update employee properties
                employee.FirstName = model.FirstName;
                employee.LastName = model.LastName;
                employee.Phone = model.Phone;
                employee.NIC = model.NIC;
                employee.DateOfBirth = model.DateOfBirth;
                employee.Gender = model.Gender;
                employee.Address = model.Address;

                // Update ApplicationUser properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;

                _logger.LogInformation("Updating employee profile: {@Employee}", employee);

                _context.Entry(employee).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("Profile updated successfully for employee ID: {EmployeeId}", employee.Id);

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee profile");
                ModelState.AddModelError("", "An error occurred while updating your profile. Please try again.");
                return View("EditProfile", model);
            }
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignItems(int? id)
        {
            if (id == null)
                return NotFound();

            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            if (employee.Role != "Employee")
            {
                TempData["Error"] = "Item assignment is only available for employees.";
                return RedirectToAction(nameof(Index));
            }

            // Get available items for the employee's department
            var availableItems = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.DepartmentId == employee.DepartmentId && i.Status == "Available" && i.Quantity > 0)
                .ToListAsync();

            ViewBag.Employee = employee;
            ViewBag.AvailableItems = availableItems;

            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignItems(int id, List<int> selectedItemIds)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null)
                return NotFound();

            if (employee.Role != "Employee")
            {
                TempData["Error"] = "Item assignment is only available for employees.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (selectedItemIds != null && selectedItemIds.Any())
                {
                    foreach (var itemId in selectedItemIds)
                    {
                        var item = await _context.Items.FindAsync(itemId);
                        if (item != null && item.Quantity > 0 && item.DepartmentId == employee.DepartmentId)
                        {
                            // Create item assignment
                            var assignment = new ItemAssignment
                            {
                                ItemId = itemId,
                                EmployeeId = employee.Id,
                                AssignedDate = DateTime.Now,
                                Status = "Assigned"
                            };

                            _context.ItemAssignments.Add(assignment);

                            // Update item quantity and status
                            item.Quantity--;
                            if (item.Quantity <= 0)
                            {
                                item.Status = "Out of Stock";
                            }

                            _context.Items.Update(item);
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Successfully assigned {selectedItemIds.Count} item(s) to {employee.FirstName} {employee.LastName}.";
                }
                else
                {
                    TempData["Info"] = "No items were selected for assignment.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning items to employee {EmployeeId}", id);
                TempData["Error"] = "An error occurred while assigning items. Please try again.";
                return RedirectToAction(nameof(AssignItems), new { id = id });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetItemsByDepartment(int departmentId)
        {
            var items = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .Where(i => i.DepartmentId == departmentId && i.Status == "Available" && i.Quantity > 0)
                .Select(i => new
                {
                    i.Id,
                    i.ItemName,
                    i.ItemType,
                    i.Material,
                    i.Price,
                    i.Quantity,
                    DepartmentName = i.Department.Name,
                    SupplierName = i.Supplier.CompanyName
                })
                .ToListAsync();

            return Json(items);
        }
    }
} 