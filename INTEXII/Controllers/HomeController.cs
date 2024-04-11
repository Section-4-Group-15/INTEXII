using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using INTEXII.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace INTEXII.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BrickwellContext context { get; set; }

        private readonly UserManager<IdentityUser> _userManager;

        private readonly InferenceSession _session;

        public HomeController(ILogger<HomeController> logger, BrickwellContext con, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            context = con;

            try
            {
                _session = new InferenceSession("C:\\Users\\nhaskett\\Source\\Repos\\INTEXII\\INTEXII\\decision_tree_model.onnx");
                _logger.LogInformation("ONNX model loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error leading the ONNX model.");
            }
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

            var product = context.Products.FirstOrDefault(p => p.Product_ID == id);

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
                }
            }

            return RedirectToAction("Cart");
        }

        // Cart View
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
                        /*.Include(cp => cp.Product)*/ // Use the navigation property here
                        .ToListAsync();

                    return View(cartItems);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart");
                return RedirectToAction("Error");
            }
        }

        // Admin Controller
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminProducts()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            var products = await context.Products.OrderBy(p => p.Product_ID).ToListAsync();
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
                    context.Update(updatedProduct);
                    await context.SaveChangesAsync();
                    return Json(new { success = true });
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
                var maxProductId = await context.Products.MaxAsync(p => (int?)p.Product_ID) ?? 0;
                newProduct.Product_ID = (int)(maxProductId + 1);

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
                                         .OrderByDescending(o => o.Transaction_Id)
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

        [HttpGet]
        public IActionResult AddOrderAdmin()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddOrderAdmin(Order newOrder)
        {
            try
            {
                // Workaround for database-generated identity column, since we are using preexisting data
                var maxProductId = await context.Orders.MaxAsync(o => (int?)o.Transaction_Id) ?? 0;
                newOrder.Transaction_Id = (int)(maxProductId + 1);

                if (ModelState.IsValid)
                {
                    context.Orders.Add(newOrder);
                    await context.SaveChangesAsync();
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "Invalid order data" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order");
                return Json(new { success = false, message = "Error adding order" });
            }
        }

        [HttpPost]
        public IActionResult Predict()
        {
            var records = context.Orders
                .OrderByDescending(o => o.Date)
                .Take(20)
                .ToList();
            var predictions = new List<OrderPrediction>();

            var fraud_type_dict = new Dictionary<int, string>
            {
                {0, "no" },
                {1, "yes"},
            };

            foreach (var record in records)
            {
                //Calculate Days since January 1, 2022
                var january1_2022 = new DateOnly(2022, 1, 1);
                var recordDate = record.Date.HasValue ? new DateTime(record.Date.Value.Year, record.Date.Value.Month, record.Date.Value.Day) : DateTime.MinValue; // Convert DateOnly to DateTime
                var daysSinceJan2022 = (recordDate - new DateTime(january1_2022.Year, january1_2022.Month, january1_2022.Day)).Days;

                var input = new List<float>
                    {
                        (float)record.Customer_Id,
                        (float)record.Time,
                        // fix amount if its null
                        (float)(record.Amount ?? 0),

                        //fix date
                        daysSinceJan2022,

                        // Check the dummy coded data
                        record.Day_Of_Week == "Mon" ? 1 : 0,
                        record.Day_Of_Week == "Sat" ? 1 : 0,
                        record.Day_Of_Week == "Sun" ? 1 : 0,
                        record.Day_Of_Week == "Thu" ? 1 : 0,
                        record.Day_Of_Week == "Tue" ? 1 : 0,
                        record.Day_Of_Week == "Wed" ? 1 : 0,

                        record.Entry_Mode == "Pin" ? 1 : 0,
                        record.Entry_Mode == "Tap" ? 1 : 0,

                        record.Type_Of_Transaction == "Online" ? 1 : 0,
                        record.Type_Of_Transaction == "POS" ? 1 : 0,

                        record.Country_Of_Transaction == "India" ? 1 : 0,
                        record.Country_Of_Transaction == "Russia" ? 1 : 0,
                        record.Country_Of_Transaction == "USA" ? 1 : 0,
                        record.Country_Of_Transaction == "UnitedKingdom" ? 1 : 0,
                    }
            }

            try
            {
                var input = new List<float> { };
                var inputTensor = new DenseTensor<float>(input.ToArray(), new[] { 1, input.Count });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
                };

                using (var results = _session.Run(inputs)) // makes the prediction with the inputs from the form
                {
                    var prediction = results.FirstOrDefault(item => item.Name == "output_label")?.AsTensor<long>().ToArray();
                    if (prediction != null && prediction.Length > 0)
                    {
                        var fraudType = fraud_type_dict.GetValueOrDefault((int)prediction[0], "Unknown");
                        ViewBag.Prediction = fraudType;
                    }
                    else
                    {
                        ViewBag.Prediction = "Error: Unable to make a prediction.";
                    }
                }

                _logger.LogInformation("Prediction executed succesfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during prediction: {ex.Message}");
                ViewBag.Prediction = "Error during prediction";
            }

            return View("AddOrderAdmin");
        }
    }
}