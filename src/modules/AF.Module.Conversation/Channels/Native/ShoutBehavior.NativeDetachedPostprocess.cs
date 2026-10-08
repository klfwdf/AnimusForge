using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private readonly NativeDetachedPostprocessApplicationAdapter NativeDetachedPostprocesses;
    internal sealed class NativeDetachedPostprocessSource
    {
        internal NativeDetachedPostprocessApplicationAdapter Application;
        internal ConversationManager Manager;
        internal int Token;
        internal Hero Hero;
        internal CharacterObject Character;
        internal int AgentIndex;
    }

    internal sealed class NativeDetachedPostprocessCompletion
    {
        internal NativeDetachedPostprocessSource Source;
        internal InteractionEnvelope Envelope;
        internal SceneActionPostprocessWorkItem Work;
        internal PostprocessNetworkRequest Network;
    }



    // Capture occurs once at the existing snapshot boundary. Weak keys do not prolong a session.
    internal static InteractionEnvelope BindNativeDetachedPostprocessSource(InteractionEnvelope envelope)
    { return CurrentInstance?.NativeDetachedPostprocesses.BindNativeDetachedPostprocessSource(envelope) ?? envelope; }

    private static bool IsNativeDetachedPostprocessCurrent(NativeDetachedPostprocessSource source, InteractionEnvelope envelope)
    { return source?.Application?.IsNativeDetachedPostprocessCurrent(source, envelope) == true; }

    private static LegacyInteractionPipelinePorts CreateNativeConversationDetachedPortsCore(IEnumerable<string> allowedTagFamilies, int topN, int maxActions)
    { return NativeDetachedPostprocessApplicationAdapter.CreateNativeConversationDetachedPortsCore(allowedTagFamilies, topN, maxActions, CreateNativeConversationDetachedRuleSelectorForExternal); }

    private static Task<PromptPackage> PrepareNativeDetachedPostprocessAsync(InteractionEnvelope envelope, RuleSelection selection, string rawReply, PostprocessContext context, ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners, CancellationToken cancellation)
    { return NativeDetachedPostprocessApplicationAdapter.PrepareNativeDetachedPostprocessAsync(envelope, selection, rawReply, context, owners, cancellation); }

    private static Task<ActionPlan> CompleteNativeDetachedPostprocessAsync(string raw, PostprocessContext context, ConditionalWeakTable<PostprocessContext, NativeDetachedPostprocessCompletion> owners, LegacyActionTagParser parser, CancellationToken cancellation)
    { return NativeDetachedPostprocessApplicationAdapter.CompleteNativeDetachedPostprocessAsync(raw, context, owners, parser, cancellation); }

    private static SceneActionPostprocessWorkItem PrepareNativeDetachedPostprocessWork(NativeDetachedPostprocessSource source, InteractionEnvelope envelope, RuleSelection selection, string reply)
    { return source.Application.CaptureWork(source.Hero, source.Character, source.AgentIndex, envelope, selection, reply); }
}
