namespace EventManageApp.Models;

abstract public class Account
{
    public int Id { get; set;}
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public string? Nickname { get; set; }
}
