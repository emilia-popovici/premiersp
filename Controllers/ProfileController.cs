using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using PremierAuto.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ProfileController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var model = new ClientProfileViewModel();
            if (profile != null)
            {
                model.FirstName = profile.FirstName;
                model.LastName = profile.LastName;
                model.PhoneNumber = profile.PhoneNumber;
                model.ProfilePictureUrl = profile.ProfilePictureUrl;
            }
            else
            {
                model.FirstName = user?.FirstName;
                model.LastName = user?.LastName;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ClientProfileViewModel model, IFormFile profilePicture) 
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            
            // Actualizăm datele de bază din Identity
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);
            
            // Căutăm profilul în tabelul nostru
            var profile = await _context.ClientProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                // Creare profil nou
                profile = new ClientProfile
                {
                    UserId = user.Id,
                    FirstName = model.FirstName ?? string.Empty,
                    LastName = model.LastName ?? string.Empty,
                    PhoneNumber = model.PhoneNumber ?? string.Empty
                };
                
                await SaveProfilePicture(profile, profilePicture);
                _context.ClientProfiles.Add(profile);

                // Îi dăm rolul de Client doar dacă nu e Admin
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Client");
                }
            }
            else
            {
                // Actualizare profil existent
                profile.FirstName = model.FirstName ?? profile.FirstName;
                profile.LastName = model.LastName ?? profile.LastName;
                profile.PhoneNumber = model.PhoneNumber ?? profile.PhoneNumber;
                
                await SaveProfilePicture(profile, profilePicture);
                _context.ClientProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Profilul a fost salvat cu succes!";
            return RedirectToAction("Index", "Home");
        }

        private async Task SaveProfilePicture(ClientProfile profile, IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);
                
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }
                profile.ProfilePictureUrl = "/uploads/" + fileName;
            }
        }

        [HttpGet]
        public IActionResult ChangeEmail()
        {
            return View(new ChangeEmailViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("Utilizatorul nu a fost găsit.");

            // 1. Verificăm dacă parola curentă este corectă
            var checkPassword = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!checkPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Parola curentă este incorectă.");
                return View(model);
            }

            // 2. Verificăm dacă noul email este deja folosit de altcineva
            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("NewEmail", "Acest email este deja asociat unui alt cont.");
                return View(model);
            }

            // 3. Schimbăm emailul și username-ul (Identity folosește adesea emailul ca username)
            var setEmailResult = await _userManager.SetEmailAsync(user, model.NewEmail);
            if (!setEmailResult.Succeeded)
            {
                foreach (var error in setEmailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // Actualizăm și Username-ul pentru ca login-ul să funcționeze cu noul email
            await _userManager.SetUserNameAsync(user, model.NewEmail);

            // 4. Re-autentificăm utilizatorul silențios ca să nu fie scos din cont
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Adresa de email a fost actualizată cu succes!";
            
            // Redirecționează către pagina principală de profil în funcție de rol
            if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToAction("Index", "Admin");
            if (await _userManager.IsInRoleAsync(user, "Mecanic")) return RedirectToAction("Profile", "Mechanic");
            
            return RedirectToAction("Index"); // Pentru clienți
        }
    }
}
