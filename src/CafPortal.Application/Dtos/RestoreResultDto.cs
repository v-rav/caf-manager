namespace CafPortal.Application.Dtos;

/// <summary>Outcome of a database restore, including post-restore row counts for a sanity check.</summary>
public class RestoreResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long RestoredBytes { get; set; }
    public DateTimeOffset RestoredUtc { get; set; }
    public int Nominations { get; set; }
    public int Resources { get; set; }
    public int Accounts { get; set; }
}
