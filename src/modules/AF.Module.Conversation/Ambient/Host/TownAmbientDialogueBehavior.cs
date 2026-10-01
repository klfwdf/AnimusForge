using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Drives inexpensive, data-first settlement chatter. A mission tick scans agents
/// only at the configured interval, picks a matching preset line, shows it through
/// the existing scene bubble path, and records a compact fact for later conversation.
/// Optional ambient AI is isolated behind explicit user settings and only expands
/// occasional preset multi-speaker events.
/// </summary>
public sealed class TownAmbientDialogueMissionBehavior : MissionBehavior
{
	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	private const string ConfigFileName = "TownAmbientDialogue.json";
	private const float DefaultProbeIntervalSeconds = 7.5f;
	private const float DefaultInitialDelaySeconds = 2f;
	private const int MaxMemoryRecordsPerMission = 40;
	private const int MaxRecentLineSignatures = 60;
	private const int MaxPendingAmbientResponses = 2;
	private const float MaxAmbientResponseDistanceMeters = 12f;
	private const float FeastProbeIntervalSeconds = 18f;
	private const float FeastGlobalCooldownSeconds = 32f;
	private const float FeastTransitionDelaySeconds = 1.5f;
	private static readonly string[] ExcludedSceneTerms = { "arena", "tournament", "siege", "battle", "hideout", "bandit", "duel", "training", "deployment", "raid" };
	private static readonly string[] SettlementSceneTerms = { "town", "village", "castle", "tavern", "lordhall", "lordshall", "lord_hall", "lords_hall", "keep", "market", "alley", "street", "port" };

	private TownAmbientDialogueConfig _config;
	private float _nextProbeAt;
	private float _nextGlobalLineAt;
	private readonly Dictionary<int, float> _agentCooldownUntil = new Dictionary<int, float>();
	private readonly Dictionary<int, string> _lastLineByAgent = new Dictionary<int, string>();
	private readonly HashSet<string> _recordedMemoryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Queue<string> _recentLineSignatures = new Queue<string>();
	private readonly HashSet<string> _recentLineSignatureSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Queue<TownAmbientPendingResponse> _pendingResponses = new Queue<TownAmbientPendingResponse>();
	private readonly ConcurrentQueue<TownAmbientAiReadyBatch> _pendingAiBatches = new ConcurrentQueue<TownAmbientAiReadyBatch>();
	private volatile bool _ambientFeastModeActive;
	private int _ambientPauseEpoch;
	private List<TownAmbientLine> _contextualLinesCache;
	private string _contextualLinesCacheKey = "";
	private float _contextualLinesCacheUntil;
	private bool _disabledLogged;
	private bool _firstTickLogged;
	private bool _contextLogged;
	private bool _candidateLogged;
	private bool _bubbleLogged;
	private bool _timeDiagnosticsLogged;
	private bool _populationAttachLogged;

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_config = TownAmbientDialogueConfig.Load();
		_nextProbeAt = _config.InitialDelaySeconds >= 0f ? _config.InitialDelaySeconds : DefaultInitialDelaySeconds;
		_nextGlobalLineAt = 0f;
		_agentCooldownUntil.Clear();
		_lastLineByAgent.Clear();
		_recordedMemoryKeys.Clear();
		_recentLineSignatures.Clear();
		_recentLineSignatureSet.Clear();
		_pendingResponses.Clear();
		while (_pendingAiBatches.TryDequeue(out _)) { }
		_ambientFeastModeActive = false;
		Interlocked.Exchange(ref _ambientPauseEpoch, 0);
		_contextualLinesCache = null;
		_contextualLinesCacheKey = "";
		_contextualLinesCacheUntil = 0f;
		_disabledLogged = false;
		_firstTickLogged = false;
		_contextLogged = false;
		_candidateLogged = false;
		_bubbleLogged = false;
		_timeDiagnosticsLogged = false;
		_populationAttachLogged = false;
		try
		{
			Logger.LogImmediate("TownAmbient", "behavior_initialized enabled=" + (_config?.Enabled == true) + " lines=" + (_config?.Lines?.Count ?? 0) + " config=" + AnimusForgeModulePaths.GetModuleDataFilePath(ConfigFileName));
		}
		catch
		{
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || _config == null || !_config.Enabled || DuelSettings.GetTownAmbientDialogueDensity() <= 0)
			{
				return;
			}
			UpdateFeastModeState(mission);
			FlushPendingAiResponses(mission);
			FlushPendingResponses(mission);
			if (!_firstTickLogged)
			{
				_firstTickLogged = true;
				Logger.LogImmediate("TownAmbient", "first_tick missionTime=" + mission.CurrentTime.ToString("0.00") + " scene=" + (mission.SceneName ?? "") + " agents=" + (mission.Agents?.Count ?? 0));
			}
			if (mission.CurrentTime < _nextProbeAt)
			{
				return;
			}
			_nextProbeAt = mission.CurrentTime + GetRuntimeProbeIntervalSeconds();

			if (!TryGetSettlementContext(mission, out Settlement settlement, out string sceneTag))
			{
				if (!_contextLogged)
				{
					_contextLogged = true;
					Logger.LogImmediate("TownAmbient", "context_rejected scene=" + (mission.SceneName ?? "") + " settlement=" + GetCurrentSettlementIdSafe() + " location=" + GetCurrentLocationIdSafe());
				}
				return;
			}
			if (!_contextLogged)
			{
				_contextLogged = true;
				Logger.LogImmediate("TownAmbient", "context_ok scene=" + (mission.SceneName ?? "") + " sceneTag=" + sceneTag + " settlement=" + (settlement?.StringId ?? "null") + " location=" + GetCurrentLocationIdSafe());
			}
			if (!_ambientFeastModeActive)
			{
				TryAttachTownPopulationBehavior(mission, settlement, sceneTag);
			}
			if (mission.CurrentTime < _nextGlobalLineAt)
			{
				return;
			}
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true || ShoutBehavior.IsSceneShoutInputActiveForExternal() || ShoutBehavior.HasAnyImmediateSceneReactionInFlightForExternal())
			{
				return;
			}

			TownAmbientPlayerContext playerContext = BuildPlayerContext(settlement, sceneTag);
			string contextCacheKey = BuildContextCacheKey(settlement, sceneTag, playerContext, _ambientFeastModeActive);
			List<TownAmbientLine> contextualLines;
			if (_contextualLinesCache != null && mission.CurrentTime < _contextualLinesCacheUntil && string.Equals(_contextualLinesCacheKey, contextCacheKey, StringComparison.Ordinal))
			{
				contextualLines = _contextualLinesCache;
			}
			else
			{
				contextualLines = _config.Lines.Where(line => ContextMatches(line, settlement, sceneTag, playerContext, _ambientFeastModeActive)).ToList();
				_contextualLinesCache = contextualLines;
				_contextualLinesCacheKey = contextCacheKey;
				_contextualLinesCacheUntil = mission.CurrentTime + 4f;
			}
			if (contextualLines.Count == 0)
			{
				return;
			}
			List<Agent> candidates = FindCandidates(mission, contextualLines);
			if (!_candidateLogged)
			{
				_candidateLogged = true;
				Logger.LogImmediate("TownAmbient", "candidate_scan lines=" + contextualLines.Count + " candidates=" + candidates.Count + " maxDistance=" + _config.MaxDistanceMeters.ToString("0.0"));
			}
			if (candidates.Count == 0)
			{
				return;
			}
			Agent agent = candidates[MBRandom.RandomInt(candidates.Count)];
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc == null)
			{
				return;
			}
			TownAmbientLine line = PickLine(npc, contextualLines);
			if (line == null)
			{
				return;
			}

			string text = Render(PickTextVariant(line), npc, settlement, sceneTag, playerContext);
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			if (!_timeDiagnosticsLogged)
			{
				_timeDiagnosticsLogged = true;
				Logger.LogImmediate("TownAmbient", "time_context hour=" + CampaignTime.Now.GetHourOfDay + " band=" + TownAmbientTime.GetTag(CampaignTime.Now.GetHourOfDay) + " line=" + (line.Id ?? "") + " min=" + (line.MinHour?.ToString() ?? "") + " max=" + (line.MaxHour?.ToString() ?? "") + " bands=" + string.Join(",", line.TimeBands ?? new List<string>()));
			}
			float typingDuration = Math.Max(1.15f, Math.Min(5.5f, text.Length * 0.055f));
			bool bubbleShown = ShoutBehavior.TryShowPassiveNpcBubbleForExternal(agent, text, typingDuration);
			if (!_bubbleLogged)
			{
				_bubbleLogged = true;
				Logger.LogImmediate("TownAmbient", "bubble_result shown=" + bubbleShown + " agent=" + agent.Index + " line=" + (line.Id ?? ""));
			}
			if (!bubbleShown)
			{
				return;
			}
			TryApplyGesture(agent, line.Gesture);

			float lineCooldown = line.CooldownSeconds > 0f ? line.CooldownSeconds : _config.AgentCooldownSeconds;
			_agentCooldownUntil[agent.Index] = mission.CurrentTime + Math.Max(3f, lineCooldown * GetRuntimeAgentCooldownScale());
			_lastLineByAgent[agent.Index] = line.Id ?? string.Empty;
			RememberLineSignature(line);
			_nextGlobalLineAt = mission.CurrentTime + GetRuntimeGlobalCooldownSeconds();
			RecordForLaterConversation(npc, line, text, settlement, sceneTag, playerContext);
			ScheduleAmbientResponses(mission, agent, line, contextualLines, candidates, settlement, sceneTag, playerContext);
		}
		catch (Exception ex)
		{
			// One malformed line, unavailable agent or scene transition must not
			// silence every settlement for the rest of the mission.  Keep the
			// behavior alive and record the full exception so the offending case
			// can be diagnosed after the player leaves the scene.
			if (!_disabledLogged)
			{
				_disabledLogged = true;
				Logger.Log("TownAmbient", "ambient chatter tick recovered after error: " + ex);
			}
			_nextProbeAt = (Mission.Current?.CurrentTime ?? 0f) + 5f;
		}
	}

	private float GetRuntimeProbeIntervalSeconds()
	{
		if (_ambientFeastModeActive)
		{
			int feastDensity = DuelSettings.GetTownAmbientDialogueDensity();
			return FeastProbeIntervalSeconds * (feastDensity <= 1 ? 1.25f : feastDensity >= 3 ? 0.8f : 1f);
		}
		int density = DuelSettings.GetTownAmbientDialogueDensity();
		float scale = density <= 1 ? 1.8f : density >= 3 ? 0.55f : 1f;
		return Math.Max(3f, _config.ProbeIntervalSeconds * scale);
	}

	private void TryAttachTownPopulationBehavior(Mission mission, Settlement settlement, string sceneTag)
	{
		if (mission == null || settlement == null || (!settlement.IsTown && !settlement.IsVillage) || DuelSettings.GetTownAmbientDialogueDensity() <= 0)
		{
			return;
		}
		// Population enrichment belongs to public settlement spaces.  Lord halls,
		// taverns and castle interiors have their own native casts and must never
		// receive a town-sized civilian pass.
		if (!IsPopulationEligibleScene(sceneTag))
		{
			return;
		}
		try
		{
			if (CampaignMission.Current?.Location == null || mission.GetMissionBehavior<InterventionNativeTownCivilianPopulationMissionBehavior>() != null)
			{
				return;
			}
			mission.AddMissionBehavior(new InterventionNativeTownCivilianPopulationMissionBehavior(settlement.StringId, regularTownMultiplierMode: true));
			if (!_populationAttachLogged)
			{
				_populationAttachLogged = true;
				Logger.LogImmediate("TownAmbient", "settlement_population_behavior_attached settlement=" + settlement.StringId + " type=" + (settlement.IsVillage ? "village" : "town") + " location=" + GetCurrentLocationIdSafe());
			}
		}
		catch (Exception ex)
		{
			if (!_populationAttachLogged)
			{
				_populationAttachLogged = true;
				Logger.Log("TownAmbient", "town population behavior attach failed: " + ex.Message);
			}
		}
	}

	private static bool IsPopulationEligibleScene(string sceneTag)
	{
		string value = (sceneTag ?? "").Trim().ToLowerInvariant();
		return value == "town" || value == "market" || value == "port" || value == "village";
	}

	private float GetRuntimeGlobalCooldownSeconds()
	{
		if (_ambientFeastModeActive)
		{
			int feastDensity = DuelSettings.GetTownAmbientDialogueDensity();
			return FeastGlobalCooldownSeconds * (feastDensity <= 1 ? 1.25f : feastDensity >= 3 ? 0.8f : 1f);
		}
		int density = DuelSettings.GetTownAmbientDialogueDensity();
		float scale = density <= 1 ? 1.8f : density >= 3 ? 0.55f : 1f;
		return Math.Max(1.5f, _config.GlobalCooldownSeconds * scale);
	}

	private float GetRuntimeAgentCooldownScale()
	{
		int density = DuelSettings.GetTownAmbientDialogueDensity();
		return density <= 1 ? 1.35f : density >= 3 ? 0.7f : 1f;
	}

	public override void OnRemoveBehavior()
	{
		_agentCooldownUntil.Clear();
		_lastLineByAgent.Clear();
		_recordedMemoryKeys.Clear();
		_recentLineSignatures.Clear();
		_recentLineSignatureSet.Clear();
		_pendingResponses.Clear();
		while (_pendingAiBatches.TryDequeue(out _)) { }
		_ambientFeastModeActive = false;
		Interlocked.Increment(ref _ambientPauseEpoch);
		_contextualLinesCache = null;
		_contextualLinesCacheKey = "";
		_contextualLinesCacheUntil = 0f;
		_config = null;
		base.OnRemoveBehavior();
	}

	private void UpdateFeastModeState(Mission mission)
	{
		bool feastActive = IsCurrentFeastLordHallActive();
		if (feastActive)
		{
			if (!_ambientFeastModeActive)
			{
				_ambientFeastModeActive = true;
				Interlocked.Increment(ref _ambientPauseEpoch);
				_contextualLinesCache = null;
				_contextualLinesCacheKey = "";
				_contextualLinesCacheUntil = 0f;
				float now = mission?.CurrentTime ?? 0f;
				_nextProbeAt = Math.Max(_nextProbeAt, now + FeastTransitionDelaySeconds);
				_nextGlobalLineAt = Math.Max(_nextGlobalLineAt, now + FeastTransitionDelaySeconds);
				Logger.LogImmediate("TownAmbient", "feast_mode_entered location=" + GetCurrentLocationIdSafe() + " settlement=" + GetCurrentSettlementIdSafe());
			}
			_pendingResponses.Clear();
			while (_pendingAiBatches.TryDequeue(out _)) { }
			return;
		}
		if (_ambientFeastModeActive)
		{
			_ambientFeastModeActive = false;
			Interlocked.Increment(ref _ambientPauseEpoch);
			_contextualLinesCache = null;
			_contextualLinesCacheKey = "";
			_contextualLinesCacheUntil = 0f;
			float resumeAt = (mission?.CurrentTime ?? 0f) + 0.75f;
			_nextProbeAt = resumeAt;
			_nextGlobalLineAt = resumeAt;
			Logger.LogImmediate("TownAmbient", "feast_mode_exited location=" + GetCurrentLocationIdSafe() + " settlement=" + GetCurrentSettlementIdSafe());
		}
	}

	private static bool IsCurrentFeastLordHallActive()
	{
		try
		{
			Location location = CampaignMission.Current?.Location;
			string locationId = location?.StringId ?? "";
			bool isLordHall = string.Equals(locationId, "lordshall", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(locationId, "lords_hall", StringComparison.OrdinalIgnoreCase);
			if (!isLordHall)
			{
				return false;
			}
			Settlement settlement = PlayerEncounter.LocationEncounter?.Settlement ?? Settlement.CurrentSettlement;
			NobleGatheringBehavior gathering = NobleGatheringBehavior.Instance ?? Campaign.Current?.GetCampaignBehavior<NobleGatheringBehavior>();
			return settlement != null && gathering?.HasActiveGatheringAtSettlement(settlement) == true;
		}
		catch
		{
			return false;
		}
	}

	private List<Agent> FindCandidates(Mission mission, List<TownAmbientLine> contextualLines)
	{
		List<Agent> result = new List<Agent>();
		Agent player = Agent.Main;
		if (player == null || mission.Agents == null)
		{
			return result;
		}
		Vec3 playerPosition = player.Position;
		float minDistanceSquared = _config.MinDistanceMeters * _config.MinDistanceMeters;
		float maxDistanceSquared = _config.MaxDistanceMeters * _config.MaxDistanceMeters;
		for (int i = 0; i < mission.Agents.Count; i++)
		{
			Agent agent = mission.Agents[i];
			if (agent == null || agent == player || !agent.IsHuman || !agent.IsActive() || agent.State != AgentState.Active || agent.Health <= 0f)
			{
				continue;
			}
			if (_agentCooldownUntil.TryGetValue(agent.Index, out float cooldownUntil) && mission.CurrentTime < cooldownUntil)
			{
				continue;
			}
			float distanceSquared = agent.Position.DistanceSquared(playerPosition);
			if (float.IsNaN(distanceSquared) || distanceSquared < minDistanceSquared || distanceSquared > maxDistanceSquared)
			{
				continue;
			}
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(agent);
			if (npc == null || (npc.IsHero && !_config.AllowHeroes) || !HasMatchingLine(npc, contextualLines))
			{
				continue;
			}
			result.Add(agent);
		}
		return result;
	}

	private static bool HasMatchingLine(NpcDataPacket npc, List<TownAmbientLine> contextualLines)
	{
		string roleHaystack = BuildRoleHaystack(npc);
		return contextualLines.Any(line => !line.EventResponse && NpcMatches(line, npc, roleHaystack));
	}

	private TownAmbientLine PickLine(NpcDataPacket npc, List<TownAmbientLine> contextualLines, bool eventResponse = false)
	{
		List<TownAmbientLine> matches = new List<TownAmbientLine>();
		float totalWeight = 0f;
		string roleHaystack = BuildRoleHaystack(npc);
		foreach (TownAmbientLine line in contextualLines)
		{
			if (line.EventResponse != eventResponse)
			{
				continue;
			}
			if (!NpcMatches(line, npc, roleHaystack))
			{
				continue;
			}
			if (_lastLineByAgent.TryGetValue(npc.AgentIndex, out string lastId) && contextualLines.Count > 1 && string.Equals(lastId, line.Id, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			if (_recentLineSignatureSet.Contains(GetLineSignature(line)))
			{
				continue;
			}
			float weight = line.Weight > 0f ? line.Weight : 1f;
			matches.Add(line);
			totalWeight += weight;
		}
		if (matches.Count == 0)
		{
			foreach (TownAmbientLine line in contextualLines)
			{
				if (line.EventResponse != eventResponse)
				{
					continue;
				}
				if (NpcMatches(line, npc, roleHaystack))
				{
					matches.Add(line);
					totalWeight += line.Weight > 0f ? line.Weight : 1f;
				}
			}
		}
		if (matches.Count == 0)
		{
			return null;
		}
		float roll = MBRandom.RandomFloat * totalWeight;
		foreach (TownAmbientLine line in matches)
		{
			roll -= line.Weight > 0f ? line.Weight : 1f;
			if (roll <= 0f)
			{
				return line;
			}
		}
		return matches[matches.Count - 1];
	}

	private void FlushPendingResponses(Mission mission)
	{
		if (mission == null || _pendingResponses.Count == 0)
		{
			return;
		}
		while (_pendingResponses.Count > 0 && _pendingResponses.Peek().ShowAt <= mission.CurrentTime)
		{
			TownAmbientPendingResponse pending = _pendingResponses.Dequeue();
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true || ShoutBehavior.IsSceneShoutInputActiveForExternal() || ShoutBehavior.HasAnyImmediateSceneReactionInFlightForExternal())
			{
				pending.ShowAt = mission.CurrentTime + 0.75f;
				_pendingResponses.Enqueue(pending);
				break;
			}
			Agent agent = FindAgentByIndex(mission, pending.AgentIndex);
			if (agent == null || !agent.IsActive() || agent.Health <= 0f)
			{
				continue;
			}
			Agent player = Agent.Main;
			if (player != null)
			{
				float distanceSquared = agent.Position.DistanceSquared(player.Position);
				if (float.IsNaN(distanceSquared) || distanceSquared > _config.MaxDistanceMeters * _config.MaxDistanceMeters)
				{
					continue;
				}
			}
			if (!ShoutBehavior.TryShowPassiveNpcBubbleForExternal(agent, pending.Text, pending.TypingDuration))
			{
				continue;
			}
			_agentCooldownUntil[agent.Index] = mission.CurrentTime + Math.Max(3f, pending.CooldownSeconds * GetRuntimeAgentCooldownScale());
			_lastLineByAgent[agent.Index] = pending.LineId ?? string.Empty;
			RememberLineSignature(pending.Signature);
		}
	}

	private void FlushPendingAiResponses(Mission mission)
	{
		if (mission == null)
		{
			return;
		}
		while (_pendingAiBatches.TryDequeue(out TownAmbientAiReadyBatch batch))
		{
			if (_ambientFeastModeActive || batch == null || batch.PauseEpoch != Volatile.Read(ref _ambientPauseEpoch))
			{
				continue;
			}
			if (batch == null || !batch.Result.Success || batch.Result.Replies == null || batch.Result.Replies.Length == 0)
			{
				if (batch?.Result != null && !string.IsNullOrWhiteSpace(batch.Result.Error))
				{
					Logger.Log("TownAmbient", "ai_echo_fallback error=" + batch.Result.Error);
					if (DuelSettings.ShouldShowTownAmbientAiUsage() && (batch.Result.InputTokens > 0 || batch.Result.OutputTokens > 0))
					{
						InformationManager.DisplayMessage(new InformationMessage(string.Format("[环境 AI] 本次请求虽未能生成接话，但已计入约 {0} 输入 + {1} 输出 Token。", batch.Result.InputTokens, batch.Result.OutputTokens), Color.FromUint(4294967040u)));
					}
				}
				continue;
			}
			int count = Math.Min(batch.AgentIndices.Count, batch.Result.Replies.Length);
			for (int i = 0; i < count; i++)
			{
				string text = (batch.Result.Replies[i] ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				_pendingResponses.Enqueue(new TownAmbientPendingResponse
				{
					AgentIndex = batch.AgentIndices[i],
					ShowAt = mission.CurrentTime + batch.SpacingSeconds * (i + 1),
					Text = text,
					TypingDuration = Math.Max(1.0f, Math.Min(4.5f, text.Length * 0.05f)),
					CooldownSeconds = batch.CooldownSeconds,
					LineId = "ai:" + (batch.CacheKey ?? "ambient"),
					Signature = null
				});
			}
			if (DuelSettings.ShouldShowTownAmbientAiUsage())
			{
				string source = batch.Result.FromCache ? "缓存（0 Token）" : string.Format("输入 {0} + 输出 {1} Token", batch.Result.InputTokens, batch.Result.OutputTokens);
				InformationManager.DisplayMessage(new InformationMessage(string.Format("[环境 AI] {0}；本次启动累计约 {1} Token，已请求 {2} 次。", source, batch.Result.SessionEstimatedTokens, batch.Result.SessionRequests), Color.FromUint(4294967040u)));
			}
		}
	}

	private static Agent FindAgentByIndex(Mission mission, int agentIndex)
	{
		if (mission?.Agents == null)
		{
			return null;
		}
		for (int i = 0; i < mission.Agents.Count; i++)
		{
			Agent agent = mission.Agents[i];
			if (agent != null && agent.Index == agentIndex)
			{
				return agent;
			}
		}
		return null;
	}

	private void ScheduleAmbientResponses(Mission mission, Agent anchorAgent, TownAmbientLine anchorLine, List<TownAmbientLine> contextualLines, List<Agent> candidates, Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext)
	{
		int density = DuelSettings.GetTownAmbientDialogueDensity();
		if (_ambientFeastModeActive || mission == null || anchorAgent == null || anchorLine == null || string.IsNullOrWhiteSpace(anchorLine.EventKey) || anchorLine.EventResponse || !DuelSettings.IsTownAmbientEventEchoEnabled() || density <= 1)
		{
			return;
		}
		int chance = anchorLine.EventChancePercent > 0 ? anchorLine.EventChancePercent : GetRuntimeEventEchoChance();
		if (density == 2)
		{
			chance = Math.Min(chance, 10);
		}
		if (chance <= 0 || MBRandom.RandomInt(100) >= Math.Min(100, chance))
		{
			return;
		}
		List<TownAmbientLine> responseLines = contextualLines.Where(line => line.EventResponse && string.Equals(line.EventKey, anchorLine.EventKey, StringComparison.OrdinalIgnoreCase)).ToList();
		if (responseLines.Count == 0 || candidates == null || candidates.Count < 2)
		{
			return;
		}
		int requested = anchorLine.MaxEventResponses > 0 ? anchorLine.MaxEventResponses : 2;
		requested = Math.Max(1, Math.Min(MaxPendingAmbientResponses, requested));
		float spacing = anchorLine.EventResponseSpacingSeconds > 0f ? anchorLine.EventResponseSpacingSeconds : 1.1f;
		if (TryScheduleAiAmbientResponses(mission, anchorAgent, anchorLine, candidates, settlement, sceneTag, playerContext, requested, spacing))
		{
			return;
		}
		HashSet<int> usedAgents = new HashSet<int> { anchorAgent.Index };
		HashSet<string> usedSignatures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int scheduled = 0;
		foreach (Agent responseAgent in candidates.OrderBy(_ => MBRandom.RandomFloat))
		{
			if (scheduled >= requested || responseAgent == null || usedAgents.Contains(responseAgent.Index))
			{
				continue;
			}
			float distanceSquared = responseAgent.Position.DistanceSquared(anchorAgent.Position);
			if (float.IsNaN(distanceSquared) || distanceSquared > MaxAmbientResponseDistanceMeters * MaxAmbientResponseDistanceMeters)
			{
				continue;
			}
			NpcDataPacket responseNpc = ShoutUtils.ExtractNpcData(responseAgent);
			if (responseNpc == null)
			{
				continue;
			}
			TownAmbientLine responseLine = PickLine(responseNpc, responseLines, eventResponse: true);
			if (responseLine == null)
			{
				continue;
			}
			string signature = GetLineSignature(responseLine);
			if (string.IsNullOrWhiteSpace(signature) || !usedSignatures.Add(signature) || _recentLineSignatureSet.Contains(signature))
			{
				continue;
			}
			string responseText = Render(PickTextVariant(responseLine), responseNpc, settlement, sceneTag, playerContext);
			if (string.IsNullOrWhiteSpace(responseText))
			{
				continue;
			}
			_pendingResponses.Enqueue(new TownAmbientPendingResponse
			{
				AgentIndex = responseAgent.Index,
				ShowAt = mission.CurrentTime + spacing * (scheduled + 1),
				Text = responseText,
				TypingDuration = Math.Max(1.0f, Math.Min(4.5f, responseText.Length * 0.05f)),
				CooldownSeconds = responseLine.CooldownSeconds > 0f ? responseLine.CooldownSeconds : _config.AgentCooldownSeconds,
				LineId = responseLine.Id,
				Signature = responseLine
			});
			_agentCooldownUntil[responseAgent.Index] = mission.CurrentTime + Math.Max(4f, (responseLine.CooldownSeconds > 0f ? responseLine.CooldownSeconds : _config.AgentCooldownSeconds) * GetRuntimeAgentCooldownScale());
			usedAgents.Add(responseAgent.Index);
			scheduled++;
		}
		if (scheduled > 0)
		{
			Logger.LogImmediate("TownAmbient", "event_echo_scheduled key=" + anchorLine.EventKey + " responses=" + scheduled);
		}
	}

	private bool TryScheduleAiAmbientResponses(Mission mission, Agent anchorAgent, TownAmbientLine anchorLine, List<Agent> candidates, Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext, int requested, float spacing)
	{
		if (_ambientFeastModeActive)
		{
			return false;
		}
		if (!DuelSettings.IsTownAmbientAiEnabled() || DuelSettings.GetTownAmbientAiChancePercent() <= 0 || MBRandom.RandomInt(100) >= DuelSettings.GetTownAmbientAiChancePercent())
		{
			return false;
		}
		List<int> agentIndices = new List<int>();
		List<TownAmbientAiSpeaker> speakers = new List<TownAmbientAiSpeaker>();
		HashSet<int> usedAgents = new HashSet<int> { anchorAgent.Index };
		foreach (Agent responseAgent in candidates.OrderBy(_ => MBRandom.RandomFloat))
		{
			if (agentIndices.Count >= requested || responseAgent == null || usedAgents.Contains(responseAgent.Index))
			{
				continue;
			}
			float distanceSquared = responseAgent.Position.DistanceSquared(anchorAgent.Position);
			if (float.IsNaN(distanceSquared) || distanceSquared > MaxAmbientResponseDistanceMeters * MaxAmbientResponseDistanceMeters)
			{
				continue;
			}
			NpcDataPacket npc = ShoutUtils.ExtractNpcData(responseAgent);
			if (npc == null)
			{
				continue;
			}
			agentIndices.Add(responseAgent.Index);
			speakers.Add(new TownAmbientAiSpeaker
			{
				Role = GetCanonicalNpcRole(npc, BuildRoleHaystack(npc)),
				IsFemale = npc.IsFemale
			});
			usedAgents.Add(responseAgent.Index);
		}
		if (agentIndices.Count == 0)
		{
			return false;
		}
		string cacheKey = string.Join("|", new[]
		{
			"v1",
			settlement?.StringId ?? "settlement",
			sceneTag ?? "street",
			anchorLine.EventKey ?? "event",
			(anchorLine.Text ?? "").Trim(),
			playerContext?.Status ?? "旅人",
			CampaignTime.Now.GetHourOfDay / 4 + "",
			string.Join(",", speakers.Select(s => (s.Role ?? "居民") + (s.IsFemale ? ":f" : ":m")))
		});
		string townName = settlement?.Name?.ToString() ?? "定居点";
		string timePeriod = TownAmbientTime.GetDisplayName(CampaignTime.Now.GetHourOfDay);
		int requestEpoch = Volatile.Read(ref _ambientPauseEpoch);
		TownAmbientAiReadyBatch batch = new TownAmbientAiReadyBatch
		{
			AgentIndices = agentIndices,
			SpacingSeconds = spacing,
			CooldownSeconds = _config.AgentCooldownSeconds,
			CacheKey = cacheKey,
			PauseEpoch = requestEpoch
		};
		if (!TownAmbientAiClient.TryStartReplyGeneration(cacheKey, Render(PickTextVariant(anchorLine), ShoutUtils.ExtractNpcData(anchorAgent), settlement, sceneTag, playerContext), townName, sceneTag, timePeriod, playerContext?.Status, string.Join("；", anchorLine.ReplyHints ?? new List<string>()), speakers, result =>
		{
			batch.Result = result ?? new TownAmbientAiResult { Error = "empty_result" };
			if (!_ambientFeastModeActive && requestEpoch == Volatile.Read(ref _ambientPauseEpoch))
			{
				_pendingAiBatches.Enqueue(batch);
			}
		}, out string skipReason))
		{
			Logger.Log("TownAmbient", "ai_echo_skipped reason=" + skipReason);
			return false;
		}
		foreach (int agentIndex in agentIndices)
		{
			_agentCooldownUntil[agentIndex] = mission.CurrentTime + Math.Max(4f, _config.AgentCooldownSeconds * GetRuntimeAgentCooldownScale());
		}
		Logger.LogImmediate("TownAmbient", "ai_echo_requested key=" + anchorLine.EventKey + " responders=" + agentIndices.Count + " sessionTokens=" + TownAmbientAiClient.SessionEstimatedTokens);
		return true;
	}

	private int GetRuntimeEventEchoChance()
	{
		int density = DuelSettings.GetTownAmbientDialogueDensity();
		return density >= 3 ? 18 : density == 2 ? 10 : 0;
	}

	private static string BuildContextCacheKey(Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext, bool feastModeActive)
	{
		Town contextTown = GetContextTown(settlement);
		int loyaltyBucket = (int)Math.Round((contextTown?.Loyalty ?? 100f) / 5f);
		int prosperityBucket = (int)Math.Round((contextTown?.Prosperity ?? 0f) / 25f);
		int hour = CampaignTime.Now.GetHourOfDay;
		string tags = playerContext?.Tags == null ? "" : string.Join(",", playerContext.Tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase));
		return (settlement?.StringId ?? "") + "|" + (sceneTag ?? "") + "|feast=" + (feastModeActive ? "1" : "0") + "|h=" + hour + "|l=" + loyaltyBucket + "|p=" + prosperityBucket + "|g=" + (playerContext?.Gold ?? 0) / 5000 + "|r=" + (int)Math.Floor(playerContext?.Renown ?? 0f) / 25 + "|t=" + (playerContext?.ClanTier ?? 0) + "|" + tags;
	}

	private static string GetLineSignature(TownAmbientLine line)
	{
		if (line == null)
		{
			return "";
		}
		string text = line.TextVariants != null && line.TextVariants.Count > 0
			? string.Join("|", line.TextVariants.Where(x => !string.IsNullOrWhiteSpace(x)))
			: line.Text;
		return (text ?? "").Trim();
	}

	private void RememberLineSignature(TownAmbientLine line)
	{
		string signature = GetLineSignature(line);
		if (string.IsNullOrWhiteSpace(signature) || !_recentLineSignatureSet.Add(signature))
		{
			return;
		}
		_recentLineSignatures.Enqueue(signature);
		while (_recentLineSignatures.Count > MaxRecentLineSignatures)
		{
			_recentLineSignatureSet.Remove(_recentLineSignatures.Dequeue());
		}
	}

	private static bool ContextMatches(TownAmbientLine line, Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext, bool feastModeActive)
	{
		if (line == null || !line.HasText || line.Enabled == false)
		{
			return false;
		}
		if (line.FeastOnly != feastModeActive)
		{
			return false;
		}
		if (line.SceneTags != null && line.SceneTags.Count > 0 && !line.SceneTags.Any(tag => string.Equals(tag, "any", StringComparison.OrdinalIgnoreCase) || IsSceneTagMatch(tag, sceneTag) || (IsLordHallScene(sceneTag) && string.Equals((tag ?? "").Trim(), "castle", StringComparison.OrdinalIgnoreCase))))
		{
			return false;
		}
		if (IsLordHallScene(sceneTag) && IsHallIncompatibleLine(line))
		{
			return false;
		}
		int hour = CampaignTime.Now.GetHourOfDay;
		if (!TownAmbientTime.Matches(line, hour))
		{
			return false;
		}
		if (line.MinLoyalty.HasValue || line.MaxLoyalty.HasValue || line.MinProsperity.HasValue || line.MaxProsperity.HasValue)
		{
			Town contextTown = GetContextTown(settlement);
			float loyalty = contextTown?.Loyalty ?? 100f;
			float prosperity = contextTown?.Prosperity ?? 0f;
			if (line.MinLoyalty.HasValue && loyalty < line.MinLoyalty.Value || line.MaxLoyalty.HasValue && loyalty > line.MaxLoyalty.Value || line.MinProsperity.HasValue && prosperity < line.MinProsperity.Value || line.MaxProsperity.HasValue && prosperity > line.MaxProsperity.Value)
			{
				return false;
			}
		}
		if (!PlayerMatches(line, playerContext))
		{
			return false;
		}
		return true;
	}

	private static bool IsSceneTagMatch(string configuredTag, string actualTag)
	{
		string expected = (configuredTag ?? "").Trim().ToLowerInvariant();
		string actual = (actualTag ?? "").Trim().ToLowerInvariant();
		if (expected == "any") return true;
		if (expected == actual) return true;
		// Keep lord halls separate from ordinary castles.  A castle patrol line
		// must not leak into the lord's indoor hall just because both used to be
		// represented by the same broad "castle" tag.
		return false;
	}

	private static bool IsLordHallScene(string sceneTag)
	{
		string value = (sceneTag ?? "").Trim().ToLowerInvariant();
		return value == "lordhall" || value == "lord_hall" || value == "lords_hall";
	}

	private static bool IsHallIncompatibleLine(TownAmbientLine line)
	{
		string text = ((line?.Text ?? "") + " " + string.Join(" ", line?.TextVariants ?? new List<string>())).Trim();
		if (string.IsNullOrWhiteSpace(text)) return false;
		// These are outside actions and locations.  They are valid on a wall,
		// gate or street, never in the lord's indoor hall.
		return text.IndexOf("北墙", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("城墙", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("城门", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("巡逻", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("岗哨", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("粮仓", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("仓库", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("墙外", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool NpcMatches(TownAmbientLine line, NpcDataPacket npc, string roleHaystack)
	{
		if (line == null || npc == null)
		{
			return false;
		}
		if (IsRecruitmentLine(line) && IsAlreadyServingNpc(npc, roleHaystack))
		{
			return false;
		}
		if (line.Roles != null && line.Roles.Count > 0 && !line.Roles.Any(role => RoleMatches(role, npc, roleHaystack)))
		{
			return false;
		}
		if (line.Cultures != null && line.Cultures.Count > 0 && !line.Cultures.Any(culture => string.Equals(culture, "any", StringComparison.OrdinalIgnoreCase) || string.Equals(culture, npc.CultureId, StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		if (line.Genders != null && line.Genders.Count > 0 && !line.Genders.Any(gender => GenderMatches(gender, npc)))
		{
			return false;
		}
		return (!line.MinAge.HasValue || npc.Age >= line.MinAge.Value) && (!line.MaxAge.HasValue || npc.Age <= line.MaxAge.Value);
	}

	private static bool IsRecruitmentLine(TownAmbientLine line)
	{
		string haystack = ((line?.Id ?? "") + " " + (line?.Text ?? "") + " " + string.Join(" ", line?.TextVariants ?? new List<string>())).ToLowerInvariant();
		return haystack.Contains("招人") || haystack.Contains("参军") || haystack.Contains("收下我") || haystack.Contains("为您效力") || haystack.Contains("加入") || haystack.Contains("肯卖命");
	}

	private static bool IsAlreadyServingNpc(NpcDataPacket npc, string roleHaystack)
	{
		string role = GetCanonicalNpcRole(npc, roleHaystack);
		return role == "soldier" || role == "merchant" || role == "horse_trader" || role == "blacksmith" || role == "weaponsmith" || role == "armorer" || role == "tavernkeeper" || role == "musician";
	}

	private static bool GenderMatches(string expected, NpcDataPacket npc)
	{
		string value = (expected ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(value) || value == "any" || value == "any_gender")
		{
			return true;
		}
		if (value == "female" || value == "woman" || value == "f" || value == "女" || value == "女性")
		{
			return npc.IsFemale;
		}
		if (value == "male" || value == "man" || value == "m" || value == "男" || value == "男性")
		{
			return !npc.IsFemale;
		}
		return false;
	}

	private static bool PlayerMatches(TownAmbientLine line, TownAmbientPlayerContext playerContext)
	{
		if (playerContext == null)
		{
			return line.PlayerAnyTags == null || line.PlayerAnyTags.Count == 0;
		}
		if (line.PlayerAnyTags != null && line.PlayerAnyTags.Count > 0 && !line.PlayerAnyTags.Any(tag => playerContext.Tags.Contains((tag ?? "").Trim().ToLowerInvariant())))
		{
			return false;
		}
		if (line.PlayerAllTags != null && line.PlayerAllTags.Count > 0 && line.PlayerAllTags.Any(tag => !playerContext.Tags.Contains((tag ?? "").Trim().ToLowerInvariant())))
		{
			return false;
		}
		if (line.PlayerNoneTags != null && line.PlayerNoneTags.Any(tag => playerContext.Tags.Contains((tag ?? "").Trim().ToLowerInvariant())))
		{
			return false;
		}
		return (!line.MinPlayerGold.HasValue || playerContext.Gold >= line.MinPlayerGold.Value)
			&& (!line.MaxPlayerGold.HasValue || playerContext.Gold <= line.MaxPlayerGold.Value)
			&& (!line.MinPlayerRenown.HasValue || playerContext.Renown >= line.MinPlayerRenown.Value)
			&& (!line.MaxPlayerRenown.HasValue || playerContext.Renown <= line.MaxPlayerRenown.Value)
			&& (!line.MinPlayerClanTier.HasValue || playerContext.ClanTier >= line.MinPlayerClanTier.Value)
			&& (!line.MaxPlayerClanTier.HasValue || playerContext.ClanTier <= line.MaxPlayerClanTier.Value);
	}

	private static string BuildRoleHaystack(NpcDataPacket npc)
	{
		return ((npc?.RoleDesc ?? "") + " " + (npc?.TroopId ?? "") + " " + (npc?.UnnamedKey ?? "") + " " + (npc?.UnnamedRank ?? "")).ToLowerInvariant();
	}

	private static bool RoleMatches(string expected, NpcDataPacket npc, string haystack)
	{
		string role = (expected ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(role) || role == "any")
		{
			return true;
		}
		string npcRole = GetCanonicalNpcRole(npc, haystack);
		if (role == "soldier" || role == "guard" || role == "patrol")
		{
			return npcRole == "soldier";
		}
		if (role == "merchant" || role == "vendor" || role == "trader")
		{
			return npcRole == "merchant";
		}
		if (role == "singer" || role == "musician" || role == "bard")
		{
			return npcRole == "musician";
		}
		if (role == "beggar")
		{
			return npcRole == "beggar";
		}
		if (role == "blacksmith" || role == "smith" || role == "weaponsmith" || role == "armorer")
		{
			return role == "weaponsmith" ? npcRole == "weaponsmith" : role == "armorer" ? npcRole == "armorer" : npcRole == "blacksmith";
		}
		if (role == "shipwright" || role == "dockworker" || role == "ship_worker" || role == "sailor")
		{
			return npcRole == "shipwright";
		}
		if (role == "stable" || role == "groom" || role == "horse_trader")
		{
			return npcRole == "horse_trader";
		}
		if (role == "tavernkeeper" || role == "bartender" || role == "wench")
		{
			return npcRole == "tavernkeeper";
		}
		if (role == "customer")
		{
			return npcRole == "customer" || npcRole == "commoner";
		}
		if (role == "villager" || role == "commoner" || role == "citizen")
		{
			return npcRole == "villager" || npcRole == "commoner";
		}
		return npcRole == role;
	}

	/// <summary>
	/// Returns one mutually-exclusive role for an unnamed scene NPC.  The old
	/// matcher treated every UnnamedRank=commoner as every profession, which
	/// made horse traders speak like blacksmiths.  Occupation-derived RoleDesc
	/// (filled by ShoutUtils) is authoritative; text is only a conservative
	/// fallback for vanilla troop ids.
	/// </summary>
	private static string GetCanonicalNpcRole(NpcDataPacket npc, string haystack)
	{
		string roleDesc = (npc?.RoleDesc ?? "").Trim().ToLowerInvariant();
		if (roleDesc.Contains("武器") || roleDesc.Contains("weaponsmith")) return "weaponsmith";
		if (roleDesc.Contains("盔甲") || roleDesc.Contains("armorer")) return "armorer";
		if (roleDesc.Contains("铁匠") || roleDesc.Contains("blacksmith") || roleDesc.Contains("smith")) return "blacksmith";
		if (roleDesc.Contains("马商") || roleDesc.Contains("马夫") || roleDesc.Contains("horse") || roleDesc.Contains("groom") || roleDesc.Contains("stable")) return "horse_trader";
		if (roleDesc.Contains("酒馆") || roleDesc.Contains("酒保") || roleDesc.Contains("女侍") || roleDesc.Contains("tavern") || roleDesc.Contains("bartender") || roleDesc.Contains("wench")) return "tavernkeeper";
		if (roleDesc.Contains("乞丐") || roleDesc.Contains("beggar")) return "beggar";
		if (roleDesc.Contains("歌") || roleDesc.Contains("乐师") || roleDesc.Contains("musician") || roleDesc.Contains("singer") || roleDesc.Contains("bard")) return "musician";
		if (roleDesc.Contains("士兵") || roleDesc.Contains("卫兵") || roleDesc.Contains("soldier") || roleDesc.Contains("guard") || roleDesc.Contains("patrol")) return "soldier";
		if (roleDesc.Contains("商") || roleDesc.Contains("merchant") || roleDesc.Contains("vendor") || roleDesc.Contains("trader")) return "merchant";
		if (roleDesc.Contains("船") || roleDesc.Contains("码头") || roleDesc.Contains("水手") || roleDesc.Contains("ship") || roleDesc.Contains("dock") || roleDesc.Contains("sailor")) return "shipwright";
		string troop = ((npc?.TroopId ?? "") + " " + (npc?.UnnamedKey ?? "")).ToLowerInvariant();
		if (troop.Contains("weaponsmith") || troop.Contains("weapon_smith")) return "weaponsmith";
		if (troop.Contains("armorer") || troop.Contains("armor_smith")) return "armorer";
		if (troop.Contains("blacksmith") || troop.Contains("smith")) return "blacksmith";
		if (troop.Contains("horse_trader") || troop.Contains("horse_merchant") || troop.Contains("groom") || troop.Contains("stable")) return "horse_trader";
		if (troop.Contains("tavern") || troop.Contains("bartender") || troop.Contains("wench")) return "tavernkeeper";
		if (troop.Contains("beggar")) return "beggar";
		if (troop.Contains("singer") || troop.Contains("musician") || troop.Contains("bard") || troop.Contains("dancer")) return "musician";
		if (troop.Contains("merchant") || troop.Contains("vendor") || troop.Contains("trader") || troop.Contains("goods_trader")) return "merchant";
		if (troop.Contains("shipwright") || troop.Contains("ship_worker") || troop.Contains("shipworker") || troop.Contains("shipyard") || troop.Contains("dockworker") || troop.Contains("dock_worker") || troop.Contains("sailor") || troop.Contains("sea_row") || troop.Contains("sea_standing")) return "shipwright";
		if (npc?.UnnamedRank == "soldier") return "soldier";
		return troop.Contains("villager") || troop.Contains("farmer") ? "villager" : "commoner";
	}

	private static string PickTextVariant(TownAmbientLine line)
	{
		if (line == null)
		{
			return "";
		}
		if (line.TextVariants != null && line.TextVariants.Count > 0)
		{
			return line.TextVariants[MBRandom.RandomInt(line.TextVariants.Count)];
		}
		return line.Text;
	}

	private static void TryApplyGesture(Agent agent, string gesture)
	{
		if (agent == null || !agent.IsActive() || string.IsNullOrWhiteSpace(gesture))
		{
			return;
		}
		try
		{
			string actionName = string.Equals(gesture, "salute", StringComparison.OrdinalIgnoreCase)
				? "act_greeting_front_2"
				: string.Equals(gesture, "cheer", StringComparison.OrdinalIgnoreCase)
					? "act_cheer_1"
					: "act_greeting_front_1";
			ActionIndexCache action = ActionIndexCache.Create(actionName);
#if BANNERLORD_1_4_OR_GREATER
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, in action))
			{
				return;
			}
			agent.SetActionChannel(0, in action, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#else
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action))
			{
				return;
			}
			agent.SetActionChannel(0, action, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
#endif
		}
		catch
		{
		}
	}

	private static string Render(string text, NpcDataPacket npc, Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext = null)
	{
		string owner = (settlement.OwnerClan ?? settlement.Village?.Bound?.OwnerClan)?.Leader?.Name?.ToString() ?? "领主";
		string town = settlement.Name?.ToString() ?? "这座城镇";
		Town contextTown = GetContextTown(settlement);
		float loyalty = contextTown?.Loyalty ?? 100f;
		float prosperity = contextTown?.Prosperity ?? 0f;
		string taxMood = loyalty < 25f ? "税吏逼得紧" : loyalty < 60f ? "税负不轻" : "税负尚可";
		int hour = CampaignTime.Now.GetHourOfDay;
		string timeGreeting = TownAmbientTime.GetGreeting(hour);
		string timePeriod = TownAmbientTime.GetDisplayName(hour);
		playerContext ??= TownAmbientPlayerContext.Empty;
		string spokenText = ExtractSpokenText(text);
		string playerAddress = playerContext.IsFormalTitleHolder ? playerContext.Title : playerContext.Name;
		return spokenText.Replace("{speaker}", npc.Name ?? "路人")
			.Replace("{town}", town)
			.Replace("{owner}", owner)
			.Replace("{culture}", npc.CultureId ?? "本地")
			.Replace("{role}", npc.RoleDesc ?? "平民")
			.Replace("{age}", npc.Age.ToString("0"))
			.Replace("{scene}", sceneTag ?? "街道")
			.Replace("{loyalty}", loyalty.ToString("0"))
			.Replace("{prosperity}", prosperity.ToString("0"))
			.Replace("{tax_mood}", taxMood)
			.Replace("{time_greeting}", timeGreeting)
			.Replace("{time_period}", timePeriod)
			.Replace("{player_name}", playerAddress)
			.Replace("{player_title}", playerContext.Title)
			.Replace("{player_status}", playerContext.Status)
			.Replace("{player_clan}", playerContext.ClanName)
			.Replace("{player_kingdom}", playerContext.KingdomName)
			.Replace("{player_gold}", playerContext.Gold.ToString("N0"))
			.Replace("{player_renown}", playerContext.Renown.ToString("0"))
			.Replace("{player_tier}", playerContext.ClanTier.ToString("0"))
			.Trim();
	}

	private static string ExtractSpokenText(string text)
	{
		string value = (text ?? "").Trim();
		int open = value.IndexOf('「');
		if (open >= 0)
		{
			value = value.Substring(open + 1);
			int close = value.LastIndexOf('」');
			if (close >= 0) value = value.Substring(0, close);
		}
		else
		{
			value = value.Replace("「", "").Replace("」", "").Replace("\"", "").Replace("“", "").Replace("”", "");
		}
		return value.Trim();
	}

	private static Town GetContextTown(Settlement settlement)
	{
		return settlement?.Town ?? settlement?.Village?.Bound?.Town;
	}

	private static TownAmbientPlayerContext BuildPlayerContext(Settlement settlement, string sceneTag)
	{
		try
		{
			Hero hero = Hero.MainHero;
			Clan clan = hero?.Clan ?? Clan.PlayerClan;
			Kingdom kingdom = clan?.Kingdom ?? hero?.MapFaction as Kingdom;
			Kingdom settlementKingdom = settlement?.MapFaction as Kingdom ?? settlement?.OwnerClan?.Kingdom;
			int gold = Math.Max(0, hero?.Gold ?? 0);
			float renown = Math.Max(0f, clan?.Renown ?? 0f);
			int clanTier = Math.Max(0, clan?.Tier ?? 0);
			// Hero.IsFactionLeader is also true for some non-kingdom faction/clan
			// states in Bannerlord.  Only an actual kingdom leader may be called
			// 陛下; an ordinary clan leader remains a traveller until ennobled.
			bool isRuler = hero != null && kingdom != null && (kingdom.Leader == hero || kingdom.RulingClan?.Leader == hero);
			bool isClanLeader = hero != null && clan?.Leader == hero;
			bool isMercenary = clan?.IsUnderMercenaryService == true || clan?.IsClanTypeMercenary == true;
			bool ownsSettlement = settlement != null && clan != null && (settlement.OwnerClan == clan || settlement.Village?.Bound?.OwnerClan == clan);
			bool isForeignKingdom = kingdom != null && settlementKingdom != null && kingdom != settlementKingdom;
			bool isForeignRuler = isRuler && isForeignKingdom;
			bool isForeignLord = hero?.IsLord == true && !ownsSettlement && !isRuler && isForeignKingdom;
			bool isForeignHostile = isForeignKingdom && kingdom != null && settlementKingdom != null && FactionManager.IsAtWarAgainstFaction(kingdom, settlementKingdom);
			int highestEquipmentValue = GetHighestEquippedItemValue(hero);
			bool hasLuxuryEquipment = highestEquipmentValue >= 10000;
			int currentHour = CampaignTime.Now.GetHourOfDay;
			bool isNight = TownAmbientTime.IsNight(currentHour);
			string name = hero?.Name?.ToString() ?? "这位旅人";
			string clanName = clan?.Name?.ToString() ?? "无家族归属";
			string kingdomName = kingdom?.Name?.ToString() ?? "无王国归属";
			bool isLord = hero?.IsLord == true || ownsSettlement;
			string title = isRuler ? "陛下" : isLord ? "大人" : isMercenary ? "雇佣兵" : "旅人";
			string status = isRuler ? "国家统治者" : ownsSettlement ? "本地领主" : isLord ? "领主" : isMercenary ? "雇佣兵" : "冒险者";
			HashSet<string> tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"any", "player", sceneTag ?? "town", TownAmbientTime.GetTag(currentHour)
			};
			if (isNight) tags.Add("night");
			else tags.Add("day");
			if (hero?.IsLord == true) tags.Add("lord");
			if (isRuler) { tags.Add("ruler"); tags.Add("king"); tags.Add("kingdom_leader"); }
			if (isForeignRuler) { tags.Add("foreign_ruler"); tags.Add(isForeignHostile ? "foreign_hostile" : "foreign_neutral"); }
			if (isForeignLord) { tags.Add("foreign_lord"); if (isForeignHostile) tags.Add("foreign_hostile"); }
			if (isClanLeader) tags.Add("clan_leader");
			if (isMercenary) { tags.Add("mercenary"); tags.Add("free_company"); }
			if (ownsSettlement) { tags.Add("settlement_lord"); tags.Add("settlement_owner"); }
			if (gold >= 50000) { tags.Add("rich"); tags.Add("wealthy"); }
			if (gold >= 150000) tags.Add("very_rich");
			if (renown >= 100) { tags.Add("famous"); tags.Add("renowned"); }
			if (renown >= 250) tags.Add("very_famous");
			if (clanTier >= 3) { tags.Add("high_tier"); tags.Add("tier_3"); }
			if (clanTier >= 4) tags.Add("tier_4");
			if (clanTier >= 5) tags.Add("tier_5");
			if (clanTier >= 6) tags.Add("tier_6");
			if (hasLuxuryEquipment) { tags.Add("luxury_equipment"); tags.Add("fine_equipment"); }
			return new TownAmbientPlayerContext(name, title, status, clanName, kingdomName, gold, renown, clanTier, isLord || isRuler, tags);
		}
		catch
		{
			return TownAmbientPlayerContext.Empty;
		}
	}

	private static int GetHighestEquippedItemValue(Hero hero)
	{
		try
		{
			int highestValue = GetHighestEquippedItemValue(hero?.BattleEquipment);
			highestValue = Math.Max(highestValue, GetHighestEquippedItemValue(hero?.CharacterObject?.FirstCivilianEquipment));
			return highestValue;
		}
		catch
		{
			return 0;
		}
	}

	private static int GetHighestEquippedItemValue(Equipment equipment)
	{
		if (equipment == null)
		{
			return 0;
		}
		try
		{
			int highestValue = 0;
			for (EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot; slot < EquipmentIndex.NumEquipmentSetSlots; slot++)
			{
				ItemObject item = equipment[slot].Item;
				if (item != null)
				{
					highestValue = Math.Max(highestValue, item.Value);
				}
			}
			return highestValue;
		}
		catch
		{
			return 0;
		}
	}

	private void RecordForLaterConversation(NpcDataPacket npc, TownAmbientLine line, string renderedText, Settlement settlement, string sceneTag, TownAmbientPlayerContext playerContext)
	{
		if (!line.RecordInMemory || _recordedMemoryKeys.Count >= MaxMemoryRecordsPerMission || string.IsNullOrWhiteSpace(npc.UnnamedKey))
		{
			return;
		}
		int day = (int)Math.Floor(CampaignTime.Now.ToDays);
		string key = day + "|" + npc.UnnamedKey + "|" + (line.Id ?? line.Text);
		if (!_recordedMemoryKeys.Add(key))
		{
			return;
		}
		string memoryId = MyBehavior.BuildNonHeroMemoryIdForExternal(npc.UnnamedKey);
		if (string.IsNullOrWhiteSpace(memoryId))
		{
			return;
		}
		string fact = "[AFEF NPC行为补充] 城镇环境台词[" + (line.Id ?? "ambient") + "]，地点=" + (settlement.Name?.ToString() ?? "未知定居点") + "，场景=" + sceneTag + "。";
		if (!string.IsNullOrWhiteSpace(line.Memory))
		{
			fact += "背景=" + Render(line.Memory, npc, settlement, sceneTag, playerContext) + "。";
		}
		if (line.ReplyHints != null && line.ReplyHints.Count > 0)
		{
			fact += "可回应方向=" + string.Join("、", line.ReplyHints.Where(x => !string.IsNullOrWhiteSpace(x)).Take(4)) + "。";
		}
		MyBehavior.AppendExternalNonHeroSceneDialogueHistory(memoryId, npc.Name, null, (renderedText ?? line.Text ?? "").Trim(), fact, ShoutBehavior.GetCurrentSceneHistorySessionIdForExternal(), npc.AgentIndex, npc.Name);
	}

	private static bool TryGetSettlementContext(Mission mission, out Settlement settlement, out string sceneTag)
	{
		settlement = null;
		sceneTag = "";
		if (mission == null)
		{
			return false;
		}
		string scene = (mission.SceneName ?? "").Trim().ToLowerInvariant();
		string locationId = GetCurrentLocationIdSafe().ToLowerInvariant();
		if (ExcludedSceneTerms.Any(term => scene.Contains(term) || locationId.Contains(term)))
		{
			return false;
		}
		try
		{
			settlement = Settlement.CurrentSettlement;
			if (settlement == null || (!settlement.IsTown && !settlement.IsVillage && !settlement.IsCastle))
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		// Location IDs are more reliable than scene filenames, especially with
		// overhaul mods that use names such as "kings_landing_center".
		if (locationId.Contains("tavern")) sceneTag = "tavern";
		else if (locationId.Contains("lordhall") || locationId.Contains("lordshall") || locationId.Contains("lord_hall") || locationId.Contains("lords_hall") || locationId.Contains("keep")) sceneTag = "lordhall";
		else if (locationId.Contains("market") || locationId.Contains("alley") || locationId.Contains("street")) sceneTag = "market";
		else if (locationId.Contains("port")) sceneTag = "port";
		else if (locationId.Contains("village") || settlement.IsVillage) sceneTag = "village";
		else if (locationId.Contains("castle") || settlement.IsCastle) sceneTag = "castle";
		else if (scene.Contains("tavern")) sceneTag = "tavern";
		else if (scene.Contains("market") || scene.Contains("alley") || scene.Contains("street")) sceneTag = "market";
		else if (scene.Contains("port")) sceneTag = "port";
		else if (scene.Contains("village")) sceneTag = "village";
		else if (scene.Contains("lordhall") || scene.Contains("lordshall") || scene.Contains("lord_hall") || scene.Contains("lords_hall") || scene.Contains("keep")) sceneTag = "lordhall";
		else if (scene.Contains("castle")) sceneTag = "castle";
		else sceneTag = settlement.IsVillage ? "village" : settlement.IsCastle ? "castle" : "town";
		return true;
	}

	private static string GetCurrentSettlementIdSafe()
	{
		try
		{
			return Settlement.CurrentSettlement?.StringId ?? "null";
		}
		catch
		{
			return "error";
		}
	}

	private static string GetCurrentLocationIdSafe()
	{
		try
		{
			return CampaignMission.Current?.Location?.StringId ?? "null";
		}
		catch
		{
			return "error";
		}
	}
}

internal sealed class TownAmbientPlayerContext
{
	public static readonly TownAmbientPlayerContext Empty = new TownAmbientPlayerContext("这位旅人", "旅人", "冒险者", "无家族归属", "无王国归属", 0, 0f, 0, false, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "any", "player", "day" });

	public TownAmbientPlayerContext(string name, string title, string status, string clanName, string kingdomName, int gold, float renown, int clanTier, bool isFormalTitleHolder, HashSet<string> tags)
	{
		Name = name ?? "这位旅人";
		Title = title ?? "旅人";
		Status = status ?? "冒险者";
		ClanName = clanName ?? "无家族归属";
		KingdomName = kingdomName ?? "无王国归属";
		Gold = Math.Max(0, gold);
		Renown = Math.Max(0f, renown);
		ClanTier = Math.Max(0, clanTier);
		IsFormalTitleHolder = isFormalTitleHolder;
		Tags = tags ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	}

	public string Name { get; }
	public string Title { get; }
	public string Status { get; }
	public string ClanName { get; }
	public string KingdomName { get; }
	public int Gold { get; }
	public float Renown { get; }
	public int ClanTier { get; }
	public bool IsFormalTitleHolder { get; }
	public HashSet<string> Tags { get; }
}

public sealed class TownAmbientDialogueConfig
{
	public int Version = 1;
	public bool Enabled = true;
	public bool AllowHeroes = false;
	public float ProbeIntervalSeconds = 7.5f;
	public float InitialDelaySeconds = 2f;
	public float GlobalCooldownSeconds = 5.5f;
	public float AgentCooldownSeconds = 18f;
	public float MinDistanceMeters = 3f;
	public float MaxDistanceMeters = 18f;
	public List<TownAmbientLine> Lines = new List<TownAmbientLine>();

	public static TownAmbientDialogueConfig Load()
	{
		TownAmbientDialogueConfig config = null;
		try
		{
			string path = AnimusForgeModulePaths.GetModuleDataFilePath("TownAmbientDialogue.json");
			if (File.Exists(path))
			{
				config = JsonConvert.DeserializeObject<TownAmbientDialogueConfig>(File.ReadAllText(path, Encoding.UTF8));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("TownAmbient", "failed to load TownAmbientDialogue.json: " + ex.Message);
		}
		config ??= new TownAmbientDialogueConfig();
		config.ProbeIntervalSeconds = Clamp(config.ProbeIntervalSeconds, 3f, 30f, 7.5f);
		config.InitialDelaySeconds = Clamp(config.InitialDelaySeconds, 0f, 20f, 2f);
		config.GlobalCooldownSeconds = Clamp(config.GlobalCooldownSeconds, 1f, 30f, 5.5f);
		config.AgentCooldownSeconds = Clamp(config.AgentCooldownSeconds, 3f, 120f, 18f);
		config.MinDistanceMeters = Clamp(config.MinDistanceMeters, 1f, 20f, 3f);
		config.MaxDistanceMeters = Clamp(config.MaxDistanceMeters, config.MinDistanceMeters + 1f, 40f, 18f);
		config.Lines ??= new List<TownAmbientLine>();
		TownAmbientTime.ValidateLines(config.Lines);
		if (config.Enabled && config.Lines.Count == 0)
		{
			Logger.Log("TownAmbient", "TownAmbientDialogue.json has no lines; ambient chatter is idle.");
		}
		return config;
	}

	private static float Clamp(float value, float min, float max, float fallback)
	{
		return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Math.Max(min, Math.Min(max, value));
	}
}

public sealed class TownAmbientLine
{
	public string Id = "";
	public bool Enabled = true;
	public List<string> Roles = new List<string>();
	public List<string> Cultures = new List<string>();
	public List<string> Genders = new List<string>();
	public List<string> SceneTags = new List<string>();
	public List<string> TimeBands = new List<string>();
	public List<string> PlayerAnyTags = new List<string>();
	public List<string> PlayerAllTags = new List<string>();
	public List<string> PlayerNoneTags = new List<string>();
	public string Gesture = "";
	public int? MinHour;
	public int? MaxHour;
	public float? MinLoyalty;
	public float? MaxLoyalty;
	public float? MinProsperity;
	public float? MaxProsperity;
	public float? MinAge;
	public float? MaxAge;
	public int? MinPlayerGold;
	public int? MaxPlayerGold;
	public float? MinPlayerRenown;
	public float? MaxPlayerRenown;
	public int? MinPlayerClanTier;
	public int? MaxPlayerClanTier;
	public float Weight = 1f;
	public float CooldownSeconds = 0f;
	public bool RecordInMemory = true;
	public bool FeastOnly = false;
	public string Text = "";
	public List<string> TextVariants = new List<string>();
	public string EventKey = "";
	public bool EventResponse = false;
	public int EventChancePercent = 0;
	public int MaxEventResponses = 0;
	public float EventResponseSpacingSeconds = 1.1f;
	public string Memory = "";
	public List<string> ReplyHints = new List<string>();

	[JsonIgnore]
	public bool HasText => !string.IsNullOrWhiteSpace(Text) || TextVariants != null && TextVariants.Any(text => !string.IsNullOrWhiteSpace(text));
}

internal sealed class TownAmbientPendingResponse
{
	public int AgentIndex;
	public float ShowAt;
	public string Text = "";
	public float TypingDuration;
	public float CooldownSeconds;
	public string LineId = "";
	public TownAmbientLine Signature;
}

internal sealed class TownAmbientAiReadyBatch
{
	public List<int> AgentIndices = new List<int>();

	public float SpacingSeconds = 1.1f;

	public float CooldownSeconds = 18f;

	public string CacheKey = "";

	public int PauseEpoch;

	public TownAmbientAiResult Result;
}
