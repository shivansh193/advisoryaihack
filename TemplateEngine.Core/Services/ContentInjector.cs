using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace TemplateEngine.Core.Services;

public class ContentInjector
{
    // Inject values into Content Controls (SdtElement)
    public void InjectValues(Body body, Dictionary<string, string> values)
    {
        Console.WriteLine("ContentInjector: Injecting Text Values...");
        
        // Find all SdtElements (Block, Run, etc)
        // Note: SdtElement is abstract base for SdtRun, SdtBlock, etc.
        var sdtElements = body.Descendants<SdtElement>().ToList();

        foreach (var sdt in sdtElements)
        {
            var tag = sdt.SdtProperties?.GetFirstChild<Tag>();
            if (tag != null && values.TryGetValue(tag.Val, out string value))
            {
                // Update the content!
                // We need to find the Text element inside SdtContent
                var content = sdt.GetFirstChild<SdtContentRun>(); // SdtRun content
                // Or SdtContentBlock for SdtBlock...
                
                // Generic approach:
                var textElements = sdt.Descendants<Text>().ToList();
                if (textElements.Any())
                {
                    // Update first text element, clear others?
                    // Or set all?
                    // Safe approach: Update the first one found, and if multiple runs exist, it might be messy.
                    // Ideally, we replace the Text.
                    
                    textElements.First().Text = value;
                    
                    // Simple cleanup: if there were multiple runs split up "Place" + "holder"
                    // we might want to remove others. But tagging usually wraps the whole thing.
                    for (int i = 1; i < textElements.Count; i++)
                    {
                        textElements[i].Text = "";
                    }

                    Console.WriteLine($"  -> Injected '{value}' into '{tag.Val}'");
                }
            }
        }
    }

    // Table Expansion Logic
    // We assume the table is identified by: containing a specific variable in a cell?
    // Or we just look for a table where row 2 looks like a template.
    // Simplifying: User passes Table Index or ID. 
    // Or "Find Table containing [POLICY_TYPE]"
    
    public void InjectTableData(Body body, List<Dictionary<string, string>> rowData)
    {
        Console.WriteLine("ContentInjector: Injecting Table Data...");

        // 1. Find the table. 
        // Heuristic: Look for a table that contains one of the keys in rowData
        if (!rowData.Any()) return;
        var firstKey = rowData.First().Keys.First(); 
        
        // Find table containing text "[PolicyType]" or just "PolicyType" inside a Tag?
        // Since we tagged it, it's likely inside an SDT now.
        // Let's search specifically for the table containing our tags.
        
        var table = body.Descendants<Table>().FirstOrDefault(t => 
            t.Descendants<Tag>().Any(tag => rowData.First().ContainsKey(tag.Val))
        );

        if (table == null) 
        {
            Console.WriteLine("  -> No matching table found.");
            return;
        }

        // 2. Identify Template Row (usually row index 1, i.e., 2nd row)
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count < 2) return;
        
        var templateRow = rows[1]; // Index 1
        
        // Remove ALL rows except Header
        // We use the templateRow as a blueprint, then clear the table of data.
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].Remove();
        }

        // 3. Clone and Fill
        foreach (var dataItem in rowData)
        {
            var newRow = (TableRow)templateRow.CloneNode(true);
            
            // Find inputs in the new row
            // They are likely SdtRun/SdtBlock as per our tagging
            var sdts = newRow.Descendants<SdtElement>();
            
            foreach (var sdt in sdts)
            {
                var tag = sdt.SdtProperties?.GetFirstChild<Tag>();
                if (tag != null && dataItem.TryGetValue(tag.Val, out string val))
                {
                     var txt = sdt.Descendants<Text>().FirstOrDefault();
                     if (txt != null) txt.Text = val;
                }
            }
            
            table.AppendChild(newRow);
        }
        Console.WriteLine($"  -> Added {rowData.Count} rows to table.");
    }
}
