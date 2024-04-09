using Microsoft.EntityFrameworkCore;
using INTEXII.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
    options.UseSqlServer(connection));

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
    .AddEntityFrameworkStores<BrickwellContext>();

// Build the app
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseHsts();

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

// App Configuration
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
