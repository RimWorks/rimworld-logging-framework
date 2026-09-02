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

internal static class LogBundleShare {
    /// <summary>Fire-and-forget from the UI; the body catches everything, so the task never faults.</summary>
    public static async Task Upload(ViewerLogSink sink, LogViewerState state, Action invalidate) {
        state.Uploading = true;
        invalidate();
        try {
            IReadOnlyList<LogEntry> entries = sink.Snapshot();
            BundlePayload payload = BundlerSessionFactory.BuildForRunningSession(entries);
            PublishResult result = await BundleUploadCoordinator
                .Upload(payload, LoggingMod.Settings.ToPublishOptions()).ConfigureAwait(false);
            if (result.Success && !string.IsNullOrEmpty(result.Url)) {
                GUIUtility.systemCopyBuffer = result.Url;
                Messages.Message(
                    (string)"CRL_LogViewer_BundleShared".Translate(result.Url!.Named("URL")),
                    MessageTypeDefOf.PositiveEvent,
                    false
                );
            }
            else {
                Log.Error($"Bug bundle upload failed: {result.ErrorMessage ?? "(no error message)"}");
            }
        }
        catch (Exception ex) {
            Log.Error($"Bug bundle upload failed: {ex}");
        }
        finally {
            state.Uploading = false;
            invalidate();
        }
    }
}
