namespace RimWorks.RimLogging.Viewer;

/// <summary>
/// Tells a click on a link row apart from a drag across it. The url is a selectable text field
/// now, so copying it has to wait for the release and only fire when the pointer stayed put.
/// </summary>
public static class LinkClickPolicy
{
    /// <summary>How far a press may drift and still count as a click rather than a drag.</summary>
    public const float MaxTravel = 3f;

    /// <summary>True when the pointer barely moved between press and release.</summary>
    /// <param name="dx">Pointer movement on x since the press.</param>
    /// <param name="dy">Pointer movement on y since the press.</param>
    public static bool IsClick(float dx, float dy) => (dx * dx) + (dy * dy) <= MaxTravel * MaxTravel;
}
