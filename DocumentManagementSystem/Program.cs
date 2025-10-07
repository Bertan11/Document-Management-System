using DocumentManagementSystem.Data;
   // 👈 für ErrorHandlingMiddleware
using DocumentManagementSystem.Middlewares;
using DocumentManagementSystem.Repositories;
using DocumentManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔹 DbContext mit Connection String aus appsettings.json oder Docker-Umgebung
builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔹 Repository + Service registrieren
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// 🔹 RabbitMQ-Service
builder.Services.AddSingleton<RabbitMqService>();

// 🔹 CORS hinzufügen
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // React-Frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// 🔹 Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// 🔹 Migrationen automatisch ausführen
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
    db.Database.Migrate();   // Erstellt die Tabellen basierend auf Migrationen
}

// 🔹 Swagger immer aktiv
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Management API v1");
    c.RoutePrefix = "swagger"; // erreichbar unter /swagger
});

// 🔹 ErrorHandlingMiddleware einfügen (muss GANZ OBEN stehen!)
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

// 🔹 CORS aktivieren (muss vor Authorization & Controllern stehen!)
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();
app.Run();
