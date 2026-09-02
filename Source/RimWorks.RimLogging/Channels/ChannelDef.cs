using System;
using System.Collections.Generic;
using UnityEngine;

namespace RimWorks.RimLogging.Channels;

/// <summary>Verse Def describing a named logging channel and its defaults (level, color, stack-capture threshold, destinations, format).</summary>
public class ChannelDef : Verse.Def
{
    /// <summary>Minimum level for this channel, or <c>null</c> to follow the global minimum.</summary>
    public LogLevel? defaultLevel = null;

    /// <summary>Optional display color for this channel, or <c>null</c> for no color.</summary>
    public Color? color = null;

    /// <summary>Level at or above which a stack trace is captured, or <c>null</c> to follow the global switch.</summary>
    public LogLevel? captureStackAt = null;

    /// <summary>Names of the sink destinations entries on this channel are routed to.</summary>
    public List<string> destinations = new List<string>();

    /// <summary>Optional per-channel format template override, or <c>null</c> to use the default.</summary>
    public string? format = null;

    /// <summary>The channel <see cref="color"/> as an RGB hex string, or <c>null</c> when no color is set.</summary>
    public string? ColorHex => color.HasValue
        ? ColorUtility.ToHtmlStringRGB(color.Value)
        : null;

    /// <summary>Reports config errors, allowing the dots that vanilla's defName rule rejects.</summary>
    /// <returns>Every base error except the defName-characters one, plus our own dotted-name check.</returns>
    public override IEnumerable<string> ConfigErrors()
    {
        // dots are how channels nest, so vanilla's defName character rule cannot apply here
        foreach (string error in base.ConfigErrors()
            .Where(e => e.IndexOf("should only contain", StringComparison.Ordinal) < 0))
        {
            yield return error;
        }

        string? problem = ChannelDefName.Validate(defName);
        if (problem != null)
        {
            yield return problem;
        }
    }
}
