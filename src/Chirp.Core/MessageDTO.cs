using System;
using System.ComponentModel.DataAnnotations;

namespace Core;
public class MessageDTO
{
        public int Id { get; set; } 

        [StringLength(160)]
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int AuthorId { get; set; }

        public string AuthorName { get; set; } = null!;
        
        public DateTime TimeStamp { get; set; }
}
