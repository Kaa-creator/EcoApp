namespace EcoApp.API.Models
{
    public class NotificationLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string Type { get; set; } = "";
        public string Category { get; set; } = "";
        public string Message { get; set; } = "";
        public bool IsSent { get; set; } = false;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}