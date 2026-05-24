namespace EcoApp.API.Models
{
    public class EcoEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        // ✅ Теперь необязательные — можно создавать без города и адреса
        public string? City { get; set; }
        public string? Address { get; set; }

        public DateTime EventDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Category { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? Organizer { get; set; }
        public string? ContactPhone { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}