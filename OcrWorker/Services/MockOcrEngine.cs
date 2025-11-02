using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace OcrWorker.Services;

public sealed class MockOcrEngine : IOcrEngine   // <- WICHTIG: Interface implementieren
{
    private readonly int _latencyMs;

    public MockOcrEngine(IConfiguration cfg)
    {
        _latencyMs = cfg.GetValue<int?>("Processing:SimulatedLatencyMs") ?? 50;
    }

    public async Task<string> ExtractTextAsync(Guid documentId, CancellationToken ct = default) // <- Signatur wie im Interface
    {
        await Task.Delay(_latencyMs, ct);
        return $"[Simuliertes OCR-Ergebnis für DocumentId={documentId}]";
    }
}
