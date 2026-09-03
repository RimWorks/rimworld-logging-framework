namespace RimWorks.RimLogging.Analyzers;

/// <summary>What RimLogging call replaces a given <c>Verse.Log</c> method, and why.</summary>
internal static class VerseLogMethods
{
    /// <summary>A <c>Verse.Log</c> write and the RimLogging call that should replace it.</summary>
    internal readonly struct Replacement
    {
        internal Replacement(bool sharesVanillaKey, string signature)
        {
            SharesVanillaKey = sharesVanillaKey;
            Signature = signature;
        }

        /// <summary>True for the <c>Once</c> methods, which dedupe against one global int set.</summary>
        internal bool SharesVanillaKey { get; }

        /// <summary>Signature to show the caller, argument order included.</summary>
        internal string Signature { get; }
    }

    /// <summary>
    /// Maps a <c>Verse.Log</c> method name to its replacement, or null when the member is not a
    /// log write. <c>Clear</c> and <c>TryOpenLogWindow</c> have no RimLogging equivalent.
    /// </summary>
    internal static Replacement? Classify(string method)
    {
        switch (method)
        {
            case "Error":
                return new Replacement(false, "Log.ErrorTo(channel, message)");
            case "Warning":
                return new Replacement(false, "Log.WarnTo(channel, message)");
            case "Message":
                return new Replacement(false, "Log.InfoTo(channel, message)");
            case "ErrorOnce":
                return new Replacement(true, "Log.ErrorOnceTo(channel, key, message)");
            case "WarningOnce":
                return new Replacement(true, "Log.WarnOnceTo(channel, key, message)");
            default:
                return null;
        }
    }
}
