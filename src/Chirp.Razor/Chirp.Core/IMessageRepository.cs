namespace Chirp.Core
{
    public interface IMessageRepository
    {
        Task<List<MessageDTO>> ReadMessages(string userName);
        Task<int> CreateMessage(MessageDTO message);
        Task UpdateMessage(MessageDTO message);
    }
}
