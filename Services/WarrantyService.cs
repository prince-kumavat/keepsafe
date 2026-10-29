using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using KeepSafe.Infrastructure;
using KeepSafe.Models;

namespace KeepSafe.Services;

public class CreateWarrantyResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public bool Compressed { get; set; }
}

public interface IWarrantyService
{
    Task<DashboardViewModel> GetDashboardDataAsync(int userId, string userEmail);
    Task<CreateWarrantyResult> CreateWarrantyAsync(int userId, CreateWarrantyInputModel input);
    Task<(byte[] Bytes, string FileName, string ContentType)?> GetDocumentAsync(int userId, int warrantyId);
    Task<bool> DeleteWarrantyAsync(int userId, int warrantyId);
}

public class WarrantyService : IWarrantyService
{
    private readonly AppDbContext _dbContext;
    private readonly IDocumentProcessor _documentProcessor;
    private readonly IExpiryClock _expiryClock;

    public WarrantyService(
        AppDbContext dbContext,
        IDocumentProcessor documentProcessor,
        IExpiryClock expiryClock)
    {
        _dbContext = dbContext;
        _documentProcessor = documentProcessor;
        _expiryClock = expiryClock;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(int userId, string userEmail)
    {
        var today = _expiryClock.Today;

        // Query warranties without loading PDF blob
        var query = _dbContext.Warranties
            .AsNoTracking()
            .Where(w => w.UserId == userId);

        var totalCount = await query.CountAsync();
        var activeCount = await query.CountAsync(w => w.ExpirationDate >= today);
        var expiredCount = await query.CountAsync(w => w.ExpirationDate < today);

        var items = await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .Select(w => new
            {
                w.Id,
                w.ProductName,
                w.ExpirationDate,
                w.OriginalFileName,
                w.FinalPdfSize
            })
            .ToListAsync();

        var warrantyViewModels = items.Select(w => new WarrantyItemViewModel
        {
            Id = w.Id,
            ProductName = w.ProductName,
            ExpirationDate = w.ExpirationDate,
            FormattedExpirationDate = w.ExpirationDate.ToString("dd MMM yyyy").ToUpperInvariant(),
            IsExpired = _expiryClock.IsExpired(w.ExpirationDate),
            OriginalFileName = w.OriginalFileName,
            FinalPdfSize = w.FinalPdfSize
        }).ToList();

        return new DashboardViewModel
        {
            UserEmail = userEmail,
            TotalCount = totalCount,
            ActiveCount = activeCount,
            ExpiredCount = expiredCount,
            Warranties = warrantyViewModels
        };
    }

    public async Task<CreateWarrantyResult> CreateWarrantyAsync(int userId, CreateWarrantyInputModel input)
    {
        if (input.File == null || input.File.Length == 0)
        {
            return new CreateWarrantyResult
            {
                Success = false,
                ErrorMessage = "Please select a valid document or image file."
            };
        }

        // Limit upload to 10 MB
        if (input.File.Length > 10 * 1024 * 1024)
        {
            return new CreateWarrantyResult
            {
                Success = false,
                ErrorMessage = "File size exceeds the 10 MB limit."
            };
        }

        if (!input.ExpirationDate.HasValue)
        {
            return new CreateWarrantyResult
            {
                Success = false,
                ErrorMessage = "A valid expiration date is required."
            };
        }

        using var memoryStream = new MemoryStream();
        await input.File.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        var processResult = await _documentProcessor.ProcessAsync(memoryStream, input.File.FileName);
        if (!processResult.Success || processResult.PdfBytes == null)
        {
            return new CreateWarrantyResult
            {
                Success = false,
                ErrorMessage = processResult.ErrorMessage ?? "Failed to process the document."
            };
        }

        var sanitizedFileName = Path.GetFileName(input.File.FileName);
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            sanitizedFileName = "document.pdf";
        }
        if (sanitizedFileName.Length > 255)
        {
            sanitizedFileName = sanitizedFileName.Substring(0, 255);
        }

        // Save Warranty and WarrantyDocument records together using a single transaction
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var warranty = new Warranty
            {
                UserId = userId,
                ProductName = input.ProductName.Trim(),
                ExpirationDate = input.ExpirationDate.Value,
                OriginalFileName = sanitizedFileName,
                FinalPdfSize = processResult.PdfBytes.LongLength,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.Warranties.Add(warranty);
            await _dbContext.SaveChangesAsync();

            var doc = new WarrantyDocument
            {
                WarrantyId = warranty.Id,
                PdfData = processResult.PdfBytes,
                ContentType = "application/pdf"
            };

            _dbContext.WarrantyDocuments.Add(doc);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            return new CreateWarrantyResult
            {
                Success = true,
                Compressed = processResult.Compressed
            };
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return new CreateWarrantyResult
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while saving the warranty."
            };
        }
    }

    public async Task<(byte[] Bytes, string FileName, string ContentType)?> GetDocumentAsync(int userId, int warrantyId)
    {
        // Enforce ownership check: must match warrantyId and userId
        var warranty = await _dbContext.Warranties
            .AsNoTracking()
            .Where(w => w.Id == warrantyId && w.UserId == userId)
            .Select(w => new { w.Id, w.OriginalFileName })
            .FirstOrDefaultAsync();

        if (warranty == null)
        {
            return null;
        }

        var document = await _dbContext.WarrantyDocuments
            .AsNoTracking()
            .Where(d => d.WarrantyId == warrantyId)
            .FirstOrDefaultAsync();

        if (document == null)
        {
            return null;
        }

        // Determine download filename ending with .pdf
        var baseName = Path.GetFileNameWithoutExtension(warranty.OriginalFileName);
        var safeFileName = $"{baseName}.pdf";

        return (document.PdfData, safeFileName, document.ContentType);
    }

    public async Task<bool> DeleteWarrantyAsync(int userId, int warrantyId)
    {
        // Verify ownership
        var warranty = await _dbContext.Warranties
            .FirstOrDefaultAsync(w => w.Id == warrantyId && w.UserId == userId);

        if (warranty == null)
        {
            return false;
        }

        _dbContext.Warranties.Remove(warranty);
        await _dbContext.SaveChangesAsync();

        return true;
    }
}
