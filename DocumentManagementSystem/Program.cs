using DocumentManagementSystem.Data;
using DocumentManagementSystem.Middlewares;
using DocumentManagementSystem.Repositories;
using DocumentManagementSystem.Services; // <- IEventBus, RabbitMqService
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// DB
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repos/Services
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// RabbitMQ Publisher über DI (nicht die konkrete Klasse in Controllers/Services nutzen)
builder.Services.AddSingleton<IEventBus, RabbitMqService>();

//API
builder.Services.AddSingleton<IObjectStorage, MinioStorage>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// MVC/Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// DB-Migrationen beim Start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    db.Database.Migrate();
}

// RabbitMQ-Infrastruktur (Exchange/Queue/Binding) beim Start sicherstellen
using (var scope = app.Services.CreateScope())
{
    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

    var exchange = cfg["RabbitMq:Exchange"] ?? cfg["Rabbit:Exchange"] ?? "dms.events";
    var queue = cfg["RabbitMq:QueueUpload"] ?? cfg["Rabbit:QueueUpload"] ?? "dms.uploads";
    var routingKey = cfg["RabbitMq:RoutingUpload"] ?? cfg["Rabbit:RoutingUpload"] ?? "document.uploaded";

    bus.EnsureInfra(exchange, queue, routingKey);
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Management API v1");
    c.RoutePrefix = "swagger";
});

// Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseCors("AllowFrontend");
app.UseAuthorization();

app.MapControllers();
app.Run();
