using System.ComponentModel.DataAnnotations;

namespace Chirp.Infrastructure.Models
{
    
    public class CheepViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [StringLength(500)]
        public string Text { get; set; } = string.Empty;
        [Required]
        public string AuthorName { get; set; } = string.Empty;
        public string TimeStamp { get; set; } = string.Empty;
    }
}