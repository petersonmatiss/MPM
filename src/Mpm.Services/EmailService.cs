using Microsoft.Extensions.Logging;

namespace Mpm.Services;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public List<EmailAttachment> Attachments { get; set; } = new();
}

public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public class EmailSendResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
}

public interface IEmailService
{
    /// <summary>
    /// Sends an email message
    /// </summary>
    /// <param name="message">The email message to send</param>
    /// <returns>Result of the email send operation</returns>
    Task<EmailSendResult> SendEmailAsync(EmailMessage message);
    
    /// <summary>
    /// Validates an email address format
    /// </summary>
    /// <param name="email">Email address to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    bool IsValidEmailAddress(string email);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    
    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }
    
    public async Task<EmailSendResult> SendEmailAsync(EmailMessage message)
    {
        try
        {
            // In a real implementation, this would integrate with:
            // - Azure Communication Services Email
            // - Microsoft Graph API
            // - SMTP server
            // - SendGrid, etc.
            
            _logger.LogInformation("Sending email to {To} with subject '{Subject}'", 
                message.To, message.Subject);
            
            // Simulate email send
            await Task.Delay(100);
            
            // For demonstration, we'll always succeed
            return new EmailSendResult
            {
                Success = true,
                MessageId = Guid.NewGuid().ToString("N")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", message.To);
            
            return new EmailSendResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
    
    public bool IsValidEmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;
            
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}