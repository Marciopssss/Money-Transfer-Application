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
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var reviews = await _context.Reviews
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            ViewBag.AverageRating = Math.Round(avgRating, 1);
            ViewBag.TotalReviews = reviews.Count;
            return View(reviews);
        }

        [HttpGet]
        public IActionResult Add() => View(new AddReviewViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddReviewViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            // Check if user already reviewed
            var existing = await _context.Reviews.AnyAsync(r => r.UserId == user.Id);
            if (existing)
            {
                ModelState.AddModelError("", "You have already submitted a review.");
                return View(model);
            }

            _context.Reviews.Add(new Review
            {
                UserId = user.Id,
                Rating = model.Rating,
                Comment = model.Comment,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review!";
            return RedirectToAction("Index");
        }
    }
}