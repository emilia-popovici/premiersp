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

        // 1. CALENDARUL CU PROGRAMĂRI (Prima pagină)
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

        // 2. RECENZIILE CLIENȚILOR (Neanonimizate, cu nume și prenume)
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

        // 3. PROFILUL MECANICULUI (Exact ca cel de la clienți)
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

            // 1. Actualizăm datele personale
            mechanic.FirstName = firstName;
            mechanic.LastName = lastName;

            // 2. Gestionăm încărcarea pozei noi
            if (newPhoto != null && newPhoto.Length > 0)
            {
                // Generăm un nume unic pentru fișier pentru a evita suprascrierile
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(newPhoto.FileName);
                
                // Asigură-te că ai un folder 'mechanics' în 'wwwroot/images/'
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

                // AICI AM MODIFICAT: Salvăm în ProfilePictureUrl, nu în PhotoUrl
                mechanic.ProfilePictureUrl = $"/images/mechanics/{fileName}";
                
                // CRUCIAL: Setăm aprobarea pe false, dar poza veche (PhotoUrl) rămâne intactă și publică
                mechanic.IsPictureApproved = false; 
            }

            _context.Update(mechanic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profilul a fost actualizat cu succes!";
            return RedirectToAction("Profile");
        }
    }
}