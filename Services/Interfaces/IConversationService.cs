using messenger.DTOs;
namespace messenger.Services.Interfaces
{
    public interface IConversationService
    {
        Task<object> GetConversations(int userId);
        Task<(bool success, object? data, string? error, bool isNew)> CreateConversation(int userId, CreateConversationDto dto);
        Task<object?> GetConversationById(int id);
        Task<(bool isMember, List<object> messages)> GetMessages(int conversationId, int userId, int page, int pageSize);
    }
}