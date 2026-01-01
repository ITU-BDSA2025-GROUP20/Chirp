using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Models
{

    public class Cheep
    {
        public int CheepId { get; set; } 
        
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }

        public int AuthorId { get; set; }

        public Author Author { get; set; } = null!;
        
        public DateTime TimeStamp { get; set; }
    }
}