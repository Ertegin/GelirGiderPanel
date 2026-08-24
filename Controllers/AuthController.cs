using GelirGiderPanel.Data;
using GelirGiderPanel.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace GelirGiderPanel.Controllers
{
    public class AuthController : Controller
    {
        AppDbContext _db;
        public AuthController(AppDbContext db)
        {
            _db = db;
        }
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            return View();
     
        }

        [HttpPost]
        public async Task<IActionResult> Login(AppUser user)
        {
            if (_db.Users.Any(x=>x.UserName==user.UserName))
            {
                AppUser selectedUser = _db.Users.Where(x => x.UserName == user.UserName).FirstOrDefault();
                bool isvalid = BCrypt.Net.BCrypt.Verify(user.Password, selectedUser.Password);

                if (isvalid)
                {
                    List<Claim> claims = new List<Claim>()
                    {
                        new Claim(ClaimTypes.Name, selectedUser.UserName),
                        new Claim(ClaimTypes.Role, selectedUser.Role.ToString()),
                        new Claim("userName",selectedUser.UserName),
                        new Claim("UserId",selectedUser.ID.ToString()),
                        new Claim("role",selectedUser.Role.ToString())
                        //bu veriler cookiede tutulacak
                    };
                    // claims kullanıcıyı tanımlayan özellikler
                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                    await HttpContext.SignInAsync(principal);
                    if (selectedUser.Role == Enums.Role.Admin || selectedUser.Role == Enums.Role.User)
                    {

                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            ViewBag.Notification = "Wrong Username or Password";
            return View();
        }


        public async Task<IActionResult> LogOut()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
