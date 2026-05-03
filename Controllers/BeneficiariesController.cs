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
    public class BeneficiariesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BeneficiariesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var beneficiaries = await _context.Beneficiaries
                .Where(b => b.UserId == user.Id)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(beneficiaries);
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View(new AddBeneficiaryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddBeneficiaryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var beneficiary = new Beneficiary
            {
                UserId = user.Id,
                FullName = model.FullName,
                Email = model.Email,
                MobileNumber = model.MobileNumber,
                AccountSerial = model.AccountSerial,
                BankName = model.BankName,
                IBAN = model.IBAN,
                CreatedAt = DateTime.UtcNow
            };

            _context.Beneficiaries.Add(beneficiary);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{model.FullName} added to your beneficiaries.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var beneficiary = await _context.Beneficiaries
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.Id);

            if (beneficiary != null)
            {
                _context.Beneficiaries.Remove(beneficiary);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Beneficiary removed.";
            }

            return RedirectToAction("Index");
        }
    }
}