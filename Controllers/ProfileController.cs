using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using PremierAuto.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly Supabase.Client _supabase;

        public ProfileController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            Supabase.Client supabase)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
            
            var profile = await _context.ClientProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            var cars = await _context.ClientCars.Where(c => c.ClientId == user.Id).ToListAsync();
            ViewBag.ClientCars = cars;
            ViewBag.HasPassword = await _userManager.HasPasswordAsync(user);

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
            
            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.PhoneNumber;
            await _userManager.UpdateAsync(user);
            
            var profile = await _context.ClientProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new ClientProfile
                {
                    UserId = user.Id,
                    FirstName = model.FirstName ?? string.Empty,
                    LastName = model.LastName ?? string.Empty,
                    PhoneNumber = model.PhoneNumber ?? string.Empty
                };
                
                await SaveProfilePicture(profile, profilePicture);
                _context.ClientProfiles.Add(profile);

                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Client");
                }
            }
            else
            {
                profile.FirstName = model.FirstName ?? profile.FirstName;
                profile.LastName = model.LastName ?? profile.LastName;
                profile.PhoneNumber = model.PhoneNumber ?? profile.PhoneNumber;
                
                await SaveProfilePicture(profile, profilePicture);
                _context.ClientProfiles.Update(profile);
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Profilul a fost salvat cu succes!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCar(string carMake, string carModel, string? licensePlate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            int carCount = await _context.ClientCars.CountAsync(c => c.ClientId == user.Id);
            if (carCount >= 5)
            {
                TempData["ErrorMessage"] = "Poți adăuga maxim 5 mașini în garaj.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(carMake) || string.IsNullOrWhiteSpace(carModel))
            {
                TempData["ErrorMessage"] = "Marca și modelul sunt obligatorii.";
                return RedirectToAction(nameof(Index));
            }

            var car = new ClientCar
            {
                ClientId = user.Id,
                CarMake = carMake.Trim(),
                CarModel = carModel.Trim(),
                LicensePlate = string.IsNullOrWhiteSpace(licensePlate) ? null : licensePlate.Trim().ToUpper()
            };

            _context.ClientCars.Add(car);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mașina a fost adăugată cu succes în garaj!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCar(int id, string carMake, string carModel, string? licensePlate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var car = await _context.ClientCars.FirstOrDefaultAsync(c => c.Id == id && c.ClientId == user.Id);
            if (car == null) return NotFound();

            if (string.IsNullOrWhiteSpace(carMake) || string.IsNullOrWhiteSpace(carModel))
            {
                TempData["ErrorMessage"] = "Marca și modelul sunt obligatorii.";
                return RedirectToAction(nameof(Index));
            }

            car.CarMake = carMake.Trim();
            car.CarModel = carModel.Trim();
            car.LicensePlate = string.IsNullOrWhiteSpace(licensePlate) ? null : licensePlate.Trim().ToUpper();

            _context.ClientCars.Update(car);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mașina a fost actualizată cu succes!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCar(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var car = await _context.ClientCars.FirstOrDefaultAsync(c => c.Id == id && c.ClientId == user.Id);
            if (car != null)
            {
                _context.ClientCars.Remove(car);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Mașina a fost ștersă din garaj.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task SaveProfilePicture(ClientProfile profile, IFormFile profilePicture)
        {
            if (profilePicture != null && profilePicture.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(profilePicture.FileName);
                
                using var memoryStream = new MemoryStream();
                await profilePicture.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                await _supabase.Storage
                    .From("premier-sp-auto-public")
                    .Upload(fileBytes, fileName);

                var publicUrl = _supabase.Storage
                    .From("premier-sp-auto-public")
                    .GetPublicUrl(fileName);

                profile.ProfilePictureUrl = publicUrl;
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

            var checkPassword = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!checkPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Parola curentă este incorectă.");
                return View(model);
            }

            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError("NewEmail", "Acest email este deja asociat unui alt cont.");
                return View(model);
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, model.NewEmail);
            if (!setEmailResult.Succeeded)
            {
                foreach (var error in setEmailResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _userManager.SetUserNameAsync(user, model.NewEmail);
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Adresa de email a fost actualizată cu succes!";
            
            if (await _userManager.IsInRoleAsync(user, "Admin")) return RedirectToAction("Index", "Admin");
            if (await _userManager.IsInRoleAsync(user, "Mecanic")) return RedirectToAction("Profile", "Mechanic");
            
            return RedirectToAction("Index");
        }
    }
}