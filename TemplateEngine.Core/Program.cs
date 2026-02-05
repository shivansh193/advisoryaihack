using TemplateEngine.Core.Services;

// Create services folder if it doesn't exist (handled by write_to_file)
Console.WriteLine("Template Intelligence Engine - Batch Processing Mode");

// 1. Setup Input arguments
// Usage: dotnet run [path/to/file_or_dir] [reset]
// Defaults: Current Directory if no path argument, or "test_template.docx" if created by reset.

bool resetMode = args.Contains("reset");
string inputArg = args.FirstOrDefault(a => a != "reset") ?? "."; 

// Special Case: Reset Mode generates a dummy file
if (resetMode)
{
    string dummyFile = "test_template.docx";
    Console.WriteLine($"Reset Mode: Creating dummy file '{dummyFile}'...");
    CreateDummyFile(dummyFile);
    if (inputArg == ".") inputArg = dummyFile; // If no specific input arg given, assume we want to process the dummy
}

// 2. Identify Files to Process
List<string> filesToProcess = new List<string>();

if (File.Exists(inputArg))
{
    filesToProcess.Add(inputArg);
}
else if (Directory.Exists(inputArg))
{
    // Scan for all .docx files
    Console.WriteLine($"Scanning directory '{inputArg}' for .docx files...");
    filesToProcess.AddRange(Directory.GetFiles(inputArg, "*.docx", SearchOption.TopDirectoryOnly)
                                     .Where(f => !Path.GetFileName(f).StartsWith("~$")) // Ignore temp files
                                     .Where(f => !f.Contains("_Processed_"))); // Ignore previous outputs if in same dir
}
else
{
    Console.WriteLine($"Error: Input path '{inputArg}' not found.");
    return;
}

if (!filesToProcess.Any())
{
    Console.WriteLine("No documents found to process.");
    return;
}

// 3. Setup Output Directory
string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "Output");
if (!Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
    Console.WriteLine($"Created output directory: {outputDir}");
}

Console.WriteLine($"Found {filesToProcess.Count} file(s) to process. Saving to '{outputDir}'\n");

// 4. Process Loop
foreach (var filePath in filesToProcess)
{
    await ProcessFileAsync(filePath, outputDir);
}

Console.WriteLine("\n--- Batch Processing Complete ---");


static async Task ProcessFileAsync(string filePath, string outputDir)
{
    string fileName = Path.GetFileName(filePath);
    Console.WriteLine($"\n>>> Processing: {fileName} <<<");

    MemoryStream ms;
    try 
    {
        using (var doc = DocumentIngestor.SafeOpen(filePath, out ms))
        {
            // Note: SafeOpen calls NormalizeDocument logic
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
                Console.WriteLine($"\n--- Batch AI: Table Context ({tagsInTable.Count} items) ---");
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
    catch (DocumentFormat.OpenXml.Packaging.OpenXmlPackageException ex)
    {
         Console.WriteLine($"FATAL: OpenXML Corruption in {fileName}: {ex.Message}");
         return;
    }
    catch (Exception ex)
    {
         Console.WriteLine($"FATAL: General Error in {fileName}: {ex.Message}");
         return;
    }
    
    // Save logic
    // Format: {OriginalName}_Processed_{Timestamp}.docx
    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    string outputName = $"{Path.GetFileNameWithoutExtension(filePath)}_Processed_{timestamp}.docx";
    string outputPath = Path.Combine(outputDir, outputName);
    
    try
    {
        File.WriteAllBytes(outputPath, ms.ToArray()); 
        Console.WriteLine($"SUCCESS: Saved to {outputName}");
    }
    catch (Exception ex)
    {
         Console.WriteLine($"Error saving {outputName}: {ex.Message}");
    }
    ms.Dispose();
}


static void CreateDummyFile(string path)
{
    using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
    {
        var mainPart = doc.AddMainDocumentPart();
        mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
        var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());
        
        // Add Title with Fragmented Runs
        var p = new DocumentFormat.OpenXml.Wordprocessing.Paragraph();
        
        var rPr = new DocumentFormat.OpenXml.Wordprocessing.RunProperties(
            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "FF0000" }, // Red
            new DocumentFormat.OpenXml.Wordprocessing.Bold()
        );

        // Run 1
        var r1 = new DocumentFormat.OpenXml.Wordprocessing.Run();
        r1.RunProperties = (DocumentFormat.OpenXml.Wordprocessing.RunProperties)rPr.CloneNode(true);
        r1.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("Annual Review for "));
        
        // Run 2 (Part of placeholder)
        var r2 = new DocumentFormat.OpenXml.Wordprocessing.Run();
        r2.RunProperties = (DocumentFormat.OpenXml.Wordprocessing.RunProperties)rPr.CloneNode(true);
        r2.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("[CLIENT"));
        
        // Run 3 (Rest of placeholder)
        var r3 = new DocumentFormat.OpenXml.Wordprocessing.Run();
        r3.RunProperties = (DocumentFormat.OpenXml.Wordprocessing.RunProperties)rPr.CloneNode(true);
        r3.Append(new DocumentFormat.OpenXml.Wordprocessing.Text("_NAME]"));
        
        p.Append(r1, r2, r3);
        body.AppendChild(p);

        // Add a table
        var table = new DocumentFormat.OpenXml.Wordprocessing.Table();
        
        // Header Row
        var tr1 = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        var tc1 = new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Policy Type"))));
        var tc2 = new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("Premium"))));
        tr1.Append(tc1, tc2);
        table.Append(tr1);

        // Data Row (Template)
        var tr2 = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
        var tc3 = new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[POLICY_TYPE]"))));
        var tc4 = new DocumentFormat.OpenXml.Wordprocessing.TableCell(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("[PREMIUM_AMOUNT]"))));
        tr2.Append(tc3, tc4);
        table.Append(tr2);

        body.Append(table);
    }
}
