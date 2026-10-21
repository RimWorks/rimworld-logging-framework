namespace RimWorks.RimLogging.Bundle;

/// <summary>Everything the upload path needs from settings, without depending on Verse.</summary>
internal sealed class PublishOptions
{
    /// <summary>Which publisher to use: <c>rimworks</c>, <c>docbin</c> or <c>gist</c>.</summary>
    internal string Publisher = "rimworks";

    /// <summary>The rimworks.org upload endpoint, used only by the rimworks publisher.</summary>
    internal string RimWorksUrl = string.Empty;

    /// <summary>The Docbin base URL.</summary>
    internal string DocbinUrl = string.Empty;

    /// <summary>The Docbin API key; empty means an anonymous paste.</summary>
    internal string DocbinApiKey = string.Empty;

    /// <summary>The visibility applied to authenticated Docbin pastes.</summary>
    internal string DocbinVisibility = "unlisted";

    /// <summary>The gist proxy endpoint, used only by the gist publisher.</summary>
    internal string ProxyUrl = string.Empty;

    /// <summary>The user's GitHub PAT, relayed by the gist publisher.</summary>
    internal string GitHubToken = string.Empty;
}
