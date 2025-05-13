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
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            
            if (ModelState.IsValid)
            {
                _logger.LogInformation($"Login attempt for email: {model.Email}");
                
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    _logger.LogInformation($"User found: {user.Email}, UserName: {user.UserName}, EmailConfirmed: {user.EmailConfirmed}, LockoutEnabled: {user.LockoutEnabled}, LockoutEnd: {user.LockoutEnd}");
                    
                    var result = await _signInManager.PasswordSignInAsync(
                        model.Email,  // Use email for sign in
                        model.Password, 
                        isPersistent: false, 
                        lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Login successful for user: {user.Email}");
                        
                        var userRoles = await _userManager.GetRolesAsync(user);
                        _logger.LogInformation($"User roles: {string.Join(", ", userRoles)}");

                        if (userRoles.Contains("Admin"))
                        {
                            return RedirectToAction("Index", "Dashboard");
                        }
                        else if (userRoles.Contains("Employee"))
                        {
                            return RedirectToAction("Index", "Employee");
                        }
                        else if (userRoles.Contains("PropertyManager"))
                        {
                            return RedirectToAction("Index", "Item");
                        }
                        else if (userRoles.Contains("StockManager"))
                        {
                            return RedirectToAction("Index", "Item");
                        }
                        else
                        {
                            _logger.LogWarning($"User {user.Email} has no valid roles assigned");
                            ModelState.AddModelError(string.Empty, "User has no valid roles assigned.");
                            await _signInManager.SignOutAsync();
                            return View(model);
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Login failed for user: {user.Email}. Result: {result}, IsLockedOut: {result.IsLockedOut}, RequiresTwoFactor: {result.RequiresTwoFactor}, IsNotAllowed: {result.IsNotAllowed}");
                        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    }
                }
                else
                {
                    _logger.LogWarning($"User not found: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            else
            {
                _logger.LogWarning($"Model state is invalid for login attempt. Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
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
        public async Task<IActionResult> Logout(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
} 