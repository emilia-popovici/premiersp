using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PremierAuto.Data;
using PremierAuto.Models;
using PremierAuto.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace PremierAuto.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public HomeController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (user != null && await _userManager.IsInRoleAsync(user, "Mecanic"))
                {
                    return RedirectToAction("Calendar", "Mechanic");
                }
            }

            var viewModel = new HomeViewModel
            {
                Services = await _context.Services.ToListAsync(),
                Mechanics = await _context.Mechanics.ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Services(string searchString)
        {
            var servicesQuery = _context.Services.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                var searchLower = searchString.ToLower();

                servicesQuery = servicesQuery.Where(s => 
                    s.Name.ToLower().Contains(searchLower) || 
                    s.Description.ToLower().Contains(searchLower));
            }

            ViewData["CurrentFilter"] = searchString;

            var services = await servicesQuery.ToListAsync();
            return View(services);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public async Task<IActionResult> MechanicDetails(int id)
        {
            var mechanic = await _context.Mechanics
                .Include(m => m.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mechanic == null) return NotFound();

            var reviews = await _context.Reviews
                .Include(r => r.Appointment)
                .ThenInclude(a => a.Client)
                .Where(r => r.Appointment.MechanicId == id)
                .OrderByDescending(r => r.Id)
                .ToListAsync();

            ViewBag.Reviews = reviews;

            return View(mechanic);
        }
    }
}
