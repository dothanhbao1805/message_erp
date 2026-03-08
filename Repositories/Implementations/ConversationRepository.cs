using Microsoft.EntityFrameworkCore;
using messenger.Data;
using messenger.Models;
using messenger.Repositories.Interfaces;
using messenger.Controllers.site;

namespace messenger.Repositories.Implementations
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly MessengerContext _context;

        public ConversationRepository(MessengerContext context)
        {
            _context = context;
        }

        public async Task<object?> GetExistingPrivateConversation(int userId, int otherUserId)
        {
            return await _context.Conversations
                .Where(c => c.Type == "private")
                .Where(c => c.ConversationMembers.Count == 2
                    && c.ConversationMembers.Any(m => m.UserId == userId && m.LeftAt == null)
                    && c.ConversationMembers.Any(m => m.UserId == otherUserId && m.LeftAt == null))
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    type = c.Type,
                    avatar = c.Avatar,
                    createdAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<Conversation> CreateConversation(Conversation conversation)
        {
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task AddMembers(List<ConversationMember> members)
        {
            _context.ConversationMembers.AddRange(members);
            await _context.SaveChangesAsync();
        }

        public async Task<object?> GetConversationById(int id)
        {
            return await _context.Conversations
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    type = c.Type,
                    avatar = c.Avatar,
                    members = c.ConversationMembers
                        .Where(cm => cm.LeftAt == null)
                        .Select(cm => new
                        {
                            id = cm.User.Id,
                            fullName = cm.User.FullName,
                            avatar = cm.User.Avatar,
                            role = cm.Role
                        })
                })
                .FirstOrDefaultAsync();
        }

        public async Task<List<ConversationQueryResult>> GetUserConversations(int userId)
        {
            var sql = @"
                SELECT 
                    c.Id,
                    c.Name,
                    c.Type,
                    c.Avatar,
                    lm.Id AS LastMessageId,
                    lm.Content AS LastMessageContent,
                    lm.SenderId AS LastMessageSenderId,
                    u.FullName AS LastMessageSenderName,
                    lm.CreatedAt AS LastMessageCreatedAt,
                    cm.IsPinned,
                    cm.IsMuted,
                    cm.LastSeenMessageId,
                    COALESCE(lm.CreatedAt, '1900-01-01') AS SortDate
                FROM ConversationMembers cm
                INNER JOIN Conversations c ON cm.ConversationId = c.Id
                LEFT JOIN Messages lm ON c.LastMessageId = lm.Id
                LEFT JOIN Users u ON lm.SenderId = u.Id
                WHERE cm.UserId = {0} AND cm.LeftAt IS NULL
                ORDER BY SortDate DESC
            ";

            return await _context.Database
                .SqlQueryRaw<ConversationQueryResult>(sql, userId)
                .ToListAsync();
        }

        public async Task<List<object>> GetMembersByConversationIds(List<int> conversationIds)
        {
            return await _context.ConversationMembers
                .Where(cm => conversationIds.Contains(cm.ConversationId) && cm.LeftAt == null)
                .Include(cm => cm.User)
                .Select(cm => (object)new
                {
                    conversationId = cm.ConversationId,
                    id = cm.User.Id,
                    fullName = cm.User.FullName,
                    avatar = cm.User.Avatar
                })
                .ToListAsync();
        }

        public async Task<List<object>> GetUnreadMessages(List<int> conversationIds, int userId)
        {
            return await _context.Messages
                .Where(m => conversationIds.Contains(m.ConversationId) && m.SenderId != userId)
                .Select(m => (object)new
                {
                    m.ConversationId,
                    m.Id
                })
                .ToListAsync();
        }

        public async Task<bool> IsMember(int conversationId, int userId)
        {
            return await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId
                    && cm.UserId == userId
                    && cm.LeftAt == null);
        }

        public async Task<List<object>> GetMessages(int conversationId, int page, int pageSize)
        {
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => (object)new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = m.Sender.FullName,
                    senderAvatar = m.Sender.Avatar,
                    content = m.Content,
                    type = m.Type,
                    fileUrl = m.FileUrl,
                    fileName = m.FileName,
                    fileSize = m.FileSize,
                    isEdited = m.IsEdited,
                    createdAt = m.CreatedAt,
                    readBy = m.MessageReads.Select(mr => new
                    {
                        userId = mr.UserId,
                        readAt = mr.ReadAt
                    })
                })
                .ToListAsync();
        }
    }
}