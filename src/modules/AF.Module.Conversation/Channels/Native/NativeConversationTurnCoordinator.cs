using System;
using System.Threading.Tasks;

namespace AnimusForge;

// This is the only Native turn sequencer. A provider/commit exception propagates to
// the existing admission/UI owner; it must never cause another phase or a retry.
internal static class NativeConversationTurnCoordinator
{
    internal static async Task<string> RunAsync(INativeConversationTurnHost host)
    {
        if (host == null) throw new ArgumentNullException(nameof(host));
        NativeConversationTurnStep step = await host.PrepareAsync().ConfigureAwait(false);
        if (!step.CanContinue) return step.StopText;
        step = await host.BuildPromptAsync().ConfigureAwait(false);
        if (!step.CanContinue) return step.StopText;
        step = await host.ReceiveAndPresentAsync().ConfigureAwait(false);
        if (!step.CanContinue) return step.StopText;
        step = await host.PostprocessAndCommitAsync().ConfigureAwait(false);
        if (step.CanContinue) throw new InvalidOperationException("Native commit must return a terminal result.");
        return step.StopText;
    }
}

// Internal, same-DLL port. External sub-MOD DTOs and public signatures do not change.
internal interface INativeConversationTurnHost
{
    Task<NativeConversationTurnStep> PrepareAsync();
    Task<NativeConversationTurnStep> BuildPromptAsync();
    Task<NativeConversationTurnStep> ReceiveAndPresentAsync();
    Task<NativeConversationTurnStep> PostprocessAndCommitAsync();
}

internal readonly struct NativeConversationTurnStep
{
    // Default is stopped, not permission to run a side effect.
    internal bool CanContinue { get; }
    internal string StopText => _stopText ?? string.Empty;
    private readonly string _stopText;
    private NativeConversationTurnStep(bool canContinue, string stopText)
    { CanContinue = canContinue; _stopText = stopText; }
    internal static NativeConversationTurnStep Continue() => new NativeConversationTurnStep(true, "");
    internal static NativeConversationTurnStep Stop(string text) => new NativeConversationTurnStep(false, text);
}
