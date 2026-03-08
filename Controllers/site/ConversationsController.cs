using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using messenger.DTOs;
using messenger.Services.Interfaces;
using System.Security.Claims;

namespace messenger.Controllers.site
{
    [Authorize]
    [ApiController]
    [Route("api/conversations")]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _service;

        public ConversationsController(IConversationService service)
        {
            _service = service;
        }

        // Lấy userId từ JWT token — dùng lại nhiều nơi nên tách ra helper
        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // GET: api/conversations
        [HttpGet]
        public async Task<ActionResult> GetConversations()
        {
            var result = await _service.GetConversations(GetUserId());
            return Ok(result);
        }

        // POST: api/conversations
        [HttpPost]
        public async Task<ActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var (success, data, error, isNew) = await _service.CreateConversation(GetUserId(), dto);

            if (!success)
                return BadRequest(new { error });

            // isNew = false: conversation đã tồn tại → 200 OK
            // isNew = true:  conversation mới tạo   → 201 Created
            if (!isNew)
                return Ok(data);

            return CreatedAtAction(nameof(GetConversation), new { id = ((dynamic)data!).id }, data);
        }

        // GET: api/conversations/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetConversation(int id)
        {
            var result = await _service.GetConversationById(id);

            if (result == null)
                return NotFound(new { error = "Conversation not found" });

            return Ok(result);
        }

        // GET: api/conversations/{id}/messages
        [HttpGet("{id}/messages")]
        public async Task<ActionResult> GetMessages(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var (isMember, messages) = await _service.GetMessages(id, GetUserId(), page, pageSize);

            if (!isMember)
                return Forbid();

            return Ok(messages);
        }
    }

    // DTO cho raw SQL result — giữ ở đây vì chỉ dùng nội bộ trong data layer
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
}