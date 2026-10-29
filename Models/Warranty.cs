using System;

namespace KeepSafe.Models;

public class Warranty
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public DateOnly ExpirationDate { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long FinalPdfSize { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public WarrantyDocument? Document { get; set; }
}
