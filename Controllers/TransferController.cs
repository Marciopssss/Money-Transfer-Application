// File: Controllers/TransferController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Data;
using SwiftPay.Models;
using SwiftPay.ViewModels;

namespace SwiftPay.Controllers
{
    [Authorize]
    public class TransferController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransferController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── GET: /Transfer/Send ──────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Send()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            var beneficiaries = await _context.Beneficiaries
                .Where(b => b.UserId == user.Id)
                .ToListAsync();

            var model = new SendMoneyViewModel
            {
                Balance = account?.Balance ?? 0,
                AccountSerial = account?.SerialNumber ?? "N/A",
                FromCurrency = user.PreferredCurrency,
                ToCurrency = user.PreferredCurrency,
                CurrencyOptions = GetCurrencyOptions(),
                BeneficiaryOptions = beneficiaries.Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.FullName + (b.AccountSerial != null ? $" ({b.AccountSerial})" : $" ({b.MobileNumber})")
                }).ToList()
            };

            return View(model);
        }

        // ── POST: /Transfer/Send ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(SendMoneyViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            // Re-populate dropdowns
            model.CurrencyOptions = GetCurrencyOptions();
            model.BeneficiaryOptions = (await _context.Beneficiaries
                .Where(b => b.UserId == user.Id).ToListAsync())
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = b.FullName
                }).ToList();

            if (account == null)
            {
                ModelState.AddModelError("", "No active account found.");
                return View(model);
            }

            // Validate method-specific fields
            if (model.Method == TransferMethod.Beneficiary && model.BeneficiaryId == null)
            {
                ModelState.AddModelError("BeneficiaryId", "Please select a beneficiary.");
                model.Balance = account.Balance;
                return View(model);
            }

            if (model.Method == TransferMethod.Mobile &&
                (string.IsNullOrEmpty(model.ReceiverMobileNumber) || string.IsNullOrEmpty(model.ReceiverName)))
            {
                ModelState.AddModelError("ReceiverMobileNumber", "Mobile number and receiver name are required.");
                model.Balance = account.Balance;
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.Balance = account.Balance;
                return View(model);
            }

            // Check sufficient balance
            var exchangeRate = GetExchangeRate(model.FromCurrency, model.ToCurrency);
            var convertedAmount = model.Amount * exchangeRate;
            var commissionRate = await GetCommissionRate(model.FromCurrency, model.ToCurrency);
            var commissionAmount = model.Amount * (commissionRate / 100);
            var totalDebit = model.Amount + commissionAmount;

            if (account.Balance < totalDebit)
            {
                ModelState.AddModelError("Amount", $"Insufficient balance. You need {totalDebit:C} but have {account.Balance:C}.");
                model.Balance = account.Balance;
                return View(model);
            }

            // Find receiver account (for account-to-account)
            Account? receiverAccount = null;
            if (model.Method == TransferMethod.Beneficiary && model.BeneficiaryId.HasValue)
            {
                var beneficiary = await _context.Beneficiaries.FindAsync(model.BeneficiaryId.Value);
                if (beneficiary?.AccountSerial != null)
                {
                    receiverAccount = await _context.Accounts
                        .FirstOrDefaultAsync(a => a.SerialNumber == beneficiary.AccountSerial && a.IsActive);
                }
            }

            // Create transaction
            var transaction = new Transaction
            {
                SerialNumber = GenerateTransactionSerial(),
                Amount = model.Amount,
                ConvertedAmount = convertedAmount,
                ExchangeRate = exchangeRate,
                CommissionAmount = commissionAmount,
                FromCurrency = model.FromCurrency,
                ToCurrency = model.ToCurrency,
                Type = model.Method == TransferMethod.Mobile
                                        ? TransactionType.MobileTransfer
                                        : TransactionType.AccountToAccount,
                Status = TransactionStatus.Completed,
                SenderAccountId = account.Id,
                ReceiverAccountId = receiverAccount?.Id,
                BeneficiaryId = model.BeneficiaryId,
                ReceiverMobileNumber = model.ReceiverMobileNumber,
                ReceiverName = model.ReceiverName,
                Notes = model.Notes,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            account.Balance -= totalDebit;

            if (receiverAccount != null)
                receiverAccount.Balance += convertedAmount;

            _context.Transactions.Add(transaction);

            _context.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "Transfer Sent ✅",
                Message = $"Your transfer of {model.Amount:C} ({model.FromCurrency}) was sent successfully. Ref: {transaction.SerialNumber}",
                Type = NotificationType.TransferUpdate,
                CreatedAt = DateTime.UtcNow
            });

            if (receiverAccount != null)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = receiverAccount.UserId,
                    Title = "Money Received 💰",
                    Message = $"You received {convertedAmount:C} ({model.ToCurrency}). Ref: {transaction.SerialNumber}",
                    Type = NotificationType.TransferUpdate,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Receipt", new { id = transaction.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            var transaction = await _context.Transactions
                .Include(t => t.SenderAccount)
                .Include(t => t.ReceiverAccount)
                .Include(t => t.Beneficiary)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transaction == null) return NotFound();

            var receiverName = transaction.ReceiverName
                ?? transaction.Beneficiary?.FullName
                ?? transaction.ReceiverAccount?.SerialNumber
                ?? "Unknown";

            var model = new TransferConfirmViewModel
            {
                SerialNumber = transaction.SerialNumber,
                Amount = transaction.Amount,
                ConvertedAmount = transaction.ConvertedAmount,
                ExchangeRate = transaction.ExchangeRate,
                CommissionAmount = transaction.CommissionAmount,
                FromCurrency = transaction.FromCurrency,
                ToCurrency = transaction.ToCurrency,
                ReceiverName = receiverName,
                Status = transaction.Status.ToString(),
                CreatedAt = transaction.CreatedAt
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SendByMobile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            var model = new SendMoneyViewModel
            {
                Method = TransferMethod.Mobile,
                Balance = account?.Balance ?? 0,
                AccountSerial = account?.SerialNumber ?? "N/A",
                FromCurrency = user.PreferredCurrency,
                ToCurrency = user.PreferredCurrency,
                CurrencyOptions = GetCurrencyOptions()
            };

            return View("Send", model);
        }

        // ── Helpers ──────────────────────────────────────────────
        private static string GenerateTransactionSerial()
        {
            var year = DateTime.UtcNow.Year;
            var random = new Random().Next(10000, 99999);
            return $"TXN-{year}-{random}";
        }

        private static decimal GetExchangeRate(string from, string to)
        {
            if (from == to) return 1m;

            // Static rates for demo — replace with a live API in production
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

            var key = $"{from}_{to}";
            return rates.TryGetValue(key, out var rate) ? rate : 1m;
        }

        private async Task<decimal> GetCommissionRate(string from, string to)
        {
            var rate = await _context.CommissionRates
                .FirstOrDefaultAsync(c => c.FromCurrency == from && c.ToCurrency == to);
            return rate?.Rate ?? 1.5m; // default 1.5%
        }

        private static List<SelectListItem> GetCurrencyOptions() => new()
        {
            new SelectListItem { Value = "USD", Text = "🇺🇸 USD — US Dollar" },
            new SelectListItem { Value = "EUR", Text = "🇪🇺 EUR — Euro" },
            new SelectListItem { Value = "GBP", Text = "🇬🇧 GBP — British Pound" },
            new SelectListItem { Value = "LBP", Text = "🇱🇧 LBP — Lebanese Pound" },
            new SelectListItem { Value = "AED", Text = "🇦🇪 AED — UAE Dirham" },
            new SelectListItem { Value = "SAR", Text = "🇸🇦 SAR — Saudi Riyal" }
        };
    }
}