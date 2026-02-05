using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace TemplateEngine.Core.Services;

public class TableContextService
{
    /// <summary>
    /// Serializes a Word Table into a Markdown string for LLM Context.
    /// Replaces known tags with {{TAG_ID}} to help the LLM identify targets.
    /// </summary>
    public string SerializeTableToMarkdown(Table table, Dictionary<string, string> tagMapping)
    {
        StringBuilder sb = new StringBuilder();
        
        var rows = table.Elements<TableRow>().ToList();
        if (rows.Count == 0) return "";

        // Iterate Rows
        foreach (var row in rows)
        {
            var cells = row.Elements<TableCell>().ToList();
            var cellValues = new List<string>();

            foreach (var cell in cells)
            {
                // check for SdtRun inside cell
                var sdt = cell.Descendants<SdtRun>().FirstOrDefault();
                if (sdt != null)
                {
                    var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                    if (tag != null)
                    {
                        // It's a target! Use the Tag ID so the LLM knows what to generate.
                        cellValues.Add($"{{{{{tag}}}}}"); 
                        continue;
                    }
                }

                // Otherwise, get plain text
                string text = cell.InnerText;
                cellValues.Add(text);
            }

            sb.AppendLine("| " + string.Join(" | ", cellValues) + " |");
            
            // Add separator for header if it's the first row (Heuristic)
            // Real logic might need to check TableProperties for headers.
            if (row == rows.First())
            {
                var separators = cellValues.Select(c => "---");
                sb.AppendLine("| " + string.Join(" | ", separators) + " |");
            }
        }

        return sb.ToString();
    }
}
