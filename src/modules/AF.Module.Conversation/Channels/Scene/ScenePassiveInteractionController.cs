using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.SceneActions.Core;
using AnimusForge.SiegeAftermathIntervention;
using AnimusForge.XihaiAction;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.Refactor.Runtime;
using RichExecutions.Core;
using RichExecutions.Scene;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Towns;
using SandBox.Objects.AnimationPoints;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions;

using static AnimusForge.SceneMovementController;

using static AnimusForge.ShoutBehavior;
namespace AnimusForge;

internal sealed class ScenePassiveInteractionController
{
    private readonly ScenePassiveInteractionControllerPorts _ports;
    internal ScenePassiveInteractionController(ScenePassiveInteractionControllerPorts ports) { _ports = ports ?? throw new ArgumentNullException(nameof(ports)); }

	internal Agent _currentStareTarget = null;

	internal float _stareTimer = 0f;

	internal float _stareTargetLostGraceTimer = 0f;

	internal float _interactionGraceTimer = 0f;

	internal Dictionary<string, float> _passiveCooldowns = new Dictionary<string, float>(StringComparer.Ordinal);

	internal const float DEFAULT_STARE_TRIGGER_TIME = 15f;

	internal const float STARE_TARGET_LOST_GRACE = 2f;

	internal const float PASSIVE_STARE_COOLDOWN = 10f;

	internal const float ACTIVE_CHAT_COOLDOWN = 300f;

	internal const float PASSIVE_INTERACTION_GRACE = 0.75f;

	internal float _tickTimer = 0f;

	internal float _nextProactiveSceneOpeningProbeMissionTime = 0f;

	internal void TryTriggerPendingProactiveSceneOpening()
	{
		try
		{
			Mission mission = Mission.Current;
			if (mission == null || mission.Scene == null || Agent.Main == null || !Agent.Main.IsActive() || Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				return;
			}
			float missionTime = mission.CurrentTime;
			if (missionTime < _nextProactiveSceneOpeningProbeMissionTime)
			{
				return;
			}
			_nextProactiveSceneOpeningProbeMissionTime = missionTime + SceneShoutInputController.ProactiveSceneOpeningProbeIntervalSeconds;
			if (!ProactiveNpcRequestBehavior.TryPeekPendingSceneOpening(out var targetHero, out var extraFact, out var promptText))
			{
				return;
			}
			Agent targetAgent = FindProactiveSceneOpeningAgent(mission, targetHero);
			if (!CanAgentParticipateInSceneSpeech(targetAgent))
			{
				return;
			}
			string factText = BuildProactiveSceneOpeningFactText(extraFact, promptText);
			if (TriggerImmediateSceneBehaviorReaction(factText, targetAgent.Index, persistHeroPrivateHistory: true, suppressStare: false, postSpeechLeaveSeconds: 3f, skipSceneFactRecord: false, returnSceneSummonOnTimeout: false))
			{
				ProactiveNpcRequestBehavior.TryConsumePendingSceneOpeningForHero(targetHero, out var _, out var _);
				Logger.Log("ProactiveNpcRequest", "scene opening triggered hero=" + (targetHero?.StringId ?? "") + " agentIndex=" + targetAgent.Index);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("ProactiveNpcRequest", "scene opening trigger failed: " + ex.Message);
		}
	}

	internal static Agent FindProactiveSceneOpeningAgent(Mission mission, Hero targetHero)
	{
		if (mission?.Agents == null || targetHero == null)
		{
			return null;
		}
		string targetHeroId = (targetHero.StringId ?? "").Trim();
		foreach (Agent agent in mission.Agents)
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			Hero agentHero = (agent.Character as CharacterObject)?.HeroObject;
			if (agentHero == null)
			{
				continue;
			}
			if (ReferenceEquals(agentHero, targetHero) || string.Equals((agentHero.StringId ?? "").Trim(), targetHeroId, StringComparison.OrdinalIgnoreCase))
			{
				return agent;
			}
		}
		CharacterObject targetCharacter = targetHero.CharacterObject;
		if (targetCharacter == null)
		{
			return null;
		}
		return mission.Agents.FirstOrDefault(agent => agent != null && agent.IsActive() && agent.Character == targetCharacter);
	}

	internal void ResetPassiveStareTracking()
	{
		_stareTimer = 0f;
		_stareTargetLostGraceTimer = 0f;
		_currentStareTarget = null;
	}

	internal void UpdatePassiveStareLogic(float dt)
	{
		if (Mission.Current == null || Agent.Main == null || !Agent.Main.IsActive() || _isProcessingShout)
		{
			return;
		}
        if (_shoutHotkeyChargeActive || ShoutTextInputPopup.IsOpen ||
            IsScenePresentationActiveForExternal || AnimusForgeNativeConversationOverlay.IsOpen ||
            Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			ResetPassiveStareTracking();
			return;
		}
		if (IsMultiNpcSceneConversationActive())
		{
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_currentStareTarget = null;
			UpdateCooldowns(dt);
			return;
		}
		if (_interactionGraceTimer > 0f)
		{
			_stareTimer = 0f;
			_stareTargetLostGraceTimer = 0f;
			_currentStareTarget = null;
			return;
		}
		Agent closestFacingAgent = ShoutUtils.GetClosestFacingAgent(6.5f);
		if (closestFacingAgent != null)
		{
			_stareTargetLostGraceTimer = 0f;
			if (closestFacingAgent == _currentStareTarget)
			{
				_stareTimer += dt;
				if (_stareTimer >= GetPassiveStareTriggerTime() && IsCooldownReady(closestFacingAgent))
				{
					TriggerPassiveReaction(closestFacingAgent);
					_stareTimer = 0f;
				}
			}
			else
			{
				_currentStareTarget = closestFacingAgent;
				_stareTimer = 0f;
			}
		}
		else if (_currentStareTarget != null)
		{
			_stareTargetLostGraceTimer += dt;
			if (_stareTargetLostGraceTimer >= STARE_TARGET_LOST_GRACE)
			{
				_currentStareTarget = null;
				_stareTimer = 0f;
				_stareTargetLostGraceTimer = 0f;
			}
		}
		else
		{
			_stareTargetLostGraceTimer = 0f;
			_stareTimer = 0f;
		}
		UpdateCooldowns(dt);
	}

	internal static float GetPassiveStareTriggerTime()
	{
		try
		{
			int seconds = DuelSettings.GetSettings()?.PassiveStareTriggerSeconds ?? (int)DEFAULT_STARE_TRIGGER_TIME;
			return Math.Max(1f, Math.Min(120f, seconds));
		}
		catch
		{
			return DEFAULT_STARE_TRIGGER_TIME;
		}
	}

	internal void UpdateCooldowns(float dt)
	{
		List<string> list = new List<string>(_passiveCooldowns.Keys);
		foreach (string item in list)
		{
			_passiveCooldowns[item] -= dt;
			if (_passiveCooldowns[item] <= 0f)
			{
				_passiveCooldowns.Remove(item);
			}
		}
	}

	internal string GetCooldownIdentityKey(NpcDataPacket data, int fallbackAgentIndex = -1)
	{
		if (data != null)
		{
			if (data.IsHero)
			{
				Hero hero = ResolveHeroFromAgentIndex(data.AgentIndex);
				string heroId = (hero?.StringId ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					return "hero:" + heroId;
				}
			}
			string unnamedKey = (data.UnnamedKey ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(unnamedKey))
			{
				int idx = data.AgentIndex;
				if (idx < 0)
				{
					idx = fallbackAgentIndex;
				}
				if (idx >= 0)
				{
					return "unnamed:" + unnamedKey + "|agent:" + idx;
				}
				return "unnamed:" + unnamedKey;
			}
			string troopId = (data.TroopId ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(troopId))
			{
				int idx2 = data.AgentIndex;
				if (idx2 < 0)
				{
					idx2 = fallbackAgentIndex;
				}
				if (idx2 >= 0)
				{
					return "troop:" + troopId + "|agent:" + idx2;
				}
				return "troop:" + troopId;
			}
			if (data.AgentIndex >= 0)
			{
				return "agent:" + data.AgentIndex;
			}
		}
		if (fallbackAgentIndex >= 0)
		{
			return "agent:" + fallbackAgentIndex;
		}
		return "";
	}

	internal string GetCooldownIdentityKey(Agent agent)
	{
		if (agent == null || !agent.IsActive() || !agent.IsHuman)
		{
			return "";
		}
		try
		{
			if (agent.Character is CharacterObject { HeroObject: not null } characterObject)
			{
				string heroId = (characterObject.HeroObject?.StringId ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(heroId))
				{
					return "hero:" + heroId;
				}
			}
		}
		catch
		{
		}
		NpcDataPacket data = null;
		try
		{
			data = ShoutUtils.ExtractNpcData(agent);
		}
		catch
		{
			data = null;
		}
		return GetCooldownIdentityKey(data, agent.Index);
	}

	internal void ApplyInteractionGraceAndGroupCooldown(float graceSeconds, float cooldownSeconds, IEnumerable<Agent> participants, Agent extraTarget = null, IEnumerable<NpcDataPacket> participantsData = null)
	{
		_interactionGraceTimer = Math.Max(_interactionGraceTimer, graceSeconds);
		HashSet<string> affectedIdentityKeys = new HashSet<string>(StringComparer.Ordinal);
		if (participantsData != null)
		{
			foreach (NpcDataPacket npc in participantsData)
			{
				string identityKey = GetCooldownIdentityKey(npc, npc?.AgentIndex ?? (-1));
				if (!string.IsNullOrWhiteSpace(identityKey))
				{
					affectedIdentityKeys.Add(identityKey);
				}
			}
		}
		if (participants != null)
		{
			foreach (Agent participant in participants)
			{
				string identityKey2 = GetCooldownIdentityKey(participant);
				if (!string.IsNullOrWhiteSpace(identityKey2))
				{
					affectedIdentityKeys.Add(identityKey2);
				}
			}
		}
		if (extraTarget != null)
		{
			string identityKey3 = GetCooldownIdentityKey(extraTarget);
			if (!string.IsNullOrWhiteSpace(identityKey3))
			{
				affectedIdentityKeys.Add(identityKey3);
			}
		}
		foreach (string affectedIdentityKey in affectedIdentityKeys)
		{
			if (_passiveCooldowns.TryGetValue(affectedIdentityKey, out var currentCooldown))
			{
				_passiveCooldowns[affectedIdentityKey] = Math.Max(currentCooldown, cooldownSeconds);
			}
			else
			{
				_passiveCooldowns[affectedIdentityKey] = cooldownSeconds;
			}
		}
	}

	internal bool IsCooldownReady(Agent agent)
	{
		if (agent == null)
		{
			return false;
		}
		string identityKey = GetCooldownIdentityKey(agent);
		if (string.IsNullOrWhiteSpace(identityKey))
		{
			return false;
		}
		return !_passiveCooldowns.ContainsKey(identityKey);
	}

	internal List<Agent> GetPassiveCooldownGroupAgents(Agent targetAgent)
	{
		List<Agent> list = new List<Agent>();
		Mission mission = Mission.Current;
		var agents = mission?.Agents;
		if (targetAgent == null || !targetAgent.IsActive() || agents == null || Agent.Main == null || !Agent.Main.IsActive())
		{
			return list;
		}
		Vec3 playerPos = Agent.Main.Position;
		Vec3 playerLook = Agent.Main.LookDirection;
		foreach (Agent agent in agents)
		{
			if (agent == null || agent == Agent.Main || !agent.IsActive() || !agent.IsHuman)
			{
				continue;
			}
			float num = agent.Position.Distance(targetAgent.Position);
			if (num > 7f)
			{
				continue;
			}
			if (agent == targetAgent || num <= 3f)
			{
				list.Add(agent);
				continue;
			}
			Vec3 v = agent.Position - playerPos;
			v.Normalize();
			if (Vec3.DotProduct(playerLook, v) > 0.866f)
			{
				list.Add(agent);
			}
		}
		if (!list.Any((Agent a) => a != null && a.Index == targetAgent.Index))
		{
			list.Add(targetAgent);
		}
		return list;
	}

	internal void TriggerPassiveReaction(Agent targetAgent)
	{
		if (targetAgent == null || _isProcessingShout || IsMultiNpcSceneConversationActive())
		{
			return;
		}
		_isProcessingShout = true;
		NpcDataPacket npcData = ShoutUtils.ExtractNpcData(targetAgent);
		if (npcData == null)
		{
			_isProcessingShout = false;
			return;
		}
		string sceneDesc = ShoutUtils.GetCurrentSceneDescription();
		List<Agent> source = GetPassiveCooldownGroupAgents(targetAgent);
		List<NpcDataPacket> allNpcData = (from a in source
			select ShoutUtils.ExtractNpcData(a) into d
			where d != null
			select d).ToList();
		if (!allNpcData.Any((NpcDataPacket d) => d != null && d.AgentIndex == npcData.AgentIndex))
		{
			allNpcData.Add(npcData);
		}
		ApplyInteractionGraceAndGroupCooldown(PASSIVE_INTERACTION_GRACE, PASSIVE_STARE_COOLDOWN, source, targetAgent, allNpcData);
		InformationManager.DisplayMessage(new InformationMessage("你盯着 " + npcData.Name + " 看了很久...", new Color(0.7f, 0.7f, 0.7f)));
		string factText = BuildPassiveReactionFactText(targetAgent);
		try
		{
			TriggerImmediateSceneBehaviorReactionForExternal(factText, npcData.AgentIndex, persistHeroPrivateHistory: true, suppressStare: true);
		}
		catch (Exception ex)
		{
			Logger.Log("ShoutBehavior", "[ERROR] TriggerPassiveReaction immediate failed: " + ex.Message);
		}
		finally
		{
			_isProcessingShout = false;
		}
	}

	internal static string BuildPassiveReactionFactText(Agent targetAgent)
	{
		if (ShouldUseCombatPassiveReactionText(targetAgent, out var armed))
		{
			bool flag = IsPassiveReactionOpponentContext();
			return armed ? BuildCombatPassiveArmedFactText(targetAgent, flag) : BuildCombatPassiveBrawlFactText(flag);
		}
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		return playerName + "看着你";
	}

	internal static bool ShouldUseCombatPassiveReactionText(Agent targetAgent, out bool armed)
	{
		armed = false;
		try
		{
			if (IsSceneConversationMissionEnding())
			{
				return false;
			}
			if (IsMeetingPseudoCombatContext())
			{
				return false;
			}
			Agent main = Agent.Main;
			if (main == null || !main.IsActive() || targetAgent == null || !targetAgent.IsActive())
			{
				return false;
			}
			bool flag = IsActiveSceneConversationDuelCombat();
			if (!flag)
			{
				try
				{
					MissionFightHandler missionBehavior = Mission.Current?.GetMissionBehavior<MissionFightHandler>();
					flag = missionBehavior != null && missionBehavior.IsThereActiveFight();
				}
				catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				try
				{
					if (AreAgentsHostileForSceneConversation(main, targetAgent))
					{
						flag = true;
					}
				}
				catch
				{
					flag = false;
				}
			}
			if (!flag)
			{
				return false;
			}
			armed = IsAgentUsingRealWeaponForPassiveReaction(main) || IsAgentUsingRealWeaponForPassiveReaction(targetAgent);
			return true;
		}
		catch
		{
			armed = false;
			return false;
		}
	}

	internal static bool IsPassiveReactionOpponentContext()
	{
		try
		{
			if (IsActiveSceneConversationDuelCombat())
			{
				return true;
			}
			if (IsArenaLikePassiveReactionContext())
			{
				return true;
			}
			return false;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsArenaLikePassiveReactionContext()
	{
		try
		{
			if (DuelBehavior.IsArenaMissionActive)
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text = (CampaignMission.Current?.Location?.StringId ?? "").Trim().ToLowerInvariant();
			if (text == "arena")
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text2 = (Mission.Current?.SceneName ?? "").Trim().ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(text2) && text2.Contains("arena"))
			{
				return true;
			}
		}
		catch
		{
		}
		try
		{
			string text3 = (ShoutUtils.GetCurrentSceneDescription() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text3) && text3.IndexOf("竞技场", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		catch
		{
		}
		return HasMissionBehaviorTypeNameForPassiveReaction("ArenaPracticeFightMissionController", "TournamentFightMissionController", "ArenaDuelMissionController", "TournamentJoustingMissionController", "TournamentArcheryMissionController");
	}

	internal static bool HasMissionBehaviorTypeNameForPassiveReaction(params string[] typeNames)
	{
		try
		{
			Mission current = Mission.Current;
			if (current == null || typeNames == null || typeNames.Length == 0)
			{
				return false;
			}
			foreach (MissionBehavior missionBehavior in current.MissionBehaviors)
			{
				string text = missionBehavior?.GetType()?.Name;
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}
				for (int i = 0; i < typeNames.Length; i++)
				{
					if (string.Equals(text, typeNames[i], StringComparison.Ordinal))
					{
						return true;
					}
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static string BuildCombatPassiveBrawlFactText(bool opponentContext)
	{
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你和" + playerName + "只是对手，正在赤手较量") : ("[AFEF NPC行为补充] 你和" + playerName + "互为敌人，正在互相殴打");
	}

	internal static string BuildCombatPassiveArmedFactText(Agent targetAgent, bool opponentContext)
	{
		string playerName = GetPlayerDisplayNameForShout();
		if (string.IsNullOrWhiteSpace(playerName))
		{
			playerName = "玩家";
		}
		string text = TryGetActiveWeaponDisplayNameForPassiveReaction(targetAgent);
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "武器";
		}
		return opponentContext ? ("[AFEF NPC行为补充] 你现在拿着" + text + "与" + playerName + "作为对手激烈交锋，但还未决出胜负") : ("[AFEF NPC行为补充] 你现在正拿着" + text + "与" + playerName + "作为敌人拼杀，还未决出胜负");
	}

	internal static string BuildFallbackSceneTauntSpeech(bool escalatedToFight)
	{
		return escalatedToFight ? "少废话，既然你想找打，那就来吧。" : "嘴巴放干净点，别逼我动手。";
	}

	internal static string BuildCombatActiveShoutExtraFact(Agent targetAgent)
	{
		try
		{
			if (!ShouldUseCombatPassiveReactionText(targetAgent, out var armed))
			{
				return "";
			}
			bool opponentContext = IsPassiveReactionOpponentContext();
			return armed ? BuildCombatPassiveArmedFactText(targetAgent, opponentContext) : BuildCombatPassiveBrawlFactText(opponentContext);
		}
		catch
		{
			return "";
		}
	}

	internal static bool IsActiveSceneConversationDuelCombat()
	{
		try
		{
			if (DuelBehavior.IsDuelEnded)
			{
				return false;
			}
			if (DuelBehavior.IsArenaMissionActive)
			{
				return true;
			}
			if (!DuelBehavior.IsFormalDuelActive)
			{
				return false;
			}
			return !DuelBehavior.IsFormalDuelPreFightActive;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsMeetingPseudoCombatContext()
	{
		try
		{
			return MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated && !IsActiveSceneConversationDuelCombat();
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsSceneConversationMissionEnding()
	{
		try
		{
			Mission current = Mission.Current;
			return current != null && (current.IsMissionEnding || current.MissionEnded);
		}
		catch
		{
			return false;
		}
	}

	internal static string TryGetActiveWeaponDisplayNameForPassiveReaction(Agent agent)
	{
		try
		{
			if (agent == null)
			{
				return "";
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(agent, primaryWieldedItemIndex, out var weaponName))
			{
				return weaponName;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			if (TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(agent, offhandWieldedItemIndex, out weaponName))
			{
				return weaponName;
			}
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[equipmentIndex]))
				{
					continue;
				}
				string text3 = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(text3))
				{
					return text3.Trim();
				}
			}
		}
		catch
		{
		}
		return "";
	}

	internal static bool IsAgentUsingRealWeaponForPassiveReaction(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsHuman || !agent.IsActive())
			{
				return false;
			}
			EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
			if (IsRealWeaponWieldedSlotForPassiveReaction(agent, primaryWieldedItemIndex))
			{
				return true;
			}
			EquipmentIndex offhandWieldedItemIndex = agent.GetOffhandWieldedItemIndex();
			return IsRealWeaponWieldedSlotForPassiveReaction(agent, offhandWieldedItemIndex);
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGetRealWeaponDisplayNameFromWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex, out string weaponName)
	{
		weaponName = "";
		try
		{
			if (!IsRealWeaponWieldedSlotForPassiveReaction(agent, equipmentIndex))
			{
				return false;
			}
			string text = null;
			try
			{
				text = agent.Equipment[equipmentIndex].Item?.Name?.ToString();
			}
			catch
			{
				text = null;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				try
				{
					text = agent.SpawnEquipment[equipmentIndex].Item?.Name?.ToString();
				}
				catch
				{
					text = null;
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			weaponName = text.Trim();
			return weaponName.Length > 0;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsRealWeaponWieldedSlotForPassiveReaction(Agent agent, EquipmentIndex equipmentIndex)
	{
		try
		{
			if (agent == null || equipmentIndex == EquipmentIndex.None || equipmentIndex < EquipmentIndex.WeaponItemBeginSlot || equipmentIndex >= EquipmentIndex.NumAllWeaponSlots)
			{
				return false;
			}
			if (IsRealWeaponMissionWeaponForPassiveReaction(agent.Equipment[equipmentIndex]))
			{
				return true;
			}
			try
			{
				return IsRealWeaponEquipmentElementForPassiveReaction(agent.SpawnEquipment[equipmentIndex]);
			}
			catch
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsRealWeaponMissionWeaponForPassiveReaction(MissionWeapon missionWeapon)
	{
		try
		{
			WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
			return currentUsageItem != null && !currentUsageItem.IsShield;
		}
		catch
		{
			return false;
		}
	}

	internal static bool IsRealWeaponEquipmentElementForPassiveReaction(EquipmentElement equipmentElement)
	{
		try
		{
			ItemObject item = equipmentElement.Item;
			if (item == null)
			{
				return false;
			}
			WeaponComponentData primaryWeapon = item.PrimaryWeapon;
			return primaryWeapon != null && !primaryWeapon.IsShield && item.Type != ItemObject.ItemTypeEnum.Shield;
		}
		catch
		{
			return false;
		}
	}

    private bool _isProcessingShout { get => _ports.Get_isProcessingShout(); set => _ports.Set_isProcessingShout(value); }
    private bool _shoutHotkeyChargeActive { get => _ports.Get_shoutHotkeyChargeActive(); set => _ports.Set_shoutHotkeyChargeActive(value); }
    private bool IsMultiNpcSceneConversationActive() => _ports.IsMultiNpcSceneConversationActive_L7716();
    private Hero ResolveHeroFromAgentIndex(int agentIndex) => _ports.ResolveHeroFromAgentIndex_L17635(agentIndex);
    private bool TriggerImmediateSceneBehaviorReaction(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds = -1f, bool skipSceneFactRecord = false, bool returnSceneSummonOnTimeout = false, Action onNoSpeech = null, bool runSiegeReactionPostprocess = false, Func<bool> canStillPublish = null, Action<bool> onCompleted = null) => _ports.TriggerImmediateSceneBehaviorReaction_L147(factText, targetAgentIndex, persistHeroPrivateHistory, suppressStare, postSpeechLeaveSeconds, skipSceneFactRecord, returnSceneSummonOnTimeout, onNoSpeech, runSiegeReactionPostprocess, canStillPublish, onCompleted);
}

internal sealed class ScenePassiveInteractionControllerPorts
{
    internal Func<bool> Get_isProcessingShout;
    internal Action<bool> Set_isProcessingShout;
    internal Func<bool> Get_shoutHotkeyChargeActive;
    internal Action<bool> Set_shoutHotkeyChargeActive;
    internal delegate bool IsMultiNpcSceneConversationActive_L7716Callback();
    internal IsMultiNpcSceneConversationActive_L7716Callback IsMultiNpcSceneConversationActive_L7716;
    internal delegate Hero ResolveHeroFromAgentIndex_L17635Callback(int agentIndex);
    internal ResolveHeroFromAgentIndex_L17635Callback ResolveHeroFromAgentIndex_L17635;
    internal delegate bool TriggerImmediateSceneBehaviorReaction_L147Callback(string factText, int targetAgentIndex, bool persistHeroPrivateHistory, bool suppressStare, float postSpeechLeaveSeconds, bool skipSceneFactRecord, bool returnSceneSummonOnTimeout, Action onNoSpeech, bool runSiegeReactionPostprocess, Func<bool> canStillPublish, Action<bool> onCompleted);
    internal TriggerImmediateSceneBehaviorReaction_L147Callback TriggerImmediateSceneBehaviorReaction_L147;
}
