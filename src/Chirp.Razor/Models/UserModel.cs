namespace Chirp.Razor.Models
{
    
    public class UserModel
    {
        public int Id { get; set; }
        public string name { get; set; } = string.Empty;
        public ICollection<MessageModel> Messages { get; set;} 
    }
}