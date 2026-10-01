using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static AnimusForge.ShoutBehavior;
namespace AnimusForge;
public partial class ShoutBehavior
{
    private SceneExternalPromptCaptureAdapter CreateSceneExternalPromptCaptureAdapter()
        => new SceneExternalPromptCaptureAdapter(new SceneExternalPromptCapturePorts
        {
            StripScenePersonaBlocks = StripScenePersonaBlocks,
            ExtractTrustPromptBlock = ExtractTrustPromptBlock,
            GetSceneNpcHistoryNameForPrompt = GetSceneNpcHistoryNameForPrompt,
            BuildScenePresentNpcListBlockForPrompt = BuildScenePresentNpcListBlockForPrompt,
            BuildSceneMechanismPromptSection = BuildSceneMechanismPromptSection,
            HasPartyTransferRuleContext = HasPartyTransferRuleContext,
            AppendPlayerCustomPromptRuleToSystemPrompt = AppendPlayerCustomPromptRuleToSystemPrompt,
            BuildSceneSystemTopPromptIntroForSingle = BuildSceneSystemTopPromptIntroForSingle,
            BuildSceneUserRuntimeContextForSingle = BuildSceneUserRuntimeContextForSingle,
            BuildScenePublicHistorySection = BuildScenePublicHistorySection,
            SplitSceneExtraSections = SplitSceneExtraSections,
            BuildSceneSystemRuleBlock = BuildSceneSystemRuleBlock,
            BuildSceneSingleNpcTaskSystemBlock = BuildSceneSingleNpcTaskSystemBlock,
            GetSceneReplyLengthLimits = GetSceneReplyLengthLimits,
            SplitPersistedHeroHistorySections = SplitPersistedHeroHistorySections,
            TryGetKingdomIdOverrideFromAgent = TryGetKingdomIdOverrideFromAgent,
            BuildPreprocessExcludedRuleIdsForCurrentInteraction = BuildPreprocessExcludedRuleIdsForCurrentInteraction,
            GetPlayerDisplayNameForShout = GetPlayerDisplayNameForShout,
            BuildSceneActionPostprocessUserPrompt = BuildSceneActionPostprocessUserPrompt,
            BuildStrictSceneMessagesSystemPrompt = BuildStrictSceneMessagesSystemPrompt,
            BuildPersistedHeroHistoryContext = BuildPersistedHeroHistoryContext,
            CaptureVisibleSceneHistoryLinesForPrompt = CaptureVisibleSceneHistoryLinesForPrompt,
            SummonTargets = _sceneMovement.BuildSceneSummonPromptTargets,
            GuideTargets = _sceneMovement.BuildSceneGuidePromptTargets,
            SummonClosure = _sceneMovement.BuildSceneSummonClosurePromptInstruction,
            FollowControl = _sceneMovement.BuildSceneFollowControlPromptInstruction
        });
}
