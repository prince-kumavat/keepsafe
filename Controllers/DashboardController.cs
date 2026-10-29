using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KeepSafe.Services;

namespace KeepSafe.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IWarrantyService _warrantyService;

    public DashboardController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    [HttpGet]
    [Route("dashboard")]
    public async Task<IActionResult> Index()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Challenge();
        }

        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "user@example.com";
        var viewModel = await _warrantyService.GetDashboardDataAsync(userId, userEmail);

        return View(viewModel);
    }
}
