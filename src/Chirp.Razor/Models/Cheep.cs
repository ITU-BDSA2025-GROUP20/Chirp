namespace Chirp.Razor.Models
{
    
    public class Cheep
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public Author Author { get; set; }
        public DateTime Timestamp { get; set; }
    }
}