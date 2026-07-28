using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    [Authorize(Roles = "Client")]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int appointmentId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);

            var appointment = await _context.Appointments
                .Include(a => a.Review)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null || appointment.ClientId != user.Id)
                return Forbid();

            if (appointment.Status != AppointmentStatus.Done)
                return BadRequest("Programarea nu este finalizata.");

            if (rating < 1 || rating > 5)
                return BadRequest("Rating invalid.");

            if (appointment.Review != null)
            {
                appointment.Review.Rating = rating;
                appointment.Review.Comment = comment;
                _context.Reviews.Update(appointment.Review);
            }
            else
            {
                var review = new Review
                {
                    AppointmentId = appointmentId,
                    Rating = rating,
                    Comment = comment
                };
                _context.Reviews.Add(review);
            }

            await _context.SaveChangesAsync();
            await UpdateMechanicRating(appointment.MechanicId);

            return RedirectToAction("Index", "Appointment");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int reviewId)
        {
            var user = await _userManager.GetUserAsync(User);

            var review = await _context.Reviews
                .Include(r => r.Appointment)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null || review.Appointment.ClientId != user.Id)
                return Forbid();

            var mechanicId = review.Appointment.MechanicId;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
            await UpdateMechanicRating(mechanicId);

            return RedirectToAction("Index", "Appointment");
        }

        private async Task UpdateMechanicRating(int? mechanicId)
        {
            if (mechanicId == null) return;

            var mechanic = await _context.Mechanics.FindAsync(mechanicId.Value);
            if (mechanic == null) return;

            var ratings = await _context.Reviews
                .Where(r => r.Appointment.MechanicId == mechanicId)
                .Select(r => r.Rating)
                .ToListAsync();

            mechanic.Rating = ratings.Count > 0 ? ratings.Average() : 0;
            await _context.SaveChangesAsync();
        }
    }
}
