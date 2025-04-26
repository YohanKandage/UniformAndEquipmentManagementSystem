using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EmployeeController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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

                _context.Add(employee);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Employee created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewData["Departments"] = new SelectList(_context.Departments, "Id", "Name", employee.DepartmentId);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the employee: " + ex.Message);
                return View(employee);
            }
        }
    }
} 