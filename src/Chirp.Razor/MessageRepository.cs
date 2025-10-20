
public class MessageRepository : IMessageRepository{

public async Task<int> CreateMessage(MessageDTO message)
{
    Message newMessage = new() { Text = message.Text, ...};
    var queryResult = await _dbContext.Messages.AddAsync(newMessage); // does not write to the database!

    await _dbContext.SaveChangesAsync(); // persist the changes in the database
    return queryResult.Entity.CheepId;
}

public async Task<List<MessageDTO>> ReadMessages(string userName)
{
  // Formulate the query - will be translated to SQL by EF Core
  var query = _dbContext.Messages.Select(message => new { message.User, message.Text });
  // Execute the query
  var result = await query.ToListAsync();

  // ...
}


}