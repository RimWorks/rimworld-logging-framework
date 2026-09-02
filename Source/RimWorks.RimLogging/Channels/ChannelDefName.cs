using System;

namespace RimWorks.RimLogging.Channels;

/// <summary>Validates dotted channel defNames, which vanilla's Def rules reject. Pure so it tests without Verse.</summary>
internal static class ChannelDefName
{
    /// <summary>Returns a config-error string, or <c>null</c> when the name is a valid channel name.</summary>
    internal static string? Validate(string? defName)
    {
        if (string.IsNullOrEmpty(defName))
        {
            return "ChannelDef lacks a defName.";
        }

        foreach (string segment in defName!.Split('.'))
        {
            if (segment.Length == 0)
            {
                return $"defName {defName} has an empty segment; channel names cannot start, end, or double up on dots.";
            }

            if (segment.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-'))
            {
                return $"defName {defName} should only contain letters, numbers, underscores, dashes, and dots.";
            }
        }

        return null;
    }
}
