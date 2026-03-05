using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "message"; // message, mention, system, friend_request

        [StringLength(255)]
        public string? Title { get; set; }

        [Column(TypeName = "text")]
        public string? Content { get; set; }

        [StringLength(500)]
        public string? Url { get; set; }

        [StringLength(50)]
        public string EntityType { get; set; } = "message"; // message, user, conversation

        public int? EntityId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
    }
}