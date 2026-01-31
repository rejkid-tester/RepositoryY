using Backend.Interfaces;
using BackEnd.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BackEnd.Services
{
    public sealed class SmsServiceBrevo : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SmsServiceBrevo> _logger;
        private readonly string _brevoApiKey;
        private readonly string _sender;

        public SmsServiceBrevo(HttpClient httpClient, ILogger<SmsServiceBrevo> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;

            // User Secrets key: Brevo:ApiKey / Brevo:Sender
            _brevoApiKey = (Environment.GetEnvironmentVariable("BREVO_API_KEY")
                ?? config["Brevo:BREVO_API_KEY"]
                ?? string.Empty).Trim();

            _sender = (Environment.GetEnvironmentVariable("BREVO_SENDER")
                ?? config["Brevo:BREVO_SENDER"]
                ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(_brevoApiKey))
                _logger.LogWarning("Brevo ApiKey is missing (config key `Brevo:ApiKey`).");

            if (string.IsNullOrWhiteSpace(_sender))
                _logger.LogWarning("Brevo Sender is missing (config key `Brevo:Sender`).");

            // Safe diagnostics: never log the api key, only length/prefix
            if (!string.IsNullOrWhiteSpace(_brevoApiKey))
                _logger.LogInformation("Brevo ApiKey loaded (length={Length}).", _brevoApiKey.Length);
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(_brevoApiKey) || string.IsNullOrWhiteSpace(_sender))
            {
                _logger.LogWarning("Brevo credentials not configured.");
                return false;
            }

            var url = "https://api.brevo.com/v3/transactionalSMS/sms";

            var payload = new
            {
                sender = _sender,
                recipient = phoneNumber,
                content = message,
                type = "transactional"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            // Brevo expects this exact header.
            request.Headers.TryAddWithoutValidation("api-key", _brevoApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _httpClient.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("SMS sent successfully to {PhoneNumber}", phoneNumber);
                return true;
            }

            _logger.LogError("Failed to send SMS via Brevo. Status: {Status}. Error: {Error}", response.StatusCode, body);
            return false;
        }

        public Task<bool> SendMfaCodeAsync(string phoneNumber, string code)
        {
            var message = $"Your verification code is: {code}. This code expires in 5 minutes.";
            return SendSmsAsync(phoneNumber, message);
        }
    }
}
