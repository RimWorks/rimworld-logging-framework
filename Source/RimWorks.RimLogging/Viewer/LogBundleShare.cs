using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorks.RimLogging.Bundle;
using RimWorks.RimLogging.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using LogEntry = RimWorks.RimLogging.LogEntry;

namespace RimWorks.RimLogging.Viewer;

internal static class LogBundleShare
{
    private static readonly BundleShareSession Session = new BundleShareSession();

    /// <summary>Fire-and-forget from the UI; the body catches everything, so the task never faults.</summary>
    public static async Task Upload(
        ViewerLogSink sink,
        LogViewerState state,
        Action invalidate,
        LogEntry? line = null)
    {
        state.Uploading = true;
        invalidate();
        try
        {
            IReadOnlyList<LogEntry> entries = sink.Snapshot();
            int lineIndex = ShareLink.IndexOf(entries, line);
            PublishResult result = await Send(entries).ConfigureAwait(false);
            if (result.Success && !string.IsNullOrEmpty(result.Url))
            {
                string url = ShareLink.WithLine(result.Url!, lineIndex);
                GUIUtility.systemCopyBuffer = url;
                Messages.Message(
                    (string)"CRL_LogViewer_BundleShared".Translate(url.Named("URL")),
                    MessageTypeDefOf.PositiveEvent,
                    false);
            }
            else
            {
                Log.ErrorTo(Log.SelfChannel, $"Bug bundle upload failed: {result.ErrorMessage ?? "(no error message)"}");
            }
        }
        catch (Exception ex)
        {
            Log.ErrorTo(Log.SelfChannel, $"Bug bundle upload failed: {ex}");
        }
        finally
        {
            state.Uploading = false;
            invalidate();
        }
    }

    private static async Task<PublishResult> Send(IReadOnlyList<LogEntry> entries)
    {
        PublishOptions options = LoggingMod.Settings.ToPublishOptions();

        if (!Session.CanAppend(entries))
        {
            BundlePayload payload = BundlerSessionFactory.BuildForRunningSession(entries);
            PublishResult created =
                await BundleUploadCoordinator.Upload(payload, options).ConfigureAwait(false);
            if (created.Success) Session.Record(created, created.Url ?? string.Empty, entries);
            return created;
        }

        List<LogEntry> pending = Session.Pending(entries);
        if (pending.Count == 0) return new PublishResult { Success = true, Url = Session.Url };

        List<BundlePayload.EntryDto> dtos = BundlerSessionFactory.BuildForRunningSession(pending).Entries;
        PublishResult appended = await BundleUploadCoordinator
            .Append(Session.Id!, Session.Token!, dtos, options).ConfigureAwait(false);
        if (!appended.Success) return appended;

        Session.Record(appended, Session.Url ?? string.Empty, entries);
        return new PublishResult { Success = true, Url = Session.Url };
    }
}
