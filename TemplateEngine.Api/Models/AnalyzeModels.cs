namespace TemplateEngine.Api.Models;

public class AnalyzeResponse
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public Dictionary<string, string> DetectedPlaceholders { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class ProcessRequest
{
    public string Mode { get; set; } = "manual"; // "manual" or "ai"
    public Dictionary<string, string>? Values { get; set; }
    public string? GeminiApiKey { get; set; }
}
