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
        public IActionResult Products(int pageNum, List<string> categories)
        {
            int pageSize = 5;
            pageNum = Math.Max(1, pageNum);

            // Adjusted query to perform an inner join with ProductCategories and Categories
            var query = from p in context.Products
                        join pc in context.ProductCategories on p.ProductId equals pc.ProductId
                        join c in context.Categories on pc.CategoryId equals c.CategoryId
                        select new { Product = p, CategoryDescription = c.CategoryDescription };

            if (categories != null && categories.Count > 0)
            {
                // Assuming categories contains category descriptions
                query = query.Where(p => categories.Contains(p.CategoryDescription));
            }

            var productsWithCategories = query
                                         .OrderBy(x => x.Product.Name)
                                         .Skip((pageNum - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToList()
                                         .Select(x => new ProductViewModel
                                         {
                                             Product = x.Product,
                                             CategoryDescription = x.CategoryDescription
                                         });

            var model = new ProductsListViewModel
            {
                Products = productsWithCategories,
                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = pageNum,
                    ItemsPerPage = pageSize,
                    TotalItems = query.Count() // Adjust this count as necessary
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
