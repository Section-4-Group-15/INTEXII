using Microsoft.EntityFrameworkCore;
using INTEXII.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

try
{
    services.AddAuthentication().AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = configuration["Authentication:Google:ClientSecret"];
    });
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}



var connection = String.Empty;
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddEnvironmentVariables().AddJsonFile("appsettings.Development.json");
    connection = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");
}
else
{
    connection = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
}

builder.Services.AddDbContext<BrickwellContext>(options =>
    options.UseSqlServer(connection, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(45),
            errorNumbersToAdd: null);
    }));

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Sign-in settings
    options.SignIn.RequireConfirmedAccount = true;

    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BrickwellContext>();

// Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
    options.Secure = CookieSecurePolicy.Always;
    options.HttpOnly = HttpOnlyPolicy.Always;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Build the app
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    // Production settings
    app.UseDeveloperExceptionPage();
}
else
{
    // Production settings
    app.UseExceptionHandler("/Home/Error");
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseHsts();

// CSP Configuration goes here
app.Use(async (context, next) =>
{
    // Other middleware

    // CSP Configuration
    var csp = "default-src 'self'; " +
              "script-src 'self' 'unsafe-inline' https://apis.google.com https://ajax.googleapis.com https://code.jquery.com https://stackpath.bootstrapcdn.com https://www.chatbase.co; " + // Scripts
              "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://stackpath.bootstrapcdn.com; " + // Styles
              "img-src 'self' data: https://m.media-amazon.com https://*.lego.com https://*.blob.core.windows.net https://images.brickset.com https://th.bing.com https://cdn.rebrickable.com https://www.brickeconomy.com https://www.barnorama.com https://i.ytimg.com https://img.brickowl.com; " + // Images
              "font-src 'self' https://fonts.gstatic.com data:; " + // Fonts
              "connect-src 'self' https://accounts.google.com https://www.chatbase.co ws://localhost:* wss://localhost:* http://localhost:*; " + // Connections including WebSockets
              "frame-src 'self' https://accounts.google.com https://www.chatbase.co; " + // Frames
              "frame-ancestors 'none';"; // No framing allowed

    context.Response.Headers.Add("Content-Security-Policy", csp);
    await next();
});


// App Configuration
app.UseStaticFiles();

// Cookie Policy
app.UseCookiePolicy();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "contact",
    pattern: "contact",
    defaults: new { controller = "Home", action = "Contact" });

app.MapControllerRoute(
    name: "about",
    pattern: "about",
    defaults: new { controller = "Home", action = "About" });

app.MapControllerRoute(
    name: "privacy",
    pattern: "privacy",
    defaults: new { controller = "Home", action = "Privacy" });

// Enhanced Products route with pagination and filtering
app.MapControllerRoute(
    name: "products",
    pattern: "products/{pageNum?}",
    defaults: new { controller = "Home", action = "Products" });

app.MapControllerRoute(
    name: "filteredProducts",
    pattern: "products/{pageNum?}/{categories?}/{colors?}",
    defaults: new { controller = "Home", action = "Products" });

app.MapControllerRoute(
    name: "indProducts",
    pattern: "Product/{id:int}",
    defaults: new { controller = "Home", action = "IndProducts" });

// Cart management routes
app.MapControllerRoute(
    name: "viewCart",
    pattern: "cart",
    defaults: new { controller = "Home", action = "Cart" });

app.MapControllerRoute(
    name: "addToCart",
    pattern: "cart/add/{productId:int}/{quantity:int}",
    defaults: new { controller = "Home", action = "AddToCart" });

app.MapControllerRoute(
    name: "updateCart",
    pattern: "cart/update/{productId:int}/{quantity:int}",
    defaults: new { controller = "Home", action = "UpdateQuantity" });

app.MapControllerRoute(
    name: "removeFromCart",
    pattern: "cart/remove/{productId:int}",
    defaults: new { controller = "Home", action = "RemoveFromCart" });

app.MapControllerRoute(
    name: "adminProducts",
    pattern: "AdminTools/ManageProducts",
    defaults: new { controller = "Home", action = "AdminProducts" });

app.MapControllerRoute(
    name: "adminUsers",
    pattern: "AdminTools/ManageUsers",
    defaults: new { controller = "Home", action = "AdminUsers" });

app.MapControllerRoute(
    name: "adminOrders",
    pattern: "AdminTools/ManageOrders",
    defaults: new { controller = "Home", action = "AdminOrders" });

// Define custom routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.MapControllerRoute("pages", "{action}", new { Controller = "Home", action = "Index" });


app.MapRazorPages();

// Seed Roles, if they don't exist
//static async Task SeedRoles(IServiceProvider serviceProvider)
//{
//    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

//    string[] roleNames = { "Admin", "User" };
//    foreach (var roleName in roleNames)
//    {
//        var roleExist = await roleManager.RoleExistsAsync(roleName);
//        if (!roleExist)
//        {
//            await roleManager.CreateAsync(new IdentityRole(roleName));
//        }
//    }
//}

//var serviceProvider = app.Services.GetRequiredService<IServiceProvider>().CreateScope().ServiceProvider;
//await SeedRoles(serviceProvider);

app.Run();
