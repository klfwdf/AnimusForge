using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public partial class ShoutBehavior
{
    private AnimusForge.Refactor.Adapters.ScenePersonaPreparationAdapter _scenePersonaPreparation;
    private AnimusForge.Refactor.Adapters.ScenePersonaPreparationAdapter ScenePersonaPreparation => _scenePersonaPreparation ??= new AnimusForge.Refactor.Adapters.ScenePersonaPreparationAdapter(
        _conversationGameThreadDispatcher, NativeAdmissions, () => ReferenceEquals(CurrentInstance, this), () => _sceneConversationEpoch, NativeConversationPersonaGenerationWaitTimeoutMs);

    private Task<bool> EnsureNativeConversationPersonaReadyAsync(NativeConversationAdmission admission, Action<string> onStreamText)
    {
        return ScenePersonaPreparation.EnsureNativeConversationPersonaReadyAsync(admission, onStreamText);
    }

    internal sealed class ScenePersonaPreparationScope
    {
        internal Mission Mission;
        internal long Generation;
        internal int Session, Epoch;
        internal MyBehavior PersonaOwner;
        internal NpcDataPacket[] Candidates;
    }

    private bool IsScenePersonaScopeCurrent(ScenePersonaPreparationScope scope)
    {
        return ScenePersonaPreparation.IsScenePersonaScopeCurrent(scope);
    }

    internal sealed class ScenePersonaCandidate
    {
        internal Hero Hero;
        internal bool Generate;
    }

    private Task EnsurePersonaForCandidatesAsync(List<NpcDataPacket> candidates, Dictionary<int, Hero> resolvedHeroes)
    {
        return ScenePersonaPreparation.EnsurePersonaForCandidatesAsync(candidates, resolvedHeroes);
    }
}
