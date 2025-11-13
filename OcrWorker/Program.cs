// OcrWorker/Program.cs
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcrWorker.Services;
using OcrWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

// MinIO Storage
builder.Services.AddSingleton<IObjectStorage, MinioStorage>();

// OCR Service
builder.Services.AddSingleton<OcrService>();

// Worker
builder.Services.AddHostedService<UploadOcrWorker>();

var host = builder.Build();
await host.RunAsync();
