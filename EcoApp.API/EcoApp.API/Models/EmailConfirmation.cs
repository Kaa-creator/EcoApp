namespace EcoApp.API.Models
{
    public class EmailConfirmation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}