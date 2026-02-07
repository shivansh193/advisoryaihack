using System.Collections.Concurrent;
using TemplateEngine.Api.Models;
using TemplateEngine.Core;

namespace TemplateEngine.Api.Services;

public class DocumentProcessingService
{
    private readonly ConcurrentDictionary<string, ProcessingJob> _jobs = new();
    private readonly GeminiService _geminiService = new();
    private readonly DocumentProcessor _processor = new();
    private readonly string _outputDirectory;

    public DocumentProcessingService(IConfiguration configuration)
    {
        _outputDirectory = configuration["OutputDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Output");
        
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public async Task<ProcessingJob> ProcessDocumentAsync(
        IFormFile file,
        ProcessingMode mode = ProcessingMode.Auto,
        string? customJson = null,
        string? geminiApiKey = null)
    {
        var job = new ProcessingJob
        {
            FileName = file.FileName,
            Status = JobStatus.Pending,
            Mode = mode,
            CustomJson = customJson
        };

        _jobs[job.JobId] = job;

        // Process in background
        _ = Task.Run(async () => await ProcessJobAsync(job, file, geminiApiKey));

        return job;
    }

    public async Task<List<ProcessingJob>> ProcessBatchAsync(
        IFormFileCollection files,
        ProcessingMode mode = ProcessingMode.Auto,
        string? customJson = null,
        string? geminiApiKey = null)
    {
        var jobs = new List<ProcessingJob>();

        foreach (var file in files)
        {
            var job = await ProcessDocumentAsync(file, mode, customJson, geminiApiKey);
            jobs.Add(job);
        }

        return jobs;
    }

    public ProcessingJob? GetJobStatus(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public byte[]? GetProcessedDocument(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var job) || job.OutputPath == null)
            return null;

        if (!File.Exists(job.OutputPath))
            return null;

        return File.ReadAllBytes(job.OutputPath);
    }

    private async Task ProcessJobAsync(ProcessingJob job, IFormFile file, string? geminiApiKey)
    {
        try
        {
            job.Status = JobStatus.Processing;

            byte[] inputBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                inputBytes = ms.ToArray();
            }

            byte[] outputBytes;

            // Route based on processing mode
            switch (job.Mode)
            {
                case ProcessingMode.Auto:
                    // Current automatic processing
                    outputBytes = await _processor.ProcessDocumentAsync(inputBytes);
                    break;

                case ProcessingMode.Manual:
                    // User-provided JSON values
                    if (string.IsNullOrEmpty(job.CustomJson))
                    {
                        throw new Exception("Custom JSON is required for Manual mode");
                    }
                    var customValues = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(job.CustomJson);
                    outputBytes = await _processor.ProcessDocumentWithValuesAsync(inputBytes, customValues ?? new());
                    break;

                case ProcessingMode.AIGenerated:
                    // Gemini AI generates values
                    if (string.IsNullOrEmpty(geminiApiKey))
                    {
                        throw new Exception("Gemini API key is required for AIGenerated mode");
                    }
                    // This endpoint is deprecated for AI mode - use /analyze and /process instead
                    throw new Exception("AIGenerated mode requires using /analyze and /process endpoints");
                    break;

                default:
                    throw new Exception($"Unknown processing mode: {job.Mode}");
            }

            // Save to output directory
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var outputFileName = $"{Path.GetFileNameWithoutExtension(file.FileName)}_Processed_{timestamp}.docx";
            var outputPath = Path.Combine(_outputDirectory, outputFileName);

            await File.WriteAllBytesAsync(outputPath, outputBytes);

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            job.OutputPath = outputPath;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }
    }
}
