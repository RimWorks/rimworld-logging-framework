using System;
using System.Collections.Generic;
using System.IO;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class SinkLoaderTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static readonly IReadOnlyDictionary<Type, Func<LogLevel, ILogSink?>> EmptyFactories =
        new Dictionary<Type, Func<LogLevel, ILogSink?>>();

    private static SinkSpec Spec(string sinkClass, bool enabled = true, LogLevel minLevel = LogLevel.Trace) =>
        new SinkSpec("TestDef", sinkClass, minLevel, enabled);

    [Fact]
    public void Build_SkipsSpecsThatResolvedToDisabled()
    {
        List<string> warnings = [];
        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!, enabled: false)],
            EmptyFactories,
            warnings.Add);

        Assert.Empty(result);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Build_UserOverride_DecidesWhetherASinkOffByDefaultIsBuilt()
    {
        // the composition SinkLoader.LoadFrom does: def default in, override applied, spec out
        List<string> names = [];
        List<bool> states = [];
        string sinkClass = typeof(TestParameterlessSink).AssemblyQualifiedName!;

        SinkSpec offByDefault = new SinkSpec("RollingJson", sinkClass, LogLevel.Trace,
            SinkToggles.IsEnabled("RollingJson", false, names, states));
        Assert.Empty(SinkPlan.Build([offByDefault], EmptyFactories, _ => { }));

        SinkToggles.Set("RollingJson", true, names, states);
        SinkSpec turnedOn = new SinkSpec("RollingJson", sinkClass, LogLevel.Trace,
            SinkToggles.IsEnabled("RollingJson", false, names, states));

        Assert.Single(SinkPlan.Build([turnedOn], EmptyFactories, _ => { }));
    }

    [Fact]
    public void Build_SkipsAndWarnsOnUnresolvableType()
    {
        List<string> warnings = [];
        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [Spec("Some.Bogus.Type, NonexistentAssembly")],
            EmptyFactories,
            warnings.Add);

        Assert.Empty(result);
        string warning = Assert.Single(warnings);
        Assert.Contains("unknown type", warning);
    }

    [Fact]
    public void Build_RegistersSinkViaParameterlessFallback()
    {
        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!)],
            EmptyFactories,
            _ => { });

        KeyValuePair<string, ILogSink> sink = Assert.Single(result);
        Assert.Equal("TestDef", sink.Key);
        Assert.IsType<TestParameterlessSink>(sink.Value);
    }

    [Fact]
    public void Build_DispatchesThroughFactoryMapWithSpecMinLevel()
    {
        LogLevel? captured = null;
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = new()
        {
            [typeof(TestParameterlessSink)] = minLevel =>
            {
                captured = minLevel;
                return new TestParameterlessSink();
            },
        };

        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!, minLevel: LogLevel.Warn)],
            factories,
            _ => { });

        Assert.Single(result);
        Assert.Equal(LogLevel.Warn, captured);
    }

    [Fact]
    public void Build_ContinuesAfterFailedSpecAndRegistersLaterOnes()
    {
        List<string> warnings = [];
        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [
                Spec("Some.Bogus.Type, NonexistentAssembly"),
                Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!),
            ],
            EmptyFactories,
            warnings.Add);

        Assert.IsType<TestParameterlessSink>(Assert.Single(result).Value);
        Assert.Single(warnings);
    }

    [Fact]
    public void TryCreate_HonorsLogLevelConstructorWhenNoFactory()
    {
        ILogSink? sink = SinkPlan.TryCreate(
            Spec(typeof(TestLevelCtorSink).AssemblyQualifiedName!, minLevel: LogLevel.Error),
            EmptyFactories,
            _ => { });

        TestLevelCtorSink created = Assert.IsType<TestLevelCtorSink>(sink);
        Assert.Equal(LogLevel.Error, created.MinLevel);
    }

    [Fact]
    public void TryCreate_PropagatesFactoryNullAsSkip()
    {
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = new()
        {
            [typeof(TestParameterlessSink)] = _ => null,
        };

        ILogSink? sink = SinkPlan.TryCreate(
            Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!),
            factories,
            _ => { });

        Assert.Null(sink);
    }

    [Fact]
    public void Build_RegistersMemorySinkViaFactoryWithSpecMinLevel()
    {
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = new()
        {
            [typeof(MemoryLogSink)] = minLevel => new MemoryLogSink(minLevel: minLevel),
        };

        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [Spec(typeof(MemoryLogSink).AssemblyQualifiedName!, minLevel: LogLevel.Info)],
            factories,
            _ => { });

        MemoryLogSink sink = Assert.IsType<MemoryLogSink>(Assert.Single(result).Value);
        Assert.Equal(LogLevel.Info, sink.MinLevel);
    }

    [Fact]
    public void Build_SpecWithPrintLevel_GivesTheTextSinkThatLevel()
    {
        using RollingTextFileSink sink = BuildTextSink(LogLevel.Error);
        Assert.Equal(LogLevel.Error, sink.PrintStackAt);
    }

    [Fact]
    public void Build_SpecWithoutPrintLevel_LeavesTheTextSinkFollowingTheGlobalLevel()
    {
        using RollingTextFileSink sink = BuildTextSink(null);
        Assert.Null(sink.PrintStackAt);
    }

    [Fact]
    public void Build_SpecWithPrintLevel_StillBuildsASinkTypeWithoutOne()
    {
        List<string> warnings = [];
        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [new SinkSpec("TestDef", typeof(TestParameterlessSink).AssemblyQualifiedName!, LogLevel.Trace, true, LogLevel.Error)],
            EmptyFactories,
            warnings.Add);

        Assert.IsType<TestParameterlessSink>(Assert.Single(result).Value);
        Assert.Empty(warnings);
    }

    private RollingTextFileSink BuildTextSink(LogLevel? printStackAt)
    {
        Dictionary<Type, Func<LogLevel, ILogSink?>> factories = new()
        {
            [typeof(RollingTextFileSink)] = minLevel => new RollingTextFileSink(_tempDir, 1, minLevel),
        };

        List<KeyValuePair<string, ILogSink>> result = SinkPlan.Build(
            [new SinkSpec("TestDef", typeof(RollingTextFileSink).AssemblyQualifiedName!, LogLevel.Trace, true, printStackAt)],
            factories,
            _ => { });

        return Assert.IsType<RollingTextFileSink>(Assert.Single(result).Value);
    }

    private sealed class TestParameterlessSink : ILogSink
    {
        public string Name => "TestParameterless";
        public LogLevel MinLevel => LogLevel.Trace;
        public void Write(LogEntry entry) { }
        public void Flush() { }
        public void Dispose() { }
    }

    private sealed class TestLevelCtorSink : ILogSink
    {
        public TestLevelCtorSink(LogLevel minLevel) => MinLevel = minLevel;
        public string Name => "TestLevelCtor";
        public LogLevel MinLevel { get; }
        public void Write(LogEntry entry) { }
        public void Flush() { }
        public void Dispose() { }
    }
}
