using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimWorks.RimLogging.Tests;

/// <summary>
/// RimLogging's own messages must name their channel. A default-channel overload files them
/// under "default", which also costs them their mod_id, since the self stamp keys off the channel.
/// </summary>
public class SelfChannelUsageTests
{
    // Log.Info( but not Log.InfoTo( and not something.Log.Info(
    private static readonly Regex DefaultChannelCall = new Regex(
        @"(?<![.\w])Log\.(Trace|Debug|Info|Warn|Error|Fatal)\s*\(",
        RegexOptions.Compiled);

    // real calls only. nameof(Verse.Log.Error) and Verse.Log.Messages have no paren after.
    private static readonly Regex VerseLogWrite = new Regex(
        @"Verse\.Log\.(Message|Warning|Error)\s*\(",
        RegexOptions.Compiled);

    /// <summary>The one file that writes to Verse.Log on purpose, pushing entries back into
    /// vanilla's own buffer so its log window still works.</summary>
    private const string WritebackFile = "VanillaBufferWriteback.cs";

    private static readonly string[] ProductionProjects =
    {
        "RimWorks.RimLogging",
        "RimWorks.RimLogging.Patches.Concord",
        "RimWorks.RimLogging.Patches.Harmony",
    };

    [Fact]
    public void NoInternalCallUsesADefaultChannelOverload()
    {
        List<string> offenders = new List<string>();

        foreach (string file in ProductionSources())
        {
            // Log.cs declares the overloads, so every match in it is a definition
            if (Path.GetFileName(file) == "Log.cs") continue;

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (DefaultChannelCall.IsMatch(lines[i]))
                {
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void NoInternalCodeWritesToVerseLog()
    {
        List<string> offenders = new List<string>();

        foreach (string file in ProductionSources())
        {
            if (Path.GetFileName(file) == WritebackFile) continue;

            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                if (VerseLogWrite.IsMatch(lines[i]))
                {
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}  {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void TheWritebackFileStillExists_SoItsExemptionCannotGoStale()
    {
        Assert.Contains(ProductionSources(), f => Path.GetFileName(f) == WritebackFile);
    }

    private static IEnumerable<string> ProductionSources()
    {
        string root = RepositoryRoot();

        foreach (string project in ProductionProjects)
        {
            string dir = Path.Combine(root, "Source", project);
            Assert.True(Directory.Exists(dir), $"missing project directory: {dir}");

            foreach (string file in Directory.EnumerateFiles(dir, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                yield return file;
            }
        }
    }

    // fails loudly rather than skipping, so the guard cannot quietly stop running
    private static string RepositoryRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !dir.EnumerateFiles("RimWorks.RimLogging.sln").Any())
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir.FullName;
    }
}
