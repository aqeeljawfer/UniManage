using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UniManage.Models;
using UniManage.ViewModels;

namespace UniManage.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _um;
        private readonly SignInManager<AppUser> _sm;
        public AccountController(UserManager<AppUser> um, SignInManager<AppUser> sm) { _um=um; _sm=sm; }

        [HttpGet] public IActionResult Login() { if(User.Identity!=null&&User.Identity.IsAuthenticated) return Dash(); return View(); }
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM m) {
            if(!ModelState.IsValid) return View(m);
            var r=await _sm.PasswordSignInAsync(m.Email,m.Password,m.RememberMe,false);
            if(r.Succeeded) return Dash();
            ModelState.AddModelError("","Invalid email or password.");
            return View(m);
        }
        [HttpGet] public IActionResult Register() => View();
        [HttpPost,ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM m) {
            if(!ModelState.IsValid) return View(m);
            var u=new AppUser{UserName=m.Email,Email=m.Email,FullName=m.FullName,Role=m.Role,EmailConfirmed=true};
            var r=await _um.CreateAsync(u,m.Password);
            if(r.Succeeded){await _um.AddToRoleAsync(u,m.Role);await _sm.SignInAsync(u,false);return Dash();}
            foreach(var e in r.Errors) ModelState.AddModelError("",e.Description);
            return View(m);
        }
        [HttpPost,ValidateAntiForgeryToken,Authorize]
        public async Task<IActionResult> Logout() { await _sm.SignOutAsync(); return RedirectToAction("Index","Home"); }
        public IActionResult AccessDenied() => View();

        private IActionResult Dash() {
            if(User.IsInRole("Administrator")) return RedirectToAction("Dashboard","Admin");
            if(User.IsInRole("Lecturer")) return RedirectToAction("Dashboard","Lecturer");
            return RedirectToAction("Dashboard","Student");
        }
    }
}
