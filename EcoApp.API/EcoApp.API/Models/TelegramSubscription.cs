namespace EcoApp.API.Models
{
    public class TelegramSubscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public long ChatId { get; set; }
        public bool NotifyEvents { get; set; } = true;
        public bool NotifyArticles { get; set; } = true;
        public bool NotifyTasks { get; set; } = true;
        public bool NotifyTaskApproved { get; set; } = true;
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
    }
}