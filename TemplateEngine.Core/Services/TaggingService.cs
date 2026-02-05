using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace TemplateEngine.Core.Services;

public class TaggingService
{
    public void TagDocument(DocumentFormat.OpenXml.Wordprocessing.Body body, Dictionary<string, string> mapping)
    {
        Console.WriteLine("TaggingService: Wrapping identified elements in Content Controls...");
        
        // Find all text elements
        // Note: Modifying the tree while iterating requires care. 
        // We'll collect the candidates first.
        var textNodes = body.Descendants<Text>().ToList();

        foreach (var textNode in textNodes)
        {
            string text = textNode.Text;
            
            // Check if this text node contains any of our keys
            // This is a naive substring check. Real implementation would be smarter about partial runs.
            foreach (var kvp in mapping)
            {
                if (text.Contains(kvp.Key))
                {
                    // WRAP IT!
                    // Parent is usually a Run.
                    var run = textNode.Parent as Run;
                    if (run != null)
                    {
                        // Create Content Control (SdtRun for inline)
                        // Note: SdtBlock is for Paragraphs, SdtRun is for inside Paragraphs.
                        
                        SdtRun sdtRun = new SdtRun();
                        
                        // Properties
                        SdtProperties sdtPr = new SdtProperties();
                        Tag tag = new Tag { Val = kvp.Value }; // The Variable Name
                        SdtAlias alias = new SdtAlias { Val = kvp.Value }; // Re-use for display name
                        sdtPr.Append(tag);
                        sdtPr.Append(alias);
                        sdtRun.Append(sdtPr);

                        // Content
                        SdtContentRun sdtContent = new SdtContentRun();
                        
                        // We need to move the Run INTO the SdtContentRun.
                        // But we can't just move 'run' directly because it's attached to the paragraph.
                        // So we remove 'run' from parent, and append it to sdtContent.
                        
                        // Wait, 'run' is the parent of 'textNode'.
                        // We want to wrap the *run* or just the *text*?
                        // Wrapping the Run preserves formatting (Red Bold Text remains Red Bold).
                        
                        // If we replace: Paragraph -> Run -> Text
                        // With: Paragraph -> SdtRun -> SdtContent -> Run -> Text
                        
                        var parentParagraph = run.Parent;
                        if (parentParagraph != null)
                        {
                            run.Remove();
                            sdtContent.Append(run);
                            sdtRun.Append(sdtContent);
                            parentParagraph.Append(sdtRun); 
                            // Note: Append appends to end. We want to insert where it was.
                            // But since we are iterating, simpler matching is hard.
                            // Ideally: parentParagraph.InsertAfter(sdtRun, referenceNode)
                            // But since we removed 'run', we lost the position? 
                            // Actually, let's substitute.
                        }
                    }
                }
            }
        }
        
        // Better Replace Logic:
        // 1. Find Run. 
        // 2. Create SdtRun.
        // 3. Move Run children (run properties, text) to SdtContentRun -> Run? 
        // Actually, easiest is: Paragraph -> SdtRun -> SdtContentRun -> Run.
    }
    
    public void TagDocumentRefined(DocumentFormat.OpenXml.Wordprocessing.Body body, Dictionary<string, string> mapping)
    {
         Console.WriteLine("TaggingService: Semantically Anchoring placeholders...");
         
         // Iterate backwards or use a queue to handle modifications (splitting runs)
         // We'll scan Paragraphs, then Runs.
         
         foreach (var para in body.Descendants<Paragraph>())
         {
             // We need to loop until no more replacements in this paragraph?
             // Or iterate runs. Since we split runs, the list changes.
             // Let's iterate a snapshot, but if we split, we might miss the "After" part if not careful.
             // Actually, simplest is: Find First Match in Paragraph, Split/Wrap, Repeat Paragraph Scan.
             
             bool changeMade = true;
             int safetyCounter = 0;
             while (changeMade)
             {
                 safetyCounter++;
                 if (safetyCounter > 1000)
                 {
                     Console.WriteLine("ERROR: Infinite loop detected in TaggingService. Aborting paragraph scan.");
                     break;
                 }

                 changeMade = false;
                 var runs = para.Elements<Run>().ToList();
                 // Console.WriteLine($"   Debug: Paragraph has {runs.Count} runs.");
                 
                 foreach (var run in runs)
                 {
                     string text = run.InnerText;
                     
                     // Find first matching key
                     var match = mapping.FirstOrDefault(kvp => text.Contains(kvp.Key));
                     if (!string.IsNullOrEmpty(match.Key))
                     {
                         // Console.WriteLine($"   Tagging Scan: Found '{match.Key}' in run text: '{text}'");
                         
                         // Found one!
                         // Is it exact match?
                         if (text == match.Key)
                         {
                             // Wrap exact
                             WrapRunInSdt(run, match.Value);
                         }
                         else
                         {
                             // Split!
                             // "Hello [Name]!" -> "Hello ", "[Name]", "!"
                             int index = text.IndexOf(match.Key);
                             string before = text.Substring(0, index);
                             string after = text.Substring(index + match.Key.Length);
                             
                             Run? runBefore = null;
                             if (!string.IsNullOrEmpty(before))
                             {
                                 runBefore = (Run)run.CloneNode(true);
                                 runBefore.GetFirstChild<Text>().Text = before;
                                 para.InsertBefore(runBefore, run);
                             }
                             
                             Run runMatch = (Run)run.CloneNode(true);
                             runMatch.GetFirstChild<Text>().Text = match.Key;
                             para.InsertBefore(runMatch, run);
                             
                             var sdtRun = WrapRunInSdt(runMatch, match.Value);
                             
                             Run? runAfter = null;
                             if (!string.IsNullOrEmpty(after))
                             {
                                 runAfter = (Run)run.CloneNode(true);
                                 runAfter.GetFirstChild<Text>().Text = after;
                                 para.InsertAfter(runAfter, sdtRun); 
                             }
                             
                             run.Remove();
                         }
                         
                         changeMade = true;
                         break; // Restart paragraph scan to be safe
                     }
                 }
             }
         }
    }
    
    public Dictionary<string, string> DetectHighlights(DocumentFormat.OpenXml.Wordprocessing.Body body)
    {
         Console.WriteLine("TaggingService: Scanning for Highlights (Grouping Contiguous Runs)...");
         var mapping = new Dictionary<string, string>();
         int count = 0;
         
         // Iterate Paragraphs to preserve contiguous context
         foreach (var para in body.Descendants<Paragraph>())
         {
             var runs = para.Elements<Run>().ToList();
             if (runs.Count == 0) continue;

             List<Run> currentGroup = new List<Run>();
             
             foreach (var run in runs)
             {
                 bool isHighlighted = run.RunProperties?.Highlight != null && !string.IsNullOrWhiteSpace(run.InnerText);
                 
                 if (isHighlighted)
                 {
                     currentGroup.Add(run);
                 }
                 else
                 {
                     if (currentGroup.Count > 0)
                     {
                         // End of a group -> Process it
                         ProcessHighlightGroup(currentGroup, ref count, mapping);
                         currentGroup.Clear();
                     }
                 }
             }
             
             // Process any remaining group at end of paragraph
             if (currentGroup.Count > 0)
             {
                 ProcessHighlightGroup(currentGroup, ref count, mapping);
             }
         }
         
         return mapping;
    }

    private void ProcessHighlightGroup(List<Run> runs, ref int count, Dictionary<string, string> mapping)
    {
        if (runs.Count == 0) return;

        // 1. Calculate Combined Text
        string fullText = string.Join("", runs.Select(r => r.InnerText));
        string variableName = $"AI_GEN_CONTENT_{count++}";
        
        Console.WriteLine($"  -> Detected Highlight Group: '{fullText}' mapped to '{variableName}'");
        mapping[variableName] = fullText;

        // 2. Create SDT Wrapper
        SdtRun sdtRun = new SdtRun();
        SdtProperties sdtPr = new SdtProperties(
             new Tag { Val = variableName },
             new SdtAlias { Val = variableName }
        );
        sdtRun.Append(sdtPr);
        SdtContentRun sdtContent = new SdtContentRun();
        sdtRun.Append(sdtContent);

        // 3. Insert SDT into the Document
        // We will insert the SDT before the *first* run of the group, then move all runs into it.
        Run firstRun = runs[0];
        firstRun.Parent.InsertBefore(sdtRun, firstRun);

        // 4. Move Runs into SDT & Remove Highlight Property
        foreach (var run in runs)
        {
            run.Remove(); // Remove from original Paragraph
            
            // Clean highlight property
            if (run.RunProperties?.Highlight != null)
            {
                run.RunProperties.Highlight.Remove();
            }
            
            sdtContent.Append(run); // Add to SDT
        }
    }

    private SdtRun WrapRunInSdt(Run run, string tagValue)
    {
         // Create the Sdt structure
         SdtRun sdtRun = new SdtRun();
         SdtProperties sdtPr = new SdtProperties(
             new Tag { Val = tagValue },
             new SdtAlias { Val = tagValue }
         );
         sdtRun.Append(sdtPr);
         
         SdtContentRun sdtContent = new SdtContentRun();
         
         // Move the run inside
         run.Parent.ReplaceChild(sdtRun, run);
         sdtContent.Append(run); // Run is now child of sdtContent
         sdtRun.Append(sdtContent);
         
         // Console.WriteLine($"  -> Anchored '{tagValue}'");
         return sdtRun;
    }
}
