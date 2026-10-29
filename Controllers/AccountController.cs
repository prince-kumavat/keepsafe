using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KeepSafe.Models;
using KeepSafe.Services;

namespace KeepSafe.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [Route("")]
    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        var model = new AuthPageViewModel
        {
            ActiveTab = "login"
        };
        return View(model);
    }

    [HttpPost]
    [Route("account/signup")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup([FromForm] SignupViewModel signup)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            var model = new AuthPageViewModel
            {
                Signup = new SignupViewModel { Email = signup.Email },
                ActiveTab = "signup"
            };
            return View("Index", model);
        }

        var result = await _authService.RegisterAsync(signup.Email, signup.Password, HttpContext);
        if (!result.Success)
        {
            var model = new AuthPageViewModel
            {
                Signup = new SignupViewModel { Email = signup.Email },
                ActiveTab = "signup",
                ErrorMessage = result.ErrorMessage
            };
            return View("Index", model);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Route("account/login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromForm] LoginViewModel login)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (!ModelState.IsValid)
        {
            var model = new AuthPageViewModel
            {
                Login = new LoginViewModel { Email = login.Email },
                ActiveTab = "login"
            };
            return View("Index", model);
        }

        var result = await _authService.LoginAsync(login.Email, login.Password, HttpContext);
        if (!result.Success)
        {
            var model = new AuthPageViewModel
            {
                Login = new LoginViewModel { Email = login.Email },
                ActiveTab = "login",
                ErrorMessage = result.ErrorMessage
            };
            return View("Index", model);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Route("account/logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(HttpContext);
        return RedirectToAction("Index", "Account");
    }
}
