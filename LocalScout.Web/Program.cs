using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using LocalScout.Infrastructure.Repositories;
using LocalScout.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, b => b.MigrationsAssembly("LocalScout.Infrastructure"))
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add HttpContextAccessor for AuditService
builder.Services.AddHttpContextAccessor();

// Register TimeZone Service
builder.Services.AddSingleton<LocalScout.Application.Services.ITimeZoneService, 
    LocalScout.Infrastructure.Services.TimeZoneService>();

// Configure Identity with ApplicationUser
builder
    .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// Configure Token Lifespan (15 minutes)
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(15);
});

// Register EmailSender
builder.Services.AddTransient<IEmailSender, EmailService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IServiceProviderRepository, ServiceProviderRepository>();
builder.Services.AddScoped<IVerificationRepository, VerificationRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddHttpClient<ILocationService, LocationService>();
builder.Services.AddScoped<IServiceCategoryRepository, ServiceCategoryRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<ICategoryRequestRepository, CategoryRequestRepository>();
builder.Services.AddScoped<LocalScout.Application.Interfaces.IReviewRepository, 
    LocalScout.Infrastructure.Repositories.ReviewRepository>();
builder.Services.AddScoped<LocalScout.Application.Interfaces.IReportPdfService,
    LocalScout.Infrastructure.Services.ReportPdfService>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHttpClient<IAIService, AIService>();

// SSLCommerz Payment Gateway
builder.Services.Configure<LocalScout.Application.DTOs.PaymentDTOs.SSLCommerzSettings>(
    builder.Configuration.GetSection("SSLCommerz"));
builder.Services.AddHttpClient<LocalScout.Application.Interfaces.ISSLCommerzService, 
    LocalScout.Infrastructure.Services.SSLCommerzService>();

// Platform Limits Configuration
builder.Services.Configure<LocalScout.Application.Settings.LimitsSettings>(
    builder.Configuration.GetSection("Limits"));

// Scheduling Services (Enhanced Booking System)
builder.Services.AddScoped<IProviderTimeSlotRepository, ProviderTimeSlotRepository>();
builder.Services.AddScoped<IServiceBlockRepository, ServiceBlockRepository>();
builder.Services.AddScoped<IRescheduleRepository, RescheduleRepository>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();

// Background Services for Auto-Cancel and Service Unblock
builder.Services.AddHostedService<LocalScout.Infrastructure.Services.BookingAutoCancelService>();
builder.Services.AddHostedService<LocalScout.Infrastructure.Services.ServiceUnblockService>();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


var app = builder.Build();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
