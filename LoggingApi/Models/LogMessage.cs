namespace LoggingApi.Models;

public class LogMessage
{
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string>? Properties { get; set; }
}
