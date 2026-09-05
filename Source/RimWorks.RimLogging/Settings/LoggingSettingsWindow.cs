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
    private const float TabHeight = 32f;
    private const float TabInset = 10f;
    private const float ScrollbarWidth = 20f;

    private static readonly Color SubtleText = new Color(0.62f, 0.66f, 0.68f);

    private enum Tab
    {
        Destinations,
        Capture,
        Reports,
    }

    private static Tab tab = Tab.Destinations;
    private static Vector2 scroll;
    private static float contentHeight;

    /// <summary>Draws the tab strip and the selected page, mutating <paramref name="s"/> directly.</summary>
    /// <param name="s">The settings instance to display and edit.</param>
    /// <param name="rect">The rect to draw the UI within.</param>
    public static void Render(LoggingSettings s, Rect rect)
    {
        Rect body = new Rect(rect.x, rect.y + TabHeight, rect.width, rect.height - TabHeight);
        Widgets.DrawMenuSection(body);
        TabDrawer.DrawTabs(body, BuildTabs());

        Rect inner = body.ContractedBy(TabInset);
        // never shorter than the visible area: Listing_Standard wraps to a new column once its rect
        // fills, so a short view rect pushes every control after the first off-screen
        float pageHeight = Mathf.Max(inner.height, contentHeight);
        Rect view = new Rect(0f, 0f, inner.width - ScrollbarWidth, pageHeight);
        Widgets.BeginScrollView(inner, ref scroll, view);

        Listing_Standard l = new();
        l.Begin(view);
        switch (tab)
        {
            case Tab.Capture: DrawCapture(s, l); break;
            case Tab.Reports: DrawReports(s, l); break;
            default: DrawDestinations(s, l); break;
        }
        contentHeight = l.CurHeight;
        l.End();

        Widgets.EndScrollView();
    }

    private static List<TabRecord> BuildTabs() => new()
    {
        new TabRecord("CRL_Settings_Tab_Destinations".Translate(), () => Select(Tab.Destinations), tab == Tab.Destinations),
        new TabRecord("CRL_Settings_Tab_Capture".Translate(), () => Select(Tab.Capture), tab == Tab.Capture),
        new TabRecord("CRL_Settings_Tab_Reports".Translate(), () => Select(Tab.Reports), tab == Tab.Reports),
    };

    // each page has its own length, so a carried-over offset can land past the bottom of the next one
    private static void Select(Tab next)
    {
        tab = next;
        scroll = Vector2.zero;
    }

    private static void DrawDestinations(LoggingSettings s, Listing_Standard l)
    {
        DrawSinks(s, l);

        l.Gap();
        l.Label("CRL_Settings_LogDir".Translate());
        s.logDirectory = l.TextEntry(s.logDirectory);

        l.Gap();
        l.Label("CRL_Settings_Retention".Translate() + ": " + s.retentionCount);
        s.retentionCount = (int)l.Slider(s.retentionCount, 1, 50);
    }

    private static void DrawCapture(LoggingSettings s, Listing_Standard l)
    {
        l.Label("CRL_Settings_GlobalMinLevel".Translate() + ": " + s.globalMinLevel);
        if (l.ButtonText(s.globalMinLevel.ToString()))
        {
            List<FloatMenuOption> opts = new();
            foreach (LogLevel lv in Enum.GetValues(typeof(LogLevel)))
                opts.Add(new FloatMenuOption(lv.ToString(), () => s.globalMinLevel = lv));
            Find.WindowStack.Add(new FloatMenu(opts));
        }

        l.Gap();
        l.CheckboxLabeled("CRL_Settings_CaptureStackTraces".Translate(), ref s.captureStackTraces);

        l.Gap();
        l.CheckboxLabeled("CRL_Settings_LogViewerCombinedDetail".Translate(), ref s.logViewerCombinedDetail);

        l.Gap();
        if (l.ButtonText("CRL_Settings_Reset".Translate())) Reset(s);
    }

    private static void DrawReports(LoggingSettings s, Listing_Standard l)
    {
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
        l.Label("CRL_Settings_GitHubToken".Translate());
        s.githubToken = l.TextEntry(s.githubToken);
        l.Label("CRL_Settings_GitHubToken_Note".Translate());

        l.Gap();
        if (l.ButtonText("CRL_Settings_UploadBundle".Translate())) _ = StartUpload(s);
    }

    private static void Reset(LoggingSettings s)
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
        s.sinkOverrideNames.Clear();
        s.sinkOverrideStates.Clear();
        SinkLoader.Reload();
    }

    /// <summary>A checkbox per SinkDef. Toggling one records an override and rebuilds the sink set.</summary>
    private static void DrawSinks(LoggingSettings s, Listing_Standard l)
    {
        foreach (SinkDef def in DefDatabase<SinkDef>.AllDefsListForReading)
        {
            bool enabled = SinkToggles.IsEnabled(def.defName, def.enabledByDefault,
                s.sinkOverrideNames, s.sinkOverrideStates);
            bool wanted = enabled;
            l.CheckboxLabeled(def.LabelCap.NullOrEmpty() ? def.defName : def.LabelCap, ref wanted, def.description);
            if (!def.description.NullOrEmpty())
            {
                GUI.color = SubtleText;
                l.Label(def.description);
                GUI.color = Color.white;
            }
            l.Gap(4f);

            if (wanted == enabled) continue;
            SinkToggles.Set(def.defName, wanted, s.sinkOverrideNames, s.sinkOverrideStates);
            SinkLoader.Reload();
        }
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
