namespace Chirp.Razor.Models
{
    
    public class CheepViewModel
    {
        public int Id { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}