using System.ComponentModel.DataAnnotations;

namespace Chirp.Models
{
    public class Cheep
    {
        [StringLength(140)]
        public string Content { get; set; }
    }
}
namespace Chirp.Razor.Models
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