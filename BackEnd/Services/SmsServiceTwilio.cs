using BackEnd.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;

namespace Backend.Services;

public sealed class SmsServiceTwilio : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SmsServiceTwilio> _logger;
    private readonly string _accountSid;
    private readonly string _fromPhoneNumber;
    private readonly string _authToken;

    public SmsServiceTwilio(HttpClient httpClient, ILogger<SmsServiceTwilio> logger, IConfiguration config)
    {
        _httpClient = httpClient;
        _logger = logger;

        // Read from configuration (User Secrets, appsettings, env vars, etc.)
        _accountSid = (Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
            ?? config["Twilio:TWILIO_ACCOUNT_SID"]
            ?? "").Trim();

        _fromPhoneNumber = (Environment.GetEnvironmentVariable("TWILIO_PHONE_NUMBER")
            ?? config["Twilio:TWILIO_PHONE_NUMBER"]
            ?? "").Trim();

        _authToken = (Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
            ?? config["Twilio:TWILIO_AUTH_TOKEN"]
            ?? "").Trim();
    }

    public async Task<bool> SendMfaCodeAsync(string phoneNumber, string code)
    {
        var message = $"Your verification code is: {code}. This code expires in 5 minutes.";
        return await SendSmsAsync(phoneNumber, message);
    }

    public async Task<bool> SendSmsAsync(string phoneNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(_accountSid) ||
            string.IsNullOrWhiteSpace(_fromPhoneNumber) ||
            string.IsNullOrWhiteSpace(_authToken))
        {
            _logger.LogWarning("Twilio credentials not configured. Check User Secrets or environment variables.");
            return false;
        }

        var url = $"https://api.twilio.com/2010-04-01/Accounts/{_accountSid}/Messages.json";

        var formData = new Dictionary<string, string>
        {
            { "From", _fromPhoneNumber },
            { "To", phoneNumber },
            { "Body", message }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(formData)
        };

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{_accountSid}:{_authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        using var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
            return true;
        }

        var error = await response.Content.ReadAsStringAsync();
        _logger.LogError("Failed to send SMS. Status: {Status}. Error: {Error}", response.StatusCode, error);
        return false;
    }
}