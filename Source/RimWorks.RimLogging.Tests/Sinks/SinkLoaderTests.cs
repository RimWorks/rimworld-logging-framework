using System;
using System.Collections.Generic;
using RimWorks.RimLogging;
using RimWorks.RimLogging.Sinks;
using Xunit;

namespace RimWorks.RimLogging.Tests.Sinks;

public class SinkLoaderTests
{
    private static readonly IReadOnlyDictionary<Type, Func<LogLevel, ILogSink?>> EmptyFactories =
        new Dictionary<Type, Func<LogLevel, ILogSink?>>();

    private static SinkSpec Spec(string sinkClass, bool enabled = true, LogLevel minLevel = LogLevel.Trace) =>
        new SinkSpec("TestDef", sinkClass, minLevel, enabled);

    [Fact]
    public void Build_SkipsSpecsWithEnabledByDefaultFalse()
    {
        List<string> warnings = [];
        List<ILogSink> result = SinkPlan.Build(
            [Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!, enabled: false)],
            EmptyFactories,
            warnings.Add);

        Assert.Empty(result);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Build_SkipsAndWarnsOnUnresolvableType()
    {
        List<string> warnings = [];
        List<ILogSink> result = SinkPlan.Build(
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
        List<ILogSink> result = SinkPlan.Build(
            [Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!)],
            EmptyFactories,
            _ => { });

        ILogSink sink = Assert.Single(result);
        Assert.IsType<TestParameterlessSink>(sink);
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

        List<ILogSink> result = SinkPlan.Build(
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
        List<ILogSink> result = SinkPlan.Build(
            [
                Spec("Some.Bogus.Type, NonexistentAssembly"),
                Spec(typeof(TestParameterlessSink).AssemblyQualifiedName!),
            ],
            EmptyFactories,
            warnings.Add);

        Assert.IsType<TestParameterlessSink>(Assert.Single(result));
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

        List<ILogSink> result = SinkPlan.Build(
            [Spec(typeof(MemoryLogSink).AssemblyQualifiedName!, minLevel: LogLevel.Info)],
            factories,
            _ => { });

        MemoryLogSink sink = Assert.IsType<MemoryLogSink>(Assert.Single(result));
        Assert.Equal(LogLevel.Info, sink.MinLevel);
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
