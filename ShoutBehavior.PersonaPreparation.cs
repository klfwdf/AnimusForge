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
    private async Task<bool> EnsureNativeConversationPersonaReadyAsync(NativeConversationAdmission admission, Action<string> onStreamText)
    {
        MyBehavior personaOwner = MyBehavior.Instance;
        Task<NpcPersonaReadinessSnapshot> Observe(bool showHint = false, bool showFailure = false) =>
            RunNativeConversationMainThreadFuncAsync("native_persona_readiness", admission.NpcName, admission.AgentIndex, () =>
            {
                if (!IsNativeConversationAdmissionCurrent(admission, out _)) return null;
                NpcPersonaReadinessSnapshot state = MyBehavior.CaptureNpcPersonaReadiness(admission.Hero, personaOwner);
                if (state == null) return null;
                try
                {
                    if (showHint && state.Available && state.NeedsGeneration)
                        onStreamText?.Invoke("正在生成" + state.Name + "的个性与背景，请稍等。生成完成后会继续回复你的上一句话。");
                    if (showFailure)
                        onStreamText?.Invoke(state.Name + "的个性与背景暂时生成失败，请稍后再试。");
                }
                catch { } // Presentation failure must not replace the readiness decision.
                return state;
            }, (NpcPersonaReadinessSnapshot)null);

        NpcPersonaReadinessSnapshot state = await Observe(showHint: true).ConfigureAwait(false);
        if (state == null) return false;
        if (!state.Available || !state.NeedsGeneration) return true;
        Stopwatch watch = Stopwatch.StartNew();
        bool requested = false;
        while (watch.ElapsedMilliseconds < NativeConversationPersonaGenerationWaitTimeoutMs)
        {
            state = await Observe().ConfigureAwait(false);
            if (state == null) return false;
            if (!state.Available || !state.NeedsGeneration) return true;
            if (state.CoolingDown && !state.Active && requested) break;
            if (!requested || !state.Active)
            {
                requested = true;
                Task generation = MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync(admission.Hero, ignoreRetryCooldown: true);
                if (!await PersonaGenerationWaiter.WaitAsync(generation,
                    async () => await Observe().ConfigureAwait(false) != null,
                    () => watch.ElapsedMilliseconds < NativeConversationPersonaGenerationWaitTimeoutMs).ConfigureAwait(false))
                {
                    await Observe(showFailure: true).ConfigureAwait(false);
                    return false;
                }
                state = await Observe().ConfigureAwait(false);
                if (state == null) return false;
                if (!state.Available || !state.NeedsGeneration) return true;
                if (state.CoolingDown && !state.Active) break;
            }
            await Task.Delay(500).ConfigureAwait(false);
        }
        Logger.Log("NpcPersona", "[NativeConversation][WARN] persona unavailable before reply hero=" + state.HeroId + " waitMs=" + watch.ElapsedMilliseconds);
        await Observe(showFailure: true).ConfigureAwait(false);
        return false;
    }

    private sealed class ScenePersonaPreparationScope
    {
        internal Mission Mission;
        internal long Generation;
        internal int Session, Epoch;
        internal MyBehavior PersonaOwner;
        internal NpcDataPacket[] Candidates;
    }

    private bool IsScenePersonaScopeCurrent(ScenePersonaPreparationScope scope)
    {
        return scope != null && ReferenceEquals(CurrentInstance, this)
            && SaveRuntimeGuard.IsCurrentGeneration(scope.Generation)
            && ReferenceEquals(scope.Mission, Mission.Current)
            && scope.Session == _sceneHistorySessionId && scope.Epoch == _sceneConversationEpoch
            && ReferenceEquals(scope.PersonaOwner, MyBehavior.Instance);
    }

    private sealed class ScenePersonaCandidate
    {
        internal Hero Hero;
        internal bool Generate;
    }

    private async Task EnsurePersonaForCandidatesAsync(List<NpcDataPacket> candidates, Dictionary<int, Hero> resolvedHeroes)
    {
        if (candidates == null || candidates.Count == 0) return;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        int session = Volatile.Read(ref _sceneHistorySessionId), epoch = Volatile.Read(ref _sceneConversationEpoch);
        MyBehavior expectedPersonaOwner = MyBehavior.Instance;
        ScenePersonaPreparationScope scope = await RunNativeConversationMainThreadFuncAsync("scene_persona_scope", "scene", -1, () =>
        {
            if (!ReferenceEquals(CurrentInstance, this) || !SaveRuntimeGuard.IsCurrentGeneration(generation)
                || session != _sceneHistorySessionId || epoch != _sceneConversationEpoch
                || !ReferenceEquals(expectedPersonaOwner, MyBehavior.Instance)) return null;
            return new ScenePersonaPreparationScope
            {
                Mission = Mission.Current, Generation = generation, Session = _sceneHistorySessionId,
                Epoch = _sceneConversationEpoch, PersonaOwner = MyBehavior.Instance, Candidates = candidates.ToArray()
            };
        }, (ScenePersonaPreparationScope)null).ConfigureAwait(false);
        if (scope == null) return;
        var notified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (NpcDataPacket npc in scope.Candidates)
        {
            if (npc == null) continue;
            ScenePersonaCandidate prepared = await RunNativeConversationMainThreadFuncAsync("scene_persona_capture", npc.Name, npc.AgentIndex, () =>
            {
                if (!IsScenePersonaScopeCurrent(scope)) return null;
                if (!npc.IsHero)
                {
                    string key = (npc.UnnamedKey ?? "").Trim().ToLower();
                    if (!string.IsNullOrEmpty(key) && ShoutUtils.TryGetUnnamedPersonaByKey(key, out string up, out string ub))
                    {
                        if (!string.IsNullOrWhiteSpace(up)) npc.PersonalityDesc = up.Trim();
                        if (!string.IsNullOrWhiteSpace(ub)) npc.BackgroundDesc = ub.Trim();
                    }
                    return null;
                }
                Hero hero = null;
                if (resolvedHeroes != null) resolvedHeroes.TryGetValue(npc.AgentIndex, out hero);
                if (hero == null) return null;
                NpcPersonaReadinessSnapshot state = MyBehavior.CaptureNpcPersonaReadiness(hero, scope.PersonaOwner);
                if (state == null) return null;
                bool generate = string.IsNullOrWhiteSpace(state.Personality) && string.IsNullOrWhiteSpace(state.Background);
                if (generate && (string.IsNullOrWhiteSpace(state.HeroId) || notified.Add(state.HeroId))
                    && (!state.Available || (state.NeedsGeneration && !state.Active && !state.CoolingDown)))
                    InformationManager.DisplayMessage(new InformationMessage(MyBehavior.BuildNpcPersonaGenerationHintForExternal(hero), new Color(1f, 0.85f, 0.3f)));
                return new ScenePersonaCandidate { Hero = hero, Generate = generate };
            }, (ScenePersonaCandidate)null).ConfigureAwait(false);
            if (prepared == null) continue;
            if (prepared.Generate)
            {
                try
                {
                    Task generationTask = MyBehavior.EnsureNpcPersonaGeneratedForExternalAsync(prepared.Hero);
                    if (!await PersonaGenerationWaiter.WaitAsync(generationTask,
                        () => RunNativeConversationMainThreadFuncAsync("scene_persona_wait", npc.Name, npc.AgentIndex,
                            () => IsScenePersonaScopeCurrent(scope) && resolvedHeroes != null
                                && resolvedHeroes.TryGetValue(npc.AgentIndex, out Hero waitingHero)
                                && ReferenceEquals(waitingHero, prepared.Hero), false), () => true).ConfigureAwait(false)) return;
                }
                catch { } // Scene retains its factual fallback when persona generation is unavailable.
            }
            await RunNativeConversationMainThreadFuncAsync("scene_persona_accept", npc.Name, npc.AgentIndex, () =>
            {
                if (!IsScenePersonaScopeCurrent(scope) || resolvedHeroes == null
                    || !resolvedHeroes.TryGetValue(npc.AgentIndex, out Hero current) || !ReferenceEquals(current, prepared.Hero)) return false;
                NpcPersonaReadinessSnapshot state = MyBehavior.CaptureNpcPersonaReadiness(current, scope.PersonaOwner);
                if (state == null) return false;
                string p = state.Personality, b = state.Background;
                if (string.IsNullOrWhiteSpace(p) || string.IsNullOrWhiteSpace(b))
                {
                    BuildHeroPersonaFallback(current, out string fp, out string fb);
                    if (string.IsNullOrWhiteSpace(p)) p = fp;
                    if (string.IsNullOrWhiteSpace(b)) b = fb;
                }
                if (!string.IsNullOrWhiteSpace(p)) npc.PersonalityDesc = p.Trim();
                if (!string.IsNullOrWhiteSpace(b)) npc.BackgroundDesc = b.Trim();
                return true;
            }, false).ConfigureAwait(false);
        }
    }
}
