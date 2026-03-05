using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using messenger.Data;
using messenger.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace messenger.Controllers.site
{
    [Authorize]
    [ApiController]
    [Route("api/conversations")]
    public class ConversationsController : ControllerBase
    {
        private readonly MessengerContext _context;

        public ConversationsController(MessengerContext context)
        {
            _context = context;
        }

        // GET: api/conversations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetConversations()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

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

            var conversationData = await _context.Database
                .SqlQueryRaw<ConversationQueryResult>(sql, userId)
                .ToListAsync();

            var conversationIds = conversationData.Select(c => c.Id).ToList();

            // Load members
            var allMembers = await _context.ConversationMembers
                .Where(cm => conversationIds.Contains(cm.ConversationId) && cm.LeftAt == null)
                .Include(cm => cm.User)
                .Select(cm => new
                {
                    conversationId = cm.ConversationId,
                    id = cm.User.Id,
                    fullName = cm.User.FullName,
                    avatar = cm.User.Avatar
                })
                .ToListAsync();

            // Load unread counts - FIX: Query riêng, không reference conversationData
            var unreadMessages = await _context.Messages
                .Where(m => conversationIds.Contains(m.ConversationId) && m.SenderId != userId)
                .Select(m => new
                {
                    m.ConversationId,
                    m.Id
                })
                .ToListAsync();

            // Calculate unread counts in memory
            var unreadCounts = conversationData
                .Select(c => new
                {
                    conversationId = c.Id,
                    count = unreadMessages.Count(m =>
                        m.ConversationId == c.Id &&
                        (c.LastSeenMessageId == null || m.Id > c.LastSeenMessageId))
                })
                .ToDictionary(x => x.conversationId, x => x.count);

            // Build result
            var result = conversationData.Select(c => new
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
                members = allMembers.Where(m => m.conversationId == c.Id),
                unreadCount = unreadCounts.GetValueOrDefault(c.Id, 0),
                isPinned = c.IsPinned,
                isMuted = c.IsMuted
            });

            return Ok(result);
        }

        // POST: api/conversations
        [HttpPost]
        public async Task<ActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (dto.MemberIds.Contains(userId))
            {
                return BadRequest(new { error = "Cannot create conversation with yourself" });
            }

            // Kiểm tra private conversation đã tồn tại
            if (dto.Type == "private" && dto.MemberIds.Count == 1)
            {
                var otherUserId = dto.MemberIds[0];
                var existingConv = await _context.Conversations
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

                if (existingConv != null)
                {
                    return Ok(existingConv);
                }
            }

            var conversation = new Conversation
            {
                Name = dto.Name,
                Type = dto.Type,
                Avatar = dto.Avatar,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

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

            _context.ConversationMembers.AddRange(members);
            await _context.SaveChangesAsync();

            var result = new
            {
                id = conversation.Id,
                name = conversation.Name,
                type = conversation.Type,
                avatar = conversation.Avatar,
                createdAt = conversation.CreatedAt,
                updatedAt = conversation.UpdatedAt
            };

            return CreatedAtAction(nameof(GetConversation), new { id = conversation.Id }, result);
        }

        // GET: api/conversations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetConversation(int id)
        {
            var conversation = await _context.Conversations
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

            if (conversation == null)
            {
                return NotFound(new { error = "Conversation not found" });
            }

            return Ok(conversation);
        }

        // GET: api/conversations/{id}/messages
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<IEnumerable<object>>> GetMessages(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == id && cm.UserId == userId && cm.LeftAt == null);

            if (!isMember)
            {
                return Forbid();
            }

            var messages = await _context.Messages
                .Where(m => m.ConversationId == id && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
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

            return Ok(messages.OrderBy(m => m.createdAt));
        }
    }

    // DTO cho raw SQL result
    public class ConversationQueryResult
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string Type { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int? LastMessageId { get; set; }
        public string? LastMessageContent { get; set; }
        public int? LastMessageSenderId { get; set; }
        public string? LastMessageSenderName { get; set; }
        public DateTime? LastMessageCreatedAt { get; set; }
        public bool IsPinned { get; set; }
        public bool IsMuted { get; set; }
        public int? LastSeenMessageId { get; set; }
        public DateTime SortDate { get; set; }
    }

    public class CreateConversationDto
    {
        public string? Name { get; set; }
        public string Type { get; set; } = "private";
        public string? Avatar { get; set; }
        public List<int> MemberIds { get; set; } = new();
    }
}