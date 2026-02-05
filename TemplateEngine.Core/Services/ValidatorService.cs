using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Packaging;

namespace TemplateEngine.Core.Services;

public class ValidatorService
{
    public bool ValidateDocument(WordprocessingDocument doc)
    {
        Console.WriteLine("ValidatorService: Validating document structure...");
        
        var validator = new OpenXmlValidator();
        var errors = validator.Validate(doc);

        if (!errors.Any())
        {
            Console.WriteLine("  -> Document is valid.");
            return true;
        }

        Console.WriteLine($"  -> Found {errors.Count()} validation error(s).");
        foreach (var error in errors)
        {
            Console.WriteLine($"    - [{error.Node?.LocalName}] {error.Description}");
        }
        
        return false;
    }
}
