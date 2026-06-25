using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarRescue.Data;
using ScholarRescue.Models;
using ScholarRescue.Services;

namespace ScholarRescue.Controllers
{
    public class HomeController : Controller
    {
        private readonly ScholarRescueDbContext _context;
        private readonly ILogger<HomeController> _logger;
        private readonly ICacheService _cache;

        public HomeController(ScholarRescueDbContext context, ILogger<HomeController> logger, ICacheService cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
        }

        [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Tutoring()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult ResearchGuidance()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Editing()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Proofreading()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult CitationAssistance()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult FormattingAssistance()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult BecomeATutor()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Opportunities()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult FAQ()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Reviews()
        {
            return View();
        }

        [ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any)]
        public IActionResult Blog()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactMessage());
        }

        /// <summary>
        /// Public 'Order Now' entry point. Branches based on the visitor's authentication status and role.
        /// - Guests: redirected to register with a message.
        /// - Clients: redirected to the create-order form.
        /// - Writers/Admins: shown a notice that only client accounts can submit requests.
        /// </summary>
        [HttpGet]
        public IActionResult OrderNow()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                TempData["OrderNowMessage"] = "Create an account to submit your academic support request.";
                TempData["OrderNowShowLogin"] = "true";
                return RedirectToAction("Register", "Account");
            }

            if (User.IsInRole("Client"))
            {
                return RedirectToAction("Create", "Orders");
            }

            if (User.IsInRole("Writer") || User.IsInRole("Administrator"))
            {
                TempData["OrderNowMessage"] = "Only client accounts can submit requests.";
                TempData["OrderNowIsError"] = "true";
            }
            else
            {
                TempData["OrderNowMessage"] = "Only client accounts can submit requests.";
                TempData["OrderNowIsError"] = "true";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactMessage model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.SubmittedAt = DateTime.UtcNow;
                _context.ContactMessages.Add(model);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contact message received from {Name} ({Email})", model.Name, model.Email);
                TempData["ContactSuccess"] = "Thank you for your message! We'll get back to you within 24 hours.";
                return RedirectToAction(nameof(Contact));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving contact message from {Name}", model.Name);
                ModelState.AddModelError(string.Empty, "An error occurred while sending your message. Please try again.");
                return View(model);
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}