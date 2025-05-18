using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using UniformAndEquipmentManagementSystem.Models;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace UniformAndEquipmentManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            _logger.LogInformation("Login attempt for email: {Email}", model.Email);

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    _logger.LogInformation("User found: {Email}, UserName: {UserName}, EmailConfirmed: {EmailConfirmed}, LockoutEnabled: {LockoutEnabled}, LockoutEnd: {LockoutEnd}",
                        user.Email, user.UserName, user.EmailConfirmed, user.LockoutEnabled, user.LockoutEnd);

                    var result = await _signInManager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User logged in successfully: {Email}", model.Email);
                        
                        // Get user roles
                        var roles = await _userManager.GetRolesAsync(user);
                        
                        // Redirect based on role
                        if (roles.Contains("Employee"))
                        {
                            return RedirectToAction("EmployeeDashboard", "Dashboard");
                        }
                        else if (roles.Contains("Admin"))
                        {
                            return RedirectToAction("AdminDashboard", "Dashboard");
                        }
                        else if (roles.Contains("StockManager"))
                        {
                            return RedirectToAction("Index", "StockManager");
                        }
                        else if (roles.Contains("PropertyManager"))
                        {
                            return RedirectToAction("Index", "PropertyManager");
                        }
                        
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        _logger.LogWarning("Login failed for user: {Email}. Result: {Result}, IsLockedOut: {IsLockedOut}, RequiresTwoFactor: {RequiresTwoFactor}, IsNotAllowed: {IsNotAllowed}",
                            model.Email, result, result.IsLockedOut, result.RequiresTwoFactor, result.IsNotAllowed);
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                }
                else
                {
                    _logger.LogWarning("Login attempt failed - user not found: {Email}", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Register()
        {
            ViewBag.Roles = _roleManager.Roles.ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Attempting to register new user: {model.Email} with role: {model.Role}");

                var user = new ApplicationUser 
                { 
                    UserName = model.Email, 
                    Email = model.Email,
                    EmailConfirmed = true // Auto-confirm email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User created successfully: {model.Email}");

                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                        if (roleResult.Succeeded)
                        {
                            _logger.LogInformation($"Role {model.Role} assigned to user: {model.Email}");
                        }
                        else
                        {
                            _logger.LogError($"Failed to assign role {model.Role} to user {model.Email}. Errors: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                    }
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    _logger.LogError($"Error creating user {model.Email}: {error.Description}");
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = _roleManager.Roles.ToList();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Logout(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager,Employee")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "The new password and confirmation password do not match.");
                return View();
            }
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }
            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (result.Succeeded)
            {
                ViewBag.SuccessMessage = "Your password has been changed successfully.";
                return View();
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }
    }
} 