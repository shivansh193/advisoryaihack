namespace TemplateEngine.Api.Models;

public enum ProcessingMode
{
    Auto,       // Current automatic processing
    Manual,     // User-provided JSON values
    AIGenerated // Gemini API generates values
}
