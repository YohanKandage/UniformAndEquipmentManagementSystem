using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<RoleController> _logger;

        public RoleController(RoleManager<IdentityRole> roleManager, ILogger<RoleController> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var roles = _roleManager.Roles.ToList();
            return View(roles);
        }

        [HttpGet]
        public async Task<IActionResult> VerifyRoles()
        {
            var roles = new[] { "Admin", "Employee", "PropertyManager", "StockManager" };
            var results = new System.Collections.Generic.Dictionary<string, bool>();

            foreach (var role in roles)
            {
                var exists = await _roleManager.RoleExistsAsync(role);
                results.Add(role, exists);
                _logger.LogInformation($"Role {role} exists: {exists}");
            }

            return Json(results);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return BadRequest("Role name is required");
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Role {roleName} created successfully");
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    _logger.LogError($"Failed to create role {roleName}. Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    return BadRequest(result.Errors);
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 