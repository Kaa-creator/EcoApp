namespace EcoApp.API.DTO
{
    public class CreateTaskReportDto
    {
        public int UserId { get; set; }
        public int TaskId { get; set; }
        public string PhotoUrl { get; set; } = "";
        public string Comment { get; set; } = "";
    }
}