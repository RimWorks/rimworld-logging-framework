using System;
using System.Collections.Generic;
using RimWorks.RimLogging.Bundle;
using RimWorks.RimLogging.Settings;
using RimWorld;
using UnityEngine;
using Verse;
using LogEntry = RimWorks.RimLogging.LogEntry;

namespace RimWorks.RimLogging.Viewer;

internal static class LogBundleShare {
    public static async void Upload(ViewerLogSink sink, LogViewerState state, Action invalidate) {
        state.Uploading = true;
        invalidate();
        try {
            IReadOnlyList<LogEntry> entries = sink.Snapshot();
            BundlePayload payload = BundlerSessionFactory.BuildForRunningSession(entries);
            ProxyClient proxy = new ProxyClient(LoggingMod.Settings.proxyUrl);
            ProxyResult result = await proxy.UploadAsync(payload).ConfigureAwait(false);
            if (result.Success && !string.IsNullOrEmpty(result.GistUrl)) {
                GUIUtility.systemCopyBuffer = result.GistUrl;
                Messages.Message(
                    (string)"CRL_LogViewer_BundleShared".Translate(result.GistUrl!.Named("URL")),
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
