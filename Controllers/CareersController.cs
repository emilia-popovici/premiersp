using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    public class CareersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly Supabase.Client _supabase;
        public CareersController(ApplicationDbContext context, Supabase.Client supabase)
        {
            _context = context;
            _supabase = supabase;
        }

        public async Task<IActionResult> Index()
        {
            var jobs = await _context.JobPositions.ToListAsync();
            return View(jobs);
        }

        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            var job = await _context.JobPositions.FindAsync(id);
            if (job == null || !job.IsHiring) return RedirectToAction(nameof(Index));

            ViewBag.JobTitle = job.Title;
            var application = new JobApplication { JobPositionId = id };
            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(JobApplication model, IFormFile? cvFile)
        {
            var job = await _context.JobPositions.FindAsync(model.JobPositionId);
            if (job == null || !job.IsHiring) return NotFound();

            if (ModelState.IsValid)
            {
                if (cvFile != null && cvFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(cvFile.FileName);

                    using var memoryStream = new MemoryStream();
                    await cvFile.CopyToAsync(memoryStream);
                    var fileBytes = memoryStream.ToArray();

                    await _supabase.Storage
                        .From("premier-sp-auto-cvs")
                        .Upload(fileBytes, fileName);

                    model.CvUrl = fileName; 
                }

                var bucharestTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Bucharest"));
                model.SubmittedAt = DateTime.SpecifyKind(bucharestTime, DateTimeKind.Utc);
                model.Status = ApplicationStatus.Nou;

                _context.JobApplications.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Aplicația ta a fost trimisă cu succes! Te vom contacta curând.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.JobTitle = job.Title;
            return View(model);
        }
    }
}