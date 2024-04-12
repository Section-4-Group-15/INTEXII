using INTEXII.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using INTEXII.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace INTEXII.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private BrickwellContext context { get; set; }

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private readonly InferenceSession _session;

        public HomeController(ILogger<HomeController> logger, BrickwellContext con, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            context = con;

            try
            {
                _session = new InferenceSession("fraud_model.onnx");
                _logger.LogInformation("ONNX model loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading the ONNX model: {ex.Message}");
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateQuantity(int productId, int quantity)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var cartItem = await context.CartProducts.FirstOrDefaultAsync(cp => cp.user_Id == userId && cp.product_Id == productId);

                if (cartItem != null)
                {
                    cartItem.quantity = quantity;
                    await context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Cart");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var cartItem = await context.CartProducts.FirstOrDefaultAsync(cp => cp.user_Id == userId && cp.product_Id == productId);

                if (cartItem != null)
                {
                    context.CartProducts.Remove(cartItem);
                    await context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Cart");
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
        public async Task<IActionResult> SubmitOrder(string address, string bank, string cardType)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                    return RedirectToAction("Login");

                // Get the max customer ID and increment for new customers
                var maxCustomerId = await context.Customers.MaxAsync(c => (int?)c.customer_ID) ?? 0;
                var newCustomerId = (int)(maxCustomerId + 1);

                //
                // Model Logic to get the fraud prediction
                //

                // Logic to calculate the amount of the order
                var purchaseAmount = context.CartProducts
                    .Where(cp => cp.user_Id == _userManager.GetUserId(User))
                    .Sum(cp => cp.cost * cp.quantity);

                // Create a new order record
                var newOrder = new Order
                {
                    transaction_ID = (int)(await context.Orders.MaxAsync(o => (int?)o.transaction_ID) ?? 0) + 1,
                    customer_ID = newCustomerId,
                    date = DateOnly.FromDateTime(DateTime.Now),
                    bank = bank,
                    type_of_card = cardType,
                    country_of_transaction = address, 
                    shipping_address = address,
                    type_of_transaction = "Online",
                    entry_mode = "CVC",
                    time = (byte?)DateTime.Now.Hour,
                    day_of_week = DateTime.Now.DayOfWeek.ToString(),
                    amount = (short?)purchaseAmount,
                    fraud = 1 // Enter the fraud prediction here
                };

                //Calculate Days since January 1, 2022
                var january1_2022 = new DateOnly(2022, 1, 1);
                var recordDate = newOrder.date.HasValue ? new DateTime(newOrder.date.Value.Year, newOrder.date.Value.Month, newOrder.date.Value.Day) : DateTime.MinValue; // Convert DateOnly to DateTime
                var daysSinceJan2022 = (recordDate - new DateTime(january1_2022.Year, january1_2022.Month, january1_2022.Day)).Days;

                // Calculate the Current Day of the week
                var dayOfWeekEnum = DateTime.Now.DayOfWeek; // Get the current day of the week as an enum
                var dayOfWeekString = dayOfWeekEnum.ToString();

                // Calculate the Current Hour for the time field
                int currentHour = DateTime.Now.Hour;

                // Set the online order thing
                var entryMode = "CVC";
                var typeOfTransaction = "Online";

                // Set the Country_of_Transaction
                var countryOfTransaction = newOrder.shipping_address ?? newOrder.country_of_transaction;

                // Hard code the fraud value because it doesn't matter, will get dropped in the prediction after selecting features.
                var fraudValue = 0;

                try
                {
                    var input = new List<float>
                        {
                            (float)newOrder.customer_ID, // need to get the customer id from something
                            currentHour, // need to get the hour dynamically
                            // fix amount if its null
                            (float)(purchaseAmount), // Get the purchase amount from the form
                            //fix date
                            daysSinceJan2022,
                            // Hard code based on the actual day
                            dayOfWeekString == "Monday" ? 1 : 0,
                            dayOfWeekString == "Saturday" ? 1 : 0,
                            dayOfWeekString == "Sunday" ? 1 : 0,
                            dayOfWeekString == "Thursday" ? 1 : 0,
                            dayOfWeekString == "Tuesday" ? 1 : 0,
                            dayOfWeekString == "Wednesday" ? 1 : 0,
                            // Hard code to CVC since this is all online orders
                            entryMode == "Pin" ? 1 : 0,
                            entryMode == "Tap" ? 1 : 0,
                            // Hard code to online since this is all online orders
                            typeOfTransaction == "Online" ? 1 : 0,
                            typeOfTransaction == "POS" ? 1 : 0,
                            // Get from the Shipping Address value, hard code
                            countryOfTransaction == "India" ? 1 : 0,
                            countryOfTransaction == "Russia" ? 1 : 0,
                            countryOfTransaction == "USA" ? 1 : 0,
                            countryOfTransaction == "United Kingdom" ? 1 : 0,
                            // Get from form
                            (newOrder.shipping_address ?? newOrder.country_of_transaction) == "India" ? 1 : 0,
                            (newOrder.shipping_address ?? newOrder.country_of_transaction) == "Russia" ? 1 : 0,
                            (newOrder.shipping_address ?? newOrder.country_of_transaction) == "USA" ? 1 : 0,
                            (newOrder.shipping_address ?? newOrder.country_of_transaction) == "United Kingdom" ? 1 : 0,
                            // Get from form
                            newOrder.bank == "HSBC" ? 1 : 0,
                            newOrder.bank == "Halifax" ? 1 : 0,
                            newOrder.bank == "Lloyds" ? 1 : 0,
                            newOrder.bank == "Metro" ? 1 : 0,
                            newOrder.bank == "Monzo" ? 1 : 0,
                            newOrder.bank == "RBS" ? 1 : 0,
                            // Get from form
                            newOrder.type_of_card == "Visa" ? 1 : 0,
                            fraudValue
                        };
                    var inputTensor = new DenseTensor<float>(input.ToArray(), new[] { 1, input.Count });

                    var inputs = new List<NamedOnnxValue>
                        {
                            NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
                        };

                    using (var results = _session.Run(inputs)) // makes the prediction with the inputs from the form (i.e. class_type 1-7)
                    {
                        var prediction = results.FirstOrDefault(item => item.Name == "output_label")?.AsTensor<long>().ToArray();
                        if (prediction != null && prediction.Length > 0)
                        {
                            // Use the prediction to get the animal type from the dictionary
                            ViewBag.Prediction = prediction;
                            newOrder.fraud = (byte?)prediction[0];
                        }
                        else
                        {
                            ViewBag.Prediction = "Error: Unable to make a prediction.";
                            newOrder.fraud = (byte?)2;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error during prediction: {ex.Message}");
                    ViewBag.Prediction = "Error during prediction.";
                }

                // Save new order to get OrderID
                context.Orders.Add(newOrder);
                await context.SaveChangesAsync();

                // Get the current user's cart items
                var userId = _userManager.GetUserId(User);
                var cartItems = await context.CartProducts.Where(cp => cp.user_Id == userId).ToListAsync();

                // Get the max line item ID and increment for new line items
                var maxLineItemId = await context.LineItems.MaxAsync(li => (int?)li.line_id) ?? 0;
                var newLineItemId = (int)(maxLineItemId + 1);

                // Convert cart items to line items
                foreach (var item in cartItems)
                {
                    var lineItem = new LineItem
                    {
                        line_id = newLineItemId++,
                        transaction_id = (int)newOrder.transaction_ID,
                        product_ID = (byte)item.product_Id,
                        qty = (byte)item.quantity,
                        rating = 0 // Hard coded rating
                    };

                    context.LineItems.Add(lineItem);
                }

                // Clear the cart
                context.CartProducts.RemoveRange(cartItems);
                await context.SaveChangesAsync();


                // Check fraud prediction and redirect to different routes
                if (newOrder.fraud == 1)
                {
                    return RedirectToAction("OrderHold");
                }
                else
                {
                    return RedirectToAction("OrderConfirmation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order");
                return RedirectToAction("Error");
            }
        }

        public IActionResult OrderConfirmation()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
        }

        public IActionResult OrderHold()
        {
            ViewBag.CartItemCount = GetCartItemCount();
            return View();
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

        public async Task<IActionResult> AdminOrders(int page = 1)
        {
            ViewBag.CartItemCount = GetCartItemCount();

            int pageSize = 100; // Set page size

            try
            {
                // Order by Date descending, then by Time descending to get most recent orders first
                var ordersQuery = context.Orders
                                         .OrderByDescending(o => o.date)
                                         .ThenByDescending(o => o.time)
                                         .AsQueryable();

                var orders = await ordersQuery
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToListAsync();

                // Predict fraud type for orders
                var predictions = new List<Prediction>();
                var fraudTypeDict = new Dictionary<int, string>
        {
            {0, "no" },
            {1, "yes"},
        };

                foreach (var order in orders)
                {
                    // Calculate Days since January 1, 2022
                    var january1_2022 = new DateOnly(2022, 1, 1);
                    var recordDate = order.date.HasValue ? new DateTime(order.date.Value.Year, order.date.Value.Month, order.date.Value.Day) : DateTime.MinValue; // Convert DateOnly to DateTime
                    var daysSinceJan2022 = (recordDate - new DateTime(january1_2022.Year, january1_2022.Month, january1_2022.Day)).Days;

                    var input = new List<float>
            {
                (float)order.customer_ID,
                (float)order.time,
                // fix amount if it's null
                (float)(order.amount ?? 0),
                // fix date
                daysSinceJan2022,
                // Check the dummy coded data
                order.day_of_week == "Mon" ? 1 : 0,
                order.day_of_week == "Sat" ? 1 : 0,
                order.day_of_week == "Sun" ? 1 : 0,
                order.day_of_week == "Thu" ? 1 : 0,
                order.day_of_week == "Tue" ? 1 : 0,
                order.day_of_week == "Wed" ? 1 : 0,
                order.entry_mode == "Pin" ? 1 : 0,
                order.entry_mode == "Tap" ? 1 : 0,
                order.type_of_transaction == "Online" ? 1 : 0,
                order.type_of_transaction == "POS" ? 1 : 0,
                order.country_of_transaction == "India" ? 1 : 0,
                order.country_of_transaction == "Russia" ? 1 : 0,
                order.country_of_transaction == "USA" ? 1 : 0,
                order.country_of_transaction == "United Kingdom" ? 1 : 0,
                (order.shipping_address ?? order.country_of_transaction) == "India" ? 1 : 0,
                (order.shipping_address ?? order.country_of_transaction) == "Russia" ? 1 : 0,
                (order.shipping_address ?? order.country_of_transaction) == "USA" ? 1 : 0,
                (order.shipping_address ?? order.country_of_transaction) == "United Kingdom" ? 1 : 0,
                order.bank == "HSBC" ? 1 : 0,
                order.bank == "Halifax" ? 1 : 0,
                order.bank == "Lloyds" ? 1 : 0,
                order.bank == "Metro" ? 1 : 0,
                order.bank == "Monzo" ? 1 : 0,
                order.bank == "RBS" ? 1 : 0,
                order.type_of_card == "Visa" ? 1 : 0,
                (float)(order.fraud ?? 0.0)
            };

                    var inputTensor = new DenseTensor<float>(input.ToArray(), new[] { 1, input.Count });
                    var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
            };

                    int predictionResult;

                    using (var results = _session.Run(inputs)) // makes the prediction with the inputs from the form
                    {
                        var prediction = results.FirstOrDefault(item => item.Name == "output_label")?.AsTensor<long>().ToArray();
                        predictionResult = prediction != null && prediction.Length > 0 ? (int)prediction[0] : -1; // Default value in case of error
                    }

                    int orderIdentifier = (int)order.transaction_ID; // Assuming Transaction_Id is the correct identifier for the order

                    // Add the prediction to the list
                    predictions.Add(new Prediction { Order_Id = orderIdentifier, Prediction_Outcome = predictionResult });
                }

                // Pass orders, predictions, total order count, and current page to the view for pagination
                ViewData["TotalOrders"] = ordersQuery.Count();
                ViewData["CurrentPage"] = page;

                return View((orders.AsEnumerable(), predictions.AsEnumerable()));
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
        public class AdminUsersViewModel
        {
            public IEnumerable<UserRoleViewModel> UserRoles { get; set; }
        }

        public class UserRoleViewModel
        {
            public IdentityUser User { get; set; }
            public string Role { get; set; }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userRoles = new List<UserRoleViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var role = roles.FirstOrDefault() ?? "User";  // Default to "User" if no roles
                    userRoles.Add(new UserRoleViewModel
                    {
                        User = user,
                        Role = role
                    });
                }

                var viewModel = new AdminUsersViewModel
                {
                    UserRoles = userRoles
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleUserRole(string userId, string role)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound();
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains(role))
                {
                    await _userManager.RemoveFromRoleAsync(user, role);
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user role");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUser(IdentityUser model, string role)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var result = await _userManager.CreateAsync(model);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(model, role);
                        return RedirectToAction("Users");
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }

                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user");
                return RedirectToAction("Error");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }

                return RedirectToAction("AdminUsers");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return RedirectToAction("Error");
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
            // Dictionary mapping the numeric prediction to an animal type
            var fraud_type_dict = new Dictionary<int, string>
            {
                {0, "Good Purchase" },
                {1, "Fraudulent Purchase"},
            };

            //Calculate Days since January 1, 2022
            var january1_2022 = new DateOnly(2022, 1, 1);
            var recordDate = newOrder.date.HasValue ? new DateTime(newOrder.date.Value.Year, newOrder.date.Value.Month, newOrder.date.Value.Day) : DateTime.MinValue; // Convert DateOnly to DateTime
            var daysSinceJan2022 = (recordDate - new DateTime(january1_2022.Year, january1_2022.Month, january1_2022.Day)).Days;

            try
            {
                var input = new List<float>
                    {
                        (float)newOrder.customer_ID,
                        (float)newOrder.time,
                        // fix amount if its null
                        (float)(newOrder.amount ?? 0),

                        //fix date
                        daysSinceJan2022,

                        // Check the dummy coded data
                        newOrder.day_of_week == "Mon" ? 1 : 0,
                        newOrder.day_of_week == "Sat" ? 1 : 0,
                        newOrder.day_of_week == "Sun" ? 1 : 0,
                        newOrder.day_of_week == "Thu" ? 1 : 0,
                        newOrder.day_of_week == "Tue" ? 1 : 0,
                        newOrder.day_of_week == "Wed" ? 1 : 0,

                        newOrder.entry_mode == "Pin" ? 1 : 0,
                        newOrder.entry_mode == "Tap" ? 1 : 0,

                        newOrder.type_of_transaction == "Online" ? 1 : 0,
                        newOrder.type_of_transaction == "POS" ? 1 : 0,

                        newOrder.country_of_transaction == "India" ? 1 : 0,
                        newOrder.country_of_transaction == "Russia" ? 1 : 0,
                        newOrder.country_of_transaction == "USA" ? 1 : 0,
                        newOrder.country_of_transaction == "United Kingdom" ? 1 : 0,

                        (newOrder.shipping_address ?? newOrder.country_of_transaction) == "India" ? 1 : 0,
                        (newOrder.shipping_address ?? newOrder.country_of_transaction) == "Russia" ? 1 : 0,
                        (newOrder.shipping_address ?? newOrder.country_of_transaction) == "USA" ? 1 : 0,
                        (newOrder.shipping_address ?? newOrder.country_of_transaction) == "United Kingdom" ? 1 : 0,

                        newOrder.bank == "HSBC" ? 1 : 0,
                        newOrder.bank == "Halifax" ? 1 : 0,
                        newOrder.bank == "Lloyds" ? 1 : 0,
                        newOrder.bank == "Metro" ? 1 : 0,
                        newOrder.bank == "Monzo" ? 1 : 0,
                        newOrder.bank == "RBS" ? 1 : 0,

                        newOrder.type_of_card == "Visa" ? 1 : 0,

                        (float)newOrder.fraud
                    };

                var inputTensor = new DenseTensor<float>(input.ToArray(), new[] { 1, input.Count });

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("float_input", inputTensor)
                };

                using (var results = _session.Run(inputs)) // makes the prediction with the inputs from the form (i.e. class_type 1-7)
                {
                    var prediction = results.FirstOrDefault(item => item.Name == "output_label")?.AsTensor<long>().ToArray();
                    if (prediction != null && prediction.Length > 0)
                    {
                        // Use the prediction to get the animal type from the dictionary
                        var fraudType = fraud_type_dict.GetValueOrDefault((int)prediction[0], "Unknown");
                        ViewBag.Prediction = fraudType;

                        // Workaround for database-generated identity column, since we are using preexisting data
                        var maxProductId = await context.Orders.MaxAsync(o => (int?)o.transaction_ID) ?? 0;
                        newOrder.transaction_ID = (int)(maxProductId + 1);

                        if (ModelState.IsValid)
                        {
                            context.Orders.Add(newOrder);
                            await context.SaveChangesAsync();
                            // return Json(new { success = true });
                        }
                        // return Json(new { success = false, message = "Invalid order data" });
                    }
                    else
                    {
                        ViewBag.Prediction = "Error: Unable to make a prediction.";
                    }
                }

                _logger.LogInformation("Prediction executed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during prediction: {ex.Message}");
                ViewBag.Prediction = "Error during prediction.";
            }

            return View();
        }
    }
}