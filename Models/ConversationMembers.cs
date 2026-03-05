using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models
{
    [Table("ConversationMembers")]
    public class ConversationMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [StringLength(50)]
        public string Role { get; set; } = "member"; // admin, member, moderator

        public int? LastSeenMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }

        public bool IsMuted { get; set; } = false;
        public bool IsPinned { get; set; } = false;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LeftAt { get; set; }

        // Navigation properties
        [ForeignKey("ConversationId")]
        public Conversation Conversation { get; set; } = null!;

        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        [ForeignKey("LastSeenMessageId")]
        public Message? LastSeenMessage { get; set; }
    }
}