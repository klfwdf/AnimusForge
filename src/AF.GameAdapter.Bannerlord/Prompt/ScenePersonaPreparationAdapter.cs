using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using ScenePersonaPreparationScope = AnimusForge.ShoutBehavior.ScenePersonaPreparationScope;
using ScenePersonaCandidate = AnimusForge.ShoutBehavior.ScenePersonaCandidate;
using NativeConversationAdmission = AnimusForge.ShoutBehavior.NativeConversationAdmission;
namespace AnimusForge.Refactor.Adapters;
internal sealed class ScenePersonaPreparationAdapter
{
    private readonly ConversationGameThreadDispatcher _dispatcher;
    private readonly NativeAdmissionApplicationAdapter _nativeAdmission;
    private readonly Func<bool> _isCurrentOwner;
    private readonly Func<int> _sceneEpoch;
    private readonly int _nativeWaitTimeoutMs;
    internal ScenePersonaPreparationAdapter(ConversationGameThreadDispatcher dispatcher, NativeAdmissionApplicationAdapter nativeAdmission, Func<bool> isCurrentOwner, Func<int> sceneEpoch, int nativeWaitTimeoutMs)
    {
        _dispatcher=dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _nativeAdmission=nativeAdmission ?? throw new ArgumentNullException(nameof(nativeAdmission));
        _isCurrentOwner=isCurrentOwner ?? throw new ArgumentNullException(nameof(isCurrentOwner));
        _sceneEpoch=sceneEpoch ?? throw new ArgumentNullException(nameof(sceneEpoch));
        _nativeWaitTimeoutMs=nativeWaitTimeoutMs;
    }
	internal static void BuildHeroPersonaFallback(Hero hero, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (hero == null)
		{
			return;
		}
		string text = "";
		string text2 = "";
		string value = "";
		try
		{
			text = hero.Culture?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		try
		{
			text2 = hero.Clan?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		try
		{
			value = hero.MapFaction?.Name?.ToString() ?? hero.Clan?.Kingdom?.Name?.ToString() ?? "";
		}
		catch
		{
		}
		string arg = "英雄";
		try
		{
			if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out _))
			{
				arg = "统治者";
			}
			else if (hero.Clan?.Leader == hero)
			{
				arg = "家族族长";
			}
			else if (hero.IsLord)
			{
				arg = "领主";
			}
			else if (hero.IsWanderer)
			{
				arg = "流浪者";
			}
			else if (hero.IsNotable)
			{
				arg = "要人";
			}
		}
		catch
		{
		}
		string arg2 = (string.IsNullOrWhiteSpace(text) ? "" : (text + "的"));
		personality = $"{hero.Name}是一位{arg2}{arg}，处事谨慎务实，重视秩序与利益平衡。";
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(hero.Name).Append("出身于");
		stringBuilder.Append(string.IsNullOrWhiteSpace(text2) ? "本地家族" : text2);
		if (!string.IsNullOrWhiteSpace(value))
		{
			stringBuilder.Append("，当前活跃于").Append(value).Append("的政治与军事事务中");
		}
		background = stringBuilder.ToString().TrimEnd('。', '.', ' ') + "。";
	}
    internal bool IsScenePersonaScopeCurrent(ScenePersonaPreparationScope scope)
    {
        return scope != null && _isCurrentOwner()
            && SaveRuntimeGuard.IsCurrentGeneration(scope.Generation)
            && ReferenceEquals(scope.Mission, Mission.Current)
            && scope.Session == Volatile.Read(ref SceneConversationHistoryOwner.SessionId) && scope.Epoch == _sceneEpoch()
            && ReferenceEquals(scope.PersonaOwner, MyBehavior.Instance);
    }
    internal async Task EnsurePersonaForCandidatesAsync(List<NpcDataPacket> candidates, Dictionary<int, Hero> resolvedHeroes)
    {
        if (candidates == null || candidates.Count == 0) return;
        long generation = SaveRuntimeGuard.CaptureGeneration();
        int session = Volatile.Read(ref SceneConversationHistoryOwner.SessionId), epoch = _sceneEpoch();
        MyBehavior expectedPersonaOwner = MyBehavior.Instance;
        ScenePersonaPreparationScope scope = await _dispatcher.RunAsync("scene_persona_scope", "scene", -1, () =>
        {
            if (!_isCurrentOwner() || !SaveRuntimeGuard.IsCurrentGeneration(generation)
                || session != Volatile.Read(ref SceneConversationHistoryOwner.SessionId) || epoch != _sceneEpoch()
                || !ReferenceEquals(expectedPersonaOwner, MyBehavior.Instance)) return null;
            return new ScenePersonaPreparationScope
            {
                Mission = Mission.Current, Generation = generation, Session = Volatile.Read(ref SceneConversationHistoryOwner.SessionId),
                Epoch = _sceneEpoch(), PersonaOwner = MyBehavior.Instance, Candidates = candidates.ToArray()
            };
        }, (ScenePersonaPreparationScope)null).ConfigureAwait(false);
        if (scope == null) return;
        var notified = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (NpcDataPacket npc in scope.Candidates)
        {
            if (npc == null) continue;
            ScenePersonaCandidate prepared = await _dispatcher.RunAsync("scene_persona_capture", npc.Name, npc.AgentIndex, () =>
            {
                if (!IsScenePersonaScopeCurrent(scope)) return null;
                if (!npc.IsHero)
                {
                    AfGcczShoutBridge.CaptureOrdinarySpeakerPerception(npc);
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
                        () => _dispatcher.RunAsync("scene_persona_wait", npc.Name, npc.AgentIndex,
                            () => IsScenePersonaScopeCurrent(scope) && resolvedHeroes != null
                                && resolvedHeroes.TryGetValue(npc.AgentIndex, out Hero waitingHero)
                                && ReferenceEquals(waitingHero, prepared.Hero), false), () => true).ConfigureAwait(false)) return;
                }
                catch { } // Scene retains its factual fallback when persona generation is unavailable.
            }
            await _dispatcher.RunAsync("scene_persona_accept", npc.Name, npc.AgentIndex, () =>
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
    internal async Task<bool> EnsureNativeConversationPersonaReadyAsync(NativeConversationAdmission admission, Action<string> onStreamText)
    {
        MyBehavior personaOwner = MyBehavior.Instance;
        Task<NpcPersonaReadinessSnapshot> Observe(bool showHint = false, bool showFailure = false) =>
            _dispatcher.RunAsync("native_persona_readiness", admission.NpcName, admission.AgentIndex, () =>
            {
                if (!_nativeAdmission.IsNativeConversationAdmissionCurrent(admission, out _)) return null;
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
        while (watch.ElapsedMilliseconds < _nativeWaitTimeoutMs)
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
                    () => watch.ElapsedMilliseconds < _nativeWaitTimeoutMs).ConfigureAwait(false))
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
    internal static NpcPersonaReadinessSnapshot CaptureNpcPersonaReadiness(Hero hero, object expectedOwner,
        Func<object> instanceOwner, Func<object> campaignOwner, Func<object,NpcPersonaGenerationApplicationAdapter> application)
    {
        if (!TWParallel.IsMainThread()) throw new InvalidOperationException("Persona readiness capture requires the game thread.");
        if (!ReferenceEquals(instanceOwner(), expectedOwner)) return null;
        object owner = campaignOwner();
        if (!ReferenceEquals(owner, expectedOwner)) return null;
        string id = (hero?.StringId ?? "").Trim();
        string name = hero?.Name?.ToString();
        if (owner == null || hero == null)
            return new NpcPersonaReadinessSnapshot(id, name, "", "", false, false, false, false);
        var persona = application(owner);
        persona.GetNpcPersonaStrings(hero, out string personality, out string background);
        persona.GetNpcPersonaGenerationRuntimeState(hero, out bool active, out bool coolingDown);
        bool needsGeneration = NpcPersonaProfilePolicy.NeedsGeneration(id, personality, background);
        return new NpcPersonaReadinessSnapshot(id, name, personality, background, true, needsGeneration, active, coolingDown);
    }
}
