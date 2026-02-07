using TemplateEngine.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin() // Allow Vercel or any other frontend host
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register document processing service as singleton
builder.Services.AddSingleton<DocumentProcessingService>();
builder.Services.AddSingleton<DocumentAnalyzer>();
builder.Services.AddSingleton<GeminiService>();

// Configure file upload size limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600; // 100MB
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
