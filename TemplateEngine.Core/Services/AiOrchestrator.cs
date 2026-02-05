using System.Text.Json;
using Microsoft.SemanticKernel; // Keeping the namespace even if we mock for now
using System.Threading.Tasks;

namespace TemplateEngine.Core.Services;

public class AiOrchestrator
{
    // Mock response for the prototype
    public async Task<Dictionary<string, string>> AnalyzeStructureAsync(string jsonSchema)
    {
        // In a real app, we would:
        // 1. Create a prompt with the schema.
        // 2. Send to GPT-4o via Semantic Kernel.
        // 3. Parse the JSON response.

        Console.WriteLine("AI is analyzing the document structure...");
        await Task.Delay(100); // Simulate network

        // Returns Map:  Id/Text Identifier -> Variable Name
        // For our test file, we want to map:
        // "[CLIENT_NAME]" -> "ClientName"
        // "[POLICY_TYPE]" -> "PolicyType"
        // "[PREMIUM_AMOUNT]" -> "PremiumAmount"
        
        // Simulating the AI simply spotting the brackets and suggesting clean names
        var mapping = new Dictionary<string, string>
        {
            { "[CLIENT_NAME]", "ClientName" },
            { "[POLICY_TYPE]", "PolicyType" },
            { "[PREMIUM_AMOUNT]", "PremiumAmount" }
        };

        return mapping;
    }

    public string GenerateContent(string originalText)
    {
         // Simulate LLM Generation based on the highlighted "Prompt"
         // e.g. "Insert Executive Summary" -> "Executive Summary: Q4 performance was strong..."
         
         Console.WriteLine($"AIOrchestrator: Generating content for '{originalText}'...");
         
         // Mock logic for demo
         if (originalText.Contains("Executive Summary"))
             return "Executive Summary: The portfolio has outperformed the benchmark by 12% this quarter due to strategic tech allocations.";
             
         if (originalText.Contains("Introduction"))
             return "Introduction: We are pleased to present your annual review. This year has seen significant volatility, yet your strategy remains resilient.";
             
         if (originalText.Contains("Conclusion"))
             return "Conclusion: We recommend rebalancing the fixed income sector to capitalize on rising rates.";
             
         // Default generic response
         return $"[AI Generated Content for: {originalText}]";
    }

    public Dictionary<string, string> GenerateTableData(string markdownTable, List<string> targetTags)
    {
        Console.WriteLine($"AIOrchestrator: Analysis of Table Context...");
        Console.WriteLine(markdownTable);
        
        var results = new Dictionary<string, string>();
        
        // Mocking LLM Logic
        // The LLM would see the table and know that {{TAG_1}} is under "Q2" and row "Revenue"
        // Here we just return mock numbers so the user sees the flow working.
        
        foreach (var tag in targetTags)
        {
            // Simulate calculation
            results[tag] = $"[AI_CALC: {new Random().Next(100, 999)}]";
        }
        
        return results;
    }
}
