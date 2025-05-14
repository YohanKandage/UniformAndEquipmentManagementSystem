using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using UniformAndEquipmentManagementSystem.Data;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRequestController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminRequestController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var requests = await _context.Requests
                .Include(r => r.Item)
                .Include(r => r.Employee)
                    .ThenInclude(e => e.Department)
                .Include(r => r.ProcessedBy)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRequest(int id, string status, string adminComment)
        {
            var request = await _context.Requests
                .Include(r => r.Item)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            var admin = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);

            if (admin == null)
            {
                return NotFound();
            }

            request.Status = status;
            request.AdminComment = adminComment;
            request.ProcessedDate = DateTime.Now;
            request.ProcessedById = admin.Id;

            if (status == "Approved")
            {
                // Update item quantity
                request.Item.Quantity--;
                if (request.Item.Quantity <= 0)
                {
                    request.Item.Status = "Unavailable";
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Request has been {status.ToLower()} successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
} 