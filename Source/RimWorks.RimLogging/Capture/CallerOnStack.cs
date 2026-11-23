using System.Diagnostics;

namespace RimWorks.RimLogging.Capture;

internal readonly struct CallerOnStack
{
    private CallerOnStack(bool live, StackTrace? walk)
    {
        Live = live;
        Walk = walk;
    }

    internal bool Live { get; }

    internal StackTrace? Walk { get; }

    public static implicit operator CallerOnStack(bool live) => new CallerOnStack(live, null);

    public static implicit operator CallerOnStack(StackTrace walk) => new CallerOnStack(true, walk);
}
