namespace EventManageApp.Models;

public class LoginViewModel
{
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
