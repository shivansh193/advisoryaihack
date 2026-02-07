using System.Collections.Concurrent;
using DocumentFormat.OpenXml.Packaging;
using TemplateEngine.Core;
using TemplateEngine.Core.Services;

namespace TemplateEngine.Api.Services;

public class DocumentAnalyzer
{
    private readonly ConcurrentDictionary<string, byte[]> _tempDocuments = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _tempPlaceholders = new();
    
    public async Task<(string JobId, Dictionary<string, string> Placeholders)> AnalyzeDocumentAsync(byte[] documentBytes)
    {
        var jobId = Guid.NewGuid().ToString();
        
        // Store document for later processing
        _tempDocuments[jobId] = documentBytes;
        
        // Detect placeholders
        var placeholders = await DetectPlaceholdersAsync(documentBytes);
        
        // Store placeholders too
        _tempPlaceholders[jobId] = placeholders;
        
        return (jobId, placeholders);
    }
    
    public byte[]? GetStoredDocument(string jobId)
    {
        _tempDocuments.TryGetValue(jobId, out var document);
        return document;
    }
    
    public Dictionary<string, string>? GetStoredPlaceholders(string jobId)
    {
        _tempPlaceholders.TryGetValue(jobId, out var placeholders);
        return placeholders;
    }
    
    public void RemoveStoredDocument(string jobId)
    {
        _tempDocuments.TryRemove(jobId, out _);
        _tempPlaceholders.TryRemove(jobId, out _);
    }
    
    private async Task<Dictionary<string, string>> DetectPlaceholdersAsync(byte[] documentBytes)
    {
        var placeholders = new Dictionary<string, string>();
        
        using var stream = new MemoryStream(documentBytes);
        using var ms = new MemoryStream();
        
        using (var doc = WordprocessingDocument.Open(stream, false))
        {
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return placeholders;
            
            // Parse document
            DocumentIngestor.ParseDocument(doc);
            
            // Generate schema and analyze
            var jsonSchema = SchemaGenerator.GenerateSchemaJson(body);
            var orchestrator = new AiOrchestrator();
            var mapping = await orchestrator.AnalyzeStructureAsync(jsonSchema);
            
            // Tag document
            var tagger = new TaggingService();
            tagger.TagDocumentRefined(body, mapping);
            
            // Detect highlights and assign labels
            var highlightMapping = tagger.DetectHighlights(body);
            
            int counter = 1;
            foreach (var kvp in highlightMapping)
            {
                var label = $"AI_GEN_CONTENT_{counter}";
                var originalText = kvp.Value;
                placeholders[label] = $"[AI Generated Content for: {originalText}]";
                counter++;
            }
        }
        
        return placeholders;
    }
}
