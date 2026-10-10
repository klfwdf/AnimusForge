using System;
using System.Collections.Concurrent;
using TaleWorlds.Library;

namespace AnimusForge;

// Manual TTS tests have no Scene owner. Display their failures on the existing application tick.
internal static class TtsTestFeedback
{
    private static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();

    internal static void ReportFailure(TtsEngine.PlaybackRequest request, string reason)
    {
        if (request == null || request.IsCancellationRequested) { return; }
        Pending.Enqueue(() =>
        {
            // StopPlayback / game teardown can invalidate an already posted failure.
            if (request.IsCancellationRequested) { return; }
            InformationManager.DisplayMessage(new InformationMessage("[TTS] 测试语音失败：" + reason, Color.FromUint(4294901760u)));
        });
    }

    internal static void OnApplicationTick()
    {
        if (!Pending.TryDequeue(out Action show)) { return; }
        try { show(); }
        catch (Exception ex) { Logger.Log("TtsTestFeedback", "[ERROR] Display failed: " + ex.GetType().Name); }
    }
}
