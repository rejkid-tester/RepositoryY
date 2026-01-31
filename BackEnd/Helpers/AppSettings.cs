namespace Backend.Helpers
{
    public class AppSettings
    {

        // refresh token time to live (in days), inactive tokens are
        // automatically deleted from the database after this time
        public int RefreshTokenTTL { get; set; }

        public string EmailFrom { get; set; } = string.Empty;
        
        // Brevo (formerly Sendinblue) settings - FREE 300 emails/day!
        
        public string? Tasks { get; set; }
        public string? GroupTasks { get; set; }
        public string ClientTimeZoneId { get; set; } = string.Empty;
    }
}