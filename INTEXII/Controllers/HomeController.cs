using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using INTEXII.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace INTEXII.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BrickwellContext context { get; set; }

        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ILogger<HomeController> logger, BrickwellContext con, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            context = con;
        }

        public IActionResult Index()
        {
            var products = context.Products.ToList();
            return View(products);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Products(int pageNum, List<string> categories, List<string> colors, int pageSize = 5)
        {
            // Ensure pageNum is at least 1 to avoid negative offset
            pageNum = Math.Max(1, pageNum);

            // Fetch distinct categories from Category_1
            var categories1 = context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category_1))
                .Select(p => p.Category_1)
                .Distinct()
                .ToList();

            // Fetch distinct categories from Category_2
            var categories2 = context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category_2))
                .Select(p => p.Category_2)
                .Distinct()
                .ToList();

            // Combine both category lists and remove duplicates
            var allCategories = categories1.Concat(categories2).Distinct().OrderBy(c => c).ToList();

            // Pass all categories to the view
            ViewData["Model"] = allCategories;

            // Fetch distinct colors from Primary_Color
            var allColors = context.Products
                .Where(p => !string.IsNullOrEmpty(p.Primary_Color))
                .Select(p => p.Primary_Color)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Pass all colors to the view
            ViewData["Colors"] = allColors;

            // Start with the base query
            IQueryable<Product> query = context.Products.OrderBy(x => x.Name);

            if (categories != null && categories.Any())
            {
                // Filter products based on selected categories
                query = query.Where(p => categories.Contains(p.Category_1) || categories.Contains(p.Category_2)).OrderBy(x => x.Name);
            }

            if (colors != null && colors.Any())
            {
                // Filter products based on selected colors (AND logic with categories)
                query = query.Where(p => colors.Contains(p.Primary_Color) &&
                                          (categories == null || !categories.Any() ||
                                           categories.Contains(p.Category_1) || categories.Contains(p.Category_2))).OrderBy(x => x.Name);
            }

            var model = new ProjectsListViewModel
            {
                Products = query.Skip((pageNum - 1) * pageSize)
                                .Take(pageSize)
                                .ToList(),

                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = pageNum,
                    ItemsPerPage = pageSize,
                    TotalItems = query.Count()
                }
            };

            ViewBag.SelectedCategories = categories; // Pass selected categories to the view
            ViewBag.SelectedColors = colors; // Pass selected colors to the view
            ViewBag.PageSize = pageSize; // Pass the page size to the view

            return View(model);
        }




        public IActionResult Error()
        {
            return View();
        }

        // Admin Controller
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                return RedirectToAction("Index"); // Redirect to the appropriate page
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return RedirectToAction("Error"); // Redirect to the appropriate page
            }
        }
    }

}