using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Data;
using SwiftPay.Models;
using SwiftPay.ViewModels;
using Stripe;

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

            try
            {
                // Create Stripe charge using the token from the form
                var options = new ChargeCreateOptions
                {
                    Amount = (long)(model.Amount * 100), // Stripe uses cents
                    Currency = account.Currency.ToLower(),
                    Source = model.StripeToken,          // token from frontend
                    Description = $"SwiftPay Top Up — {account.SerialNumber}"
                };

                var service = new ChargeService();
                var charge = await service.CreateAsync(options);

                if (charge.Status == "succeeded")
                {
                    var topUp = new TopUp
                    {
                        AccountId = account.Id,
                        Amount = model.Amount,
                        Currency = account.Currency,
                        StripePaymentId = charge.Id,
                        Status = TopUpStatus.Completed,
                        CreatedAt = DateTime.UtcNow
                    };

                    account.Balance += model.Amount;
                    _context.TopUps.Add(topUp);

                    _context.Notifications.Add(new Notification
                    {
                        UserId = user.Id,
                        Title = "Top Up Successful 💳",
                        Message = $"{model.Amount:C} added to account {account.SerialNumber}.",
                        Type = NotificationType.TopUp,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"{model.Amount:C} successfully added to your wallet!";
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError("", "Payment failed. Please try again.");
                    return View(model);
                }
            }
            catch (StripeException ex)
            {
                ModelState.AddModelError("", $"Payment error: {ex.StripeError.Message}");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Exchange()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            var model = new ExchangeViewModel
            {
                Balance = account?.Balance ?? 0,
                AccountSerial = account?.SerialNumber ?? "N/A",
                FromCurrency = account?.Currency ?? "USD",
                ToCurrency = "EUR"
            };

            return View(model);
        }

  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exchange(ExchangeViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var accounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();

            var fromAccount = accounts.FirstOrDefault(a => a.Currency == model.FromCurrency);
            model.Balance = fromAccount?.Balance ?? 0;
            model.AccountSerial = fromAccount?.SerialNumber ?? "N/A";

            if (!ModelState.IsValid) return View(model);

            if (model.FromCurrency == model.ToCurrency)
            {
                ModelState.AddModelError("", "From and To currencies must be different.");
                return View(model);
            }

            if (fromAccount == null || fromAccount.Balance < model.Amount)
            {
                ModelState.AddModelError("Amount", $"Insufficient {model.FromCurrency} balance. You have {fromAccount?.Balance ?? 0:N2} {model.FromCurrency}.");
                return View(model);
            }

            var rate = GetExchangeRate(model.FromCurrency, model.ToCurrency);
            var converted = model.Amount * rate;

         
            fromAccount.Balance -= model.Amount;

           
            var toAccount = accounts.FirstOrDefault(a => a.Currency == model.ToCurrency);
            if (toAccount == null)
            {
                toAccount = new Account
                {
                    UserId = user.Id,
                    SerialNumber = $"SP-{DateTime.UtcNow.Year}-{new Random().Next(10000, 99999)}",
                    Currency = model.ToCurrency,
                    Balance = converted,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Accounts.Add(toAccount);
                await _context.SaveChangesAsync();
            }
            else
            {
                toAccount.Balance += converted;
            }

            
            _context.Transactions.Add(new Transaction
            {
                SerialNumber = $"EXC-{DateTime.UtcNow.Year}-{new Random().Next(10000, 99999)}",
                Amount = model.Amount,
                ConvertedAmount = converted,
                ExchangeRate = rate,
                FromCurrency = model.FromCurrency,
                ToCurrency = model.ToCurrency,
                Type = TransactionType.AccountToAccount,
                Status = TransactionStatus.Completed,
                SenderAccountId = fromAccount.Id,
                ReceiverAccountId = toAccount.Id,
                Notes = "Currency exchange",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            model.ExchangeRate = rate;
            model.ConvertedAmount = converted;
            model.Balance = fromAccount.Balance;
            model.Exchanged = true;
            return View(model);
        }

       
        private static decimal GetExchangeRate(string from, string to)
        {
            if (from == to) return 1m;
            var rates = new Dictionary<string, decimal>
            {
                { "USD_EUR", 0.92m }, { "EUR_USD", 1.09m },
                { "USD_GBP", 0.79m }, { "GBP_USD", 1.27m },
                { "USD_LBP", 89500m }, { "LBP_USD", 0.0000112m },
                { "USD_AED", 3.67m }, { "AED_USD", 0.27m },
                { "USD_SAR", 3.75m }, { "SAR_USD", 0.27m },
                { "EUR_GBP", 0.86m }, { "GBP_EUR", 1.16m },
                { "AED_EUR", 0.25m }, { "EUR_AED", 4.01m },
                { "SAR_EUR", 0.25m }, { "EUR_SAR", 4.08m },
            };
            return rates.TryGetValue($"{from}_{to}", out var r) ? r : 1m;
        }
    }
}