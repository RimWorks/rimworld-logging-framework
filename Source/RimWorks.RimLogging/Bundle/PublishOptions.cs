namespace RimWorks.RimLogging.Bundle;

/// <summary>Everything the upload path needs from settings, without depending on Verse.</summary>
internal sealed class PublishOptions
{
    /// <summary>Which publisher to use: <c>docbin</c> or <c>gist</c>.</summary>
    internal string Publisher = "docbin";

    /// <summary>The Docbin base URL.</summary>
    internal string DocbinUrl = "";

    /// <summary>The Docbin API key; empty means an anonymous paste.</summary>
    internal string DocbinApiKey = "";

    /// <summary>The visibility applied to authenticated Docbin pastes.</summary>
    internal string DocbinVisibility = "unlisted";

    /// <summary>The gist proxy endpoint, used only by the gist publisher.</summary>
    internal string ProxyUrl = "";

    /// <summary>The user's GitHub PAT, relayed by the gist publisher.</summary>
    internal string GitHubToken = "";
}
