using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using PremierAuto.ViewModels;
using System;
using System.Collections.Generic;

namespace PremierAuto.Controllers
{
    [Authorize]
    public class AppointmentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AppointmentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            IQueryable<Appointment> query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Service)
                .Include(a => a.Mechanic)
                .Include(a => a.Client)
                .Include(a => a.Review);

            if (User.IsInRole("Admin"))
            {
                var allAppointments = await query
                    .OrderByDescending(a => a.AppointmentDate)
                    .ToListAsync();
                return View(allAppointments);
            }

            if (User.IsInRole("Mecanic"))
            {
                var user = await _userManager.GetUserAsync(User);
                var mechanic = await _context.Mechanics.AsNoTracking()
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (mechanic == null)
                {
                    ViewBag.NotLinked = true;
                    return View(Enumerable.Empty<Appointment>());
                }

                var mechanicAppointments = await query
                    .Where(a => a.MechanicId == mechanic.Id && a.Status == AppointmentStatus.Accepted)
                    .OrderBy(a => a.AppointmentDate)
                    .ToListAsync();
                return View(mechanicAppointments);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var clientAppointments = await query
                .Where(a => a.ClientId == currentUser.Id)
                .OrderByDescending(a => a.AppointmentDate)
                .ToListAsync();
            return View(clientAppointments);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (!await EnsureClientAccess()) return Forbid();
            
            var viewModel = new AppointmentCreateViewModel
            {
                Services = await _context.Services
                    .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name })
                    .ToListAsync(),

                Mechanics = await _context.Mechanics
                    .Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.FirstName + " " + m.LastName })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AppointmentCreateViewModel model, DateTime selectedDate, string selectedTime)
        {
            if (!await EnsureClientAccess()) return Forbid();

            if (TimeSpan.TryParse(selectedTime, out TimeSpan parsedTime))
            {
                model.AppointmentDate = DateTime.SpecifyKind(selectedDate.Date + parsedTime, DateTimeKind.Utc);
            }
            else
            {
                ModelState.AddModelError("", "Te rugăm să selectezi o oră validă din listă.");
                await PopulateCreateLists(model);
                return View(model);
            }

            if (model.MechanicId.HasValue)
            {
                var service = await _context.Services.FindAsync(model.ServiceId);
                int durationMinutes = service != null ? service.DurationMinutes : 30;
                
                var newStart = model.AppointmentDate;
                var newEnd = newStart.AddMinutes(durationMinutes);

                var bookedAppointments = await _context.Appointments
                    .Include(a => a.Service)
                    .Where(a => a.MechanicId == model.MechanicId &&
                                a.AppointmentDate.Date == newStart.Date &&
                                a.Status == AppointmentStatus.Accepted)
                    .ToListAsync();

                bool conflict = bookedAppointments.Any(booked =>
                {
                    var bookedStart = booked.AppointmentDate;
                    int bookedDuration = booked.Service != null ? booked.Service.DurationMinutes : 30;
                    var bookedEnd = bookedStart.AddMinutes(bookedDuration);

                    return newStart < bookedEnd && bookedStart < newEnd;
                });

                if (conflict)
                {
                    ModelState.AddModelError(string.Empty, "Mecanicul ales este deja ocupat în acest interval orar. Te rugăm să alegi altă oră.");
                    await PopulateCreateLists(model);
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateCreateLists(model);
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            var appointment = new Appointment
            {
                ServiceId = model.ServiceId,
                MechanicId = model.MechanicId,
                AppointmentDate = model.AppointmentDate,
                CarMake = model.CarMake,
                CarModel = model.CarModel,
                Notes = model.Notes,
                ClientId = user.Id,
                Status = AppointmentStatus.Pending
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Programarea a fost trimisă! Vei fi anunțat când este confirmată.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableHours(int mechanicId, int serviceId, DateTime date)
        {
            DateTime utcDate = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            var service = await _context.Services.FindAsync(serviceId);
            int durationMinutes = service != null ? service.DurationMinutes : 30; 

            var workingHours = new List<TimeSpan>();
            var startTime = new TimeSpan(9, 0, 0);
            var endTime = new TimeSpan(17, 0, 0);

            for (var time = startTime; time + TimeSpan.FromMinutes(durationMinutes) <= endTime; time = time.Add(TimeSpan.FromMinutes(15)))
            {
                workingHours.Add(time);
            }

            if (utcDate.DayOfWeek == DayOfWeek.Saturday || utcDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return Json(new List<string>()); 
            }

            var bookedAppointments = await _context.Appointments
                .Include(a => a.Service)
                .Where(a => a.MechanicId == mechanicId 
                        && a.AppointmentDate.Date == utcDate.Date
                        && a.Status == AppointmentStatus.Accepted)
                .ToListAsync();

            var availableHours = new List<string>();

            foreach (var slot in workingHours)
            {
                var slotStart = utcDate.Date + slot;
                var slotEnd = slotStart.AddMinutes(durationMinutes);

                bool isOverlap = bookedAppointments.Any(booked =>
                {
                    var bookedStart = booked.AppointmentDate;
                    int bookedDuration = booked.Service != null ? booked.Service.DurationMinutes : 30;
                    var bookedEnd = bookedStart.AddMinutes(bookedDuration);

                    return slotStart < bookedEnd && bookedStart < slotEnd;
                });

                if (!isOverlap)
                {
                    if (utcDate.Date != DateTime.Today || slotStart.TimeOfDay > DateTime.UtcNow.TimeOfDay)
                    {
                        availableHours.Add(slot.ToString(@"hh\:mm"));
                    }
                }
            }

            return Json(availableHours);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!await EnsureClientAccess()) return Forbid();
            var user = await _userManager.GetUserAsync(User);
            var appointment = await _context.Appointments.FindAsync(id);

            if (appointment == null || appointment.ClientId != user.Id)
            {
                return Forbid();
            }

            if (appointment.Status == AppointmentStatus.Done)
            {
                TempData["ErrorMessage"] = "O programare finalizată nu mai poate fi anulată.";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = AppointmentStatus.Canceled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Programarea a fost anulată.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Mecanic")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsDone(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var mechanic = await _context.Mechanics.FirstOrDefaultAsync(m => m.UserId == user.Id);

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment != null && mechanic != null && appointment.MechanicId == mechanic.Id)
            {
                appointment.Status = AppointmentStatus.Done;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Programarea a fost marcată ca finalizată.";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> EnsureClientAccess()
        {
            if (User.IsInRole("Admin") || User.IsInRole("Mecanic")) return false;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return false;
            if (!await _userManager.IsInRoleAsync(user, "Client"))
                await _userManager.AddToRoleAsync(user, "Client");
            return true;
        }

        private async Task PopulateCreateLists(AppointmentCreateViewModel model)
        {
            model.Services = await _context.Services.Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name }).ToListAsync();
            model.Mechanics = await _context.Mechanics.Select(m => new SelectListItem { Value = m.Id.ToString(), Text = m.FirstName + " " + m.LastName }).ToListAsync();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Chat(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            
            var appointment = await _context.Appointments
                .Include(a => a.Service)
                .Include(a => a.Mechanic)
                .Include(a => a.Messages)
                .FirstOrDefaultAsync(a => a.Id == id && a.ClientId == user.Id);

            if (appointment == null) return NotFound("Nu am găsit programarea sau nu ai acces la ea.");

            int clientAppointmentNumber = await _context.Appointments
                .Where(a => a.ClientId == user.Id && a.AppointmentDate <= appointment.AppointmentDate)
                .CountAsync();
                
            ViewBag.AppointmentNumber = clientAppointmentNumber;
            
            bool shouldSave = false;

            var unreadMessages = appointment.Messages.Where(m => m.IsAdmin && !m.IsRead).ToList();
            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                shouldSave = true;
                await _context.SaveChangesAsync();
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Url.Contains(id.ToString()))
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notif in unreadNotifications)
                {
                    notif.IsRead = true;
                }
                shouldSave = true;
            }

            if (shouldSave)
            {
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
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage(int appointmentId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return RedirectToAction(nameof(Chat), new { id = appointmentId });
            }

            var user = await _userManager.GetUserAsync(User);
            
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == user.Id);
                
            if (appointment == null) return Unauthorized();
            var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));

            var message = new AppointmentMessage
            {
                AppointmentId = appointmentId,
                SenderId = user.Id,
                IsAdmin = false,
                Text = text,
                CreatedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc)
            };

            _context.AppointmentMessages.Add(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Chat), new { id = appointmentId });
        }
    }
}