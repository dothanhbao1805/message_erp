using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "text"; // text, image, file, video, audio

        [Column(TypeName = "text")]
        public string? Content { get; set; }

        [StringLength(1000)]
        public string? FileUrl { get; set; }

        [StringLength(255)]
        public string? FileName { get; set; }

        public long? FileSize { get; set; }

        // Reply/Thread support
        public int? ParentMessageId { get; set; }
        public int? ReplyToMessageId { get; set; }

        // Message status tracking
        [StringLength(20)]
        public string Status { get; set; } = "sent"; // sent, delivered, read

        public DateTime? DeliveredAt { get; set; }

        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Navigation properties
        [ForeignKey("ConversationId")]
        public Conversation Conversation { get; set; } = null!;

        [ForeignKey("SenderId")]
        public Users Sender { get; set; } = null!;

        [ForeignKey("ParentMessageId")]
        public Message? ParentMessage { get; set; }

        [ForeignKey("ReplyToMessageId")]
        public Message? ReplyToMessage { get; set; }

        public ICollection<Message> Replies { get; set; } = new List<Message>();
        public ICollection<MessageRead> MessageReads { get; set; } = new List<MessageRead>();
    }
}