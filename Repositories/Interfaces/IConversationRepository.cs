using messenger.Models;
using messenger.Controllers.site;

namespace messenger.Repositories.Interfaces
{
    public interface IConversationRepository
    {
        Task<object?> GetExistingPrivateConversation(int userId, int otherUserId);
        Task<Conversation> CreateConversation(Conversation conversation);
        Task AddMembers(List<ConversationMember> members);
        Task<object?> GetConversationById(int id);
        Task<List<ConversationQueryResult>> GetUserConversations(int userId);
        Task<List<object>> GetMembersByConversationIds(List<int> conversationIds);
        Task<List<object>> GetUnreadMessages(List<int> conversationIds, int userId);
        Task<List<object>> GetMessages(int conversationId, int page, int pageSize);
        Task<bool> IsMember(int conversationId, int userId);
    }
}