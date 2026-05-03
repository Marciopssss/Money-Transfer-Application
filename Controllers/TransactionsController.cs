// File: Controllers/TransactionsController.cs
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
    public class TransactionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Transactions
        public async Task<IActionResult> Index(string? status, string? type, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.IsActive);

            if (account == null)
                return View(new TransactionHistoryViewModel());

            var query = _context.Transactions
                .Where(t => t.SenderAccountId == account.Id || t.ReceiverAccountId == account.Id)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TransactionStatus>(status, out var s))
                query = query.Where(t => t.Status == s);

            if (!string.IsNullOrEmpty(type) && Enum.TryParse<TransactionType>(type, out var tp))
                query = query.Where(t => t.Type == tp);

            var total = await query.CountAsync();
            const int pageSize = 10;

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var rows = transactions.Select(t =>
            {
                bool isSend = t.SenderAccountId == account.Id;
                return new TransactionRowViewModel
                {
                    SerialNumber = t.SerialNumber,
                    Description = isSend
                        ? $"Sent to {t.ReceiverName ?? t.ReceiverAccount?.SerialNumber ?? "account"}"
                        : $"Received",
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

            var model = new TransactionHistoryViewModel
            {
                Transactions = rows,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                StatusFilter = status,
                TypeFilter = type
            };

            return View(model);
        }
    }
}