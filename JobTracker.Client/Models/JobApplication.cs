namespace FrontendWasm.Models;

public class JobApplication
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string Status { get; set; } = "Applied";
    public DateTime DateApplied { get; set; } = DateTime.Now;
    public string? Location { get; set; }
}