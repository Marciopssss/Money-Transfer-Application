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
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != UserRole.Admin)
                return RedirectToAction("Login", "Auth");

            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var totalVolume = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var lastMonthVolume = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed && t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var thisMonthVolume = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed && t.CreatedAt >= startOfMonth)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            var activeUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            var newUsersThisMonth = await _userManager.Users.CountAsync(u => u.CreatedAt >= startOfMonth);
            var activeAgents = await _context.Agents.CountAsync(a => a.Status == AgentStatus.Approved);
            var pendingAgents = await _context.Agents.CountAsync(a => a.Status == AgentStatus.Pending);

            var totalCommission = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed)
                .SumAsync(t => (decimal?)t.CommissionAmount) ?? 0;

            var lastMonthCommission = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed && t.CreatedAt >= startOfLastMonth && t.CreatedAt < startOfMonth)
                .SumAsync(t => (decimal?)t.CommissionAmount) ?? 0;

            var thisMonthCommission = await _context.Transactions
                .Where(t => t.Status == TransactionStatus.Completed && t.CreatedAt >= startOfMonth)
                .SumAsync(t => (decimal?)t.CommissionAmount) ?? 0;

            var pendingAgentRequests = await _context.Agents
                .Where(a => a.Status == AgentStatus.Pending)
                .Include(a => a.User)
                .OrderByDescending(a => a.AppliedAt)
                .Take(5)
                .Select(a => new AgentRequestViewModel
                {
                    Id = a.Id,
                    StoreName = a.StoreName,
                    Location = a.Location,
                    Initials = a.User.FirstName.Substring(0, 1) + a.User.LastName.Substring(0, 1),
                    AppliedDate = a.AppliedAt
                })
                .ToListAsync();

            var commissionRates = await _context.CommissionRates
                .Select(c => new CommissionRateViewModel
                {
                    Id = c.Id,
                    FromCurrency = c.FromCurrency,
                    ToCurrency = c.ToCurrency,
                    Rate = c.Rate,
                    FromFlag = GetFlag(c.FromCurrency)
                })
                .ToListAsync();

            var recentTxns = await _context.Transactions
                .Include(t => t.SenderAccount).ThenInclude(a => a.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(10)
                .Select(t => new AdminTransactionViewModel
                {
                    SerialNumber = t.SerialNumber,
                    UserFullName = t.SenderAccount.User.FirstName + " " + t.SenderAccount.User.LastName,
                    Type = t.Type.ToString(),
                    Amount = t.Amount,
                    FormattedAmount = (t.Amount >= 0 ? "+" : "") + t.Amount.ToString("C"),
                    CurrencyPair = t.FromCurrency + " → " + t.ToCurrency,
                    Status = t.Status.ToString(),
                    StatusClass = t.Status == TransactionStatus.Completed ? "approve" :
                                     t.Status == TransactionStatus.Pending ? "pending" : "rejected",
                    Date = t.CreatedAt
                })
                .ToListAsync();

            ViewBag.PendingAgents = pendingAgents;

            var model = new AdminDashboardViewModel
            {
                TotalVolume = totalVolume,
                VolumeChangePercent = lastMonthVolume > 0 ? (int)(((thisMonthVolume - lastMonthVolume) / lastMonthVolume) * 100) : 0,
                ActiveUsers = activeUsers,
                NewUsersThisMonth = newUsersThisMonth,
                ActiveAgents = activeAgents,
                PendingAgents = pendingAgents,
                TotalCommission = totalCommission,
                CommissionChangePercent = lastMonthCommission > 0 ? (int)(((thisMonthCommission - lastMonthCommission) / lastMonthCommission) * 100) : 0,
                PendingAgentRequests = pendingAgentRequests,
                CommissionRates = commissionRates,
                RecentTransactions = recentTxns
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAgent(int agentId)
        {
            var agent = await _context.Agents.FindAsync(agentId);
            if (agent != null)
            {
                agent.Status = AgentStatus.Approved;
                agent.ApprovedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAgent(int agentId)
        {
            var agent = await _context.Agents.FindAsync(agentId);
            if (agent != null)
            {
                agent.Status = AgentStatus.Rejected;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> EditCommission(int id)
        {
            var rate = await _context.CommissionRates.FindAsync(id);
            if (rate == null) return NotFound();
            return View(rate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCommission(CommissionRate model)
        {
            if (!ModelState.IsValid) return View(model);
            var rate = await _context.CommissionRates.FindAsync(model.Id);
            if (rate != null)
            {
                rate.Rate = model.Rate;
                rate.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ExportTransactions()
        {
            var txns = await _context.Transactions
                .Include(t => t.SenderAccount).ThenInclude(a => a.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var csv = "SerialNumber,User,Type,Amount,Currency,Status,Date\n";
            foreach (var t in txns)
                csv += $"{t.SerialNumber},{t.SenderAccount.User.FullName},{t.Type},{t.Amount},{t.FromCurrency},{t.Status},{t.CreatedAt:yyyy-MM-dd}\n";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "transactions.csv");
        }

        private static string GetFlag(string currency) => currency switch
        {
            "USD" => "🇺🇸",
            "EUR" => "🇪🇺",
            "GBP" => "🇬🇧",
            "LBP" => "🇱🇧",
            "AED" => "🇦🇪",
            "SAR" => "🇸🇦",
            _ => "🏳"
        };
    }
}