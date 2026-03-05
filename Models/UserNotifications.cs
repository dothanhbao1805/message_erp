using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models
{
    [Table("UserNotifications")]
    public class UserNotification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int NotificationId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public Users User { get; set; } = null!;

        [ForeignKey("NotificationId")]
        public Notification Notification { get; set; } = null!;
    }
}