using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using INTEXII.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;

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

        private int GetCartItemCount()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var cartItems = context.CartProducts.Where(cp => cp.user_Id == userId);
                return cartItems.Sum(cp => cp.quantity);
            }

            return 0;
        }

        public IActionResult Index()
        {
            // User is authenticated, but no recommendations in table, that's why nothing is showing up. 
            var userEmail = User.Identity.IsAuthenticated ? User.Identity.Name : null;

            List<UserRec> recommendations = null;

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Retrieve user-specific recommendations based on their email
                recommendations = context.UserRecs
                    .Where(ur => ur.Email == userEmail)
                    .ToList();
            }

            // Retrieve 5 random products
            var randomProducts = context.Products
                .OrderBy(x => Guid.NewGuid())
                .Take(5)
                .ToList();

            // Pass the random products to the view
            ViewData["RandomProducts"] = randomProducts;

            // Pass the recommendations to the view
            ViewData["Recommendations"] = recommendations;

            // Calculate and pass the cart item count to the view
            ViewBag.CartItemCount = GetCartItemCount();

            return View();
        }

        public IActionResult About()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
        }

        public IActionResult Privacy()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
        }

        [HttpGet]
        public IActionResult CreateCookie()
        {
            var consentFeature = HttpContext.Features.Get<ITrackingConsentFeature>();
            consentFeature.GrantConsent();

            HttpContext.Response.Cookies.Append("ConsentCookie", "Consented", new CookieOptions
            {
                IsEssential = true,
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddYears(1)
            });

            return Ok();
        }

        public IActionResult Products(int pageNum = 1, List<string> categories = null, List<string> colors = null, int pageSize = 5)
        {
            // Calculate and pass the cart item count to the view
            ViewBag.CartItemCount = GetCartItemCount();

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

        public IActionResult IndProducts(int id)
        {
            ViewBag.CartItemCount = GetCartItemCount();

            var product = context.Products.FirstOrDefault(p => p.product_ID == id);

            if (product == null)
            {
                return NotFound(); // Return a 404 Not Found response if the product is not found
            }

            var productRecs = context.ProductRecs
                                     .Where(pr => pr.product_ID == id)
                                     .ToList();

            var model = new ProjectsListViewModel
            {
                Products = new List<Product> { product }, // Create a list with the product
                PaginationInfo = null, // You may need to populate this if required by the view
                ProductRecs = productRecs
            };

            return View(model);
        }
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var product = await context.Products.FindAsync(productId);

                if (product != null)
                {
                    var cartItem = await context.CartProducts.FirstOrDefaultAsync(cp => cp.user_Id == userId && cp.product_Id == productId);

                    if (cartItem == null)
                    {
                        cartItem = new CartProduct
                        {
                            user_Id = userId,
                            product_Id = productId,
                            quantity = quantity,
                            cost = (decimal)product.Price
                        };

                        context.CartProducts.Add(cartItem);
                    }
                    else
                    {
                        cartItem.quantity += quantity;
                    }

                    await context.SaveChangesAsync();
                    return Json(new { success = true, productName = product.Name });
                }
            }

            return Json(new { success = false });
        }
        [Authorize]
        public async Task<IActionResult> Cart()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);
                    ViewBag.CartItemCount = GetCartItemCount();

                    var cartItems = await context.CartProducts
                        .Where(cp => cp.user_Id == userId)
                        .ToListAsync();

                    // Retrieve the product names for each cart item
                    var productIds = cartItems.Select(cp => cp.product_Id).ToList();
                    var products = await context.Products
                        .Where(p => productIds.Contains(p.product_ID))
                        .ToListAsync();

                    // Create a dictionary to map product IDs to product names
                    var productNameDict = products.ToDictionary(p => p.product_ID, p => p.Name);

                    // Create a new list to store the cart items with product names
                    var cartItemsWithNames = new List<(CartProduct CartItem, string ProductName)>();

                    // Assign the product name to each cart item
                    foreach (var cartItem in cartItems)
                    {
                        if (productNameDict.TryGetValue(cartItem.product_Id, out var productName))
                        {
                            cartItemsWithNames.Add((cartItem, productName));
                        }
                    }

                    return View(cartItemsWithNames);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart");
                return RedirectToAction("Error");
            }
        }
        [Authorize]
        public async Task<IActionResult> CheckoutForm()
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);
                    var cartItems = await context.CartProducts
                        .Where(cp => cp.user_Id == userId)
                        .ToListAsync();

                    var subtotal = cartItems.Sum(item => item.cost * item.quantity);
                    ViewBag.Subtotal = subtotal.ToString("C");

                    return View();
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting checkout form");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> SubmitOrder(string address)
        {
            try
            {
                // Logic to create an order record goes here
                _logger.LogInformation($"Order submitted for address: {address}"); // DEBUG

                // Implement order confirmation popup or redirect as needed
                return RedirectToAction("OrderConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order");
                return RedirectToAction("Error");
            }
        }


        // Admin Controller
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminProducts()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            var products = await context.Products.OrderBy(p => p.product_ID).ToListAsync();
            return View(products);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(Product updatedProduct)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // First, check if the product exists in the database
                    var existingProduct = await context.Products
                        .FirstOrDefaultAsync(p => p.product_ID == updatedProduct.product_ID);

                    if (existingProduct != null)
                    {
                        // If the product exists, EF now tracks existingProduct, 
                        // Copy the updated values onto the tracked entity
                        context.Entry(existingProduct).CurrentValues.SetValues(updatedProduct);

                        await context.SaveChangesAsync();
                        return Json(new { success = true });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Product not found" });
                    }
                }
                return Json(new { success = false, message = "Invalid product data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return Json(new { success = false, message = "Error updating product" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddProduct(Product newProduct)
        {
            try
            {
                // Workaround for database-generated identity column, since we are using preexisting data
                var maxProductId = await context.Products.MaxAsync(p => (int?)p.product_ID) ?? 0;
                newProduct.product_ID = (int)(maxProductId + 1);

                if (ModelState.IsValid)
                {
                    context.Products.Add(newProduct);
                    await context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Invalid product data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                return Json(new { success = false, message = "Error adding product" });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int Product_Id)
        {
            try
            {
                var product = await context.Products.FindAsync(Product_Id);
                if (product != null)
                {
                    context.Products.Remove(product);
                    await context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return Json(new { success = false, message = "Error deleting product" });
            }
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AdminUsers()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminOrders(int page = 1)
        {
            ViewBag.CartItemCount = GetCartItemCount();

            int pageSize = 100; // Set page size

            try
            {
                // Order by Date descending, then by Time descending to get most recent orders first
                var ordersQuery = context.Orders
                                         .OrderByDescending(o => o.Date)
                                         .ThenByDescending(o => o.Time)
                                         .AsQueryable();

                var orders = await ordersQuery
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

                // Pass total order count and current page to the view for pagination
                ViewData["TotalOrders"] = ordersQuery.Count();
                ViewData["CurrentPage"] = page;

                return View(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return RedirectToAction("Error");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignRole(string userId)
        {
            ViewBag.CartItemCount = GetCartItemCount();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    await _userManager.AddToRoleAsync(user, "Admin");
                }
                return RedirectToAction("Index"); // Redirect to the appropriate page
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return RedirectToAction("Error"); // Redirect to the appropriate page
            }
        }
        public IActionResult Error()
        {
            return View();
        }
    }
}