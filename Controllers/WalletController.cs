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
    public class WalletController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WalletController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Wallet/TopUp
        [HttpGet]
        public async Task<IActionResult> TopUp()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            ViewBag.Balance = account?.Balance ?? 0;
            ViewBag.AccountSerial = account?.SerialNumber ?? "N/A";
            return View(new TopUpViewModel());
        }

        // POST: /Wallet/TopUp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUp(TopUpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            if (account == null)
            {
                ModelState.AddModelError("", "No active account found.");
                return View(model);
            }

            // In production: process Stripe payment here
            // For now: simulate successful payment
            var topUp = new TopUp
            {
                AccountId = account.Id,
                Amount = model.Amount,
                Currency = account.Currency,
                StripePaymentId = "DEMO_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                Status = TopUpStatus.Completed,
                CreatedAt = DateTime.UtcNow
            };

            account.Balance += model.Amount;
            _context.TopUps.Add(topUp);

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Top Up Successful 💳",
                Message = $"{model.Amount:C} has been added to your account {account.SerialNumber}.",
                Type = NotificationType.TopUp,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"{model.Amount:C} successfully added to your wallet!";
            return RedirectToAction("Index", "Dashboard");
        }

        // GET: /Wallet/Exchange
        [HttpGet]
        public async Task<IActionResult> Exchange()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            ViewBag.Balance = account?.Balance ?? 0;
            ViewBag.Currency = account?.Currency ?? "USD";
            return View();
        }
    }
}
