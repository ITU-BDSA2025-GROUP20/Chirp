namespace Chirp.Razor.Models
{
    public class MessageModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; } = string.Empty;
        public UserModel User { get; set;} = null!;
    }
}