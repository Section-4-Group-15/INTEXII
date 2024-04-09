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
        public IActionResult Products(int pageNum)
        {
            int pageSize = 5;

            // Ensure pageNum is at least 1 to avoid negative offset
            pageNum = Math.Max(1, pageNum);

            var query = context.Products.OrderBy(x => x.Name);

            var blah = new ProjectsListViewModel
            {
                Products = query.Skip((pageNum - 1) * pageSize)
                                .Take(pageSize)
                                .ToList(),

                PaginationInfo = new PaginationInfo
                {
                    CurrentPage = pageNum,
                    ItemsPerPage = pageSize,
                    TotalItmes = query.Count()
                }
            };

            return View(blah);
        }
        public IActionResult Error()
        {
            return View();
        }
    }

}
