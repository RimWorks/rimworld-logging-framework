namespace RimWorks.RimLogging;

/// <summary>
/// Severity levels for log messages, ordered from least to most severe.
/// Backing integer values (0..5) allow cheap integer comparison at the global min-level gate.
/// </summary>
public enum LogLevel
{
    /// <summary>Fine-grained diagnostic output, lowest severity.</summary>
    Trace = 0,

    /// <summary>Debugging information useful during development.</summary>
    Debug = 1,

    /// <summary>General informational messages about normal operation.</summary>
    Info = 2,

    /// <summary>Potentially harmful situations that deserve attention.</summary>
    Warn = 3,

    /// <summary>Error events that may still allow the application to continue.</summary>
    Error = 4,

    /// <summary>Severe errors that will likely cause the application to abort.</summary>
    Fatal = 5,
}
