using Microsoft.EntityFrameworkCore;
using DocumentManagementSystem.Data;
using DocumentManagementSystem.Services;
using DocumentManagementSystem.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DocumentManagementSystem",
        Version = "v1",
        Description = "Document Management System API für SWEN3 Sprint 1"
    });
});

// InMemory Database für Sprint 1
builder.Services.AddDbContext<DocumentDbContext>(options =>
{
    options.UseInMemoryDatabase("DocumentManagementDb");
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
    }
});

// Register repositories and services
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DocumentManagementSystem v1");
        c.RoutePrefix = "swagger";
    });
}

// Database Initialisierung für InMemory
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
        context.Database.EnsureCreated();
        Console.WriteLine("Database connection successful!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}

app.UseRouting();
app.MapControllers();

// Test-Endpoints
app.MapGet("/", () => "SUCCESS! Document Management System API läuft!");

app.MapGet("/health", () => new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Message = "Sprint 1 - Basic functionality running",
    Database = "InMemory"
});

Console.WriteLine("Document Management System API gestartet!");
Console.WriteLine("Verfügbare Endpoints:");
Console.WriteLine("- GET  /                       -> Status check");
Console.WriteLine("- GET  /health                 -> Health check");
Console.WriteLine("- GET  /swagger                -> API Documentation");
Console.WriteLine("- GET  /api/document           -> Get all documents");
Console.WriteLine("- POST /api/document/upload    -> Upload document");

app.Run();