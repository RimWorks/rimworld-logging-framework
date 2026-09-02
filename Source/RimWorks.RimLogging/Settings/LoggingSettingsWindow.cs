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
        if (l.ButtonTextLabeled("CRL_Settings_Publisher".Translate(), PublisherLabel(s)))
        {
            Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
            {
                new FloatMenuOption("CRL_Settings_Publisher_Docbin".Translate(), () => s.publisher = "docbin"),
                new FloatMenuOption("CRL_Settings_Publisher_Gist".Translate(), () => s.publisher = "gist"),
            }));
        }

        l.Gap();
        l.Label("CRL_Settings_DocbinUrl".Translate());
        s.docbinUrl = l.TextEntry(s.docbinUrl);

        l.Gap();
        l.Label("CRL_Settings_DocbinApiKey".Translate());
        s.docbinApiKey = l.TextEntry(s.docbinApiKey);
        l.Label("CRL_Settings_DocbinApiKey_Note".Translate());

        l.Gap();
        if (Bundle.DocbinVisibility.CanChoose(s.docbinApiKey))
        {
            if (l.ButtonTextLabeled("CRL_Settings_DocbinVisibility".Translate(), VisibilityLabel(s)))
            {
                Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new FloatMenuOption("CRL_Settings_Visibility_Unlisted".Translate(),
                        () => s.docbinVisibility = Bundle.DocbinVisibility.Unlisted),
                    new FloatMenuOption("CRL_Settings_Visibility_Public".Translate(),
                        () => s.docbinVisibility = Bundle.DocbinVisibility.Public),
                }));
            }
        }
        else
        {
            l.Label("CRL_Settings_DocbinVisibility".Translate() + ": " + "CRL_Settings_Visibility_Public".Translate());
            l.Label("CRL_Settings_DocbinVisibility_AnonNote".Translate());
        }

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
            s.publisher = LoggingSettingsDefaults.Publisher;
            s.docbinUrl = LoggingSettingsDefaults.DocbinUrl;
            s.docbinApiKey = LoggingSettingsDefaults.DocbinApiKey;
            s.docbinVisibility = LoggingSettingsDefaults.DocbinVisibility;
            s.logViewerCombinedDetail = false;
        }

        l.End();
    }

    private static string VisibilityLabel(LoggingSettings s)
        => (Bundle.DocbinVisibility.Effective(true, s.docbinVisibility) == Bundle.DocbinVisibility.Public
            ? "CRL_Settings_Visibility_Public"
            : "CRL_Settings_Visibility_Unlisted").Translate();

    private static string PublisherLabel(LoggingSettings s)
        => (Bundle.BundleUploadCoordinator.UsesGist(s.publisher)
            ? "CRL_Settings_Publisher_Gist"
            : "CRL_Settings_Publisher_Docbin").Translate();

    /// <summary>
    /// Uploads the current log buffer through the configured publisher. Runs async and
    /// marshals the resulting URL or error back to the main thread to show it.
    /// </summary>
    /// <param name="s">The settings supplying the publisher choice and its credentials.</param>
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
            PublishResult result = await BundleUploadCoordinator.Upload(payload, s.ToPublishOptions());

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
