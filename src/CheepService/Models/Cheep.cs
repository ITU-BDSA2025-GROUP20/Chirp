namespace ChirpService.Models
{
    public class Cheep
    {
        public int Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}