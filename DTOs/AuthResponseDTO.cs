namespace messenger.DTOs
{
    public class AuthResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UsersDTO? User { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}