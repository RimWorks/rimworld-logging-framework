namespace RimWorks.RimLogging.Analyzers;

/// <summary>What RimLogging call replaces a given <c>Verse.Log</c> method, and why.</summary>
internal static class VerseLogMethods
{
    /// <summary>A <c>Verse.Log</c> write and the RimLogging call that should replace it.</summary>
    internal readonly struct Replacement
    {
        internal Replacement(bool sharesVanillaKey, string signature, string fixMethodName, bool swapsArguments)
        {
            SharesVanillaKey = sharesVanillaKey;
            Signature = signature;
            FixMethodName = fixMethodName;
            SwapsArguments = swapsArguments;
        }

        /// <summary>True for the <c>Once</c> methods, which dedupe against one global int set.</summary>
        internal bool SharesVanillaKey { get; }

        /// <summary>Signature to show the caller, argument order included.</summary>
        internal string Signature { get; }

        /// <summary>Method name on <c>RimWorks.RimLogging.Log</c> the code fix rewrites the call to.</summary>
        internal string FixMethodName { get; }

        /// <summary>True when the fix moves the key argument in front of the message.</summary>
        internal bool SwapsArguments { get; }
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
                return new Replacement(false, "Log.ErrorTo(channel, message)", "Error", false);
            case "Warning":
                return new Replacement(false, "Log.WarnTo(channel, message)", "Warn", false);
            case "Message":
                return new Replacement(false, "Log.InfoTo(channel, message)", "Info", false);
            case "ErrorOnce":
                return new Replacement(true, "Log.ErrorOnceTo(channel, key, message)", "ErrorOnce", true);
            case "WarningOnce":
                return new Replacement(true, "Log.WarnOnceTo(channel, key, message)", "WarnOnce", true);
            default:
                return null;
        }
    }
}
