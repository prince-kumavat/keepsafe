using System;
using System.IO;
using System.Threading.Tasks;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Drawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace KeepSafe.Services;

public enum DocumentType
{
    Unknown,
    Jpeg,
    Png,
    Pdf
}

public class ProcessDocumentResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public byte[]? PdfBytes { get; set; }
    public bool Compressed { get; set; }
}

public interface IDocumentProcessor
{
    DocumentType DetectType(Stream stream);
    Task<ProcessDocumentResult> ProcessAsync(Stream stream, string originalFileName);
}

public class DocumentProcessor : IDocumentProcessor
{
    public DocumentType DetectType(Stream stream)
    {
        if (stream.Length < 4) return DocumentType.Unknown;

        long originalPos = stream.Position;
        stream.Position = 0;
        byte[] header = new byte[8];
        int bytesRead = stream.Read(header, 0, 8);
        stream.Position = originalPos;

        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return DocumentType.Jpeg;
        }

        if (bytesRead >= 8 &&
            header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47 &&
            header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A)
        {
            return DocumentType.Png;
        }

        if (bytesRead >= 4 &&
            header[0] == (byte)'%' && header[1] == (byte)'P' && header[2] == (byte)'D' && header[3] == (byte)'F')
        {
            return DocumentType.Pdf;
        }

        return DocumentType.Unknown;
    }

    public async Task<ProcessDocumentResult> ProcessAsync(Stream stream, string originalFileName)
    {
        var docType = DetectType(stream);
        if (docType == DocumentType.Unknown)
        {
            return new ProcessDocumentResult
            {
                Success = false,
                ErrorMessage = "Unsupported file type. Please upload a JPEG, PNG, or PDF file."
            };
        }

        stream.Position = 0;

        if (docType == DocumentType.Jpeg || docType == DocumentType.Png)
        {
            return await ConvertImageToPdfAsync(stream);
        }
        else // PDF
        {
            return await CompressPdfAsync(stream);
        }
    }

    private async Task<ProcessDocumentResult> ConvertImageToPdfAsync(Stream imageStream)
    {
        try
        {
            // 1. Safely decode and auto-orient with ImageSharp
            using var image = await Image.LoadAsync<Rgba32>(imageStream);
            image.Mutate(x => x.AutoOrient());

            // 2. Resize proportionally to max longest side 1600px without enlarging
            int maxDim = 1600;
            if (image.Width > maxDim || image.Height > maxDim)
            {
                int newWidth, newHeight;
                if (image.Width >= image.Height)
                {
                    newWidth = maxDim;
                    newHeight = (int)Math.Round((double)image.Height * maxDim / image.Width);
                }
                else
                {
                    newHeight = maxDim;
                    newWidth = (int)Math.Round((double)image.Width * maxDim / image.Height);
                }

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Max
                }));
            }

            // 3. Flatten PNG transparency onto white background
            using var whiteBackground = new Image<Rgba32>(image.Width, image.Height, Color.White);
            whiteBackground.Mutate(x => x.DrawImage(image, 1.0f));

            // 4. Re-encode as JPEG at quality 70
            using var jpegStream = new MemoryStream();
            var encoder = new JpegEncoder
            {
                Quality = 70
            };
            await whiteBackground.SaveAsJpegAsync(jpegStream, encoder);
            jpegStream.Position = 0;

            // 5. Embed image into PDF page sized to the image dimensions
            using var pdfDoc = new PdfDocument();
            var page = pdfDoc.AddPage();

            page.Width = XUnit.FromPoint(image.Width);
            page.Height = XUnit.FromPoint(image.Height);

            using (var xImage = XImage.FromStream(jpegStream))
            using (var gfx = XGraphics.FromPdfPage(page))
            {
                gfx.DrawImage(xImage, 0, 0, page.Width.Point, page.Height.Point);
            }

            // 6. Generate PDF in memory
            using var outputPdf = new MemoryStream();
            pdfDoc.Options.CompressContentStreams = true;
            pdfDoc.Save(outputPdf);
            byte[] pdfBytes = outputPdf.ToArray();

            return new ProcessDocumentResult
            {
                Success = true,
                PdfBytes = pdfBytes,
                Compressed = true
            };
        }
        catch (Exception)
        {
            return new ProcessDocumentResult
            {
                Success = false,
                ErrorMessage = "Failed to process image file. It may be corrupted or invalid."
            };
        }
    }

    private Task<ProcessDocumentResult> CompressPdfAsync(Stream pdfStream)
    {
        try
        {
            byte[] originalBytes;
            if (pdfStream is MemoryStream ms)
            {
                originalBytes = ms.ToArray();
            }
            else
            {
                using var copyMs = new MemoryStream();
                pdfStream.CopyTo(copyMs);
                originalBytes = copyMs.ToArray();
            }

            // Attempt to open with PdfSharp
            using var inputMs = new MemoryStream(originalBytes);
            PdfDocument document;
            try
            {
                document = PdfReader.Open(inputMs, PdfDocumentOpenMode.Modify);
            }
            catch (PdfReaderException)
            {
                return Task.FromResult(new ProcessDocumentResult
                {
                    Success = false,
                    ErrorMessage = "Encrypted or corrupted PDF is not supported."
                });
            }
            catch (Exception)
            {
                return Task.FromResult(new ProcessDocumentResult
                {
                    Success = false,
                    ErrorMessage = "Encrypted or corrupted PDF is not supported."
                });
            }

            using (document)
            {
                // Reject encrypted or password-protected PDF
                if (document.SecuritySettings.IsEncrypted)
                {
                    return Task.FromResult(new ProcessDocumentResult
                    {
                        Success = false,
                        ErrorMessage = "Encrypted or corrupted PDF is not supported."
                    });
                }

                document.Options.CompressContentStreams = true;
                document.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;

                using var compressedMs = new MemoryStream();
                document.Save(compressedMs);
                byte[] compressedBytes = compressedMs.ToArray();

                if (compressedBytes.Length < originalBytes.Length)
                {
                    return Task.FromResult(new ProcessDocumentResult
                    {
                        Success = true,
                        PdfBytes = compressedBytes,
                        Compressed = true
                    });
                }
                else
                {
                    return Task.FromResult(new ProcessDocumentResult
                    {
                        Success = true,
                        PdfBytes = originalBytes,
                        Compressed = false
                    });
                }
            }
        }
        catch (Exception)
        {
            return Task.FromResult(new ProcessDocumentResult
            {
                Success = false,
                ErrorMessage = "Encrypted or corrupted PDF is not supported."
            });
        }
    }
}
