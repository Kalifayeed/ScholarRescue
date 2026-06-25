using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarRescue.Models;
using ScholarRescue.Models.Enums;
using ScholarRescue.Services;

namespace ScholarRescue.Controllers
{
    /// <summary>
    /// Writer Knowledge Center. Verified writers and admins can browse
    /// FAQ, writing guides, citation guides, checklists, and platform rules.
    /// </summary>
    [Authorize(Roles = "Writer,Administrator")]
    public class WriterResourcesController : Controller
    {
        private readonly IWriterResourceService _resourceService;
        private readonly IWriterApplicationService _writerApplicationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WriterResourcesController> _logger;

        public WriterResourcesController(
            IWriterResourceService resourceService,
            IWriterApplicationService writerApplicationService,
            UserManager<ApplicationUser> userManager,
            ILogger<WriterResourcesController> logger)
        {
            _resourceService = resourceService;
            _writerApplicationService = writerApplicationService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Knowledge Center landing page showing all available resource categories.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] = "Your writer application must be approved to access the Knowledge Center.";
                return RedirectToAction("Dashboard", "Writers");
            }

            ViewBag.FaqCount = (await _resourceService.GetFaqAsync()).Count;
            ViewBag.WriterRulesCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.WriterRules)).Count;
            ViewBag.WritingGuideCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.WritingGuide)).Count;
            ViewBag.CitationGuidesCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.CitationGuides)).Count;
            ViewBag.FormattingGuideCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.FormattingGuide)).Count;
            ViewBag.WriterChecklistCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.WriterChecklist)).Count;
            ViewBag.AcademicResourcesCount = (await _resourceService.GetByCategoryAsync(WriterResourceCategory.AcademicResources)).Count;

            return View();
        }

        /// <summary>
        /// FAQ page with search and category filtering.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Faq(string? q, string? subCategory)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] = "Your writer application must be approved to access the Knowledge Center.";
                return RedirectToAction("Dashboard", "Writers");
            }

            List<WriterResource> items;

            if (!string.IsNullOrWhiteSpace(q))
            {
                items = await _resourceService.SearchFaqAsync(q);
                ViewBag.SearchQuery = q;
            }
            else
            {
                items = await _resourceService.GetFaqAsync();
            }

            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                items = items.Where(i => i.SubCategory == subCategory).ToList();
                ViewBag.SelectedSubCategory = subCategory;
            }

            ViewBag.SubCategories = await _resourceService.GetSubCategoriesAsync(WriterResourceCategory.FAQ);

            return View(items);
        }

        /// <summary>
        /// Generic category page for WriterRules, WritingGuide, FormattingGuide,
        /// WriterChecklist, AcademicResources.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Category(WriterResourceCategory id, string? subCategory)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] = "Your writer application must be approved to access the Knowledge Center.";
                return RedirectToAction("Dashboard", "Writers");
            }

            var items = await _resourceService.GetByCategoryAsync(id);

            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                items = items.Where(i => i.SubCategory == subCategory).ToList();
                ViewBag.SelectedSubCategory = subCategory;
            }

            ViewBag.Category = id;
            ViewBag.CategoryName = GetCategoryDisplayName(id);
            ViewBag.SubCategories = await _resourceService.GetSubCategoriesAsync(id);

            return View(items);
        }

        /// <summary>
        /// View a single resource article.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Article(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (!User.IsInRole("Administrator") &&
                !await _writerApplicationService.IsWriterActiveAsync(currentUser.Id))
            {
                TempData["ErrorMessage"] = "Your writer application must be approved to access the Knowledge Center.";
                return RedirectToAction("Dashboard", "Writers");
            }

            var resource = await _resourceService.GetByIdAsync(id);
            if (resource == null || !resource.IsActive)
                return NotFound();

            ViewBag.CategoryName = GetCategoryDisplayName(resource.Category);

            return View(resource);
        }

        private static string GetCategoryDisplayName(WriterResourceCategory category)
        {
            return category switch
            {
                WriterResourceCategory.FAQ => "FAQ",
                WriterResourceCategory.WriterRules => "Writer Rules",
                WriterResourceCategory.WritingGuide => "Writing Guide",
                WriterResourceCategory.CitationGuides => "Citation Guides",
                WriterResourceCategory.FormattingGuide => "Formatting Guide",
                WriterResourceCategory.WriterChecklist => "Writer Checklist",
                WriterResourceCategory.AcademicResources => "Academic Resources",
                _ => category.ToString()
            };
        }

        /// <summary>
        /// Returns resources matching a department for knowledge base suggestions.
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ByDepartment(string department)
        {
            try
            {
                if (string.IsNullOrEmpty(department))
                    return Json(new { success = true, resources = new object[0] });

                // Map department to relevant resource categories
                var categories = department switch
                {
                    "GeneralSupport" => new[] { WriterResourceCategory.FAQ, WriterResourceCategory.WriterRules },
                    "Orders" => new[] { WriterResourceCategory.WritingGuide, WriterResourceCategory.WriterChecklist },
                    "WriterApplications" => new[] { WriterResourceCategory.WriterRules, WriterResourceCategory.AcademicResources },
                    "BillingPayments" => new[] { WriterResourceCategory.FAQ },
                    "DisputesCompliance" => new[] { WriterResourceCategory.WriterRules, WriterResourceCategory.FAQ },
                    "TechnicalSupport" => new[] { WriterResourceCategory.FAQ, WriterResourceCategory.FormattingGuide },
                    "Administration" => new[] { WriterResourceCategory.WriterRules, WriterResourceCategory.WriterChecklist },
                    _ => new[] { WriterResourceCategory.FAQ }
                };

                var resources = await _resourceService.GetAllForAdminAsync(null);
                var filtered = resources
                    .Where(r => r.IsActive && categories.Contains(r.Category))
                    .Take(5)
                    .Select(r => new { id = r.Id, title = r.Title, summary = r.Content.Length > 150 ? r.Content.Substring(0, 150) + "..." : r.Content, category = r.Category.ToString() })
                    .ToList();

                return Json(new { success = true, resources = filtered });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading KB suggestions for department {Department}.", department);
                return Json(new { success = false, resources = new object[0] });
            }
        }
    }
}
