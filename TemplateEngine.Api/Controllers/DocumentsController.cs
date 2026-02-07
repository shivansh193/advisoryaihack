using Microsoft.AspNetCore.Mvc;
using TemplateEngine.Api.Models;
using TemplateEngine.Api.Services;

namespace TemplateEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentProcessingService _processingService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(DocumentProcessingService processingService, ILogger<DocumentsController> logger)
    {
        _processingService = processingService;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult<JobResponse>> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .docx files are supported" });

        try
        {
            var job = await _processingService.ProcessDocumentAsync(file);
            return Ok(MapToResponse(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "Failed to process document" });
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchUploadResponse>> UploadBatch(IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No files uploaded" });

        var invalidFiles = files.Where(f => !f.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)).ToList();
        if (invalidFiles.Any())
            return BadRequest(new { error = $"Invalid files: {string.Join(", ", invalidFiles.Select(f => f.FileName))}" });

        try
        {
            var jobs = await _processingService.ProcessBatchAsync(files);
            
            return Ok(new BatchUploadResponse
            {
                Jobs = jobs.Select(MapToResponse).ToList(),
                TotalFiles = jobs.Count,
                Message = $"Successfully queued {jobs.Count} documents for processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading batch");
            return StatusCode(500, new { error = "Failed to process batch upload" });
        }
    }

    [HttpGet("jobs/{jobId}")]
    public ActionResult<JobResponse> GetJobStatus(string jobId)
    {
        var job = _processingService.GetJobStatus(jobId);
        
        if (job == null)
            return NotFound(new { error = "Job not found" });

        return Ok(MapToResponse(job));
    }

    [HttpGet("jobs/{jobId}/download")]
    public ActionResult DownloadDocument(string jobId)
    {
        var job = _processingService.GetJobStatus(jobId);
        
        if (job == null)
            return NotFound(new { error = "Job not found" });

        if (job.Status != JobStatus.Completed)
            return BadRequest(new { error = "Document processing not completed yet" });

        var fileBytes = _processingService.GetProcessedDocument(jobId);
        
        if (fileBytes == null)
            return NotFound(new { error = "Processed document not found" });

        var fileName = Path.GetFileName(job.OutputPath ?? "processed_document.docx");
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
    }

    private JobResponse MapToResponse(ProcessingJob job)
    {
        return new JobResponse
        {
            JobId = job.JobId,
            FileName = job.FileName,
            Status = job.Status.ToString(),
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            ErrorMessage = job.ErrorMessage,
            DownloadUrl = job.Status == JobStatus.Completed ? $"/api/documents/jobs/{job.JobId}/download" : null
        };
    }
}
