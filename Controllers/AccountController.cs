using CMS.Models;
using CMS.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

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
            return RedirectToAction("Index", "Home");
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
                        new Claim(ClaimTypes.Role, user.Role), // Use role from DB
                        new Claim("UserId", user.Id)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        new AuthenticationProperties { IsPersistent = false });

                    // SET SESSION MARKER (to detect browser close/reopen)
                    HttpContext.Session.SetString("AuthActive", "true");

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        var redirectUrl = user.Role == "SuperAdmin" ? "/SuperAdmin/Index" :
                                         user.Role == "Admin" ? "/Admin/Index" : 
                                         user.Role == "DeptHead" ? "/Head/Dashboard" : 
                                         "/Complaint/Dashboard";
                        return Ok(new { success = true, redirectUrl });
                    }

                    if (user.Role == "SuperAdmin") return RedirectToAction("Index", "SuperAdmin");
                    if (user.Role == "Admin") return RedirectToAction("Index", "Admin");
                    if (user.Role == "DeptHead") return RedirectToAction("Dashboard", "Head");

                    return RedirectToAction("Dashboard", "Complaint");
                }
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { success = false, message = "Invalid login attempt. Please check your credentials." });
                }

                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return BadRequest(new { success = false, message = "Please fill in all required fields." });
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return RedirectToAction("DirectSubmit", "Complaint");
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Mobile number uniqueness check removed per user requirements

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
            return RedirectToAction("Index", "Home");
        }
    }
}
