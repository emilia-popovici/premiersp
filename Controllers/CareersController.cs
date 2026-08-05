using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
    public class CareersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CareersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
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
                    string webRootPath = _webHostEnvironment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsFolder = Path.Combine(webRootPath, "cv_uploads");
                    
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await cvFile.CopyToAsync(stream);
                    }
                    model.CvUrl = "/cv_uploads/" + fileName;
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