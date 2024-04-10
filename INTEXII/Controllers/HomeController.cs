using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using INTEXII.Models.ViewModels;

namespace INTEXII.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BrickwellContext context { get; set; }

        public HomeController(ILogger<HomeController> logger, BrickwellContext con = null)
        {
            _logger = logger;
            context = con;
        }

        public IActionResult Index()
        {
            var userEmail = User.Identity.IsAuthenticated ? User.Identity.Name : null;

            List<UserRec> recommendations = null;

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Retrieve user-specific recommendations based on their email
                recommendations = context.UserRecs
                    .Where(ur => ur.Email == userEmail)
                    .ToList();
            }
            //else
            //{
            //    // Fetch generic recommendations if no user is logged in or no user-specific recommendations are found
            //    recommendations = context.UserRecs
            //        .Where(ur => ur.Person_ID == 0)
            //        .ToList();
            //}

            ViewData["Recommendations"] = recommendations;

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
        [Authorize]
        public IActionResult Products(int pageNum, List<string> categories)
        {
            int pageSize = 5;

            // Ensure pageNum is at least 1 to avoid negative offset
            pageNum = Math.Max(1, pageNum);

            // Start with the base query
            IQueryable<Product> query = context.Products.OrderBy(x => x.Name);

            if (categories != null && categories.Count > 0)
            {
                // Filter products based on selected categories
                query = query.Where(p => categories.Contains(p.Category_1)).OrderBy(x => x.Name);
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

            return View(model);
        }


        public IActionResult Error()
        {
            return View();
        }
    }

}