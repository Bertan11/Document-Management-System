using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OcrWorker.Services;

public interface IOcrEngine
{
    Task<string> ExtractTextAsync(Guid documentId, CancellationToken ct = default);
}
