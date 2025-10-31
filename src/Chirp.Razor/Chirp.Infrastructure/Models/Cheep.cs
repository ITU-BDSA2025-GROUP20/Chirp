using System.ComponentModel.DataAnnotations;

namespace Chirp.Infrastructure.Models
{

    public class Cheep
    {
        public int CheepId { get; set; }

        [StringLength(160)]
        public string Text { get; set; } = string.Empty;

        public int AuthorId { get; set; }
        public Author Author { get; set; } = null!;
        public DateTime TimeStamp { get; set; }
    }
}