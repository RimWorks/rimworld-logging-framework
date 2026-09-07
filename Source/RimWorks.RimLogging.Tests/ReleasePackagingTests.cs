using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace RimWorks.RimLogging.Tests;

/// <summary>
/// Guards the release zip's file list. Textures was missing from it for several releases, so
/// anyone installing from GitHub got a viewer that could not find its own button art.
/// </summary>
public class ReleasePackagingTests
{
    // RimWorld loads these from the mod root by convention. Assemblies, Concord and Harmony
    // are build output, so they are not in the repo and cannot be checked this way.
    private static readonly string[] ModContentDirectories =
    {
        "About", "Defs", "Languages", "Textures", "Patches", "Sounds",
    };

    [Fact]
    public void TheReleaseZipShipsEveryModContentDirectory()
    {
        string root = RepoRoot();
        string[] shipped = ShippedPaths(File.ReadAllText(Path.Combine(root, "release.config.mjs")));

        string[] missing = ModContentDirectories
            .Where(d => Directory.Exists(Path.Combine(root, d)))
            .Where(d => !shipped.Contains(d))
            .ToArray();

        Assert.True(missing.Length == 0,
            $"release.config.mjs does not copy: {string.Join(", ", missing)}");
    }

    [Fact]
    public void TheShipListIsReadable()
    {
        Assert.Contains("About", ShippedPaths(File.ReadAllText(Path.Combine(RepoRoot(), "release.config.mjs"))));
    }

    private static string[] ShippedPaths(string config)
    {
        Match cp = Regex.Match(config, @"cp -r (?<paths>.+?) dist/RimLogging/");
        Assert.True(cp.Success, "the release config no longer has a 'cp -r ... dist/RimLogging/' step");
        return cp.Groups["paths"].Value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    }

    private static string RepoRoot()
    {
        DirectoryInfo? dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "release.config.mjs")))
            dir = dir.Parent;

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
