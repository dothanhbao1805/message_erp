using System;
using System.ComponentModel.DataAnnotations;

namespace messenger.DTOs;

public class CreateConversationDto
{
    public string? Name { get; set; }
    public string Type { get; set; } = "private";
    public string? Avatar { get; set; }
    public List<int> MemberIds { get; set; } = new();
}