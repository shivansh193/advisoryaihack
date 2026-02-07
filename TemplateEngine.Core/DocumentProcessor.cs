using DocumentFormat.OpenXml.Wordprocessing;
using TemplateEngine.Core.Services;

namespace TemplateEngine.Core;

/// <summary>
/// Core document processing service that can be used as a library
/// </summary>
public class DocumentProcessor
{
    public async Task<byte[]> ProcessDocumentAsync(byte[] inputDocumentBytes)
    {
        MemoryStream ms;
        
        try 
        {
            // Load document from byte array
            var tempStream = new MemoryStream(inputDocumentBytes);
            
            using (var doc = DocumentIngestor.SafeOpenFromStream(tempStream, out ms))
            {
                // Phase 1: Parse and Normalize
                DocumentIngestor.ParseDocument(doc);
                
                // Phase 2: The Brain (Schema Gen + Tagging)
                var jsonSchema = SchemaGenerator.GenerateSchemaJson(doc.MainDocumentPart.Document.Body);
                
                var orchestrator = new AiOrchestrator();
                var mapping = await orchestrator.AnalyzeStructureAsync(jsonSchema);
                
                var tagger = new TaggingService();
                // 1. Structural Tagging ( [Placeholders] )
                tagger.TagDocumentRefined(doc.MainDocumentPart.Document.Body, mapping);
                
                // 2. Highlight Tagging ( Yellow Text )
                var highlightMapping = tagger.DetectHighlights(doc.MainDocumentPart.Document.Body);
                
                // Phase 3: The Surgeon (Injection)
                var injector = new ContentInjector();
                
                // 1. Text Injection
                var values = new Dictionary<string, string>
                {
                    { "ClientName", "Acme Corp (Verified Style)" },
                    { "PolicyType", "General Liability" } // Fallback for single insertion
                };
                
                // 3. AI Content Generation for Highlights
                
                // Separate into Table-Bound vs Inline
                var sdtRuns = doc.MainDocumentPart.Document.Body.Descendants<DocumentFormat.OpenXml.Wordprocessing.SdtRun>().ToList();
                
                var tableGroups = new Dictionary<DocumentFormat.OpenXml.Wordprocessing.Table, List<string>>();
                var inlineTags = new List<string>();
                var processedTags = new HashSet<string>();

                foreach (var sdt in sdtRuns)
                {
                    var tagVal = sdt.SdtProperties?.GetFirstChild<DocumentFormat.OpenXml.Wordprocessing.Tag>()?.Val?.Value;
                    if (tagVal != null && highlightMapping.ContainsKey(tagVal))
                    {
                        if (processedTags.Contains(tagVal)) continue; // Avoid dupes if any
                        processedTags.Add(tagVal);

                        var parentTable = sdt.Ancestors<DocumentFormat.OpenXml.Wordprocessing.Table>().FirstOrDefault();
                        if (parentTable != null)
                        {
                            if (!tableGroups.ContainsKey(parentTable)) 
                                tableGroups[parentTable] = new List<string>();
                            
                            tableGroups[parentTable].Add(tagVal);
                        }
                        else
                        {
                            inlineTags.Add(tagVal);
                        }
                    }
                }
                
                // A. Process Table Groups (Context-Aware)
                var tableContextService = new TableContextService();
                
                foreach (var group in tableGroups)
                {
                    var table = group.Key;
                    var tagsInTable = group.Value;
                    
                    // Serialize Table (Context)
                    string markdownTable = tableContextService.SerializeTableToMarkdown(table, highlightMapping);
                    
                    // AI Generation Batch
                    var tableResults = orchestrator.GenerateTableData(markdownTable, tagsInTable);
                    
                    foreach(var kvp in tableResults)
                    {
                        values[kvp.Key] = kvp.Value;
                    }
                }
                
                // B. Process Inline (Simple Prompt)
                foreach (var tag in inlineTags)
                {
                    string originalText = highlightMapping[tag];
                    string generatedContent = orchestrator.GenerateContent(originalText);
                    values[tag] = generatedContent;
                }
                
                injector.InjectValues(doc.MainDocumentPart.Document.Body, values);
                
                // 2. Table Injection
                var tableData = new List<Dictionary<string, string>>
                {
                    new() { { "PolicyType", "General Liability" }, { "PremiumAmount", "$1,200.00" } },
                    new() { { "PolicyType", "Workers Comp" }, { "PremiumAmount", "$5,000.00" } },
                    new() { { "PolicyType", "Cyber Security" }, { "PremiumAmount", "$850.00" } }
                };
                injector.InjectTableData(doc.MainDocumentPart.Document.Body, tableData);
                
                // Phase 4: Quality Control
                var validator = new ValidatorService();
                validator.ValidateDocument(doc);
        
                doc.Save();
            }
        }
        catch (Exception ex)
        {
            throw new DocumentProcessingException($"Error processing document: {ex.Message}", ex);
        }
        
        return ms.ToArray();
    }
}

public class DocumentProcessingException : Exception
{
    public DocumentProcessingException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
