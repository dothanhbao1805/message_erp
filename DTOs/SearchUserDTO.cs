
using System;
using System.ComponentModel.DataAnnotations;

namespace messenger.DTOs;

public class SearchUserDTO
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public required string Email { get; set; }

}