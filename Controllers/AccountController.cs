using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Data;
using SwiftPay.Models;
using SwiftPay.ViewModels;

namespace SwiftPay.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            var model = new SettingsViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                PreferredCurrency = user.PreferredCurrency,
                AccountSerial = account?.SerialNumber ?? "N/A",
                Balance = account?.Balance ?? 0,
                MemberSince = user.CreatedAt
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            model.AccountSerial = account?.SerialNumber ?? "N/A";
            model.Balance = account?.Balance ?? 0;
            model.MemberSince = user.CreatedAt;
            model.Email = user.Email ?? "";

            if (!ModelState.IsValid)
                return View(model);

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.PreferredCurrency = model.PreferredCurrency;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError("", e.Description);
                return View(model);
            }

            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Current password is required to set a new password.");
                    return View(model);
                }

                var passResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!passResult.Succeeded)
                {
                    foreach (var e in passResult.Errors)
                        ModelState.AddModelError("", e.Description);
                    return View(model);
                }

                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["Success"] = "Settings saved successfully!";
            return RedirectToAction("Settings");
        }
    }
}