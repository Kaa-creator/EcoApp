namespace EcoApp.API.Models
{
    public class EcoTask
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Points { get; set; }

        public string Category { get; set; }

        public bool RequiresPhoto { get; set; }
    }
}