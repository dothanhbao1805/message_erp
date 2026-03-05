using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models
{
    [Table("Conversations")]
    public class Conversation
    {
        [Key]
        public int Id { get; set; }

        [StringLength(255)]
        public string? Name { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "private"; // private, group, department, team

        [StringLength(500)]
        public string? Avatar { get; set; }

        // Thêm để tối ưu hiển thị
        public int? LastMessageId { get; set; }
        public DateTime? LastMessageAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        [ForeignKey("LastMessageId")]
        public Message? LastMessage { get; set; }

        public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}