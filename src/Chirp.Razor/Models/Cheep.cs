namespace Chirp.Razor.Models
{
    
    public class Cheep
    {
        public int CheepId { get; set; }
        public string Text { get; set; } = string.Empty;

        public int AuthorId { get; set; }
        public Author Author { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}