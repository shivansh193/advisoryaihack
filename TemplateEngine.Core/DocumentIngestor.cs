using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TemplateEngine.Core.Services;

public static class DocumentIngestor
{
    public static WordprocessingDocument SafeOpen(string filepath, out MemoryStream ms)
    {
        byte[] bytes = File.ReadAllBytes(filepath);
        ms = new MemoryStream();
        ms.Write(bytes, 0, bytes.Length);
        ms.Position = 0;

        using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
        {
            SanitizeGoogleDocsXml(doc);

            if (doc.MainDocumentPart?.Document?.Body != null)
            {
                MergeRuns(doc.MainDocumentPart.Document.Body);
            }
            doc.Save();
        }
        
        ms.Position = 0;
        return WordprocessingDocument.Open(ms, true);
    }

    public static WordprocessingDocument SafeOpenFromStream(Stream inputStream, out MemoryStream ms)
    {
        ms = new MemoryStream();
        inputStream.CopyTo(ms);
        ms.Position = 0;

        using (WordprocessingDocument doc = WordprocessingDocument.Open(ms, true))
        {
            SanitizeGoogleDocsXml(doc);

            if (doc.MainDocumentPart?.Document?.Body != null)
            {
                MergeRuns(doc.MainDocumentPart.Document.Body);
            }
            doc.Save();
        }
        
        ms.Position = 0;
        return WordprocessingDocument.Open(ms, true);
    }


    public static void ParseDocument(WordprocessingDocument doc)
    {
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return;

        Console.WriteLine("--- Starting Document Analysis ---");
        FlattenStructure(body, 0);
        Console.WriteLine("--- Analysis Complete ---");
    }

    private static void FlattenStructure(OpenXmlElement element, int depth)
    {
        string indent = new string(' ', depth * 2);

        if (element is Paragraph p)
        {
            var text = p.InnerText;
            if (string.IsNullOrWhiteSpace(text)) return; 
            
            bool hasPlaceholder = System.Text.RegularExpressions.Regex.IsMatch(text, @"\[.*?\]");
            string type = hasPlaceholder ? "[DYNAMIC PARAGRAPH]" : "[STATIC PARAGRAPH]";
            
            Console.WriteLine($"{indent}{type} Text: {text.Substring(0, Math.Min(text.Length, 50))}...");
        }
        else if (element is Table tbl)
        {
            Console.WriteLine($"{indent}[TABLE] found.");
            var rows = tbl.Elements<TableRow>().ToList();
            if (rows.Any())
            {
                Console.WriteLine($"{indent}  - Rows: {rows.Count}");
                Console.WriteLine($"{indent}  - Header candidates: {GetRowText(rows.First())}");
            }
        }
        else if (element is SdtBlock sdt) 
        {
             Console.WriteLine($"{indent}[CONTENT CONTROL] found. Tag: {sdt?.SdtProperties?.GetFirstChild<Tag>()?.Val}");
        }

        foreach (var child in element.Elements())
        {
            if (child is Paragraph || child is Table || child is SdtBlock || child is Body)
            {
                FlattenStructure(child, depth + 1);
            }
        }
    }

    private static string GetRowText(TableRow row)
    {
        return string.Join(" | ", row.Descendants<Text>().Select(t => t.Text));
    }

    private static void SanitizeGoogleDocsXml(WordprocessingDocument doc)
    {
        if (doc.MainDocumentPart?.Document == null) return;
        
        // Use a broader search to ensure we catch everything
        var elements = doc.MainDocumentPart.Document.Descendants().Where(e => e.HasAttributes).ToList();
        int fixCount = 0;
        
        foreach (var element in elements)
        {
            var attributes = element.GetAttributes();
            var attrsToFix = new List<OpenXmlAttribute>();
            var attrsToRemove = new List<OpenXmlAttribute>();

            foreach (var attr in attributes)
            {
                string val = attr.Value ?? "";

                // 1. Fix "100.0" integer bugs
                if (val.EndsWith(".0") && int.TryParse(val.Replace(".0", ""), out int intVal))
                {
                     // Console.WriteLine($"Sanitizing: Found float-like int {val} on {element.LocalName}");
                     attrsToFix.Add(new OpenXmlAttribute(attr.Prefix, attr.LocalName, attr.NamespaceUri, intVal.ToString()));
                     fixCount++;
                }
                
                // 2. Fix Boolean "0"/"1" constraints
                if ((val == "0" || val == "1") && attr.LocalName == "val")
                {
                     // Heuristic for known boolean types
                     string elName = element.LocalName.ToLower();
                     if (elName == "tblheader" || elName == "cantsplit" || elName == "bidi" || elName == "rtl" || elName == "noWrap") 
                     {
                         string boolVal = val == "1" ? "true" : "false"; // "on" / "off" works too? true/false is safer for boolean.
                         attrsToFix.Add(new OpenXmlAttribute(attr.Prefix, attr.LocalName, attr.NamespaceUri, boolVal));
                         fixCount++;
                     }
                }
                
                // 3. Remove undeclared paraId
                if (attr.LocalName == "paraId")
                {
                    attrsToRemove.Add(attr);
                    fixCount++;
                }
            }

            foreach (var remove in attrsToRemove)
            {
                element.RemoveAttribute(remove.LocalName, remove.NamespaceUri);
            }
            
            foreach (var newAttr in attrsToFix)
            {
                element.SetAttribute(newAttr);
            }
        }
        
        if (fixCount > 0) Console.WriteLine($"SanitizeGoogleDocsXml: Fixed {fixCount} issues.");
    }

    private static void MergeRuns(Body body)
    {
        foreach (var para in body.Descendants<Paragraph>())
        {
            var runs = para.Elements<Run>().ToList();
            if (runs.Count < 2) continue;

            StringBuilder sb = new StringBuilder();
            foreach (var run in runs) sb.Append(run.InnerText);

            var firstRun = runs[0];
            firstRun.RemoveAllChildren<Text>(); 
            firstRun.AppendChild(new Text(sb.ToString())); 

            for (int i = 1; i < runs.Count; i++) runs[i].Remove();
        }
    }
}
