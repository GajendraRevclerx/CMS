using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

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
builder.Services.AddHostedService<DailyReportWorker>();

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

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var _ = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
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
