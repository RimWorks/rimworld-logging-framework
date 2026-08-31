using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Serializable container for a bug-report bundle: the RimWorld and framework versions, the loaded mod
/// list, and the captured log entries. Serialized to JSON and uploaded via the proxy.
/// </summary>
public sealed class BundlePayload
{
    /// <summary>The RimWorld game version the bundle was captured under.</summary>
    public string RimWorldVersion { get; set; } = "";

    /// <summary>The RimLogging framework revision the bundle was captured under.</summary>
    public string FrameworkVersion { get; set; } = "";

    /// <summary>The mods that were loaded when the bundle was captured.</summary>
    public List<ModInfo> Mods { get; set; } = new List<ModInfo>();

    /// <summary>The log entries included in the bundle.</summary>
    public List<EntryDto> Entries { get; set; } = new List<EntryDto>();

    /// <summary>Describes a single loaded mod within the bundle.</summary>
    public sealed class ModInfo
    {
        /// <summary>Human-readable mod name.</summary>
        public string Name { get; set; } = "";

        /// <summary>The mod's package identifier (e.g. <c>author.modname</c>).</summary>
        public string PackageId { get; set; } = "";

        /// <summary>The mod version read from its manifest, or <c>null</c> if unavailable.</summary>
        public string? Version { get; set; }

        /// <summary>Whether the mod was active in the load order.</summary>
        public bool Active { get; set; }
    }

    /// <summary>Flattened, serialization-friendly representation of a single log entry.</summary>
    public sealed class EntryDto
    {
        /// <summary>The entry timestamp in ISO-8601 round-trip ("o") format.</summary>
        [JsonPropertyName("ts")]
        public string Timestamp { get; set; } = "";

        /// <summary>The log level name.</summary>
        public string Level { get; set; } = "";

        /// <summary>The channel the entry was logged to.</summary>
        public string Channel { get; set; } = "";

        /// <summary>The caller source location as <c>file:line</c>, or empty when not caller-provided.</summary>
        public string Source { get; set; } = "";

        /// <summary>The rendered message with RimWorld rich-text markup stripped.</summary>
        [JsonPropertyName("msg")]
        public string Message { get; set; } = "";

        /// <summary>Structured context key/value pairs attached to the entry, or <c>null</c> if none.</summary>
        [JsonPropertyName("ctx")]
        public Dictionary<string, object?>? Context { get; set; }

        /// <summary>The stack trace, or the formatted exception when no explicit stack trace is present; <c>null</c> if neither.</summary>
        public string? Stack { get; set; }
    }
}
