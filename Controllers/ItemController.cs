using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using UniformAndEquipmentManagementSystem.Models;
using UniformAndEquipmentManagementSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin,StockManager")]
    public class ItemController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ItemController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var items = await _context.Items.ToListAsync();
            return View(items);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Item item, IFormFile? ImageFile)
        {
            if (ModelState.IsValid)
            {
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "items");
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

                    item.ImagePath = "/images/items/" + uniqueFileName;
                }

                _context.Add(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item added successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.Items.FindAsync(id);
            if (item == null)
                return NotFound();

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

                        item.ImagePath = "/images/items/" + uniqueFileName;
                    }

                    _context.Update(item);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Item updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Items.Any(e => e.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(item);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var item = await _context.Items.FirstOrDefaultAsync(m => m.Id == id);
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
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Items.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Item deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
} 