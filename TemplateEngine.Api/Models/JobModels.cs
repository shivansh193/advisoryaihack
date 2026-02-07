namespace TemplateEngine.Api.Models;

public class ProcessingJob
{
    public string JobId { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public ProcessingMode Mode { get; set; } = ProcessingMode.Auto;
    public string? CustomJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? OutputPath { get; set; }
}

public enum JobStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class JobResponse
{
    public string JobId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DownloadUrl { get; set; }
}

public class BatchUploadResponse
{
    public List<JobResponse> Jobs { get; set; } = new();
    public int TotalFiles { get; set; }
    public string Message { get; set; } = string.Empty;
}
