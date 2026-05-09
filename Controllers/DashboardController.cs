using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SwiftPay.Data;
using SwiftPay.Models;
using SwiftPay.ViewModels;
using System.Globalization;

namespace SwiftPay.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public DashboardController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var accounts = await _context.Accounts
                .Where(a => a.UserId == user.Id && a.IsActive)
                .ToListAsync();
            var account = accounts.FirstOrDefault();

            var transactions = account != null
                ? await _context.Transactions
                    .Where(t => t.SenderAccountId == account.Id || t.ReceiverAccountId == account.Id)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync()
                : new List<Transaction>();

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var monthlySent = account != null
                ? await _context.Transactions
                    .Where(t => t.SenderAccountId == account.Id && t.CreatedAt >= startOfMonth && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0
                : 0;

            var lastMonthSent = account != null
                ? await _context.Transactions
                    .Where(t => t.SenderAccountId == account.Id && t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => (decimal?)t.Amount) ?? 0
                : 0;

            var monthlyReceived = account != null
                ? await _context.Transactions
                    .Where(t => t.ReceiverAccountId == account.Id && t.CreatedAt >= startOfMonth && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => (decimal?)t.ConvertedAmount) ?? 0
                : 0;

            var lastMonthReceived = account != null
                ? await _context.Transactions
                    .Where(t => t.ReceiverAccountId == account.Id && t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth && t.Status == TransactionStatus.Completed)
                    .SumAsync(t => (decimal?)t.ConvertedAmount) ?? 0
                : 0;

            var unreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == user.Id && !n.IsRead);

            var txnRows = transactions.Select(t =>
            {
                bool isSend = account != null && t.SenderAccountId == account.Id;
                bool isTopUp = t.Type == TransactionType.AccountToAccount && t.SenderAccountId == t.ReceiverAccountId;

                return new TransactionRowViewModel
                {
                    SerialNumber = t.SerialNumber,
                    Description = isSend ? $"Transfer sent" : "Transfer received",
                    Amount = t.Amount,
                    FormattedAmount = (isSend ? "-" : "+") + t.Amount.ToString("C"),
                    AmountClass = isSend ? "send" : "recv",
                    IconClass = isSend ? "send" : "recv",
                    IconSymbol = isSend ? "↑" : "↓",
                    Status = t.Status.ToString(),
                    StatusClass = t.Status.ToString().ToLower(),
                    Date = t.CreatedAt
                };
            }).ToList();


            var model = new DashboardViewModel
            {
                FirstName = user.FirstName,
                FullName = user.FullName,
                Initials = $"{user.FirstName[0]}{user.LastName[0]}".ToUpper(),
                AccountSerial = account?.SerialNumber ?? "N/A",
                Balance = account?.Balance ?? 0,
                PreferredCurrency = user.PreferredCurrency,
                CultureInfo = CultureInfo.GetCultureInfo("en-US"),
                MonthlySent = monthlySent,
                MonthlyReceived = monthlyReceived,
                SentChangePercent = lastMonthSent > 0 ? (int)(((monthlySent - lastMonthSent) / lastMonthSent) * 100) : 0,
                ReceivedChangePercent = lastMonthReceived > 0 ? (int)(((monthlyReceived - lastMonthReceived) / lastMonthReceived) * 100) : 0,
                UnreadNotifications = unreadNotifications,
                RecentTransactions = txnRows,
                Accounts = accounts.Select(a => new AccountBalanceViewModel
                {
                    Currency = a.Currency,
                    Balance = a.Balance,
                    SerialNumber = a.SerialNumber
                }).ToList()
            };

            return View(model);
        }
        private static CultureInfo GetCultureForCurrency(string currency) => currency switch
        {
            "EUR" => CultureInfo.GetCultureInfo("fr-FR"),
            "GBP" => CultureInfo.GetCultureInfo("en-GB"),
            "LBP" => CultureInfo.GetCultureInfo("ar-LB"),
            "AED" => CultureInfo.GetCultureInfo("ar-AE"),
            "SAR" => CultureInfo.GetCultureInfo("ar-SA"),
            _ => CultureInfo.GetCultureInfo("en-US")  // USD default
        };
    }
}