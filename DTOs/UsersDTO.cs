
namespace messenger.DTOs;

public class UsersDTO
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Avatar { get; set; }
    public string Role { get; set; } = "User";


}