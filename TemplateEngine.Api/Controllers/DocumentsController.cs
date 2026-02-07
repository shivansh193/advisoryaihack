using Microsoft.AspNetCore.Mvc;
using TemplateEngine.Api.Models;
using TemplateEngine.Api.Services;

namespace TemplateEngine.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentProcessingService _processingService;
    private readonly DocumentAnalyzer _analyzer;
    private readonly GeminiService _geminiService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        DocumentProcessingService processingService,
        DocumentAnalyzer analyzer,
        GeminiService geminiService,
        ILogger<DocumentsController> logger)
    {
        _processingService = processingService;
        _analyzer = analyzer;
        _geminiService = geminiService;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AnalyzeResponse>> AnalyzeDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .docx files are supported" });

        try
        {
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var (jobId, placeholders) = await _analyzer.AnalyzeDocumentAsync(fileBytes);

            return Ok(new AnalyzeResponse
            {
                JobId = jobId,
                FileName = file.FileName,
                DetectedPlaceholders = placeholders,
                Message = $"Detected {placeholders.Count} placeholder(s)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing document");
            return StatusCode(500, new { error = "Failed to analyze document" });
        }
    }

    [HttpPost("process/{jobId}")]
    public async Task<ActionResult> ProcessDocument(string jobId, [FromBody] ProcessRequest request)
    {
        try
        {
            var documentBytes = _analyzer.GetStoredDocument(jobId);
            if (documentBytes == null)
                return NotFound(new { error = "Document not found or expired" });

            Dictionary<string, string> values;

            if (request.Mode == "ai")
            {
                if (string.IsNullOrEmpty(request.GeminiApiKey))
                    return BadRequest(new { error = "Gemini API key required for AI mode" });

                // Get the detected placeholders from analyze step
                var detectedPlaceholders = _analyzer.GetStoredPlaceholders(jobId);
                if (detectedPlaceholders == null)
                    return BadRequest(new { error = "Placeholder data not found. Please re-analyze the document." });

                values = await _geminiService.GenerateValuesFromDocumentAsync(documentBytes, request.GeminiApiKey, detectedPlaceholders);
            }
            else
            {
                values = request.Values ?? new Dictionary<string, string>();
            }

            // Process document with values
            var processor = new TemplateEngine.Core.DocumentProcessor();
            var processedBytes = await processor.ProcessDocumentWithValuesAsync(documentBytes, values);

            // Clean up temp storage
            _analyzer.RemoveStoredDocument(jobId);

            // Return processed file
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"Processed_{timestamp}.docx";
            
            return File(processedBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing document");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("upload")]
    public async Task<ActionResult<JobResponse>> UploadDocument(
        IFormFile file,
        [FromForm] string? processingMode = null,
        [FromForm] string? customJson = null,
        [FromForm] string? geminiApiKey = null)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded" });

        if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .docx files are supported" });

        // Parse processing mode
        var mode = ProcessingMode.Auto;
        if (!string.IsNullOrEmpty(processingMode))
        {
            if (!Enum.TryParse<ProcessingMode>(processingMode, true, out mode))
            {
                return BadRequest(new { error = "Invalid processing mode" });
            }
        }

        try
        {
            var job = await _processingService.ProcessDocumentAsync(file, mode, customJson, geminiApiKey);
            return Ok(MapToResponse(job));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document");
            return StatusCode(500, new { error = "Failed to process document" });
        }
    }

    [HttpPost("batch")]
    public async Task<ActionResult<BatchUploadResponse>> UploadBatch(
        IFormFileCollection files,
        [FromForm] string? processingMode = null,
        [FromForm] string? customJson = null,
        [FromForm] string? geminiApiKey = null)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = "No files uploaded" });

        var invalidFiles = files.Where(f => !f.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)).ToList();
        if (invalidFiles.Any())
            return BadRequest(new { error = $"Invalid files: {string.Join(", ", invalidFiles.Select(f => f.FileName))}" });

        // Parse processing mode
        var mode = ProcessingMode.Auto;
        if (!string.IsNullOrEmpty(processingMode))
        {
            if (!Enum.TryParse<ProcessingMode>(processingMode, true, out mode))
            {
                return BadRequest(new { error = "Invalid processing mode" });
            }
        }

        try
        {
            var jobs = await _processingService.ProcessBatchAsync(files, mode, customJson, geminiApiKey);
            
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
