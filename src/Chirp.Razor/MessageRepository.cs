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
            var query = _dbContext.Cheeps
                .Where(m => m.Author.Name == userName)
                .Select(m => new MessageDTO
                {
                    Id = m.CheepId,
                    Text = m.Text,
                    UserName = m.Author.Name,
                    CreatedAt = m.CreatedAt
                });

            return await query.ToListAsync();
        }

        public async Task<int> CreateMessage(MessageDTO message)
        {
            var newMessage = new Text
            {
                Text = message.Text,
                CreatedAt = message.CreatedAt,
                AuthorId = _dbContext.Authors
                    .Where(u => u.Name == message.UserName)
                    .Select(u => u.AuthorId)
                    .FirstOrDefault()
            };

            var result = await _dbContext.Cheeps.AddAsync(newMessage);
            await _dbContext.SaveChangesAsync();

            return result.Entity.CheepId;
        }

        public async Task UpdateMessage(MessageDTO message)
        {
            var existing = await _dbContext.Cheeps.FindAsync(message.Id);
            if (existing != null)
            {
                existing.Text = message.Text;
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
