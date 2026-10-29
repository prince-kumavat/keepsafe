using System.ComponentModel.DataAnnotations;

namespace KeepSafe.Models;

public class SignupViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class AuthPageViewModel
{
    public SignupViewModel Signup { get; set; } = new();
    public LoginViewModel Login { get; set; } = new();
    public string ActiveTab { get; set; } = "login"; // "login" or "signup"
    public string? ErrorMessage { get; set; }
}
