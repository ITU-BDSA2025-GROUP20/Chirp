using Microsoft.EntityFrameworkCore;
using System;
using Chirp.Razor.Data;

namespace Chirp.Razor
{
    public class MessageRepository : IMessageRepository
    {
        private readonly CheepDbContext _dbContext;

        public MessageRepository(CheepDbContext dbContext)
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

        public async Task<int> CreateMessage(MessageDTO message)
        {
            var newMessage = new Message
            {
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                UserId = _dbContext.Users
                    .Where(u => u.Name == message.UserName)
                    .Select(u => u.UserId)
                    .FirstOrDefault()
            };

            var result = await _dbContext.Messages.AddAsync(newMessage);
            await _dbContext.SaveChangesAsync();

            return result.Entity.MessageId;
        }

        public async Task UpdateMessage(MessageDTO message)
        {
            var existing = await _dbContext.Messages.FindAsync(message.Id);
            if (existing != null)
            {
                existing.Text = message.Text;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
