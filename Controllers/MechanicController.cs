using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    [Authorize(Roles = "Mecanic")]
    public class MechanicController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MechanicController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Calendar()
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (mechanic == null) return NotFound("Profilul de mecanic nu a fost găsit.");

            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Where(a => a.MechanicId == mechanic.Id)
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        public async Task<IActionResult> Reviews()
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (mechanic == null) return NotFound();

            var reviews = await _context.Reviews
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Client)
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Service)
                .Where(r => r.Appointment.MechanicId == mechanic.Id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            return View(reviews);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (mechanic == null) return NotFound();

            return View(mechanic);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string firstName, string lastName, IFormFile newPhoto)
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (mechanic == null) return NotFound();

            mechanic.FirstName = firstName;
            mechanic.LastName = lastName;

            if (newPhoto != null && newPhoto.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newPhoto.FileName);
                
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/mechanics");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await newPhoto.CopyToAsync(stream);
                }
                
                mechanic.ProfilePictureUrl = $"/images/mechanics/{fileName}";
                mechanic.IsPictureApproved = false; 
            }

            _context.Update(mechanic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profilul a fost actualizat cu succes!";
            return RedirectToAction("Profile");
        }
    }
}