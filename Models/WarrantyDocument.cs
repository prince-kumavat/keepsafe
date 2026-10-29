namespace KeepSafe.Models;

public class WarrantyDocument
{
    public int WarrantyId { get; set; }
    public byte[] PdfData { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/pdf";

    public Warranty Warranty { get; set; } = null!;
}
