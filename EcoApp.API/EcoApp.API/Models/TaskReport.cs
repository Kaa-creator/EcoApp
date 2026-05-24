namespace EcoApp.API.Models
{
    public class TaskReport
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TaskId { get; set; }
        public string PhotoUrl { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }

        // НОВОЕ: время когда отклонили (для таймера 10 минут)
        public DateTime? RejectedAt { get; set; }
    }
}