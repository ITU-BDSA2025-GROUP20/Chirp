namespace Chirp.Core;
public class MessageDTO
{
    public int Id { get; set; }           
    public string Text { get; set; } = ""; 
    public string? AuthorName { get; set; }  
    public DateTime TimeStamp { get; set; } 
}
