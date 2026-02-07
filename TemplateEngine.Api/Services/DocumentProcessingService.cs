using System.Collections.Concurrent;
using TemplateEngine.Api.Models;
using TemplateEngine.Core;

namespace TemplateEngine.Api.Services;

public class DocumentProcessingService
{
    private readonly ConcurrentDictionary<string, ProcessingJob> _jobs = new();
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

    public async Task<ProcessingJob> ProcessDocumentAsync(IFormFile file)
    {
        var job = new ProcessingJob
        {
            FileName = file.FileName,
            Status = JobStatus.Pending
        };

        _jobs[job.JobId] = job;

        // Process in background
        _ = Task.Run(async () => await ProcessJobAsync(job, file));

        return job;
    }

    public async Task<List<ProcessingJob>> ProcessBatchAsync(IFormFileCollection files)
    {
        var jobs = new List<ProcessingJob>();

        foreach (var file in files)
        {
            var job = await ProcessDocumentAsync(file);
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

    private async Task ProcessJobAsync(ProcessingJob job, IFormFile file)
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

            // Process the document
            var outputBytes = await _processor.ProcessDocumentAsync(inputBytes);

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
