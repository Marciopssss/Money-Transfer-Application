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
    public class AgentsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AgentsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        public async Task<IActionResult> Map()
        {
            var agents = await _context.Agents
                .Where(a => a.Status == AgentStatus.Approved)
                .ToListAsync();

            return View(agents);
        }


        [HttpGet]
        public IActionResult Register() => View(new RegisterAgentViewModel());


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterAgentViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var existing = await _context.Agents.AnyAsync(a => a.UserId == user.Id);
            if (existing)
            {
                ModelState.AddModelError("", "You have already registered as an agent.");
                return View(model);
            }

            _context.Agents.Add(new Agent
            {
                UserId = user.Id,
                StoreName = model.StoreName,
                Location = model.Location,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                WorkingHours = model.WorkingHours,
                Status = AgentStatus.Pending,
                AppliedAt = DateTime.UtcNow
            });

            user.Role = UserRole.Agent;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Agent registration submitted! Awaiting admin approval.";
            return RedirectToAction("Map");
        }
    }
}