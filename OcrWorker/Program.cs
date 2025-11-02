using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using OcrWorker.Workers;
using OcrWorker.Storage;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<IObjectStorage, MinioStorage>();
builder.Services.AddHostedService<OcrConsumer>();

var host = builder.Build();
await host.RunAsync();
