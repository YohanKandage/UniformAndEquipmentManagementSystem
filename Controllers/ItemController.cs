using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,StockManager,PropertyManager")]
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ItemController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string itemType, int? departmentId, int? supplierId)
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
            if (supplierId.HasValue)
            {
                query = query.Where(i => i.SupplierId == supplierId);
            }

            var items = await query.ToListAsync();
            ViewBag.ItemType = itemType;
            ViewBag.DepartmentId = departmentId;
            ViewBag.SupplierId = supplierId;
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Suppliers = await _context.Suppliers.ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name");
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "CompanyName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, IFormFile? ImageFile)
        {
            try
            {
                // Verify that the Department and Supplier exist
                var departmentExists = await _context.Departments.AnyAsync(d => d.Id == item.DepartmentId);
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == item.SupplierId);

                if (!departmentExists)
                {
                    ModelState.AddModelError("DepartmentId", "Selected department does not exist.");
                }
                if (!supplierExists)
                {
                    ModelState.AddModelError("SupplierId", "Selected supplier does not exist.");
                }

                // Log the incoming model state
                if (!ModelState.IsValid)
                {
                    foreach (var entry in ModelState)
                    {
                        if (entry.Value.Errors.Any())
                        {
                            foreach (var error in entry.Value.Errors)
                            {
                                Console.WriteLine($"Validation Error - Field: {entry.Key}, Error: {error.ErrorMessage}");
                            }
                        }
                    }
                }

                // Log the item properties
                Console.WriteLine($"Debug - ItemType: {item.ItemType}");
                Console.WriteLine($"Debug - ItemId: {item.ItemId}");
                Console.WriteLine($"Debug - ItemName: {item.ItemName}");
                Console.WriteLine($"Debug - DepartmentId: {item.DepartmentId}");
                Console.WriteLine($"Debug - SupplierId: {item.SupplierId}");
                Console.WriteLine($"Debug - Material: {item.Material}");
                Console.WriteLine($"Debug - Price: {item.Price}");
                Console.WriteLine($"Debug - Quantity: {item.Quantity}");

                if (ModelState.IsValid)
                {
                    // Set default status
                    item.Status = "Available";

                    // Handle image upload
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "items");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fileStream);
                        }

                        item.ImagePath = "/images/items/" + uniqueFileName;
                    }

                    try
                    {
                        // Add the item to the context
                        _context.Add(item);
                        await _context.SaveChangesAsync();
                        
                        TempData["Success"] = "Item created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException dbEx)
                    {
                        ModelState.AddModelError("", $"Database Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                    }
                }

                // If we got this far, something failed, redisplay form
                ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", item.DepartmentId);
                ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "CompanyName", item.SupplierId);
                return View(item);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error creating item: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                ModelState.AddModelError("", "An error occurred while creating the item. Please try again.");
                ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", item.DepartmentId);
                ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "CompanyName", item.SupplierId);
                return View(item);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", item.DepartmentId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "CompanyName", item.SupplierId);
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Item item, IFormFile? ImageFile)
        {
            if (id != item.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "items");
                        if (!Directory.Exists(uploadsFolder))
                            Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + ImageFile.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImageFile.CopyToAsync(fileStream);
                        }

                        // Delete old image if it exists
                        if (!string.IsNullOrEmpty(item.ImagePath))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImagePath.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        item.ImagePath = "/images/items/" + uniqueFileName;
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Item updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ItemExists(item.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["DepartmentId"] = new SelectList(_context.Departments, "Id", "Name", item.DepartmentId);
            ViewData["SupplierId"] = new SelectList(_context.Suppliers, "Id", "CompanyName", item.SupplierId);
            return View(item);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.Items
                .Include(i => i.Department)
                .Include(i => i.Supplier)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.ImagePath))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, item.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ItemExists(int id)
        {
            return _context.Items.Any(e => e.Id == id);
        }
    }
} 