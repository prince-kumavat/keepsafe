using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KeepSafe.Models;
using KeepSafe.Services;

namespace KeepSafe.Controllers;

[Authorize]
public class WarrantiesController : Controller
{
    private readonly IWarrantyService _warrantyService;

    public WarrantiesController(IWarrantyService warrantyService)
    {
        _warrantyService = warrantyService;
    }

    [HttpPost]
    [Route("warranties")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateWarrantyInputModel input)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Invalid warranty information provided.";

            return BadRequest(new { success = false, message = firstError });
        }

        if (input.File == null || input.File.Length == 0)
        {
            return BadRequest(new { success = false, message = "Please upload a document or image." });
        }

        if (input.File.Length > 10 * 1024 * 1024)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new { success = false, message = "File size exceeds 10 MB." });
        }

        var result = await _warrantyService.CreateWarrantyAsync(userId, input);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.ErrorMessage ?? "Failed to save warranty." });
        }

        return Ok(new { success = true, compressed = result.Compressed });
    }

    [HttpGet]
    [Route("warranties/{id:int}/document")]
    public async Task<IActionResult> Document(int id, [FromQuery] bool download = false)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return NotFound();
        }

        var docResult = await _warrantyService.GetDocumentAsync(userId, id);
        if (docResult == null)
        {
            // Unauthorized or non-existent record returns 404 without revealing other users' data
            return NotFound();
        }

        var (pdfBytes, fileName, contentType) = docResult.Value;

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "private, no-store";

        if (download)
        {
            return File(pdfBytes, contentType, fileName);
        }
        else
        {
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(pdfBytes, contentType);
        }
    }

    [HttpPost]
    [Route("warranties/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId))
        {
            return NotFound();
        }

        var deleted = await _warrantyService.DeleteWarrantyAsync(userId, id);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["SuccessMessage"] = "Warranty deleted successfully.";
        return RedirectToAction("Index", "Dashboard");
    }
}
