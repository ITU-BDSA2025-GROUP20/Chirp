using System.ComponentModel.DataAnnotations;

namespace Chirp.Razor.Models
{
    
    public class Author
    {
        [Required]
        public int AuthorId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public ICollection<Cheep> Messages { get; set;} = new List<Message>(); 
    }
}