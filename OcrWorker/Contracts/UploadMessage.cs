using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OcrWorker.Messaging;

public sealed record UploadMessage(
    Guid DocumentId,
    string Name,
    string Bucket,
    string ObjectName,
    DateTime UploadedAt
);