using Mpm.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Mpm.Services;

public interface IPdfGenerationService
{
    /// <summary>
    /// Generates a PDF for a price request
    /// </summary>
    /// <param name="priceRequest">The price request to generate PDF for</param>
    /// <returns>PDF byte array and SHA256 hash</returns>
    Task<(byte[] PdfData, string Hash)> GeneratePriceRequestPdfAsync(PriceRequest priceRequest);
}

public class PdfGenerationService : IPdfGenerationService
{
    public async Task<(byte[] PdfData, string Hash)> GeneratePriceRequestPdfAsync(PriceRequest priceRequest)
    {
        // For now, create a simple text-based PDF placeholder
        // In a real implementation, you would use a library like iText7, PuppeteerSharp, or similar
        
        var content = GeneratePriceRequestContent(priceRequest);
        var pdfData = System.Text.Encoding.UTF8.GetBytes(content);
        
        // Calculate SHA256 hash
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(pdfData);
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        
        return await Task.FromResult((pdfData, hash));
    }
    
    private static string GeneratePriceRequestContent(PriceRequest priceRequest)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("PRICE REQUEST");
        sb.AppendLine("=============");
        sb.AppendLine();
        sb.AppendLine($"PR Number: {priceRequest.Number}");
        sb.AppendLine($"Date: {priceRequest.RequestDate:yyyy-MM-dd}");
        if (priceRequest.RequiredDate.HasValue)
            sb.AppendLine($"Required Date: {priceRequest.RequiredDate:yyyy-MM-dd}");
        sb.AppendLine($"Currency: {priceRequest.Currency}");
        sb.AppendLine($"Status: {priceRequest.Status}");
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(priceRequest.Description))
        {
            sb.AppendLine($"Description: {priceRequest.Description}");
            sb.AppendLine();
        }
        
        if (priceRequest.Project != null)
        {
            sb.AppendLine($"Project: {priceRequest.Project.Name} ({priceRequest.Project.Code})");
            sb.AppendLine();
        }
        
        sb.AppendLine("ITEMS REQUESTED:");
        sb.AppendLine("================");
        sb.AppendLine();
        
        foreach (var line in priceRequest.Lines)
        {
            sb.AppendLine($"Material: {line.Material?.Grade} - {line.Material?.Description}");
            sb.AppendLine($"Quantity: {line.Quantity} {line.UnitOfMeasure}");
            if (!string.IsNullOrEmpty(line.Description))
                sb.AppendLine($"Description: {line.Description}");
            if (!string.IsNullOrEmpty(line.Notes))
                sb.AppendLine($"Notes: {line.Notes}");
            sb.AppendLine();
        }
        
        if (!string.IsNullOrEmpty(priceRequest.Notes))
        {
            sb.AppendLine("ADDITIONAL NOTES:");
            sb.AppendLine("================");
            sb.AppendLine(priceRequest.Notes);
            sb.AppendLine();
        }
        
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        
        return sb.ToString();
    }
}