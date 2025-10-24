namespace Chirp.Razor.Models
{
    
    public class CheepViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}