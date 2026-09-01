using System;
using System.Collections.Generic;
using UnityEngine;

namespace RimWorks.RimLogging.Channels;

/// <summary>Verse Def describing a named logging channel and its defaults (level, color, stack-capture threshold, destinations, format).</summary>
public class ChannelDef : Verse.Def
{
    /// <summary>Minimum level at which entries on this channel are emitted by default.</summary>
    public LogLevel defaultLevel = LogLevel.Info;

    /// <summary>Optional display color for this channel, or <c>null</c> for no color.</summary>
    public Color? color = null;

    /// <summary>Level at or above which a stack trace is captured for entries on this channel.</summary>
    public LogLevel captureStackAt = LogLevel.Error;

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
        foreach (string error in base.ConfigErrors())
        {
            // dots are how channels nest, so vanilla's defName character rule cannot apply here
            if (error.IndexOf("should only contain", StringComparison.Ordinal) < 0)
            {
                yield return error;
            }
        }

        string? problem = ChannelDefName.Validate(defName);
        if (problem != null)
        {
            yield return problem;
        }
    }
}
