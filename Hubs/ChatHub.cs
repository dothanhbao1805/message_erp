using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using messenger.Data;
using messenger.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace messenger.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly MessengerContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(MessengerContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                _logger.LogInformation("========== OnConnectedAsync START ==========");
                _logger.LogInformation($"User IsAuthenticated: {Context.User?.Identity?.IsAuthenticated}");
                _logger.LogInformation($"User Identity Name: {Context.User?.Identity?.Name}");

                // Log TẤT CẢ claims
                if (Context.User?.Claims != null)
                {
                    foreach (var claim in Context.User.Claims)
                    {
                        _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                    }
                }
                else
                {
                    _logger.LogWarning("No claims found!");
                }

                var userId = GetUserId();
                _logger.LogInformation($"✅ User {userId} connected with ConnectionId: {Context.ConnectionId}");

                var conversationIds = await _context.ConversationMembers
                    .Where(cm => cm.UserId == userId && cm.LeftAt == null)
                    .Select(cm => cm.ConversationId)
                    .ToListAsync();

                _logger.LogInformation($"User {userId} is member of {conversationIds.Count} conversations");

                foreach (var convId in conversationIds)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{convId}");
                    _logger.LogInformation($"Added to group: conversation_{convId}");
                }

                await base.OnConnectedAsync();
                _logger.LogInformation("========== OnConnectedAsync END ==========");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR in OnConnectedAsync: {Message}", ex.Message);
                throw;
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetUserId();
                _logger.LogInformation($"User {userId} disconnected");

                if (exception != null)
                {
                    _logger.LogError(exception, "Disconnected with error");
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync");
            }
        }

        public async Task SendMessage(int conversationId, string content, string type = "text")
        {
            try
            {
                var userId = GetUserId();
                _logger.LogInformation($"SendMessage called by user {userId} to conversation {conversationId}");

                var isMember = await _context.ConversationMembers
                    .AnyAsync(cm => cm.ConversationId == conversationId
                        && cm.UserId == userId
                        && cm.LeftAt == null);

                if (!isMember)
                {
                    _logger.LogWarning($"User {userId} is not a member of conversation {conversationId}");
                    throw new HubException("You are not a member of this conversation");
                }

                var message = new Message
                {
                    ConversationId = conversationId,
                    SenderId = userId,
                    Content = content,
                    Type = type,
                    Status = "sent",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);

                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation != null)
                {
                    conversation.LastMessageAt = DateTime.UtcNow;
                    conversation.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                await _context.Entry(message)
                    .Reference(m => m.Sender)
                    .LoadAsync();

                if (conversation != null)
                {
                    conversation.LastMessageId = message.Id;
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation($"✅ Message {message.Id} saved and broadcasting to conversation_{conversationId}");

                await Clients.Group($"conversation_{conversationId}")
                    .SendAsync("ReceiveMessage", new
                    {
                        id = message.Id,
                        conversationId = message.ConversationId,
                        senderId = message.SenderId,
                        senderName = message.Sender.FullName,
                        senderAvatar = message.Sender.Avatar,
                        content = message.Content,
                        type = message.Type,
                        createdAt = message.CreatedAt
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in SendMessage");
                throw;
            }
        }

        public async Task StartTyping(int conversationId)
        {
            var userId = GetUserId();
            var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value
                        ?? Context.User?.FindFirst("name")?.Value
                        ?? "Unknown";

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserTyping", new { userId, userName, conversationId });
        }

        public async Task StopTyping(int conversationId)
        {
            var userId = GetUserId();

            await Clients.OthersInGroup($"conversation_{conversationId}")
                .SendAsync("UserStoppedTyping", new { userId, conversationId });
        }

        public async Task MarkAsRead(int messageId)
        {
            var userId = GetUserId();

            var message = await _context.Messages
                .Include(m => m.MessageReads)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                throw new HubException("Message not found");
            }

            var existingRead = await _context.MessageReads
                .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId);

            if (existingRead == null)
            {
                var messageRead = new MessageRead
                {
                    MessageId = messageId,
                    UserId = userId,
                    ReadAt = DateTime.UtcNow
                };

                _context.MessageReads.Add(messageRead);
                await _context.SaveChangesAsync();

                await Clients.User(message.SenderId.ToString())
                    .SendAsync("MessageRead", new
                    {
                        messageId,
                        userId,
                        readAt = messageRead.ReadAt
                    });
            }
        }

        public async Task JoinConversation(int conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation($"User {GetUserId()} joined conversation {conversationId}");
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
            _logger.LogInformation($"User {GetUserId()} left conversation {conversationId}");
        }

        private int GetUserId()
        {
            _logger.LogInformation("GetUserId() called");

            // Thử tất cả các claim type phổ biến
            var userId = Context.User?.FindFirst("sub")?.Value
                      ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("nameid")?.Value
                      ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("❌ Cannot find userId claim!");

                // Log tất cả claims có sẵn
                if (Context.User?.Claims != null)
                {
                    var claims = Context.User.Claims.Select(c => $"{c.Type}={c.Value}");
                    _logger.LogError($"Available claims: {string.Join(", ", claims)}");
                }

                throw new HubException("Unauthorized - No user ID claim found");
            }

            _logger.LogInformation($"✅ UserId found: {userId}");
            return int.Parse(userId);
        }
    }
}