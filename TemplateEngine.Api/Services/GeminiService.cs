using System.Text;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace TemplateEngine.Api.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;

    public GeminiService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<Dictionary<string, string>> GenerateValuesFromDocumentAsync(
        byte[] documentBytes, 
        string apiKey,
        Dictionary<string, string> detectedPlaceholders)
    {
        try
        {
            // Extract text from the document
            var documentText = ExtractTextFromDocument(documentBytes);

            // Get placeholder keys
            var placeholderKeys = string.Join(", ", detectedPlaceholders.Keys);

            // Create prompt
            var prompt = $@"You are analyzing a document template. The document has the following placeholders that need values:
{placeholderKeys}

Document content:
{documentText.Substring(0, Math.Min(documentText.Length, 8000))}

Generate appropriate contextual values for each placeholder based on the document content. Return ONLY a JSON object with the exact placeholder names as keys and your generated values as values.
Generate it on basis of an Indian Middle class persona, the firm could be called Reliance, or if its a person, it could be called Mukesh.
Example format:
{{
  ""AI_GEN_CONTENT_1"": ""123 Main Street, London"",
  ""AI_GEN_CONTENT_2"": ""Premium Policy Coverage"",
  ""AI_GEN_CONTENT_3"": ""January 15, 2024""
}}

IMPORTANT: Use the EXACT placeholder names I provided: {placeholderKeys}
Return only valid JSON, no additional text.";

            // Call Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                content);

            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<JsonElement>(responseText);

            // Extract text from response
            var generatedText = apiResponse
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Clean up response
            var jsonResponse = generatedText.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(3);
            }
            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }
            jsonResponse = jsonResponse.Trim();

            // Parse JSON response
            var values = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonResponse);
            return values ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            throw new Exception($"Gemini API error: {ex.Message}", ex);
        }
    }

    private string ExtractTextFromDocument(byte[] documentBytes)
    {
        using var stream = new MemoryStream(documentBytes);
        using var doc = WordprocessingDocument.Open(stream, false);
        
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body == null) return string.Empty;

        // Extract all text content
        var texts = body.Descendants<Text>().Select(t => t.Text);
        return string.Join(" ", texts);
    }
}
