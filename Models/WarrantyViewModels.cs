using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace KeepSafe.Models;

public class CreateWarrantyInputModel
{
    [Required(ErrorMessage = "Product name is required.")]
    [MaxLength(100, ErrorMessage = "Product name cannot exceed 100 characters.")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Expiration date is required.")]
    public DateOnly? ExpirationDate { get; set; }

    [Required(ErrorMessage = "A warranty document or image is required.")]
    public IFormFile? File { get; set; }
}

public class WarrantyItemViewModel
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateOnly ExpirationDate { get; set; }
    public string FormattedExpirationDate { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FinalPdfSize { get; set; }
}

public class DashboardViewModel
{
    public string UserEmail { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public List<WarrantyItemViewModel> Warranties { get; set; } = new();
}
