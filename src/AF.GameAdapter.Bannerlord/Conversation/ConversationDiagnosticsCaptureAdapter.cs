using System;
using System.Text;

namespace AnimusForge;

internal delegate bool ConversationSpeechQueueSnapshot(out int count, out bool workerRunning);

// Watchdog frequency only; TryEnter never blocks the diagnostic thread.
internal sealed class ConversationDiagnosticsCaptureAdapter
{
    private readonly Func<bool> _tryEnter;
    private readonly Action _exit;
    private readonly Action<StringBuilder> _history;
    private readonly Func<int> _queueCount;
    private readonly ConversationSpeechQueueSnapshot _speech;
    internal ConversationDiagnosticsCaptureAdapter(Func<bool> tryEnter, Action exit,
        Action<StringBuilder> history, Func<int> queueCount, ConversationSpeechQueueSnapshot speech)
    { _tryEnter=tryEnter; _exit=exit; _history=history; _queueCount=queueCount; _speech=speech; }
    internal static string Snapshot(Action<StringBuilder> nativeHistory, Func<ConversationDiagnosticsCaptureAdapter> sceneLookup)
    {
        var sb=new StringBuilder();
        try { nativeHistory(sb); } catch { sb.Append("nativeHistory=unavailable"); }
        try { var scene=sceneLookup(); if(scene!=null) scene.Append(sb); else sb.Append(" sceneHistory=instance_unavailable"); }
        catch { sb.Append(" sceneHistory=unavailable"); }
        return sb.ToString();
    }
    internal void Append(StringBuilder sb)
    {
        if(sb==null) return;
        bool taken=false;
        try { taken=_tryEnter(); if(taken) _history(sb); else sb.Append(" sceneHistory=busy"); }
        finally { if(taken) _exit(); }
        try { sb.Append(" mainThreadActions=").Append(_queueCount()); }
        catch { sb.Append(" mainThreadActions=unavailable"); }
        if(_speech(out int count,out bool running))
            sb.Append(" sceneSpeechQueue=").Append(count).Append(" sceneSpeechWorker=").Append(running?1:0);
        else sb.Append(" sceneSpeechQueue=busy");
    }
}
