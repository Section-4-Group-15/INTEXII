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
        public IActionResult Products(int pageNum = 1, List<string>? categoryDescriptions = null)
        {
            int pageSize = 5;
            pageNum = Math.Max(1, pageNum);

            // Start with the base query for products and include category descriptions through a join
            var query = from p in context.Products
                        join pc in context.ProductCategories on p.ProductId equals pc.ProductId
                        join c in context.Categories on pc.CategoryId equals c.CategoryId
                        select new
                        {
                            Product = p,
                            CategoryDescription = c.CategoryDescription
                        };

            // If category filters are applied
            if (categoryDescriptions != null && categoryDescriptions.Any())
            {
                query = query.Where(p => categoryDescriptions.Contains(p.CategoryDescription));
            }

            // Pagination
            //var totalItems = query.Count();
            var totalItems = 10;
            var paginatedQuery = query
                                 .OrderBy(x => x.Product.Name)
                                 .Skip((pageNum - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToList();

            var productsWithCategories = paginatedQuery.ToDictionary(
                key => key.Product,
                value => value.CategoryDescription
            );

            var model = new ProjectsListViewModel
            {
                Products = productsWithCategories.Keys.ToList(),
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = pageNum,
                    ItemsPerPage = pageSize,
                    TotalItems = totalItems
                }
            };

            // Pass category descriptions to the view via ViewBag or ViewData
            ViewBag.Categories = productsWithCategories;

            return View(model);
        }



        public IActionResult Error()
        {
            return View();
        }
    }

}
