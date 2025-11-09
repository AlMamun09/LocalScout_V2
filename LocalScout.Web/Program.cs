using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using LocalScout.Infrastructure.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// FIX 2: Tell DbContext where to find the Migrations project
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("LocalScout.Infrastructure"))
); // Point to Infrastructure

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// FIX 3: Use your custom ApplicationUser, not the generic IdentityUser
builder
    .Services.AddDefaultIdentity<ApplicationUser>(options =>
        options.SignIn.RequireConfirmedAccount = true
    )
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Register EmailSender
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
