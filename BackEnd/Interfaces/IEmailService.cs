namespace Backend.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Sends an email asynchronously.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject.</param>
    /// <param name="body">Email body (plain text or HTML).</param>
    /// <returns>True if sent successfully, otherwise false.</returns>
    Task<bool> SendEmailAsync(string to, string subject, string body);
}