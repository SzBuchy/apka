namespace EventManageApp.Models;

public class Task
{
    public int Id { get; set;}
    public string? Name { get; set;}
    public string? Description { get; set;}
    public decimal Points { get; set;} = 0;
    public bool RequiresPhoto { get; set; } = false;
}
