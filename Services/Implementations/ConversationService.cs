using messenger.Models;
using messenger.DTOs;
using messenger.Repositories.Interfaces;
using messenger.Services.Interfaces;

namespace messenger.Services.Implementations
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _repo;

        public ConversationService(IConversationRepository repo)
        {
            _repo = repo;
        }

        public async Task<object> GetConversations(int userId)
        {
            var conversationData = await _repo.GetUserConversations(userId);
            var conversationIds = conversationData.Select(c => c.Id).ToList();

            var allMembers = await _repo.GetMembersByConversationIds(conversationIds);
            var unreadMessages = await _repo.GetUnreadMessages(conversationIds, userId);

            // Tính unread count trong memory
            var unreadCounts = conversationData
                .Select(c => new
                {
                    conversationId = c.Id,
                    count = unreadMessages
                        .Cast<dynamic>()
                        .Count(m => m.ConversationId == c.Id
                            && (c.LastSeenMessageId == null || m.Id > c.LastSeenMessageId))
                })
                .ToDictionary(x => x.conversationId, x => x.count);

            return conversationData.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                type = c.Type,
                avatar = c.Avatar,
                lastMessage = c.LastMessageId != null ? new
                {
                    content = c.LastMessageContent,
                    senderId = c.LastMessageSenderId,
                    senderName = c.LastMessageSenderName,
                    createdAt = c.LastMessageCreatedAt
                } : null,
                members = allMembers
                    .Cast<dynamic>()
                    .Where(m => m.conversationId == c.Id),
                unreadCount = unreadCounts.GetValueOrDefault(c.Id, 0),
                isPinned = c.IsPinned,
                isMuted = c.IsMuted
            });
        }

        public async Task<(bool success, object? data, string? error, bool isNew)> CreateConversation(int userId, CreateConversationDto dto)
        {
            // Validate: không tự add mình vào
            if (dto.MemberIds.Contains(userId))
                return (false, null, "Cannot create conversation with yourself", false);

            // Validate group cần ít nhất 2 người khác
            if (dto.Type == "group" && dto.MemberIds.Count < 2)
                return (false, null, "Group cần ít nhất 3 người", false);

            // Validate tên group bắt buộc
            if (dto.Type == "group" && string.IsNullOrWhiteSpace(dto.Name))
                return (false, null, "Group cần có tên", false);

            // Kiểm tra private conversation đã tồn tại chưa
            if (dto.Type == "private" && dto.MemberIds.Count == 1)
            {
                var existing = await _repo.GetExistingPrivateConversation(userId, dto.MemberIds[0]);
                if (existing != null)
                    return (true, existing, null, false); // isNew = false → trả 200 thay vì 201
            }

            // Tạo conversation mới
            var conversation = new Conversation
            {
                Name = dto.Name,
                Type = dto.Type,
                Avatar = dto.Avatar,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.CreateConversation(conversation);

            // Build danh sách members: creator = admin, còn lại = member
            var members = new List<ConversationMember>
            {
                new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = userId,
                    Role = "admin",
                    JoinedAt = DateTime.UtcNow
                }
            };

            foreach (var memberId in dto.MemberIds)
            {
                members.Add(new ConversationMember
                {
                    ConversationId = conversation.Id,
                    UserId = memberId,
                    Role = "member",
                    JoinedAt = DateTime.UtcNow
                });
            }

            await _repo.AddMembers(members);

            var result = new
            {
                id = conversation.Id,
                name = conversation.Name,
                type = conversation.Type,
                avatar = conversation.Avatar,
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt
            };

            return (true, result, null, true); // isNew = true → trả 201
        }

        public async Task<object?> GetConversationById(int id)
        {
            return await _repo.GetConversationById(id);
        }

        public async Task<(bool isMember, List<object> messages)> GetMessages(int conversationId, int userId, int page, int pageSize)
        {
            var isMember = await _repo.IsMember(conversationId, userId);
            if (!isMember)
                return (false, new List<object>());

            var messages = await _repo.GetMessages(conversationId, page, pageSize);
            var ordered = messages
                .Cast<dynamic>()
                .OrderBy(m => m.createdAt)
                .Cast<object>()
                .ToList();

            return (true, ordered);
        }
    }
}