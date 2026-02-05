using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System.Text.Json;

namespace TemplateEngine.Core.Services;

public static class SchemaGenerator
{
    public class SimplifiedNode
    {
        public string Id { get; set; } = "";
        public string Type { get; set; } = "";
        public string Text { get; set; } = "";
        public List<SimplifiedNode> Children { get; set; } = new();
    }

    public static string GenerateSchemaJson(Body body)
    {
        var root = new SimplifiedNode { Type = "Body", Id = "root" };
        Traverse(body, root);
        return JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void Traverse(OpenXmlElement element, SimplifiedNode parentNode)
    {
        SimplifiedNode? node = null;

        if (element is Paragraph p)
        {
            node = new SimplifiedNode 
            { 
                Type = "Paragraph", 
                Text = p.InnerText, 
                Id = GetOrGenerateId(p) 
            };
        }
        else if (element is Table tbl)
        {
            node = new SimplifiedNode 
            { 
                Type = "Table", 
                Id = GetOrGenerateId(tbl) 
            };
        }
        else if (element is TableRow tr)
        {
            node = new SimplifiedNode 
            { 
                Type = "TableRow", 
                Id = GetOrGenerateId(tr) 
            };
        }
        else if (element is TableCell tc)
        {
            node = new SimplifiedNode 
            { 
                Type = "TableCell", 
                Id = GetOrGenerateId(tc) 
            };
        }

        if (node != null)
        {
            parentNode.Children.Add(node);
            
            // Recurse ONLY for containers we care about preserving structure for
            foreach (var child in element.Elements())
            {
                Traverse(child, node);
            }
        }
        else
        {
            // If it's not a node we explicitely track (like Run, Properties, etc), 
            // we might still want to recurse if it's a container (like Body), 
            // OR just skip if we handled the text at the Paragraph level.
            // For this simplified schema, let's skip recursing into Runs for now 
            // since Paragraph.InnerText captures the text.
            // BUT we must recurse for elements that CAN contain Paragraphs/Tables 
            // even if they aren't one themselves (e.g. wrappers).
            
            // For robustness, let's just peek children.
             foreach (var child in element.Elements())
            {
                Traverse(child, parentNode);
            }
        }
    }

    private static string GetOrGenerateId(OpenXmlElement element)
    {
        // Ideally use existing RSID or generate a stable one. 
        // For prototype, simple hash or guidance.
        // We might want to inject an ID if one doesn't exist to track it back.
        return element.GetHashCode().ToString("X");
    }
}
