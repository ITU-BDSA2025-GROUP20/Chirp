using System.ComponentModel.DataAnnotations;

namespace Chirp.Razor.Models
{
    
    public class CheepViewModel
    {
        [Required]
        public int Id { get; set; }
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
        [Required]
        public string AuthorName { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
    }
}