using System;

namespace RimWorks.RimLogging;

/// <summary>Contract for log emit destinations; disposable for clean teardown of file handles.</summary>
public interface ILogSink : IDisposable
{
    /// <summary>Identifies this sink for telemetry and settings lookup.</summary>
    string Name { get; }

    /// <summary>Per-sink filter gate; entries below this level are not forwarded to this sink.</summary>
    LogLevel MinLevel { get; }

    /// <summary>Receives a fully resolved entry; the sink decides whether to render the template or emit structured data.</summary>
    void Write(LogEntry entry);

    /// <summary>Flushes buffered entries; called by ShutdownFlush and after sync-bypass of Error/Fatal.</summary>
    void Flush();
}
