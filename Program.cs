using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartWaste.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity + Roles
builder.Services
    .AddDefaultIdentity<IdentityUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Role-based redirect after login (only if ReturnUrl is empty or "/")
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth";

    options.Events.OnSignedIn = context =>
    {
        var returnUrl = context.Properties.RedirectUri;

        // If user is returning to a specific page, do not override it
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl != "/")
            return Task.CompletedTask;

        var user = context.Principal;
        if (user == null) return Task.CompletedTask;

        if (user.IsInRole("Admin"))
            context.Properties.RedirectUri = "/Admin/Dashboard";
        else if (user.IsInRole("Operator"))
            context.Properties.RedirectUri = "/Operator/Dashboard";
        else if (user.IsInRole("Driver"))
            context.Properties.RedirectUri = "/Driver/Dashboard";
        else
            context.Properties.RedirectUri = "/";

        return Task.CompletedTask;
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ---- DB create + seed roles + seed users (NO CLI needed) ----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Create/Update DB schema (recommended)
    var db = services.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Seed roles
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = { "Admin", "Operator", "Driver" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // ✅ Get userManager FIRST
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

    // --- Admin user ---
    var adminEmail = "admin@smartwaste.local";
    var adminPassword = "Admin@12345";

    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
        if (createAdmin.Succeeded)
            await userManager.AddToRoleAsync(adminUser, "Admin");
    }
    else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
    {
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    // --- Operator user ---
    var operatorEmail = "operator@smartwaste.local";
    var operatorPassword = "Operator@12345";

    var operatorUser = await userManager.FindByEmailAsync(operatorEmail);
    if (operatorUser == null)
    {
        operatorUser = new IdentityUser
        {
            UserName = operatorEmail,
            Email = operatorEmail,
            EmailConfirmed = true
        };

        var createOp = await userManager.CreateAsync(operatorUser, operatorPassword);
        if (createOp.Succeeded)
            await userManager.AddToRoleAsync(operatorUser, "Operator");
    }
    else if (!await userManager.IsInRoleAsync(operatorUser, "Operator"))
    {
        await userManager.AddToRoleAsync(operatorUser, "Operator");
    }

    // --- Driver user ---
    var driverEmail = "driver@smartwaste.local";
    var driverPassword = "Driver@12345";

    var driverUser = await userManager.FindByEmailAsync(driverEmail);
    if (driverUser == null)
    {
        driverUser = new IdentityUser
        {
            UserName = driverEmail,
            Email = driverEmail,
            EmailConfirmed = true
        };

        var createDr = await userManager.CreateAsync(driverUser, driverPassword);
        if (createDr.Succeeded)
            await userManager.AddToRoleAsync(driverUser, "Driver");
    }
    else if (!await userManager.IsInRoleAsync(driverUser, "Driver"))
    {
        await userManager.AddToRoleAsync(driverUser, "Driver");
    }
}
// --------------------------------------------------------------------

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/AppError");

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseStatusCodePagesWithReExecute("/Home/StatusCode", "?code={0}");


// IMPORTANT: Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

// Area routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
