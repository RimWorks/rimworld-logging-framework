using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;
using RimWorks.RimLogging.Sinks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.RimLogging.Settings;

/// <summary>Draws the RimWorld mod settings UI for the logging framework, editing the settings in place.</summary>
public static class LoggingSettingsWindow
{
    /// <summary>Renders controls for global min level, log directory, retention count, proxy URL, and a reset button, mutating <paramref name="s"/> directly.</summary>
    /// <param name="s">The settings instance to display and edit.</param>
    /// <param name="rect">The rect to draw the UI within.</param>
    public static void Render(LoggingSettings s, Rect rect)
    {
        Listing_Standard l = new();
        l.Begin(rect);

        l.Label("CRL_Settings_GlobalMinLevel".Translate() + ": " + s.globalMinLevel);
        if (l.ButtonText(s.globalMinLevel.ToString()))
        {
            List<FloatMenuOption> opts = new();
            foreach (LogLevel lv in Enum.GetValues(typeof(LogLevel)))
                opts.Add(new FloatMenuOption(lv.ToString(), () => s.globalMinLevel = lv));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        l.Gap();
        l.Label("CRL_Settings_LogDir".Translate());
        s.logDirectory = l.TextEntry(s.logDirectory);

        l.Gap();
        l.Label("CRL_Settings_Retention".Translate() + ": " + s.retentionCount);
        s.retentionCount = (int)l.Slider(s.retentionCount, 1, 50);

        l.Gap();
        l.Label("CRL_Settings_ProxyUrl".Translate());
        s.proxyUrl = l.TextEntry(s.proxyUrl);

        l.Gap();
        l.CheckboxLabeled("CRL_Settings_CaptureStackTraces".Translate(), ref s.captureStackTraces);

        l.Gap();
        l.CheckboxLabeled("CRL_Settings_LogViewerCombinedDetail".Translate(), ref s.logViewerCombinedDetail);

        l.Gap();
        l.Label("CRL_Settings_GitHubToken".Translate());
        s.githubToken = l.TextEntry(s.githubToken);
        l.Label("CRL_Settings_GitHubToken_Note".Translate());

        l.Gap();
        if (l.ButtonText("CRL_Settings_UploadBundle".Translate()))
        {
            _ = StartUpload(s);
        }

        l.Gap();
        if (l.ButtonText("CRL_Settings_Reset".Translate()))
        {
            s.globalMinLevel = LoggingSettingsDefaults.GlobalMinLevel;
            s.logDirectory = LogDirectory.Default;
            s.retentionCount = LoggingSettingsDefaults.RetentionCount;
            s.proxyUrl = LoggingSettingsDefaults.ProxyUrl;
            s.captureStackTraces = LoggingSettingsDefaults.CaptureStackTraces;
            s.githubToken = LoggingSettingsDefaults.GitHubToken;
            s.logViewerCombinedDetail = false;
        }

        l.End();
    }

    /// <summary>
    /// Bundles the current in-memory log buffer and uploads it via the bundle proxy, relaying the user's
    /// GitHub PAT when one is set. Runs asynchronously and reports the resulting gist URL or error to the
    /// player; the result message is marshaled back to the main thread before being shown.
    /// </summary>
    /// <param name="s">The settings supplying the proxy URL and optional GitHub token.</param>
    private static async Task StartUpload(LoggingSettings s)
    {
        try
        {
            MemoryLogSink? memory = BundleUploadCoordinator.FindMemorySink(SinkRegistry.Snapshot());
            if (memory == null)
            {
                Messages.Message("CRL_Settings_UploadNoBuffer".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }

            BundlePayload payload = BundlerSessionFactory.BuildForRunningSession(memory.Entries);
            string? token = string.IsNullOrWhiteSpace(s.githubToken) ? null : s.githubToken;
            ProxyClient client = new ProxyClient(s.proxyUrl, githubToken: token);
            ProxyResult result = await client.UploadAsync(payload);

            string message = BundleUploadCoordinator.DescribeResult(result);
            MessageTypeDef type = result.Success ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NegativeEvent;
            LongEventHandler.ExecuteWhenFinished(() => Messages.Message(message, type, false));
        }
        catch (Exception ex)
        {
            string message = $"Bundle upload failed: {ex.Message}";
            LongEventHandler.ExecuteWhenFinished(() => Messages.Message(message, MessageTypeDefOf.NegativeEvent, false));
        }
    }
}
