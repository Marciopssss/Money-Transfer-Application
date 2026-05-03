// File: Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Data;
using SwiftPay.Models;
using SwiftPay.ViewModels;

namespace SwiftPay.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        // ── GET: /Auth/Login ─────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (_signInManager.IsSignedIn(User))
                return RedirectToAction("Index", "Dashboard");

            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl,
                CurrencyOptions = GetCurrencyOptions()
            };
            return View(model);
        }

        // ── POST: /Auth/Login ────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            model.CurrencyOptions = GetCurrencyOptions();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null && user.Role == UserRole.Admin)
                    return RedirectToAction("Index", "Admin");

                return RedirectToLocal(model.ReturnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account locked. Try again in 5 minutes.");
                return View(model);
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        // ── GET: /Auth/Register ──────────────────────────────────────
        [HttpGet]
        public IActionResult Register()
        {
            // We use the same Login view with tabs
            return RedirectToAction("Login");
        }

        // ── POST: /Auth/Register ─────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.CurrencyOptions = GetCurrencyOptions();

            if (!ModelState.IsValid)
                return View("Login", new LoginViewModel
                {
                    CurrencyOptions = GetCurrencyOptions()
                });

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email already exists.");
                return View("Login", new LoginViewModel { CurrencyOptions = GetCurrencyOptions() });
            }

            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                PreferredCurrency = model.PreferredCurrency,
                Role = UserRole.User,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var account = new Account
                {
                    UserId = user.Id,
                    SerialNumber = GenerateAccountSerial(),
                    Currency = model.PreferredCurrency,
                    Balance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(account);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View("Login", new LoginViewModel { CurrencyOptions = GetCurrencyOptions() });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private List<SelectListItem> GetCurrencyOptions()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "USD", Text = "🇺🇸 USD — US Dollar" },
                new SelectListItem { Value = "EUR", Text = "🇪🇺 EUR — Euro" },
                new SelectListItem { Value = "GBP", Text = "🇬🇧 GBP — British Pound" },
                new SelectListItem { Value = "LBP", Text = "🇱🇧 LBP — Lebanese Pound" },
                new SelectListItem { Value = "AED", Text = "🇦🇪 AED — UAE Dirham" },
                new SelectListItem { Value = "SAR", Text = "🇸🇦 SAR — Saudi Riyal" }
            };
        }

        private static string GenerateAccountSerial()
        {
            var year = DateTime.UtcNow.Year;
            var random = new Random().Next(10000, 99999);
            return $"SP-{year}-{random}";
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Dashboard");
        }
    }
}