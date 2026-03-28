using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly MongoDbContext _context;

        public AccountController(MongoDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.Find(u => u.MobileNo == model.MobileNo && u.Password == model.Password).FirstOrDefaultAsync();
                
                if (user != null)
                {
                    // Authenticate the user
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.MobileNo),
                        new Claim(ClaimTypes.Name, user.FullName),
                        new Claim("UserId", user.Id)
                    };

                    // Basic role mapping
                    if (user.MobileNo == "admin")
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, "User"));
                    }

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity));

                    if (user.MobileNo == "admin")
                        return RedirectToAction("Index", "Admin");

                    return RedirectToAction("Dashboard", "Complaint");
                }
                
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.Find(u => u.MobileNo == model.MobileNo).FirstOrDefaultAsync();
                if (existingUser != null)
                {
                    ModelState.AddModelError("MobileNo", "Mobile number already registered.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    MobileNo = model.MobileNo,
                    Password = model.Password,
                    Email = model.Email
                };

                await _context.Users.InsertOneAsync(user);

                return RedirectToAction("Login");
            }

            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
