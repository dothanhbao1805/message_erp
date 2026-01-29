using System;
using System.ComponentModel.DataAnnotations;

namespace messenger.Models;

public class Users
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    // Auth provider (local, google, facebook...)
    public string Provider { get; set; } = "local";
    public string? ProviderId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;
    public DateTime? Email_Verified_At { get; set; }

    [Required]
    public string Password_Hash { get; set; } = null!;

    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }

    public string Role { get; set; } = "User";
    public string Status { get; set; } = "Active";

    public bool IsVerified { get; set; } = false;

    public string? RememberToken { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; }
}