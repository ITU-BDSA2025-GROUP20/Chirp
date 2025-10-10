namespace SimpleDB
{
    public class Cheep
{
    public string Author { get; set; }
    public string Message { get; set; }
    public string Timestamp { get; set; }

    public Cheep() { }

    public Cheep(string author, string message, string timestamp)
    {
        Author = author;
        Message = message;
        Timestamp = timestamp;
    }
}
}
