using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OpenProjectProductivity.Web.Models;
using OpenProjectProductivity.Web.Services;
using OpenProjectProductivity.Web.ViewModels;
using System.Threading.Tasks;

namespace OpenProjectProductivity.Web.Controllers
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly UserManager<AuthUser> _userManager;
        private readonly SignInManager<AuthUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(UserManager<AuthUser> userManager,
                              SignInManager<AuthUser> signInManager,
                              RoleManager<IdentityRole> roleManager,
                              JwtService jwtService,
                              ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            Console.WriteLine("GET /Auth/Register called");
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            Console.WriteLine("POST /Auth/Register called");

            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid:");
                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        Console.WriteLine($"{key}: {error.ErrorMessage}");
                    }
                }
                return View(model);
            }

            var user = new AuthUser { UserName = model.UserName, Email = model.Email };
            Console.WriteLine($"Creating user: {user.UserName}, Email: {user.Email}");

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                Console.WriteLine("User created successfully.");

                // Ensure Admin role exists
                if (!await _roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await _roleManager.CreateAsync(new IdentityRole("Admin"));
                    Console.WriteLine(roleResult.Succeeded
                        ? "Admin role created."
                        : $"Failed to create Admin role: {string.Join(", ", roleResult.Errors)}");
                }

                // Assign Admin role if username is "admin"
                if (user.UserName.ToLower() == "admin")
                {
                    var roleAssignResult = await _userManager.AddToRoleAsync(user, "Admin");
                    Console.WriteLine(roleAssignResult.Succeeded
                        ? "Admin role assigned to user."
                        : $"Failed to assign Admin role: {string.Join(", ", roleAssignResult.Errors)}");
                }

                // Sign in user
                await _signInManager.SignInAsync(user, isPersistent: false);
                Console.WriteLine("User signed in.");
                return RedirectToAction("Index", "Dashboard");
            }

            // Log creation errors
            Console.WriteLine("User creation failed:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            Console.WriteLine("GET /Auth/Login called");
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            _logger.LogInformation("Login POST called with username: {Username}", model.UserName);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login model state invalid");
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                _logger.LogWarning("User not found: {Username}", model.UserName);
                ModelState.AddModelError("", "Invalid username or password");
                return View(model);
            }

            _logger.LogInformation("User found: {UserId}", user.Id);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in successfully: {Username}", model.UserName);

                // Ensure cookie is created
                await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

                // Generate JWT token for API usage
                var token = await _jwtService.GenerateTokenAsync(user);
                _logger.LogInformation("JWT token generated for user: {Username}", model.UserName);

                // Optional: set ViewBag for debugging
                ViewBag.JwtToken = token;

                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out: {Username}", model.UserName);
                ModelState.AddModelError("", "Account locked. Try again later.");
            }
            else
            {
                _logger.LogWarning("Invalid login attempt for user: {Username}", model.UserName);
                ModelState.AddModelError("", "Invalid username or password");
            }

            return View(model);
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            Console.WriteLine("POST /Auth/Logout called");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            Console.WriteLine("GET /Auth/AccessDenied called");
            return View();
        }
    }
}
