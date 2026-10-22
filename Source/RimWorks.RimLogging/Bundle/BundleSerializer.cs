using System.Text.Json;
using System.Text.Json.Serialization;

namespace RimWorks.RimLogging.Bundle;

/// <summary>
/// Serializes and deserializes <see cref="BundlePayload"/> to and from JSON using indented, camelCase output
/// that omits null values and applies relaxed escaping.
/// </summary>
public static class BundleSerializer
{
    private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serializes a bundle payload to indented camelCase JSON.</summary>
    /// <param name="p">The payload to serialize.</param>
    /// <returns>The JSON representation of <paramref name="p"/>.</returns>
    public static string Serialize(BundlePayload p) => JsonSerializer.Serialize(p, _opts);

    /// <summary>Deserializes a bundle payload from JSON, returning an empty payload if the input deserializes to null.</summary>
    /// <param name="json">The JSON to deserialize.</param>
    /// <returns>The deserialized <see cref="BundlePayload"/>, or a new empty instance.</returns>
    public static BundlePayload Deserialize(string json)
        => JsonSerializer.Deserialize<BundlePayload>(json, _opts) ?? new BundlePayload();

    /// <summary>Serializes one batch of entries as the body the append endpoint reads.</summary>
    /// <param name="entries">The entries to send.</param>
    /// <returns>The JSON body carrying those entries.</returns>
    public static string SerializeChunk(System.Collections.Generic.IReadOnlyList<BundlePayload.EntryDto> entries)
        => JsonSerializer.Serialize(new ChunkBody { Entries = entries }, _opts);

    /// <summary>The byte count one entry costs on the wire, for planning how many fit in a batch.</summary>
    /// <param name="entry">The entry to measure.</param>
    /// <returns>The entry's serialized length in bytes.</returns>
    public static int MeasureEntry(BundlePayload.EntryDto entry)
        => JsonSerializer.SerializeToUtf8Bytes(entry, _opts).Length;

    private sealed class ChunkBody
    {
        public System.Collections.Generic.IReadOnlyList<BundlePayload.EntryDto> Entries { get; set; }
            = System.Array.Empty<BundlePayload.EntryDto>();
    }
}
