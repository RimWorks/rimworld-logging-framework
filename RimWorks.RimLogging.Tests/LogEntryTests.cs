using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Capture;
using Xunit;

namespace RimWorks.RimLogging.Tests;

public class LogEntryTests
{
    [Fact]
    public void Construct_PopulatesAllFields()
    {
        DateTime ts = new DateTime(2026, 5, 20, 14, 23, 11, 234, DateTimeKind.Utc);
        SourceLocation src = new SourceLocation("Player.cs", 42, "Spawn");
        Dictionary<string, object?> ctx = new Dictionary<string, object?> { ["Hp"] = 5 };

        LogEntry e = new LogEntry
        {
            Timestamp = ts,
            Level = LogLevel.Info,
            Channel = "Cosmere.Roshar",
            MessageTemplate = "died at {Hp}hp",
            RenderedMessage = "died at 5hp",
            Context = ctx,
            Source = src,
            StackTrace = null,
            Exception = null,
        };

        Assert.Equal(ts, e.Timestamp);
        Assert.Equal(LogLevel.Info, e.Level);
        Assert.Equal("Cosmere.Roshar", e.Channel);
        Assert.Equal("died at {Hp}hp", e.MessageTemplate);
        Assert.Equal("died at 5hp", e.RenderedMessage);
        Assert.Same(ctx, e.Context);
        Assert.Equal(src, e.Source);
        Assert.Null(e.StackTrace);
        Assert.Null(e.Exception);
    }

    [Fact]
    public void Construct_ThrowsOnNullChannel()
    {
        SourceLocation src = new SourceLocation("Player.cs", 1, "Init");

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() =>
            new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Info,
                Channel = null!,
                MessageTemplate = "msg",
                RenderedMessage = "msg",
                Context = null,
                Source = src,
                StackTrace = null,
                Exception = null,
            });

        Assert.Equal("Channel", ex.ParamName);
    }

    [Fact]
    public void Construct_CoalescesNullTemplatesToEmpty()
    {
        SourceLocation src = new SourceLocation("Player.cs", 1, "Init");

        LogEntry e = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = LogLevel.Info,
            Channel = "Test",
            MessageTemplate = null!,
            RenderedMessage = null!,
            Context = null,
            Source = src,
            StackTrace = null,
            Exception = null,
        };

        Assert.Equal(string.Empty, e.MessageTemplate);
        Assert.Equal(string.Empty, e.RenderedMessage);
    }
}
