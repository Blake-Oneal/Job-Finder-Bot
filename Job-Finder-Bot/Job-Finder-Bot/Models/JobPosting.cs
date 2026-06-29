namespace Job_Finder_Bot.Models;

public class JobPosting
{
    public int Id { get; set; }
    public required string JobTitle { get; set; }
    public required string Company { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public required string SourceUrl { get; set; }
    public string UniqueJobId { get; set; } = string.Empty;
    public DateTime PostedDate { get; set; }
    public DateTime DiscoveredDate { get; set; }
    public int Score { get; set; }
    public bool HasNotified { get; set; }
    public bool IsRelevant { get; set; }
    public string? Salary { get; set; }
}
