using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// MongoDB Configuration
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddSingleton<ComplaintService>();

// Email & Reports Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<IReportingService, ReportingService>();


// Session State Configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.Cookie.Name = "CMSAuthCookie";
    });

var app = builder.Build();

// CLI REPORT TRIGGER
if (args.Contains("--report"))
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var dbContext = services.GetRequiredService<MongoDbContext>();
            var reportingService = services.GetRequiredService<IReportingService>();
            var emailService = services.GetRequiredService<IEmailService>();
            var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>();
            
            Console.WriteLine("Triggering Daily Status Report...");
            
            // Fetch all admin emails from database
            var admins = await dbContext.Users.Find(u => u.Role == "Admin").ToListAsync();
            var adminEmails = string.Join(",", admins.Select(a => a.Email).Where(e => !string.IsNullOrEmpty(e)));

            if (string.IsNullOrEmpty(adminEmails))
            {
                Console.WriteLine("Warning: No administrators found in the database. Report will not be sent.");
                return;
            }

            var (body, csvData, fileName) = await reportingService.GenerateDailyReportAsync();
            await emailService.SendEmailWithAttachmentAsync(adminEmails, "CCMS Daily Status Report", body, csvData, fileName);
            Console.WriteLine($"Report sent successfully to {admins.Count} administrators: {adminEmails}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error generating report: " + ex.Message);
        }
    }
    return; // Exit immediately
}

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();

    // DEDICATED SUPER ADMIN SEEDING (9999999999)
    // 1. Demote any accidental SuperAdmins back to Admin
    await context.Users.UpdateManyAsync(
        u => u.Role == "SuperAdmin" && u.MobileNo != "9999999999",
        Builders<User>.Update.Set(u => u.Role, "Admin")
    );

    // 2. Ensure Dedicated SuperAdmin exists
    var dedicatedSuperAdmin = await context.Users.Find(u => u.MobileNo == "9999999999").FirstOrDefaultAsync();
    if (dedicatedSuperAdmin == null)
    {
        var sa = new User
        {
            FullName = "Master Super Admin",
            MobileNo = "9999999999",
            Password = "@@SuperAdmin123",
            Role = "SuperAdmin",
            Email = "superadmin@cms.gov"
        };
        await context.Users.InsertOneAsync(sa);
        Console.WriteLine("System: Created Dedicated SuperAdmin (9999999999)");
    }
    else if (dedicatedSuperAdmin.Role != "SuperAdmin")
    {
        await context.Users.UpdateOneAsync(
            u => u.MobileNo == "9999999999",
            Builders<User>.Update.Set(u => u.Role, "SuperAdmin")
        );
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// AUTH-SESSION CONSISTENCY CHECK
// This forces logout if the user has an auth cookie but is missing their session (e.g. browser reopen)
app.Use(async (context, next) =>
{
    if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
    {
        if (string.IsNullOrEmpty(context.Session.GetString("AuthActive")))
        {
            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(context, CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/");
            return;
        }
    }
    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
