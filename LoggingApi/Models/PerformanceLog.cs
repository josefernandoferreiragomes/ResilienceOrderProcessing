namespace LoggingApi.Models;

public class PerformanceLog
{
    public string Operation { get; set; } = string.Empty;
    public long Duration { get; set; }
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}