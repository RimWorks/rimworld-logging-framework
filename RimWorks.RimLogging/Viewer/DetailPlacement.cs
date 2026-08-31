namespace RimWorks.RimLogging.Viewer;

/// <summary>Where the viewer draws the detail pane for the selected entry.</summary>
internal enum DetailPlacement {
    /// <summary>Full-width strip under the entry list, resizable. What vanilla's log window does.</summary>
    Bottom,

    /// <summary>Third column beside the entry list.</summary>
    Right,

    /// <summary>Its own window, which drags, resizes and closes independently.</summary>
    Popout,
}
