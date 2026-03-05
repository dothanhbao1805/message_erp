using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace messenger.Models;

[Table("Users")]
public class Users
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    // Auth provider (local, google, facebook...)
    [StringLength(50)]
    public string Provider { get; set; } = "local";

    [StringLength(255)]
    public string? ProviderId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    public DateTime? Email_Verified_At { get; set; }

    [Required]
    [StringLength(500)]
    public string Password_Hash { get; set; } = null!;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    [StringLength(500)]
    public string? Avatar { get; set; }

    [StringLength(20)]
    public string Role { get; set; } = "User";

    [StringLength(20)]
    public string Status { get; set; } = "Active"; // Active, Inactive, Banned

    public bool IsVerified { get; set; } = false;

    [StringLength(500)]
    public string? RememberToken { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ConversationMember> ConversationMembers { get; set; } = new List<ConversationMember>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}