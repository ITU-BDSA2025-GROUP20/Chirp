using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Models
{
    
    public class Author
    {
        [Required]
        public int AuthorId { get; set; } 
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        public ICollection<Cheep> Cheeps { get; set;} = new List<Cheep>(); 
        public ICollection<Follow> Following { get; set; } = new List<Follow>();
        public ICollection<Follow> Followers { get; set; } = new List<Follow>();
    }
}