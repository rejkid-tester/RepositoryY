using Backend.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services;

public sealed class EmailServiceBrevo : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmailServiceBrevo> _logger;
    private readonly string _brevoApiKey;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailServiceBrevo(HttpClient httpClient, ILogger<EmailServiceBrevo> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        _brevoApiKey = (Environment.GetEnvironmentVariable("BREVO_API_KEY")
            ?? config["Brevo:BREVO_API_KEY"]
            ?? string.Empty).Trim();

        _fromEmail = (Environment.GetEnvironmentVariable("BREVO_FROM_EMAIL")
            ?? config["Brevo:BREVO_FROM_EMAIL"]
            ?? string.Empty).Trim();

        _fromName = (Environment.GetEnvironmentVariable("BREVO_FROM_NAME")
            ?? config["Brevo:BREVO_FROM_NAME"]
            ?? "BackEnd").Trim();

    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_brevoApiKey) || string.IsNullOrWhiteSpace(_fromEmail))
        {
            _logger.LogWarning("Brevo email config missing. Need `Brevo:BREVO_API_KEY` and `Brevo:BREVO_FROM_EMAIL`.");
            return false;
        }

        var url = "https://api.brevo.com/v3/smtp/email";

        var payload = new
        {
            sender = new { email = _fromEmail, name = _fromName },
            to = new[] { new { email = to } },
            subject,
            textContent = body
            // OR: htmlContent = body
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        request.Headers.TryAddWithoutValidation("api-key", _brevoApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Email sent successfully to {To}", to);
            return true;
        }

        _logger.LogError("Failed to send email. Status: {Status}. Error: {Error}", response.StatusCode, responseBody);
        return false;
    }
}
