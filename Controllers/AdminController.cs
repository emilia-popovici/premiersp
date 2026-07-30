using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace PremierAuto.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            int pendingAppointments = await _context.Appointments
                .Where(a => a.Status == AppointmentStatus.Pending)
                .CountAsync();
            ViewBag.PendingAppointmentsCount = pendingAppointments;

            int unreadMessagesCount = await _context.AppointmentMessages
                .Where(m => !m.IsAdmin && !m.IsRead)
                .CountAsync();
            ViewBag.UnreadMessagesCount = unreadMessagesCount;
            
            var topServices = await _context.Appointments
                .Include(a => a.Service)
                .GroupBy(a => a.Service.Name)
                .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.TopServices = topServices;

            var appointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.Status == AppointmentStatus.Done)
                .ToListAsync();

            var totalAppointments = appointments.Count;

            var completedServiceIds = appointments.Select(a => a.ServiceId).Distinct();
            var services = await _context.Services
                .Where(s => completedServiceIds.Contains(s.Id))
                .ToListAsync();

            var distributionData = services.Select(s => {
            int countForService = appointments.Count(a => a.ServiceId == s.Id);
            
            double percentage = 0;
            if (totalAppointments > 0)
            {
                percentage = ((double)countForService / (double)totalAppointments) * 100;
            }
            
            decimal totalRevenue = appointments.Where(a => a.ServiceId == s.Id).Sum(a => a.FinalPrice ?? 0);

            return new {
                ServiceName = s.Name,
                Description = s.Description,
                Count = countForService,
                Percentage = Math.Round(percentage, 1),
                TotalRevenue = totalRevenue
            };
        }).OrderByDescending(x => x.Count).ToList();

            ViewBag.TotalAppointments = totalAppointments;
            ViewBag.DistributionData = distributionData;
            return View();
        }

        public async Task<IActionResult> Appointments(string searchString, string mechanicId, int? serviceId, DateTime? dateFilter, DateTime? weekDate)
        {
            var query = _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Mechanic)
                .Include(a => a.Service)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a => 
                    (a.Client != null && a.Client.FirstName.Contains(searchString)) ||
                    (a.Client != null && a.Client.LastName.Contains(searchString)) ||
                    (a.Client != null && a.Client.Email.Contains(searchString)) ||
                    a.CarMake.Contains(searchString) ||
                    a.CarModel.Contains(searchString)
                );
            }

            if (!string.IsNullOrEmpty(mechanicId) && int.TryParse(mechanicId, out int parsedMechId))
            {
                query = query.Where(a => a.MechanicId == parsedMechId);
            }

            if (serviceId.HasValue)
            {
                query = query.Where(a => a.ServiceId == serviceId.Value);
            }

            if (dateFilter.HasValue)
            {
                var utcFilterDate = DateTime.SpecifyKind(dateFilter.Value.Date, DateTimeKind.Utc);
                query = query.Where(a => a.AppointmentDate.Date == utcFilterDate);
            }
            var appointments = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();

            var targetDate = weekDate ?? DateTime.Today;
            int diff = (7 + (targetDate.DayOfWeek - DayOfWeek.Monday)) % 7;
            var startOfWeek = targetDate.AddDays(-diff).Date;
            var endOfWeek = startOfWeek.AddDays(7).Date;

            ViewBag.Mechanics = await _context.Mechanics.ToListAsync();
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.CurrentSearch = searchString;
            ViewBag.SelectedMechanic = mechanicId;
            ViewBag.SelectedService = serviceId;
            ViewBag.SelectedDate = dateFilter?.ToString("yyyy-MM-dd");
            
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek.AddDays(-1);
            ViewBag.PrevWeekDate = startOfWeek.AddDays(-7).ToString("yyyy-MM-dd");
            ViewBag.NextWeekDate = startOfWeek.AddDays(7).ToString("yyyy-MM-dd");

            return View(appointments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeAppointmentStatus(int id, AppointmentStatus status)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = status;
                await _context.SaveChangesAsync();

                string statusText = status switch
                {
                    AppointmentStatus.Accepted => "confirmată",
                    AppointmentStatus.Canceled => "respinsă / anulată",
                    AppointmentStatus.Done => "finalizată",
                    _ => "actualizată"
                };

                var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));

                var notification = new Notification
                {
                    UserId = appointment.ClientId,
                    Title = "Status programare actualizat",
                    Message = $"Programarea ta pentru {appointment.CarMake} {appointment.CarModel} a fost {statusText}.",
                    Url = $"/Appointment/Chat/{appointment.Id}",
                    CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Statusul programării a fost actualizat!";
            }
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleAppointment(int id, DateTime newDate)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.AppointmentDate = DateTime.SpecifyKind(newDate, DateTimeKind.Utc);
                appointment.Status = AppointmentStatus.Rescheduled;
                await _context.SaveChangesAsync();
                var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));
                var notification = new Notification
                {
                    UserId = appointment.ClientId,
                    Title = "Programare reprogramată",
                    Message = $"Programarea ta a fost reprogramată pentru data de {appointment.AppointmentDate:dd/MM/yyyy HH:mm}.",
                    Url = $"/Appointment/Chat/{appointment.Id}",
                    CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Programarea a fost reprogramată.";
            }
            return RedirectToAction(nameof(Appointments));
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentChat(int id)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Mechanic)
                .Include(a => a.Service)
                .Include(a => a.Messages)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            var unreadMessages = appointment.Messages.Where(m => !m.IsAdmin && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            var messages = await _context.AppointmentMessages
                .Include(m => m.Sender)
                .Where(m => m.AppointmentId == id)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            ViewBag.Messages = messages;
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendAppointmentMessage(int appointmentId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return RedirectToAction(nameof(AppointmentChat), new { id = appointmentId });
            }

            var adminUser = await _userManager.GetUserAsync(User);
            var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));

            var message = new AppointmentMessage
            {
                AppointmentId = appointmentId,
                SenderId = adminUser.Id,
                IsAdmin = true,
                Text = text,
                CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
            };

            _context.AppointmentMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mesajul a fost trimis clientului.";

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            var notification = new Notification
            {
                UserId = appointment.ClientId,
                Title = "Mesaj nou de la service",
                Message = "Ai primit un mesaj nou legat de programarea ta.",
                Url = $"/Appointment/Chat/{appointmentId}",
                CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AppointmentChat), new { id = appointmentId });
        }

        //Mecanici
        public async Task<IActionResult> Mechanics()
        {
            var mechanics = await _context.Mechanics
                .Include(m => m.User)
                .ToListAsync();

            await PopulateAvailableUsers(mechanics);
            return View(mechanics);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMechanic()
        {
            await PopulateAvailableUsers();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMechanic(Mechanic model, IFormFile? photo)
        {
            ModelState.Remove(nameof(model.PhotoUrl));
            ModelState.Remove(nameof(model.ProfilePictureUrl));

            if (ModelState.IsValid)
            {
                if (photo != null && photo.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", fileName);

                    var directory = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photo.CopyToAsync(stream);
                    }
                    model.PhotoUrl = "/uploads/" + fileName;
                }
                else
                {
                    model.PhotoUrl = "/images/default-avatar.png";
                }

                model.ProfilePictureUrl = model.PhotoUrl;
                model.IsPictureApproved = true;

                if (!string.IsNullOrEmpty(model.UserId))
                {
                    var user = await _userManager.FindByIdAsync(model.UserId);
                    if (user == null || await _context.Mechanics.AnyAsync(m => m.UserId == model.UserId))
                    {
                        ModelState.AddModelError(nameof(model.UserId), "Alege un cont disponibil pentru mecanic.");
                        await PopulateAvailableUsers();
                        return View(model);
                    }

                    if (!await _userManager.IsInRoleAsync(user, "Mecanic"))
                        await _userManager.AddToRoleAsync(user, "Mecanic");

                    var clientProf = await _context.ClientProfiles.FirstOrDefaultAsync(cp => cp.UserId == model.UserId);
                    if (clientProf != null)
                    {
                        _context.ClientProfiles.Remove(clientProf);
                    }
                }

                _context.Mechanics.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Mecanicul a fost adăugat cu succes!";
                return RedirectToAction(nameof(Mechanics));
            }
            
            await PopulateAvailableUsers();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMechanic(int id)
        {
            var mechanic = await _context.Mechanics.FindAsync(id);
            if (mechanic != null)
            {
                _context.Mechanics.Remove(mechanic);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Mecanicul a fost șters!";
            }
            return RedirectToAction(nameof(Mechanics));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeUserRole(string userId, string newRole)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost găsit.";
                return RedirectToAction(nameof(UsersList));
            }

            if (newRole != "Client" && newRole != "Mecanic")
            {
                TempData["ErrorMessage"] = "Rol invalid.";
                return RedirectToAction(nameof(UsersList));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            if (currentRoles.Contains(newRole))
            {
                TempData["SuccessMessage"] = "Utilizatorul are deja acest rol.";
                return RedirectToAction(nameof(UsersList));
            }

            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }

            await _userManager.AddToRoleAsync(user, newRole);

            if (newRole == "Mecanic")
            {
                var mechanicExists = await _context.Mechanics.AnyAsync(m => m.UserId == userId);
                if (!mechanicExists)
                {
                    var mechanic = new Mechanic
                    {
                        UserId = userId,
                        FirstName = user.FirstName ?? "Mecanic",
                        LastName = user.LastName ?? "Nou",
                        PhotoUrl = "/images/default-avatar.png",
                        ProfilePictureUrl = "/images/default-avatar.png",
                        IsPictureApproved = true,
                        Rating = 5.0
                    };
                    _context.Mechanics.Add(mechanic);
                    
                    var clientProfile = await _context.ClientProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
                    if (clientProfile != null)
                    {
                        _context.ClientProfiles.Remove(clientProfile);
                    }

                    await _context.SaveChangesAsync();
                }
            }
            else if (newRole == "Client")
            {
                var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == userId);
                if (mechanic != null)
                {
                    mechanic.UserId = null;
                    await _context.SaveChangesAsync();
                }
            }

            TempData["SuccessMessage"] = $"Rolul utilizatorului a fost schimbat cu succes în {newRole}!";
            return RedirectToAction(nameof(UsersList));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkMechanicUser(int mechanicId, string userId)
        {
            var mechanic = await _context.Mechanics.FindAsync(mechanicId);
            var user = string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
            var alreadyLinked = !string.IsNullOrEmpty(userId) && await _context.Mechanics.AnyAsync(m => m.UserId == userId && m.Id != mechanicId);
            
            if (mechanic != null && user != null && !alreadyLinked)
            {
                mechanic.UserId = userId;

                var clientProf = await _context.ClientProfiles.FirstOrDefaultAsync(cp => cp.UserId == userId);
                if (clientProf != null)
                {
                    _context.ClientProfiles.Remove(clientProf);
                }

                await _context.SaveChangesAsync();

                if (!await _userManager.IsInRoleAsync(user, "Mecanic"))
                {
                    await _userManager.AddToRoleAsync(user, "Mecanic");
                }

                TempData["SuccessMessage"] = "Contul a fost legat de mecanic și i s-a atribuit rolul de Mecanic!";
            }
            else
            {
                TempData["ErrorMessage"] = "Contul ales nu este disponibil.";
            }
                
            return RedirectToAction(nameof(Mechanics));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlinkMechanicUser(int mechanicId)
        {
            var mechanic = await _context.Mechanics.FindAsync(mechanicId);
            if (mechanic != null)
            {
                mechanic.UserId = null;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Contul a fost deconectat de la mecanic.";
            }
            return RedirectToAction(nameof(Mechanics));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePicture(int id)
        {
            var mechanic = await _context.Mechanics.FindAsync(id);
            if (mechanic != null)
            {
                if (!string.IsNullOrEmpty(mechanic.ProfilePictureUrl) && mechanic.ProfilePictureUrl != mechanic.PhotoUrl)
                {
                    mechanic.PhotoUrl = mechanic.ProfilePictureUrl; 
                }
                
                mechanic.ProfilePictureUrl = null;              
                mechanic.IsPictureApproved = true;              

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Poză aprobată și actualizată cu succes!";
            }
            return RedirectToAction(nameof(Mechanics));
        }

        //Servicii
        public async Task<IActionResult> Services()
        {
            var services = await _context.Services.OrderBy(s => s.Name).ToListAsync();
            return View(services);
        }

        [HttpGet]
        public IActionResult CreateService()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(Service model)
        {
            if (ModelState.IsValid)
            {
                _context.Services.Add(model);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Serviciul a fost adăugat cu succes!";
                return RedirectToAction(nameof(Services));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            return service == null ? NotFound() : View(service);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, Service model)
        {
            if (id != model.Id) return NotFound();
            if (!ModelState.IsValid) return View(model);

            _context.Services.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Serviciul a fost actualizat.";
            return RedirectToAction(nameof(Services));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service != null)
            {
                var inUse = await _context.Appointments.AnyAsync(a => a.ServiceId == id);
                if (inUse)
                {
                    TempData["ErrorMessage"] = "Acest serviciu are programări asociate și nu poate fi șters.";
                    return RedirectToAction(nameof(Services));
                }

                _context.Services.Remove(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Serviciul a fost șters!";
            }
            return RedirectToAction(nameof(Services));
        }

        private async Task PopulateAvailableUsers(IEnumerable<Mechanic>? existingMechanics = null)
        {
            var linkedUserIds = (existingMechanics ?? await _context.Mechanics.ToListAsync())
                .Where(m => !string.IsNullOrEmpty(m.UserId)).Select(m => m.UserId!).ToHashSet();
            var adminIds = (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id).ToHashSet();
            ViewBag.UnlinkedUsers = await _userManager.Users
                .Where(u => !linkedUserIds.Contains(u.Id) && !adminIds.Contains(u.Id))
                .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ThenBy(u => u.Email)
                .ToListAsync();
        }

        public async Task<IActionResult> AllChats()
        {
            var appointments = await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Include(a => a.Mechanic)
                .Include(a => a.Messages)
                .Where(a => a.Status == AppointmentStatus.Pending || 
                            a.Status == AppointmentStatus.Accepted || 
                            a.Status == AppointmentStatus.Rescheduled)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();

            return View(appointments);
        }

        public async Task<IActionResult> ServiceDistribution()
        {
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UsersList()
        {
            var mechanicUserIds = await _context.Mechanics
                .Where(m => m.UserId != null)
                .Select(m => m.UserId!)
                .ToListAsync();

            var misplacedProfiles = await _context.ClientProfiles
                .Where(cp => mechanicUserIds.Contains(cp.UserId))
                .ToListAsync();

            if (misplacedProfiles.Any())
            {
                _context.ClientProfiles.RemoveRange(misplacedProfiles);
                await _context.SaveChangesAsync();
            }

            var clientUsers = await (from user in _context.Users
                                    join profile in _context.ClientProfiles on user.Id equals profile.UserId
                                    where !mechanicUserIds.Contains(user.Id)
                                    orderby user.Email
                                    select new ClientUserViewModel
                                    {
                                        UserId = user.Id,
                                        Email = user.Email,
                                        FirstName = profile.FirstName,
                                        LastName = profile.LastName,
                                        PhoneNumber = profile.PhoneNumber,
                                        EmailConfirmed = user.EmailConfirmed
                                    }).ToListAsync();

            return View(clientUsers);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("Utilizatorul nu a fost găsit.");

            var clientProfile = await _context.ClientProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            
            if (clientProfile == null)
            {
                TempData["ErrorMessage"] = "Acest utilizator nu deține un profil de client.";
                return RedirectToAction(nameof(UsersList));
            }

            var appointments = await _context.Appointments
                .Include(a => a.Mechanic)
                .Include(a => a.Service)
                .Include(a => a.Review)
                .Where(a => a.ClientId == user.Id || a.ClientId == userId)
                .ToListAsync();

            var favoriteMechanic = appointments
                .Where(a => a.Mechanic != null)
                .GroupBy(a => $"{a.Mechanic.FirstName} {a.Mechanic.LastName}")
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "Niciunul";

            int completedAppointmentsCount = appointments.Count(a => a.Status == AppointmentStatus.Done);

            var ratingsList = appointments
                .Where(a => a.Review != null)
                .Select(a => (double)a.Review.Rating)
                .ToList();

            double averageRatingGiven = ratingsList.Any() ? ratingsList.Average() : 0;

            string mostUsedService = appointments
                .Where(a => a.Service != null)
                .GroupBy(a => a.Service.Name)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault() ?? "Niciunul";

            decimal totalPaid = appointments
            .Where(a => a.Status == AppointmentStatus.Done)
            .Sum(a => a.FinalPrice ?? 0);

            var lastCompletedDate = appointments
                .Where(a => a.Status == AppointmentStatus.Done)
                .OrderByDescending(a => a.AppointmentDate)
                .Select(a => (DateTime?)a.AppointmentDate)
                .FirstOrDefault();

            ViewBag.ClientProfile = clientProfile;
            ViewBag.UserEmail = user.Email;
            ViewBag.FavoriteMechanic = favoriteMechanic;
            ViewBag.CompletedCount = completedAppointmentsCount;
            ViewBag.AverageRating = averageRatingGiven;
            ViewBag.MostUsedService = mostUsedService;
            ViewBag.TotalPaid = totalPaid;
            ViewBag.LastCompletedDate = lastCompletedDate;
            ViewBag.Appointments = appointments;

            return View(user);
        }

        public async Task<IActionResult> ChatHistory(string searchString, int? serviceId, string mechanicId, DateTime? searchDate)
        {
            var query = _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Service)
                .Include(a => a.Mechanic)
                .Include(a => a.Messages)
                .Where(a => a.Status == AppointmentStatus.Done || 
                            a.Status == AppointmentStatus.Canceled || 
                            a.Status == AppointmentStatus.Rejected)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();
                query = query.Where(a => 
                    (a.Client != null && (a.Client.FirstName.ToLower().Contains(searchLower) || a.Client.LastName.ToLower().Contains(searchLower) || 
                                            (a.Client.FirstName.ToLower() + " " + a.Client.LastName.ToLower()).Contains(searchLower) ||
                                            (a.Client.LastName.ToLower() + " " + a.Client.FirstName.ToLower()).Contains(searchLower) ||
                                            a.Client.Email.ToLower().Contains(searchLower) || (a.Client.PhoneNumber != null && a.Client.PhoneNumber.Contains(searchLower))
                    )) || 
                    a.CarModel.ToLower().Contains(searchLower) || a.CarMake.ToLower().Contains(searchLower)
                );
            }

            if (searchDate.HasValue)
            {
                var utcSearchDate = DateTime.SpecifyKind(searchDate.Value.Date, DateTimeKind.Utc);
                query = query.Where (a => a.AppointmentDate.Date == utcSearchDate);
            }
            
            if (serviceId.HasValue)
            {
                query = query.Where(a => a.ServiceId == serviceId.Value);
            }

            if (!string.IsNullOrEmpty(mechanicId) && int.TryParse(mechanicId, out int parsedMechId))
            {
                query = query.Where(a => a.MechanicId == parsedMechId);
            }

            var appointments = await query.OrderByDescending(a => a.AppointmentDate).ToListAsync();
            
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.Mechanics = await _context.Mechanics.ToListAsync();
            ViewBag.CurrentSearch = searchString;
            ViewBag.SelectedService = serviceId;
            ViewBag.SelectedMechanic = mechanicId;
            ViewBag.SelectedDate = searchDate?.ToString("yyyy-MM-dd");
            
            return View(appointments);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            ViewBag.Clients = await _userManager.GetUsersInRoleAsync("Client");
            ViewBag.Services = await _context.Services.ToListAsync();
            ViewBag.Mechanics = await _context.Mechanics.ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment(string? clientId, string clientFirstName, string clientLastName, string clientPhone, int serviceId, int mechanicId, DateTime appointmentDate, string carMake, string carModel, string? notes)
        {
            if (string.IsNullOrWhiteSpace(carMake) || string.IsNullOrWhiteSpace(carModel))
            {
                TempData["ErrorMessage"] = "Marca și modelul mașinii sunt obligatorii.";
                return RedirectToAction(nameof(Appointments));
            }

            if (string.IsNullOrWhiteSpace(clientFirstName) || string.IsNullOrWhiteSpace(clientPhone))
            {
                TempData["ErrorMessage"] = "Numele și numărul de telefon ale clientului sunt obligatorii.";
                return RedirectToAction(nameof(Appointments));
            }

            string finalFirstName = clientFirstName.Trim();
            string finalLastName = clientLastName?.Trim() ?? string.Empty;
            string finalPhone = clientPhone.Trim();

            if (!string.IsNullOrEmpty(clientId))
            {
                var existingUser = await _userManager.FindByIdAsync(clientId);
                if (existingUser != null)
                {
                    finalFirstName = existingUser.FirstName ?? finalFirstName;
                    finalLastName = existingUser.LastName ?? finalLastName;
                    if (!string.IsNullOrEmpty(existingUser.PhoneNumber))
                    {
                        finalPhone = existingUser.PhoneNumber;
                    }
                }
            }

            var appointment = new Appointment
            {
                ClientId = string.IsNullOrEmpty(clientId) ? null : clientId,
                ServiceId = serviceId,
                MechanicId = mechanicId,
                AppointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc),
                CarMake = carMake.Trim(),
                CarModel = carModel.Trim(),
                Notes = notes,
                Status = AppointmentStatus.Accepted 
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Programarea a fost creată cu succes!";
            return RedirectToAction(nameof(Appointments));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteAppointment(int id, int duration, decimal price)
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Done;
                appointment.FinalDurationMinutes = duration;
                appointment.FinalPrice = price;
                await _context.SaveChangesAsync();

                var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));
                var notification = new Notification
                {
                    UserId = appointment.ClientId,
                    Title = "Programare finalizată",
                    Message = $"Programarea ta a fost finalizată. A durat {duration} min, iar costul total este {price} MDL.",
                    Url = $"/Appointment/Chat/{appointment.Id}",
                    CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Programarea a fost marcată ca finalizată cu succes!";
            }
            return RedirectToAction(nameof(Appointments));
        }
    }

    public class ClientUserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
    }
}