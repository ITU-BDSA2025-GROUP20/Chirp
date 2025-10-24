public class MessageRepository : IMessageRepository
{
    private readonly ChatDBContext _dbContext;

    public MessageRepository(ChatDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MessageDTO>> ReadMessages(string userName)
{
    var query = _dbContext.Messages
        .Where(m => m.User.Name == userName)
        .Select(m => new MessageDTO
        {
            Id = m.MessageId,
            Text = m.Text,
            UserName = m.User.Name,
            CreatedAt = m.CreatedAt
        });

    return await query.ToListAsync();
}

}
