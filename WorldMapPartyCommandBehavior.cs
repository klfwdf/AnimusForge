using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	private const string LogSource = "WorldMapCommand";
	private const string StorageKey = "_af_worldmap_party_command_queues_v1";
	private const float SettlementArrivalDistance = 3.0f;
	private const float PatrolArrivalDistance = 8.0f;
	private const float PatrolLeashDistance = 24.0f;
	private const float PartyArrivalDistance = 4.0f;
	private const float FollowArrivalDistance = 10.0f;
	private const float FollowLeashDistance = 24.0f;
	private const float EngageCommitDistance = 6.0f;
	private const float EngageMaintainDistance = 10.0f;
	private const float FriendlySupportRadius = 12.0f;
	private const float AiAttackStrengthRatio = 0.85f;
	private const int DefaultHeroAttackDays = 1;
	private const int DefaultSiegeAttackDays = 15;
	private const int DefaultRaidAttackDays = 5;
	private const string AttackModeAi = "AI";
	private const string AttackModeForce = "FORCE";
	private const string LegacyAttackModeRebellionForce = "REBELLION_FORCE";

	private static readonly Regex WorldMapOrderTagRegex = new Regex("\\[ACTION:WORLDMAP_ORDER:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private readonly Dictionary<string, PartyCommandQueueState> _queues = new Dictionary<string, PartyCommandQueueState>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PendingCreateCompanionPartyRequest> _pendingCreatePartyRequests = new Dictionary<string, PendingCreateCompanionPartyRequest>(StringComparer.OrdinalIgnoreCase);
	private readonly object _queueLock = new object();
	private bool _isOpeningCreateCompanionPartyScreen;

	public static WorldMapPartyCommandBehavior Instance { get; private set; }

	private enum CommandKind
	{
		GoToSettlement,
		PatrolSettlement,
		FollowHero,
		FollowParty,
		AttackHero,
		AttackParty,
		MergeToPlayer,
		CreateCompanionParty
	}

	private enum CommandStage
	{
		New,
		Traveling,
		Active,
		Tracking,
		Engaging
	}

	private enum CommandResultOutcome
	{
		Success,
		Failure,
		Incomplete
	}

	private enum CommandMessageTone
	{
		Success,
		Progress,
		Failure,
		Neutral
	}

	private sealed class PartyCommandQueueState
	{
		public string HeroId;
		public List<PartyCommandEntry> Commands = new List<PartyCommandEntry>();
		public int CurrentIndex;
		public string Stage = CommandStage.New.ToString();
		public double CommandStartDay;
		public double ArrivalDay = -1.0;
		public double TimeoutDay = -1.0;
		public bool EngageCommitted;
		public string LastIssuedActionKey;
		public string LastStatusMessageKey;
		public string ResultKind;
		public string ResultTargetType;
		public string ResultTargetId;
		public string ResultTargetName;
		public string ResultActorFactionId;
		public string ResultTargetFactionId;
		public double ResultCommitDay = -1.0;
		public double ResultDeadlineDay = -1.0;
		public bool ResultLogged;
	}

	private sealed class PartyCommandEntry
	{
		public string Kind;
		public string TargetType;
		public string TargetId;
		public int Days = 1;
		public string Mode;
	}

	private sealed class PendingCreateCompanionPartyRequest
	{
		public string HeroId;
		public List<PartyCommandEntry> FollowUpCommands = new List<PartyCommandEntry>();
	}

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnCampaignTick);
		CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.VillageStateChanged.AddNonSerializedListener(this, OnVillageStateChanged);
	}

	public override void SyncData(IDataStore dataStore)
	{
		Dictionary<string, string> storage = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (dataStore.IsSaving)
		{
			lock (_queueLock)
			{
				foreach (KeyValuePair<string, PartyCommandQueueState> pair in _queues)
				{
					if (pair.Value != null && !string.IsNullOrWhiteSpace(pair.Key))
					{
						storage[pair.Key] = JsonConvert.SerializeObject(pair.Value);
					}
				}
			}
		}
		dataStore.SyncData(StorageKey, ref storage);
		if (!dataStore.IsLoading)
		{
			return;
		}
		lock (_queueLock)
		{
			_queues.Clear();
			foreach (KeyValuePair<string, string> pair in storage ?? new Dictionary<string, string>())
			{
				try
				{
					PartyCommandQueueState state = JsonConvert.DeserializeObject<PartyCommandQueueState>(pair.Value ?? "");
					if (state == null || string.IsNullOrWhiteSpace(state.HeroId) || state.Commands == null || state.Commands.Count == 0)
					{
						continue;
					}
					NormalizeState(state);
					_queues[state.HeroId] = state;
				}
				catch (Exception ex)
				{
					Log("load failed key=" + pair.Key + " error=" + ex.Message);
				}
			}
		}
	}

	public static bool HasWorldMapOrderTag(string text)
	{
		return WorldMapOrderTagRegex.IsMatch(text ?? "");
	}

	public static string StripWorldMapOrderTags(string text)
	{
		return WorldMapOrderTagRegex.Replace(text ?? "", "").Trim();
	}

	public static string NormalizeWorldMapOrderTagsForExternal(string raw)
	{
		List<string> tags = new List<string>();
		foreach (Match match in WorldMapOrderTagRegex.Matches(raw ?? ""))
		{
			if (TryParseTag(match.Value, validateTargets: true, out PartyCommandEntry command, out bool stop))
			{
				if (stop)
				{
					tags.Add("[ACTION:WORLDMAP_ORDER:STOP]");
				}
				else
				{
					string normalized = BuildTag(command);
					if (!string.IsNullOrWhiteSpace(normalized))
					{
						tags.Add(normalized);
					}
				}
			}
		}
		return string.Join("\n", tags).Trim();
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRulesForExternal(Hero targetHero)
	{
		List<PostprocessRuleEntry> rules = AIConfigHandler.GetGuardrailRulePostprocessRules("worldmap_party_command") ?? new List<PostprocessRuleEntry>();
		bool targetInPlayerParty = CanInjectCreateCompanionPartyRule(targetHero);
		List<PostprocessRuleEntry> filtered = new List<PostprocessRuleEntry>();
		foreach (PostprocessRuleEntry rule in rules)
		{
			if (rule == null)
			{
				continue;
			}
			if (IsCreateCompanionPartyPostprocessRule(rule) && !targetInPlayerParty)
			{
				continue;
			}
			if (IsMergeToPlayerPostprocessRule(rule))
			{
				filtered.Add(ClonePostprocessRule(rule, BuildMergeToPlayerPostprocessDescription(targetInPlayerParty)));
				continue;
			}
			filtered.Add(ClonePostprocessRule(rule));
		}
		return filtered;
	}

	private static bool CanInjectCreateCompanionPartyRule(Hero targetHero)
	{
		try
		{
			return targetHero != null
				&& targetHero != Hero.MainHero
				&& targetHero.Clan == Clan.PlayerClan
				&& targetHero.PartyBelongedTo == MobileParty.MainParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCreateCompanionPartyPostprocessRule(PostprocessRuleEntry rule)
	{
		string tag = (rule?.Tag ?? "").Trim();
		return tag.IndexOf("WORLDMAP_ORDER:CREATE_COMPANION_PARTY", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static bool IsMergeToPlayerPostprocessRule(PostprocessRuleEntry rule)
	{
		string tag = (rule?.Tag ?? "").Trim();
		return tag.IndexOf("WORLDMAP_ORDER:MERGE_TO_PLAYER", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static PostprocessRuleEntry ClonePostprocessRule(PostprocessRuleEntry rule, string descriptionOverride = null)
	{
		return new PostprocessRuleEntry
		{
			Tag = (rule?.Tag ?? "").Trim(),
			Description = (descriptionOverride ?? rule?.Description ?? "").Trim()
		};
	}

	private static string BuildMergeToPlayerPostprocessDescription(bool targetInPlayerParty)
	{
		if (targetInPlayerParty)
		{
			return "如果玩家没有主动提到无需回归队伍，那么你必须输出此标签在最下方";
		}
		return "Independent companion-party variant. Output this only when the NPC already leads an independent player-clan companion party and clearly agrees to move back to the player and merge that independent party into the player main party. {days} is the approach/merge timeout; use 1 if unspecified. Do not use this for ordinary lords, enemies, non-player-clan heroes, or heroes who are already in the player's party unless the player-party temporary-create-party variant applies.";
	}

	public static bool TryApplyWorldMapOrderTagsForExternal(Hero targetHero, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		string original = content ?? "";
		try
		{
			List<PartyCommandEntry> commands = new List<PartyCommandEntry>();
			bool hasAnyWorldMapTag = false;
			bool stop = false;
			foreach (Match match in WorldMapOrderTagRegex.Matches(original))
			{
				hasAnyWorldMapTag = true;
				if (!TryParseTag(match.Value, validateTargets: true, out PartyCommandEntry command, out bool isStop))
				{
					continue;
				}
				if (isStop)
				{
					stop = true;
					break;
				}
				if (command != null)
				{
					commands.Add(command);
				}
			}
			content = StripWorldMapOrderTags(original);
			if (!hasAnyWorldMapTag)
			{
				return false;
			}
			WorldMapPartyCommandBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<WorldMapPartyCommandBehavior>();
			if (behavior == null)
			{
				notifications.Add("大地图命令系统未初始化。");
				return false;
			}
			if (targetHero == null)
			{
				notifications.Add("大地图命令失败：当前说话对象不是可指挥的英雄。");
				return false;
			}
			if (stop)
			{
				behavior.StopQueue(targetHero, "tag_stop", out string stopFact);
				if (!string.IsNullOrWhiteSpace(stopFact))
				{
					generatedFacts.Add(stopFact);
				}
				notifications.Add(GetHeroName(targetHero) + "已停止当前大地图命令。");
				return true;
			}
			if (commands.Count == 0)
			{
				notifications.Add("大地图命令失败：没有可执行的有效目标 ID。");
				return false;
			}
			if (commands.Count > 0 && IsKind(commands[0], CommandKind.CreateCompanionParty))
			{
				List<PartyCommandEntry> followUpCommands = commands.Skip(1).ToList();
				bool opened = behavior.TryOpenCreateCompanionParty(targetHero, followUpCommands, out string createMessage);
				notifications.Add(createMessage);
				generatedFacts.Add("[AFEF NPC行为补充] " + GetHeroName(targetHero) + (opened
					? (followUpCommands.Count > 0 ? "接受创建一支同伴部队，建队完成后将继续执行后续大地图命令。" : "同意按原版流程创建一支同伴部队。")
					: "无法创建同伴部队：" + createMessage));
				return opened;
			}
			if (!behavior.TryReplaceQueue(targetHero, commands, out string fact, out string message))
			{
				if (!string.IsNullOrWhiteSpace(message))
				{
					notifications.Add(message);
				}
				return false;
			}
			if (!string.IsNullOrWhiteSpace(fact))
			{
				generatedFacts.Add(fact);
			}
			notifications.Add(message);
			return true;
		}
		catch (Exception ex)
		{
			content = StripWorldMapOrderTags(original);
			notifications.Add("大地图命令处理失败：" + ex.Message);
			Log("apply tags failed: " + ex);
			return false;
		}
	}

	public static void ProcessWorldMapOrderTagsDispatch(Hero targetHero, ref string content)
	{
		if (!TryApplyWorldMapOrderTagsForExternal(targetHero, ref content, out List<string> facts, out List<string> notifications))
		{
			return;
		}
		foreach (string fact in facts ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(fact))
			{
				MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
			}
		}
		foreach (string notification in notifications ?? new List<string>())
		{
			if (!string.IsNullOrWhiteSpace(notification))
			{
				InformationManager.DisplayMessage(new InformationMessage(notification, notification.IndexOf("失败", StringComparison.OrdinalIgnoreCase) >= 0 ? new Color(1f, 0.45f, 0.25f) : new Color(0.4f, 1f, 0.4f)));
			}
		}
	}

	private bool TryReplaceQueue(Hero hero, List<PartyCommandEntry> commands, out string fact, out string message)
	{
		fact = "";
		message = "";
		if (hero == null || hero == Hero.MainHero)
		{
			message = "大地图命令失败：不能这样指挥玩家本人的部队。";
			return false;
		}
		List<PartyCommandEntry> safeCommands = (commands ?? new List<PartyCommandEntry>()).Where(IsExecutableCommand).Select(CloneCommand).ToList();
		if (safeCommands.Count == 0)
		{
			message = "大地图命令失败：没有通过 ID 校验的命令。";
			return false;
		}
		MobileParty party = ResolveActorParty(hero);
		if (party == null)
		{
			message = "大地图命令失败：" + GetHeroName(hero) + "当前没有独立可控制部队。";
			return false;
		}
		LeaveArmyIfNeeded(party);
		ReleasePartyAi(party);
		PartyCommandQueueState state = new PartyCommandQueueState
		{
			HeroId = hero.StringId,
			Commands = safeCommands,
			CurrentIndex = 0,
			Stage = CommandStage.New.ToString()
		};
		lock (_queueLock)
		{
			_queues[hero.StringId] = state;
		}
		StartCurrentCommand(hero, party, state);
		fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "接受了玩家的大地图命令队列，共" + safeCommands.Count + "道命令。";
		message = GetHeroName(hero) + "已接受大地图命令队列（" + safeCommands.Count + "道）。";
		return true;
	}

	private void StopQueue(Hero hero, string reason, out string fact)
	{
		fact = "";
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		MobileParty party = ResolveActorParty(hero, allowNonLeaderForRelease: true);
		if (party != null)
		{
			PartyCommandQueueState state;
			lock (_queueLock)
			{
				_queues.TryGetValue(hero.StringId, out state);
			}
			AbortCurrentCommandIfNeeded(party, state);
			ReleasePartyAi(party);
		}
		lock (_queueLock)
		{
			_queues.Remove(hero.StringId);
		}
		fact = "[AFEF NPC行为补充] " + GetHeroName(hero) + "停止了当前大地图命令，回归原版行动状态。";
		Log("stop hero=" + hero.StringId + " reason=" + reason);
	}

	private void OnHourlyTickParty(MobileParty party)
	{
		try
		{
			Hero hero = party?.LeaderHero;
			if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
			{
				return;
			}
			PartyCommandQueueState state;
			lock (_queueLock)
			{
				if (!_queues.TryGetValue(hero.StringId, out state) || state == null)
				{
					return;
				}
			}
			ProcessQueueTick(hero, party, state);
		}
		catch (Exception ex)
		{
			Log("hourly tick failed: " + ex);
		}
	}

	private void OnCampaignTick(float dt)
	{
		try
		{
			ProcessPendingCreateCompanionPartyRequests();
		}
		catch (Exception ex)
		{
			Log("pending create party tick failed: " + ex);
		}
	}

	private void ProcessPendingCreateCompanionPartyRequests()
	{
		if (_isOpeningCreateCompanionPartyScreen || !CanOpenCreateCompanionPartyScreenNow(out _))
		{
			return;
		}
		PendingCreateCompanionPartyRequest request = null;
		lock (_queueLock)
		{
			if (_pendingCreatePartyRequests.Count == 0)
			{
				return;
			}
			request = _pendingCreatePartyRequests.Values.FirstOrDefault();
			if (request != null)
			{
				_pendingCreatePartyRequests.Remove(request.HeroId ?? "");
			}
		}
		if (request == null || string.IsNullOrWhiteSpace(request.HeroId))
		{
			return;
		}
		Hero hero = ResolveHeroByIdAny(request.HeroId);
		if (hero == null)
		{
			Log("pending create skipped missing hero=" + request.HeroId);
			return;
		}
		if (!TryOpenCreateCompanionParty(hero, request.FollowUpCommands, out string message))
		{
			LogFact(hero, GetHeroName(hero) + "无法创建同伴部队：" + message);
		}
	}

	private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
	{
		try
		{
			string heroId = destroyedParty?.LeaderHero?.StringId;
			string partyId = destroyedParty?.StringId;
			if (!string.IsNullOrWhiteSpace(heroId))
			{
				PartyCommandQueueState actorState = null;
				lock (_queueLock)
				{
					_queues.TryGetValue(heroId, out actorState);
				}
				if (actorState != null && IsCurrentAttackCommand(actorState))
				{
					TryCompleteCurrentAttackResult(actorState, CommandResultOutcome.Failure, "执行者部队已被消灭。", "actor_party_destroyed");
				}
				else
				{
					lock (_queueLock)
					{
						_queues.Remove(heroId);
					}
				}
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot())
			{
				if (state == null || string.Equals(state.HeroId, heroId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null)
				{
					continue;
				}
				if (IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command) && !string.IsNullOrWhiteSpace(heroId) && string.Equals(command.TargetId, heroId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesHero(destroyerParty, state.HeroId);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_party_destroyed_by_actor" : "target_party_destroyed");
					continue;
				}
				if (IsKind(command, CommandKind.AttackParty) && !string.IsNullOrWhiteSpace(partyId) && string.Equals(command.TargetId, partyId, StringComparison.OrdinalIgnoreCase))
				{
					bool actorDestroyedTarget = PartyBaseMatchesHero(destroyerParty, state.HeroId) || PartyBaseMatchesFaction(destroyerParty, state.ResultActorFactionId);
					TryCompleteCurrentAttackResult(state, actorDestroyedTarget ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, actorDestroyedTarget ? "目标部队已被击溃。" : "目标部队已经被消灭或解散。", actorDestroyedTarget ? "target_mobile_party_destroyed_by_actor" : "target_mobile_party_destroyed");
				}
			}
			Log("mobile party destroyed hero=" + (heroId ?? "") + " party=" + (partyId ?? ""));
		}
		catch (Exception ex)
		{
			Log("party destroyed handling failed: " + ex.Message);
		}
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot())
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || IsSettlementTarget(command) || (!IsKind(command, CommandKind.AttackHero) && !IsKind(command, CommandKind.AttackParty)))
				{
					continue;
				}
				BattleSideEnum actorSide = GetHeroSideInMapEvent(mapEvent, state.HeroId);
				BattleSideEnum targetSide = IsKind(command, CommandKind.AttackParty) ? GetPartySideInMapEvent(mapEvent, command.TargetId) : GetHeroSideInMapEvent(mapEvent, command.TargetId);
				if (actorSide == BattleSideEnum.None || targetSide == BattleSideEnum.None || actorSide == targetSide)
				{
					continue;
				}
				if (!mapEvent.HasWinner)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "战斗已经结束，但原版事件没有明确胜负。", "map_event_no_winner");
					continue;
				}
				bool won = mapEvent.WinningSide == actorSide;
				string detail = won ? (GetStoredTargetName(state, command) + "的部队被击败。") : (GetStoredActorName(state) + "的部队被击退。");
				detail += BuildMapEventCasualtySummary(mapEvent, actorSide, targetSide);
				TryCompleteCurrentAttackResult(state, won ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, won ? "map_event_attack_success" : "map_event_attack_failure");
			}
		}
		catch (Exception ex)
		{
			Log("map event result handling failed: " + ex.Message);
		}
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		try
		{
			string prisonerId = prisoner?.StringId;
			if (string.IsNullOrWhiteSpace(prisonerId))
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot())
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsKind(command, CommandKind.AttackHero) || IsSettlementTarget(command))
				{
					continue;
				}
				if (string.Equals(prisonerId, state.HeroId, StringComparison.OrdinalIgnoreCase))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, "执行者已经被俘，攻击命令失败。", "actor_prisoner_taken");
					continue;
				}
				if (!string.Equals(prisonerId, command.TargetId, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				if (PartyBaseMatchesHero(capturer, state.HeroId) || PartyBaseMatchesFaction(capturer, state.ResultActorFactionId))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetStoredTargetName(state, command) + "已被俘，目标部队被击败。", "target_prisoner_taken");
				}
			}
		}
		catch (Exception ex)
		{
			Log("prisoner result handling failed: " + ex.Message);
		}
	}

	private void OnSiegeCompleted(Settlement settlement, MobileParty party, bool siegeSuccess, MapEvent.BattleTypes battleType)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!PartyMatchesHero(party, state.HeroId) && !PartyMatchesFaction(party, state.ResultActorFactionId))
				{
					continue;
				}
				string detail = siegeSuccess ? (GetSettlementName(settlement) + "已经被攻下。") : "攻城方未能攻下目标。";
				TryCompleteCurrentAttackResult(state, siegeSuccess ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, siegeSuccess ? "siege_completed_success" : "siege_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("siege result handling failed: " + ex.Message);
		}
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		try
		{
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("siege"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				bool actorFactionCaptured = string.Equals(SafeFactionId(newOwner?.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase) || string.Equals(SafeFactionId(settlement.MapFaction), state.ResultActorFactionId, StringComparison.OrdinalIgnoreCase);
				bool actorCaptured = string.Equals(capturerHero?.StringId, state.HeroId, StringComparison.OrdinalIgnoreCase);
				if (actorFactionCaptured || actorCaptured)
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, GetSettlementName(settlement) + "已经被攻下并易主。", "settlement_owner_changed_success");
				}
			}
		}
		catch (Exception ex)
		{
			Log("settlement owner result handling failed: " + ex.Message);
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
	{
		try
		{
			Settlement settlement = raidEvent?.MapEventSettlement;
			if (settlement == null)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, settlement))
				{
					continue;
				}
				if (!MapEventSideHasHero(raidEvent.AttackerSide, state.HeroId))
				{
					continue;
				}
				bool success = winnerSide == BattleSideEnum.Attacker || IsVillageLooted(settlement);
				string detail = success ? "村庄已被洗劫。" : "守军击退了袭掠，村庄没有被洗劫。";
				TryCompleteCurrentAttackResult(state, success ? CommandResultOutcome.Success : CommandResultOutcome.Failure, detail, success ? "raid_completed_success" : "raid_completed_failure");
			}
		}
		catch (Exception ex)
		{
			Log("raid result handling failed: " + ex.Message);
		}
	}

	private void OnVillageStateChanged(Village village, Village.VillageStates oldState, Village.VillageStates newState, MobileParty raiderParty)
	{
		try
		{
			if (village?.Settlement == null || newState != Village.VillageStates.Looted)
			{
				return;
			}
			foreach (PartyCommandQueueState state in GetActiveAttackStatesSnapshot("raid"))
			{
				PartyCommandEntry command = GetCurrentCommand(state);
				if (command == null || !IsTargetSettlement(command, village.Settlement))
				{
					continue;
				}
				if (!PartyMatchesHero(raiderParty, state.HeroId) && !PartyMatchesFaction(raiderParty, state.ResultActorFactionId))
				{
					continue;
				}
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, "村庄已进入被洗劫状态。", "village_looted_success");
			}
		}
		catch (Exception ex)
		{
			Log("village state result handling failed: " + ex.Message);
		}
	}

	private void ProcessQueueTick(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		NormalizeState(state);
		if (!ValidateActor(hero, party, out string reason))
		{
			if (IsCurrentAttackCommand(state))
			{
				TryCompleteCurrentAttackResult(state, CommandResultOutcome.Failure, "执行者已经失去可控制部队或被俘，命令失败。", "actor_invalid:" + reason);
				return;
			}
			FinishQueue(hero, party, state, "actor_invalid:" + reason, appendFact: true);
			return;
		}
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		if (command == null || !IsExecutableCommand(command))
		{
			AdvanceCommand(hero, party, state, "invalid_command");
			return;
		}
		CommandStage stage = ParseStage(state.Stage);
		if (stage == CommandStage.New)
		{
			StartCurrentCommand(hero, party, state);
			return;
		}
		double now = NowDay();
		if (state.TimeoutDay > 0.0 && now > state.TimeoutDay)
		{
			if (TryKeepCommandAliveAfterTimeout(hero, party, state, command, now))
			{
				Log("timeout deferred hero=" + (hero?.StringId ?? "") + " index=" + state.CurrentIndex + " kind=" + (command?.Kind ?? "") + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			}
			else
			{
				if (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty))
				{
					TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, BuildAttackTimeoutDetail(command, state), "timeout");
					return;
				}
				LogFact(hero, BuildCommandTimeoutFact(hero, command));
				AdvanceCommand(hero, party, state, "timeout");
				return;
			}
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			TickGoToSettlement(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			TickPatrolSettlement(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			TickFollowHero(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			TickFollowParty(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			TickAttackHero(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			TickAttackParty(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			TickMergeToPlayer(hero, party, state, command);
			return;
		}
		if (IsKind(command, CommandKind.CreateCompanionParty))
		{
			TryOpenCreateCompanionParty(hero, out _);
			AdvanceCommand(hero, party, state, "create_party_done");
		}
	}

	private void StartCurrentCommand(Hero hero, MobileParty party, PartyCommandQueueState state)
	{
		if (state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		PartyCommandEntry command = state.Commands[state.CurrentIndex];
		ResetResultTracking(state);
		state.CommandStartDay = NowDay();
		state.ArrivalDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		state.TimeoutDay = ComputeTimeoutDay(party, command);
		PreemptBlockingWorldActivityForCommand(hero, party, command, state, "start");
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "visit:" + settlement.StringId;
			DisplayCommandMessage(GetHeroName(hero) + "开始前往" + GetSettlementName(settlement) + "，抵达后停留" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start go hero=" + hero.StringId + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			Settlement settlement = ResolveSettlementById(command.TargetId);
			if (settlement == null)
			{
				AdvanceCommand(hero, party, state, "settlement_missing");
				return;
			}
			LockPartyAi(party);
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "patrol:" + settlement.StringId;
			DisplayCommandMessage(GetHeroName(hero) + "开始前往" + GetSettlementName(settlement) + "附近，抵达后巡逻" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start patrol hero=" + hero.StringId + " settlement=" + settlement.StringId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
			if (targetParty == null)
			{
				AdvanceCommand(hero, party, state, "follow_target_missing");
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForEscortingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "escort:" + command.TargetId;
			DisplayCommandMessage(GetHeroName(hero) + "开始前往并跟随" + GetHeroName(ResolveHeroById(command.TargetId)) + "，持续" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start follow hero=" + hero.StringId + " target=" + command.TargetId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
			if (!IsPartyUsable(targetParty) || targetParty == party)
			{
				AdvanceCommand(hero, party, state, "follow_party_target_missing");
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForEscortingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "escort_party:" + command.TargetId;
			DisplayCommandMessage(GetHeroName(hero) + "开始前往并跟随" + GetPartyName(targetParty) + "，持续" + Math.Max(1, command.Days) + "天。", CommandMessageTone.Progress);
			Log("start follow_party hero=" + hero.StringId + " targetParty=" + command.TargetId + " days=" + command.Days);
			return;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			if (IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (!IsSupportedAttackSettlement(settlement))
				{
					AdvanceCommand(hero, party, state, "attack_settlement_invalid");
					return;
				}
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				string settlementAttackMode = NormalizeAttackMode(command.Mode);
				if (CanStartSettlementAttackWithVanillaAi(party, settlement, settlementAttackMode))
				{
					SynchronizeArmyObjectiveForCommand(party, command);
					CommitSettlementAttack(hero, party, settlement, state, settlementAttackMode);
					Log("start settlement_attack_vanilla hero=" + hero.StringId + " settlement=" + settlement.StringId + " mode=" + settlementAttackMode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				LockPartyAi(party);
				SynchronizeArmyObjectiveForCommand(party, command);
				MoveTowardSettlementAttackPoint(party, settlement);
				state.Stage = CommandStage.Tracking.ToString();
				state.LastIssuedActionKey = "track_settlement_attack:" + settlement.StringId;
				DisplayCommandMessage(GetHeroName(hero) + "开始向" + GetSettlementName(settlement) + "机动，准备" + (settlement.IsVillage ? "烧掠" : "围攻") + "，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
				Log("start settlement_attack_track hero=" + hero.StringId + " settlement=" + settlement.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
			if (targetParty == null)
			{
				Hero targetHero = ResolveHeroById(command.TargetId);
				Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, null);
				if (shelter != null)
				{
					state.Stage = CommandStage.Tracking.ToString();
					state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
					DisplayCommandMessage(GetHeroName(hero) + "开始前往" + GetSettlementName(shelter) + "外侧，等待" + GetHeroName(targetHero) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
					MaintainAttackShelterWaiting(hero, party, targetHero, shelter, state, command, "start_target_inside_settlement_without_party");
					Log("start attack_shelter_wait hero=" + hero.StringId + " target=" + command.TargetId + " settlement=" + shelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
					return;
				}
				AdvanceCommand(hero, party, state, "attack_target_missing");
				return;
			}
			Settlement targetShelter = ResolveTargetHeroShelterSettlement(ResolveHeroById(command.TargetId), targetParty);
			if (targetShelter != null)
			{
				state.Stage = CommandStage.Tracking.ToString();
				state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
				DisplayCommandMessage(GetHeroName(hero) + "开始前往" + GetSettlementName(targetShelter) + "外侧，等待" + GetHeroName(ResolveHeroById(command.TargetId)) + "离开定居点以执行攻击命令。", CommandMessageTone.Progress);
				MaintainAttackShelterWaiting(hero, party, ResolveHeroById(command.TargetId), targetShelter, state, command, "start_target_inside_settlement");
				Log("start attack_shelter_wait hero=" + hero.StringId + " target=" + command.TargetId + " settlement=" + targetShelter.StringId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_attack:" + command.TargetId;
			DisplayCommandMessage(GetHeroName(hero) + "开始追踪" + GetHeroName(ResolveHeroById(command.TargetId)) + "的部队，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start attack_track hero=" + hero.StringId + " target=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
			if (!IsPartyUsable(targetParty) || targetParty == party)
			{
				AdvanceCommand(hero, party, state, "attack_party_target_missing");
				return;
			}
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
			state.Stage = CommandStage.Tracking.ToString();
			state.TimeoutDay = state.CommandStartDay + Math.Max(1, command.Days);
			state.LastIssuedActionKey = "track_party_attack:" + command.TargetId;
			DisplayCommandMessage(GetHeroName(hero) + "开始追踪" + GetPartyName(targetParty) + "，准备攻击，时限" + Math.Max(1, command.Days) + "天（" + NormalizeAttackMode(command.Mode) + "）。", CommandMessageTone.Progress);
			Log("start party_attack_track hero=" + hero.StringId + " targetParty=" + command.TargetId + " mode=" + command.Mode + " untilDay=" + state.TimeoutDay.ToString("0.00"));
			return;
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			LockPartyAi(party);
			SynchronizeArmyObjectiveForCommand(party, command);
			SetPartyAiAction.GetActionForEscortingParty(party, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			state.Stage = CommandStage.Traveling.ToString();
			state.LastIssuedActionKey = "merge_to_player";
			DisplayCommandMessage(GetHeroName(hero) + "开始返回玩家部队，准备会合并转入兵力。", CommandMessageTone.Progress);
			Log("start merge hero=" + hero.StringId);
			return;
		}
		if (IsKind(command, CommandKind.CreateCompanionParty))
		{
			TryOpenCreateCompanionParty(hero, out _);
			AdvanceCommand(hero, party, state, "create_party_done");
		}
	}

	private void TickGoToSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (settlement == null)
		{
			AdvanceCommand(hero, party, state, "settlement_missing");
			return;
		}
		if (state.ArrivalDay < 0.0 || !IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
		{
			string actionKey = "visit:" + settlement.StringId;
			bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
				|| !IsPartyVisitingSettlement(party, settlement)
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_go");
				LockPartyAi(party);
				SetPartyAiAction.GetActionForVisitingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				SynchronizeArmyObjectiveForCommand(party, command);
				state.Stage = CommandStage.Traveling.ToString();
				state.LastIssuedActionKey = actionKey;
				NotifyCommandStatus(state, actionKey + ":refresh", GetHeroName(hero) + "正在前往" + GetSettlementName(settlement) + "，若原版AI打断会自动重新下达前往命令。", CommandMessageTone.Progress);
				Log("go_refresh hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
			}
		}
		if (state.ArrivalDay < 0.0 && IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			LogFact(hero, GetHeroName(hero) + "已经抵达" + GetSettlementName(settlement) + "并开始停留。");
		}
		if (state.ArrivalDay >= 0.0 && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(hero, GetHeroName(hero) + "已经完成在" + GetSettlementName(settlement) + "停留" + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "go_done");
		}
	}

	private void TickPatrolSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (settlement == null)
		{
			AdvanceCommand(hero, party, state, "settlement_missing");
			return;
		}
		string actionKey = "patrol:" + settlement.StringId;
		bool hasArrived = state.ArrivalDay >= 0.0;
		bool insideLeash = IsPartyNearSettlementForPatrol(party, settlement, PatrolLeashDistance);
		bool isEngaging = IsPartyEngagingAnyTarget(party);
		if (hasArrived && insideLeash && IsAiDecisionLockActive(party))
		{
			ReleasePartyAi(party);
			state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			Log("patrol_release_ai hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
		}
		bool shouldRefreshTravel = (!hasArrived || !insideLeash) && !isEngaging
			&& (!string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
			|| !IsPartyPatrollingSettlement(party, settlement)
			|| !IsAiDecisionLockActive(party));
		bool shouldRefreshActive = hasArrived && insideLeash && !isEngaging && !IsPartyPatrollingSettlement(party, settlement);
		if (shouldRefreshTravel || shouldRefreshActive)
		{
			PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_patrol");
			if (shouldRefreshTravel)
			{
				LockPartyAi(party);
			}
			else
			{
				ReleasePartyAi(party);
			}
			SetPartyAiAction.GetActionForPatrollingAroundSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			if (shouldRefreshActive)
			{
				ReleasePartyAi(party);
				state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			}
			else
			{
				state.LastIssuedActionKey = actionKey;
			}
			NotifyCommandStatus(state, actionKey + ":refresh", GetHeroName(hero) + "正在" + GetSettlementName(settlement) + "附近巡逻，若原版AI打断会自动重新下达巡逻命令。", CommandMessageTone.Progress);
			Log("patrol_refresh hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " " + DescribePartyAi(party));
		}
		if (state.ArrivalDay < 0.0 && IsPartyNearSettlementForPatrol(party, settlement, PatrolArrivalDistance))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			state.LastIssuedActionKey = "patrol_active:" + settlement.StringId;
			ReleasePartyAi(party);
			LogFact(hero, GetHeroName(hero) + "已经抵达" + GetSettlementName(settlement) + "附近并开始巡逻。");
		}
		if (state.ArrivalDay >= 0.0 && !isEngaging && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(hero, GetHeroName(hero) + "已经完成在" + GetSettlementName(settlement) + "附近巡逻" + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "patrol_done");
		}
	}

	private void TickFollowHero(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
		if (targetParty == null)
		{
			AdvanceCommand(hero, party, state, "follow_target_missing");
			return;
		}
		string actionKey = "escort:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
			|| !IsPartyEscortingTarget(party, targetParty)
			|| !IsAiDecisionLockActive(party);
		if (shouldRefresh)
		{
			PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_follow");
			LockPartyAi(party);
			SetPartyAiAction.GetActionForEscortingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.LastIssuedActionKey = actionKey;
			NotifyCommandStatus(state, actionKey + ":refresh", GetHeroName(hero) + "正在跟随" + GetHeroName(ResolveHeroById(command.TargetId)) + "，若原版AI打断会自动重新下达跟随命令。", CommandMessageTone.Progress);
			Log("follow_refresh hero=" + (hero?.StringId ?? "") + " target=" + command.TargetId + " " + DescribePartyAi(party));
		}
		if (state.ArrivalDay < 0.0 && IsPartyCloseEnoughToStartFollowing(party, targetParty))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			LogFact(hero, GetHeroName(hero) + "已经追上并开始跟随" + GetHeroName(ResolveHeroById(command.TargetId)) + "。");
		}
		if (state.ArrivalDay >= 0.0 && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(hero, GetHeroName(hero) + "已经完成跟随" + GetHeroName(ResolveHeroById(command.TargetId)) + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "follow_done");
		}
	}

	private void TickFollowParty(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
		if (!IsPartyUsable(targetParty) || targetParty == party)
		{
			AdvanceCommand(hero, party, state, "follow_party_target_missing");
			return;
		}
		string actionKey = "escort_party:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase)
			|| !IsPartyEscortingTarget(party, targetParty)
			|| !IsAiDecisionLockActive(party);
		if (shouldRefresh)
		{
			PreemptBlockingWorldActivityForCommand(hero, party, command, state, "tick_follow_party");
			LockPartyAi(party);
			SetPartyAiAction.GetActionForEscortingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
			SynchronizeArmyObjectiveForCommand(party, command);
			state.LastIssuedActionKey = actionKey;
			NotifyCommandStatus(state, actionKey + ":refresh", GetHeroName(hero) + "正在跟随" + GetPartyName(targetParty) + "，若原版AI打断会自动重新下达跟随命令。", CommandMessageTone.Progress);
			Log("follow_party_refresh hero=" + (hero?.StringId ?? "") + " targetParty=" + command.TargetId + " " + DescribePartyAi(party));
		}
		if (state.ArrivalDay < 0.0 && IsPartyCloseEnoughToStartFollowing(party, targetParty))
		{
			state.ArrivalDay = NowDay();
			state.TimeoutDay = -1.0;
			state.Stage = CommandStage.Active.ToString();
			LogFact(hero, GetHeroName(hero) + "已经追上并开始跟随" + GetPartyName(targetParty) + "。");
		}
		if (state.ArrivalDay >= 0.0 && NowDay() - state.ArrivalDay >= Math.Max(1, command.Days))
		{
			LogFact(hero, GetHeroName(hero) + "已经完成跟随" + GetPartyName(targetParty) + Math.Max(1, command.Days) + "天的命令。");
			AdvanceCommand(hero, party, state, "follow_party_done");
		}
	}

	private void TickAttackHero(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (IsSettlementTarget(command))
		{
			TickAttackSettlement(hero, party, state, command);
			return;
		}
		Hero targetHero = ResolveHeroById(command.TargetId);
		if (targetHero == null || targetHero.IsDead || targetHero.IsPrisoner)
		{
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标已经被击败、死亡或被俘。" : "目标已经死亡、被俘或失效。", "attack_target_defeated_or_invalid");
			return;
		}
		MobileParty targetParty = targetHero.PartyBelongedTo;
		if (targetParty == party)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标和执行者位于同一支部队，无法发起攻击。", "attack_target_same_party");
			return;
		}
		if (targetParty == null || !IsPartyUsable(targetParty))
		{
			Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
			if (shelter != null)
			{
				MaintainAttackShelterWaiting(hero, party, targetHero, shelter, state, command, "target_inside_settlement_without_party");
				return;
			}
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标部队已经被击溃或解散。" : "目标当前没有可攻击的部队。", "attack_target_party_missing");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedAttack(hero, party, targetHero, targetParty, state, command);
			return;
		}
		Settlement targetShelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
		if (targetShelter != null)
		{
			MaintainAttackShelterWaiting(hero, party, targetHero, targetShelter, state, command, "target_inside_settlement");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageCommitDistance))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "closing_distance");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool force = IsForceAttackMode(mode);
		bool requiresRebellion = RequiresPlayerClanRebellionForPartyAttack(party, targetParty);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForHeroAttack(hero, party, targetHero, targetParty, apply: false, out string precheckRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, precheckRebellionReason, "rebellion_attack_blocked");
			return;
		}
		if (force && !CanForceCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainAttackTracking(hero, party, targetParty, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForHeroAttack(hero, party, targetHero, targetParty, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_attack_blocked");
			return;
		}
		CommitAttack(hero, party, targetHero, targetParty, state, mode);
	}

	private void TickAttackParty(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
		if (!IsPartyUsable(targetParty))
		{
			TryCompleteCurrentAttackResult(state, state.EngageCommitted ? CommandResultOutcome.Success : CommandResultOutcome.Incomplete, state.EngageCommitted ? "目标部队已经被击溃或解散。" : "目标部队已经失效或不在大地图上。", "attack_party_target_missing");
			return;
		}
		if (targetParty == party)
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标部队和执行者是同一支部队，无法发起攻击。", "attack_party_same_party");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedPartyAttack(hero, party, targetParty, state, command);
			return;
		}
		if (targetParty.CurrentSettlement != null)
		{
			MaintainPartyAttackSettlementWaiting(hero, party, targetParty, targetParty.CurrentSettlement, state, command, "party_target_inside_settlement");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageCommitDistance))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "closing_distance");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool force = IsForceAttackMode(mode);
		bool requiresRebellion = RequiresPlayerClanRebellionForPartyAttack(party, targetParty);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForPartyAttack(hero, party, targetParty, apply: false, out string precheckRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, precheckRebellionReason, "rebellion_party_attack_blocked");
			return;
		}
		if (force && !CanForceCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitAttackForMode(party, targetParty, requiresRebellion))
		{
			MaintainPartyAttackTracking(hero, party, targetParty, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForPartyAttack(hero, party, targetParty, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_party_attack_blocked");
			return;
		}
		CommitPartyAttack(hero, party, targetParty, state, mode);
	}

	private void TickAttackSettlement(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		Settlement settlement = ResolveSettlementById(command.TargetId);
		if (!IsSupportedAttackSettlement(settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标定居点已经失效或不是可攻击目标。", "attack_settlement_invalid");
			return;
		}
		if (state.EngageCommitted && IsSettlementAttackComplete(party, settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, settlement.IsVillage ? "村庄已被洗劫。" : (GetSettlementName(settlement) + "已经被攻下。"), "attack_settlement_done");
			return;
		}
		if (!state.EngageCommitted && IsSettlementAttackUnavailableBeforeCommit(settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, "目标已经不可攻击。", "attack_settlement_unavailable");
			return;
		}
		string mode = NormalizeAttackMode(command.Mode);
		bool requiresRebellion = !state.EngageCommitted && RequiresPlayerClanRebellionForSettlementAttack(party, settlement);
		if (requiresRebellion && !TryPreparePlayerClanRebellionForSettlementAttack(hero, party, settlement, apply: false, out string sameFactionRebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, sameFactionRebellionReason, "rebellion_settlement_attack_blocked");
			return;
		}
		if (state.EngageCommitted)
		{
			MaintainCommittedSettlementAttack(hero, party, settlement, state, command);
			return;
		}
		if (!requiresRebellion && CanStartSettlementAttackWithVanillaAi(party, settlement, mode))
		{
			SynchronizeArmyObjectiveForCommand(party, command);
			CommitSettlementAttack(hero, party, settlement, state, mode);
			return;
		}
		if (!IsPartyNearPosition(party, GetSettlementAttackPosition(settlement), EngageCommitDistance))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "closing_distance");
			return;
		}
		bool force = IsForceAttackMode(mode);
		if (force && !CanForceCommitSettlementAttackForMode(party, settlement, requiresRebellion))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "force_commit_blocked");
			return;
		}
		if (!force && !CanAiCommitSettlementAttackForMode(party, settlement, requiresRebellion))
		{
			MaintainSettlementAttackTracking(hero, party, settlement, state, command, "ai_commit_waiting");
			return;
		}
		if (requiresRebellion && !TryPreparePlayerClanRebellionForSettlementAttack(hero, party, settlement, apply: true, out string rebellionReason))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Incomplete, rebellionReason, "rebellion_settlement_attack_blocked");
			return;
		}
		CommitSettlementAttack(hero, party, settlement, state, mode);
	}

	private void MaintainSettlementAttackTracking(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || settlement == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_settlement_attack:" + settlement.StringId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "settlement_attack_track");
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, settlement);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(actorHero, GetSettlementName(settlement), reason), CommandMessageTone.Progress);
		Log("settlement_attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || settlement == null || state == null || command == null)
		{
			return;
		}
		if (IsSettlementAttackComplete(party, settlement))
		{
			TryCompleteCurrentAttackResult(state, CommandResultOutcome.Success, settlement.IsVillage ? "村庄已被洗劫。" : (GetSettlementName(settlement) + "已经被攻下。"), "attack_settlement_done");
			return;
		}
		if (IsPartyCommittedToSettlementAttack(party, settlement))
		{
			SynchronizeArmyObjectiveForCommand(party, command);
			ReleasePartyAi(party);
			return;
		}
		if (!CanForceCommitSettlementAttack(party, settlement))
		{
			MaintainSettlementAttackTracking(actorHero, party, settlement, state, command, "settlement_attack_conditions_lost");
			return;
		}
		CommitSettlementAttack(actorHero, party, settlement, state, NormalizeAttackMode(command.Mode));
	}

	private void CommitSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_settlement_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		LeaveTargetSettlementIfInside(party, settlement);
		BeginResultTracking(state, settlement.IsVillage ? "raid" : "siege", "settlement", settlement.StringId, GetSettlementName(settlement), attackerFaction, defenderFaction);
		if (settlement.IsVillage)
		{
			SetPartyAiActionForRaidingSettlement(party, settlement);
			state.LastIssuedActionKey = "raid:" + settlement.StringId;
			LogFact(actorHero, GetHeroName(actorHero) + "已经开始烧掠" + GetSettlementName(settlement) + "，结果尚未分出。");
			Log("settlement_attack_commit_raid hero=" + actorHero.StringId + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		else
		{
			SetPartyAiAction.GetActionForBesiegingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false);
			state.LastIssuedActionKey = "besiege:" + settlement.StringId;
			LogFact(actorHero, GetHeroName(actorHero) + "已经开始围攻" + GetSettlementName(settlement) + "，结果尚未分出。");
			Log("settlement_attack_commit_siege hero=" + actorHero.StringId + " settlement=" + settlement.StringId + " mode=" + mode);
		}
		ReleasePartyAi(party);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
	}

	private static void SetPartyAiActionForRaidingSettlement(MobileParty party, Settlement settlement)
	{
#if BANNERLORD_1_4_OR_GREATER
		SetPartyAiAction.GetActionForRaidingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
#else
		SetPartyAiAction.GetActionForRaidingSettlement(party, settlement, MobileParty.NavigationType.Default, isFromPort: false);
#endif
	}

	private void MaintainAttackTracking(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_attack:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || !IsPartyTrackingTarget(party, targetParty) || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "attack_track");
		LockPartyAi(party);
		SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(actorHero, GetPartyName(targetParty), reason), CommandMessageTone.Progress);
		Log("attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " target=" + (command.TargetId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainAttackShelterWaiting(Hero actorHero, MobileParty party, Hero targetHero, Settlement shelter, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetHero == null || shelter == null || state == null || command == null)
		{
			return;
		}
		string actionKey = "wait_hero_shelter:" + targetHero.StringId + ":" + shelter.StringId;
		bool actionChanged = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase);
		bool shouldRefresh = actionChanged || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "attack_shelter_wait");
		LeaveTargetSettlementIfInside(party, shelter);
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, shelter);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		if (actionChanged)
		{
			LogFact(actorHero, GetHeroName(targetHero) + "当前躲在" + GetSettlementName(shelter) + "内，" + GetHeroName(actorHero) + "正在城外等待其离开，以继续攻击命令。");
		}
		Log("attack_shelter_wait_refresh hero=" + (actorHero?.StringId ?? "") + " target=" + targetHero.StringId + " settlement=" + shelter.StringId + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainPartyAttackTracking(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		SynchronizeArmyObjectiveForCommand(party, command);
		string actionKey = "track_party_attack:" + command.TargetId;
		bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase) || !IsPartyTrackingTarget(party, targetParty) || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "party_attack_track");
		LockPartyAi(party);
		SetPartyAiAction.GetActionForGoingAroundParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		NotifyCommandStatus(state, actionKey + ":" + reason, BuildAttackTrackingStatusMessage(actorHero, GetPartyName(targetParty), reason), CommandMessageTone.Progress);
		Log("party_attack_track_refresh hero=" + (actorHero?.StringId ?? "") + " targetParty=" + (command.TargetId ?? "") + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainPartyAttackSettlementWaiting(Hero actorHero, MobileParty party, MobileParty targetParty, Settlement shelter, PartyCommandQueueState state, PartyCommandEntry command, string reason)
	{
		if (party == null || targetParty == null || shelter == null || state == null || command == null)
		{
			return;
		}
		string actionKey = "wait_party_shelter:" + targetParty.StringId + ":" + shelter.StringId;
		bool actionChanged = !string.Equals(state.LastIssuedActionKey, actionKey, StringComparison.OrdinalIgnoreCase);
		bool shouldRefresh = actionChanged || party.DefaultBehavior != AiBehavior.GoToPoint || !IsAiDecisionLockActive(party);
		if (!shouldRefresh)
		{
			return;
		}
		PreemptBlockingWorldActivityForCommand(actorHero, party, command, state, "party_attack_shelter_wait");
		LeaveTargetSettlementIfInside(party, shelter);
		LockPartyAi(party);
		MoveTowardSettlementAttackPoint(party, shelter);
		state.EngageCommitted = false;
		state.Stage = CommandStage.Tracking.ToString();
		state.LastIssuedActionKey = actionKey;
		if (actionChanged)
		{
			LogFact(actorHero, GetPartyName(targetParty) + "当前在" + GetSettlementName(shelter) + "内，" + GetHeroName(actorHero) + "正在外侧等待其离开，以继续攻击命令。");
		}
		Log("party_attack_shelter_wait_refresh hero=" + (actorHero?.StringId ?? "") + " targetParty=" + (targetParty?.StringId ?? "") + " settlement=" + shelter.StringId + " reason=" + reason + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		if (targetParty.CurrentSettlement != null && !IsPartyEngagingTarget(party, targetParty))
		{
			MaintainPartyAttackSettlementWaiting(actorHero, party, targetParty, targetParty.CurrentSettlement, state, command, "party_target_sheltered_after_commit");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageMaintainDistance))
		{
			MaintainPartyAttackTracking(actorHero, party, targetParty, state, command, "target_left_engage_range");
			return;
		}
		if (!CanForceCommitAttack(party, targetParty))
		{
			MaintainPartyAttackTracking(actorHero, party, targetParty, state, command, "engage_conditions_lost");
			return;
		}
		if (IsPartyEngagingTarget(party, targetParty) && IsAiDecisionLockActive(party))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage_party:" + targetParty.StringId;
		NotifyCommandStatus(state, state.LastIssuedActionKey + ":reengage", GetHeroName(actorHero) + "重新追上" + GetPartyName(targetParty) + "，继续执行攻击命令。", CommandMessageTone.Progress);
		Log("party_attack_reengage hero=" + (actorHero?.StringId ?? "") + " targetParty=" + targetParty.StringId + " " + DescribePartyAi(party));
	}

	private void MaintainCommittedAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (party == null || targetHero == null || targetParty == null || state == null || command == null)
		{
			return;
		}
		Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetParty);
		if (shelter != null && !IsPartyEngagingTarget(party, targetParty))
		{
			MaintainAttackShelterWaiting(actorHero, party, targetHero, shelter, state, command, "target_sheltered_after_commit");
			return;
		}
		if (!IsPartyNearParty(party, targetParty, EngageMaintainDistance))
		{
			MaintainAttackTracking(actorHero, party, targetParty, state, command, "target_left_engage_range");
			return;
		}
		if (!CanForceCommitAttack(party, targetParty))
		{
			MaintainAttackTracking(actorHero, party, targetParty, state, command, "engage_conditions_lost");
			return;
		}
		if (IsPartyEngagingTarget(party, targetParty) && IsAiDecisionLockActive(party))
		{
			return;
		}
		LockPartyAi(party);
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage:" + targetHero.StringId;
		NotifyCommandStatus(state, state.LastIssuedActionKey + ":reengage", GetHeroName(actorHero) + "重新追上" + GetHeroName(targetHero) + "的部队，继续执行攻击命令。", CommandMessageTone.Progress);
		Log("attack_reengage hero=" + (actorHero?.StringId ?? "") + " target=" + targetHero.StringId + " " + DescribePartyAi(party));
	}

	private void TickMergeToPlayer(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command)
	{
		if (!CanMergeToPlayer(hero, party, out string reason))
		{
			AdvanceCommand(hero, party, state, "merge_invalid:" + reason);
			return;
		}
		if (!IsPartyNearParty(party, MobileParty.MainParty, PartyArrivalDistance))
		{
			bool shouldRefresh = !string.Equals(state.LastIssuedActionKey, "merge_to_player", StringComparison.OrdinalIgnoreCase)
				|| !IsPartyEscortingTarget(party, MobileParty.MainParty)
				|| !IsAiDecisionLockActive(party);
			if (shouldRefresh)
			{
				PreemptBlockingWorldActivityForCommand(hero, party, command, state, "merge_to_player");
				LockPartyAi(party);
				SetPartyAiAction.GetActionForEscortingParty(party, MobileParty.MainParty, MobileParty.NavigationType.Default, isFromPort: false, isTargetingPort: false);
				state.LastIssuedActionKey = "merge_to_player";
				NotifyCommandStatus(state, "merge_to_player_refresh", GetHeroName(hero) + "正在返回玩家部队，若原版AI打断会自动重新下达回队命令。", CommandMessageTone.Progress);
				Log("merge_refresh hero=" + (hero?.StringId ?? "") + " " + DescribePartyAi(party));
			}
			return;
		}
		int movedMembers = MoveAllMembersToMainParty(party);
		int movedPrisoners = MoveAllPrisonersToMainParty(party);
		LogFact(hero, GetHeroName(hero) + "已经与玩家部队会合，并转入" + movedMembers + "名成员、" + movedPrisoners + "名俘虏。");
		TryDestroyEmptyParty(party);
		AdvanceCommand(hero, party, state, "merge_done");
	}

	private void CommitAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " mode=" + mode);
		}
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		BeginResultTracking(state, "hero_attack", "hero", targetHero.StringId, GetHeroName(targetHero), attackerFaction, defenderFaction);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage:" + targetHero.StringId;
		LogFact(actorHero, GetHeroName(actorHero) + "已经对" + GetHeroName(targetHero) + "的部队发起攻击，结果尚未分出。");
		Log("attack_commit hero=" + actorHero.StringId + " target=" + targetHero.StringId + " mode=" + mode);
	}

	private void CommitPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, PartyCommandQueueState state, string mode)
	{
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		if (attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && !FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction))
		{
			DeclareWarAction.ApplyByDefault(attackerFaction, defenderFaction);
			Log("declare_war_on_party_attack_commit attacker=" + SafeFactionId(attackerFaction) + " defender=" + SafeFactionId(defenderFaction) + " mode=" + mode);
		}
		SetPartyAiAction.GetActionForEngagingParty(party, targetParty, MobileParty.NavigationType.Default, isFromPort: false);
		BeginResultTracking(state, "party_attack", "party", targetParty.StringId, GetPartyName(targetParty), attackerFaction, defenderFaction);
		state.EngageCommitted = true;
		state.Stage = CommandStage.Engaging.ToString();
		state.LastIssuedActionKey = "engage_party:" + targetParty.StringId;
		LogFact(actorHero, GetHeroName(actorHero) + "已经对" + GetPartyName(targetParty) + "发起攻击，结果尚未分出。");
		Log("party_attack_commit hero=" + actorHero.StringId + " targetParty=" + targetParty.StringId + " mode=" + mode);
	}

	private bool CanAiCommitAttack(MobileParty party, MobileParty targetParty)
	{
		if (!CanForceCommitAttack(party, targetParty))
		{
			return false;
		}
		bool alreadyAtWar = ArePartiesAtWar(party, targetParty);
		try
		{
			if (alreadyAtWar && Campaign.Current?.Models?.MobilePartyAIModel?.ShouldConsiderAttacking(party, targetParty) != true)
			{
				return false;
			}
		}
		catch
		{
			if (alreadyAtWar)
			{
				return false;
			}
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, targetParty);
		float defenderStrength = EstimatePartyStrength(targetParty);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanAiCommitAttackForMode(MobileParty party, MobileParty targetParty, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanAiCommitAttack(party, targetParty);
		}
		if (!CanForceCommitAttackForMode(party, targetParty, allowSameFactionViaRebellion))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, targetParty);
		float defenderStrength = EstimatePartyStrength(targetParty);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private static bool CanForceCommitAttack(MobileParty party, MobileParty targetParty)
	{
		if (!IsPartyUsable(party) || !IsPartyUsable(targetParty) || party == targetParty)
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = targetParty.MapFaction;
		return attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction;
	}

	private static bool CanForceCommitAttackForMode(MobileParty party, MobileParty targetParty, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanForceCommitAttack(party, targetParty);
		}
		return IsPartyUsable(party) && IsPartyUsable(targetParty) && party != targetParty;
	}

	private bool CanAiCommitSettlementAttack(MobileParty party, Settlement settlement)
	{
		if (!CanForceCommitSettlementAttack(party, settlement))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, settlement);
		float defenderStrength = EstimateSettlementDefenseStrength(settlement);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanAiCommitSettlementAttackForMode(MobileParty party, Settlement settlement, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanAiCommitSettlementAttack(party, settlement);
		}
		if (!CanForceCommitSettlementAttackForMode(party, settlement, allowSameFactionViaRebellion))
		{
			return false;
		}
		float attackerStrength = EstimateAttackStrengthWithNearbyAllies(party, settlement);
		float defenderStrength = EstimateSettlementDefenseStrength(settlement);
		return attackerStrength >= Math.Max(1f, defenderStrength * AiAttackStrengthRatio);
	}

	private bool CanStartSettlementAttackWithVanillaAi(MobileParty party, Settlement settlement, string mode)
	{
		if (!IsPartyAtWarWithSettlement(party, settlement))
		{
			return false;
		}
		return IsForceAttackMode(mode) ? CanForceCommitSettlementAttack(party, settlement) : CanAiCommitSettlementAttack(party, settlement);
	}

	private static bool CanForceCommitSettlementAttack(MobileParty party, Settlement settlement)
	{
		if (!IsPartyUsable(party) || !IsSupportedAttackSettlement(settlement))
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction == defenderFaction)
		{
			return false;
		}
		if (settlement.IsVillage)
		{
			if (settlement.IsRaided || settlement.SettlementHitPoints <= 0.001f)
			{
				return false;
			}
			return !settlement.IsUnderRaid || IsPartyCommittedToSettlementAttack(party, settlement);
		}
		if (settlement.IsUnderSiege && !IsPartyCommittedToSettlementAttack(party, settlement) && !IsSameFactionSiege(party, settlement))
		{
			return false;
		}
		return true;
	}

	private static bool CanForceCommitSettlementAttackForMode(MobileParty party, Settlement settlement, bool allowSameFactionViaRebellion)
	{
		if (!allowSameFactionViaRebellion)
		{
			return CanForceCommitSettlementAttack(party, settlement);
		}
		if (!IsPartyUsable(party) || !IsSupportedAttackSettlement(settlement))
		{
			return false;
		}
		IFaction attackerFaction = party.MapFaction;
		IFaction defenderFaction = settlement.MapFaction;
		if (attackerFaction == null || defenderFaction == null)
		{
			return false;
		}
		if (settlement.IsVillage)
		{
			if (settlement.IsRaided || settlement.SettlementHitPoints <= 0.001f)
			{
				return false;
			}
			return !settlement.IsUnderRaid || IsPartyCommittedToSettlementAttack(party, settlement);
		}
		if (settlement.IsUnderSiege && !IsPartyCommittedToSettlementAttack(party, settlement) && !IsSameFactionSiege(party, settlement))
		{
			return false;
		}
		return true;
	}

	private static bool IsPartyAtWarWithSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = settlement?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction != defenderFaction && FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction);
		}
		catch
		{
			return false;
		}
	}

	private static bool RequiresPlayerClanRebellionForPartyAttack(MobileParty party, MobileParty targetParty)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = targetParty?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction == defenderFaction;
		}
		catch
		{
			return false;
		}
	}

	private static bool RequiresPlayerClanRebellionForSettlementAttack(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = settlement?.MapFaction;
			return attackerFaction != null && defenderFaction != null && attackerFaction == defenderFaction;
		}
		catch
		{
			return false;
		}
	}

	private bool TryPreparePlayerClanRebellionForHeroAttack(Hero actorHero, MobileParty party, Hero targetHero, MobileParty targetParty, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = targetParty?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		if (targetHero?.Clan == Clan.PlayerClan)
		{
			reason = "目标属于玩家家族，不能通过叛乱攻击自己的家族部队。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, "攻击" + GetHeroName(targetHero) + "的部队", out reason);
	}

	private bool TryPreparePlayerClanRebellionForPartyAttack(Hero actorHero, MobileParty party, MobileParty targetParty, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = targetParty?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		Clan targetClan = targetParty?.ActualClan ?? targetParty?.LeaderHero?.Clan;
		if (targetClan == Clan.PlayerClan)
		{
			reason = "目标部队属于玩家家族，不能通过叛乱攻击自己的家族部队。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, "攻击" + GetPartyName(targetParty), out reason);
	}

	private bool TryPreparePlayerClanRebellionForSettlementAttack(Hero actorHero, MobileParty party, Settlement settlement, bool apply, out string reason)
	{
		reason = "";
		IFaction attackerFaction = party?.MapFaction;
		IFaction defenderFaction = settlement?.MapFaction;
		if (attackerFaction == null || defenderFaction == null || attackerFaction != defenderFaction)
		{
			return true;
		}
		if (settlement?.OwnerClan == Clan.PlayerClan)
		{
			reason = "目标定居点属于玩家家族，不能通过叛乱攻击自己的封地。";
			return false;
		}
		Kingdom oldKingdom = Clan.PlayerClan?.Kingdom;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, defenderFaction, out reason))
		{
			return false;
		}
		if (!apply)
		{
			return true;
		}
		string actionText = settlement?.IsVillage == true ? ("烧掠" + GetSettlementName(settlement)) : ("围攻" + GetSettlementName(settlement));
		return TryApplyPlayerClanRebellionForWorldMapAttack(actorHero, party, oldKingdom, actionText, out reason);
	}

	private static bool CanPlayerClanRebelForWorldMapAttack(Hero actorHero, MobileParty party, Kingdom oldKingdom, IFaction targetFaction, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			reason = "玩家家族不存在，无法执行叛乱攻击。";
			return false;
		}
		if (actorHero == null || actorHero == Hero.MainHero || actorHero.Clan != playerClan)
		{
			reason = "只有玩家家族的同伴独立部队可以代表玩家执行叛乱攻击。";
			return false;
		}
		if (!IsPartyUsable(party) || party == MobileParty.MainParty || party.LeaderHero != actorHero)
		{
			reason = "执行者当前没有可控制的独立同伴部队，无法执行叛乱攻击。";
			return false;
		}
		if (party.ActualClan != null && party.ActualClan != playerClan)
		{
			reason = "执行者部队不属于玩家家族，无法触发玩家家族叛乱。";
			return false;
		}
		if (oldKingdom == null)
		{
			reason = "玩家家族当前不属于任何王国，不需要也无法执行带城叛乱。";
			return false;
		}
		if (targetFaction != null && targetFaction != oldKingdom)
		{
			reason = "目标不属于玩家当前王国，不能作为带城叛乱攻击的触发目标。";
			return false;
		}
		if (oldKingdom.IsEliminated)
		{
			reason = "玩家当前王国已经灭亡，无法执行带城叛乱。";
			return false;
		}
		if (playerClan.IsUnderMercenaryService)
		{
			reason = "玩家家族当前是雇佣兵关系，不能执行带城叛乱。";
			return false;
		}
		if (oldKingdom.RulingClan == playerClan)
		{
			reason = "玩家家族是当前王国统治家族，不能对自己的王国执行带城叛乱。";
			return false;
		}
		if (playerClan.Settlements == null || playerClan.Settlements.Count <= 0)
		{
			reason = "玩家家族没有封地，不能执行带城叛乱。";
			return false;
		}
		return true;
	}

	private bool TryApplyPlayerClanRebellionForWorldMapAttack(Hero actorHero, MobileParty party, Kingdom oldKingdom, string actionText, out string reason)
	{
		reason = "";
		Clan playerClan = Clan.PlayerClan;
		if (!CanPlayerClanRebelForWorldMapAttack(actorHero, party, oldKingdom, oldKingdom, out reason))
		{
			return false;
		}
		try
		{
			string oldKingdomName = GetFactionDisplayName(oldKingdom);
			ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(playerClan, showNotification: true);
			bool leftOldKingdom = playerClan.Kingdom != oldKingdom;
			bool atWar = FactionManager.IsAtWarAgainstFaction(playerClan, oldKingdom);
			if (!leftOldKingdom || !atWar)
			{
				reason = "玩家家族叛乱动作未能建立与旧王国的战争状态。";
				Log("player_clan_rebellion_attack_failed_verify actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " left=" + leftOldKingdom + " atWar=" + atWar);
				return false;
			}
			LogFact(actorHero, "玩家家族已带领封地脱离" + oldKingdomName + "并发动叛乱，" + GetHeroName(actorHero) + "随后继续执行" + (actionText ?? "攻击目标") + "的命令。");
			Log("player_clan_rebellion_attack_commit actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " mode=" + AttackModeForce + " autoRebellion=True " + DescribePartyAi(party));
			return true;
		}
		catch (Exception ex)
		{
			reason = "玩家家族叛乱动作执行失败：" + ex.Message;
			Log("player_clan_rebellion_attack_exception actor=" + (actorHero?.StringId ?? "") + " oldKingdom=" + SafeFactionId(oldKingdom) + " error=" + ex);
			return false;
		}
	}

	private static bool ArePartiesAtWar(MobileParty party, MobileParty targetParty)
	{
		try
		{
			IFaction attackerFaction = party?.MapFaction;
			IFaction defenderFaction = targetParty?.MapFaction;
			return attackerFaction != null && defenderFaction != null && FactionManager.IsAtWarAgainstFaction(attackerFaction, defenderFaction);
		}
		catch
		{
			return false;
		}
	}

	private static bool ArePartyAndSettlementSameFaction(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction partyFaction = party?.MapFaction;
			IFaction settlementFaction = settlement?.MapFaction;
			return partyFaction != null && settlementFaction != null && partyFaction == settlementFaction;
		}
		catch
		{
			return false;
		}
	}

	private float EstimateAttackStrengthWithNearbyAllies(MobileParty party, MobileParty targetParty)
	{
		float strength = EstimatePartyStrength(party);
		try
		{
			IFaction faction = party.MapFaction;
			if (faction == null || targetParty == null)
			{
				return strength;
			}
			foreach (MobileParty other in MobileParty.All)
			{
				if (other == null || other == party || other == targetParty || !IsPartyUsable(other) || other.MapFaction != faction)
				{
					continue;
				}
				if (IsPartyNearParty(other, targetParty, FriendlySupportRadius))
				{
					strength += EstimatePartyStrength(other);
				}
			}
		}
		catch
		{
		}
		return strength;
	}

	private float EstimateAttackStrengthWithNearbyAllies(MobileParty party, Settlement settlement)
	{
		float strength = EstimatePartyStrength(party);
		try
		{
			IFaction faction = party?.MapFaction;
			if (faction == null || settlement == null)
			{
				return strength;
			}
			CampaignVec2 center = GetSettlementAttackPosition(settlement);
			foreach (MobileParty other in MobileParty.All)
			{
				if (other == null || other == party || !IsPartyUsable(other) || other.MapFaction != faction)
				{
					continue;
				}
				if (IsPartyNearPosition(other, center, FriendlySupportRadius))
				{
					strength += EstimatePartyStrength(other);
				}
			}
		}
		catch
		{
		}
		return strength;
	}

	private static float EstimatePartyStrength(MobileParty party)
	{
		try
		{
			float value = party?.GetTotalLandStrengthWithFollowers(includeNonAttachedArmyMembers: true) ?? 0f;
			if (value > 0f)
			{
				return value;
			}
		}
		catch
		{
		}
		try
		{
			return party?.Party?.EstimatedStrength ?? 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private static float EstimateSettlementDefenseStrength(Settlement settlement)
	{
		try
		{
			float value = settlement?.Party?.EstimatedStrength ?? 0f;
			if (value > 0f)
			{
				return value;
			}
		}
		catch
		{
		}
		try
		{
			return settlement?.Town?.GarrisonParty?.Party?.EstimatedStrength ?? 0f;
		}
		catch
		{
			return 0f;
		}
	}

	private List<PartyCommandQueueState> GetActiveAttackStatesSnapshot(string resultKind = null)
	{
		lock (_queueLock)
		{
			return _queues.Values
				.Where(x => x != null && IsCurrentAttackCommand(x) && !x.ResultLogged)
				.Where(x => string.IsNullOrWhiteSpace(resultKind) || string.Equals((x.ResultKind ?? "").Trim(), resultKind, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}
	}

	private static PartyCommandEntry GetCurrentCommand(PartyCommandQueueState state)
	{
		if (state?.Commands == null || state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
		{
			return null;
		}
		return state.Commands[state.CurrentIndex];
	}

	private static bool IsCurrentAttackCommand(PartyCommandQueueState state)
	{
		PartyCommandEntry command = GetCurrentCommand(state);
		return command != null && (IsKind(command, CommandKind.AttackHero) || IsKind(command, CommandKind.AttackParty));
	}

	private static void BeginResultTracking(PartyCommandQueueState state, string resultKind, string targetType, string targetId, string targetName, IFaction actorFaction, IFaction targetFaction)
	{
		if (state == null)
		{
			return;
		}
		state.ResultKind = (resultKind ?? "").Trim();
		state.ResultTargetType = (targetType ?? "").Trim();
		state.ResultTargetId = (targetId ?? "").Trim();
		state.ResultTargetName = (targetName ?? "").Trim();
		state.ResultActorFactionId = SafeFactionId(actorFaction);
		state.ResultTargetFactionId = SafeFactionId(targetFaction);
		state.ResultCommitDay = NowDay();
		state.ResultDeadlineDay = state.TimeoutDay;
		state.ResultLogged = false;
	}

	private static void ResetResultTracking(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		state.ResultKind = "";
		state.ResultTargetType = "";
		state.ResultTargetId = "";
		state.ResultTargetName = "";
		state.ResultActorFactionId = "";
		state.ResultTargetFactionId = "";
		state.ResultCommitDay = -1.0;
		state.ResultDeadlineDay = -1.0;
		state.ResultLogged = false;
	}

	private bool TryCompleteCurrentAttackResult(PartyCommandQueueState state, CommandResultOutcome outcome, string detail, string reason)
	{
		if (state == null || state.ResultLogged || !IsCurrentAttackCommand(state))
		{
			return false;
		}
		PartyCommandEntry command = GetCurrentCommand(state);
		Hero hero = ResolveHeroByIdAny(state.HeroId);
		state.ResultLogged = true;
		if (hero != null)
		{
			LogFact(hero, BuildAttackResultFact(hero, state, command, outcome, detail));
		}
		MobileParty activeParty = ResolveActorParty(hero);
		if (activeParty != null)
		{
			AdvanceCommand(hero, activeParty, state, reason);
			return true;
		}
		MobileParty releaseParty = ResolveActorParty(hero, allowNonLeaderForRelease: true);
		FinishQueue(hero, releaseParty, state, reason, appendFact: true);
		return true;
	}

	private static string BuildAttackResultFact(Hero hero, PartyCommandQueueState state, PartyCommandEntry command, CommandResultOutcome outcome, string detail)
	{
		string actorName = GetStoredActorName(state, hero);
		string targetName = GetStoredTargetName(state, command);
		string safeDetail = NormalizeResultDetail(detail, outcome);
		if (IsSettlementTarget(command))
		{
			bool isRaid = string.Equals((state?.ResultKind ?? "").Trim(), "raid", StringComparison.OrdinalIgnoreCase) || ResolveSettlementById(command?.TargetId)?.IsVillage == true;
			if (isRaid)
			{
				if (outcome == CommandResultOutcome.Success)
				{
					return actorName + "成功烧掠" + targetName + "：" + safeDetail;
				}
				if (outcome == CommandResultOutcome.Failure)
				{
					return actorName + "烧掠" + targetName + "失败：" + safeDetail;
				}
				return actorName + "对" + targetName + "的烧掠未能完成：" + safeDetail;
			}
			if (outcome == CommandResultOutcome.Success)
			{
				return actorName + "围攻" + targetName + "成功：" + safeDetail;
			}
			if (outcome == CommandResultOutcome.Failure)
			{
				return actorName + "围攻" + targetName + "失败：" + safeDetail;
			}
			return actorName + "对" + targetName + "的围攻未能完成：" + safeDetail;
		}
		if (outcome == CommandResultOutcome.Success)
		{
			return actorName + "对" + targetName + "的攻击成功：" + safeDetail;
		}
		if (outcome == CommandResultOutcome.Failure)
		{
			return actorName + "对" + targetName + "的攻击失败：" + safeDetail;
		}
		return actorName + "对" + targetName + "的攻击未能完成：" + safeDetail;
	}

	private static string NormalizeResultDetail(string detail, CommandResultOutcome outcome)
	{
		string text = (detail ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			text = outcome == CommandResultOutcome.Success ? "目标已被达成。" : (outcome == CommandResultOutcome.Failure ? "原版事件判定为失败。" : "没有取得明确结果。");
		}
		if (!text.EndsWith("。", StringComparison.Ordinal) && !text.EndsWith("！", StringComparison.Ordinal) && !text.EndsWith("？", StringComparison.Ordinal))
		{
			text += "。";
		}
		return text;
	}

	private static string BuildAttackTimeoutDetail(PartyCommandEntry command, PartyCommandQueueState state)
	{
		try
		{
			if (command != null && IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command) && !string.IsNullOrWhiteSpace(state?.LastIssuedActionKey) && state.LastIssuedActionKey.StartsWith("wait_hero_shelter:", StringComparison.OrdinalIgnoreCase))
			{
				Hero targetHero = ResolveHeroByIdAny(command.TargetId);
				Settlement shelter = ResolveTargetHeroShelterSettlement(targetHero, targetHero?.PartyBelongedTo);
				if (shelter != null)
				{
					return GetHeroName(targetHero) + "仍在" + GetSettlementName(shelter) + "内，命令时限已到，未能接战。";
				}
			}
			if (command != null && IsKind(command, CommandKind.AttackParty) && !string.IsNullOrWhiteSpace(state?.LastIssuedActionKey) && state.LastIssuedActionKey.StartsWith("wait_party_shelter:", StringComparison.OrdinalIgnoreCase))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				Settlement shelter = targetParty?.CurrentSettlement;
				if (shelter != null)
				{
					return GetPartyName(targetParty) + "仍在" + GetSettlementName(shelter) + "内，命令时限已到，未能接战。";
				}
			}
		}
		catch
		{
		}
		return "命令时限已到，未能取得明确结果。";
	}

	private static string BuildCommandTimeoutFact(Hero hero, PartyCommandEntry command)
	{
		string actorName = GetHeroName(hero);
		if (command == null)
		{
			return actorName + "的大地图命令时限已到，已跳过当前命令。";
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return actorName + "未能在时限内抵达" + GetSettlementName(ResolveSettlementById(command.TargetId)) + "，已跳过当前前往命令。";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return actorName + "未能在时限内抵达" + GetSettlementName(ResolveSettlementById(command.TargetId)) + "附近，已跳过当前巡逻命令。";
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return actorName + "未能在时限内追上" + GetHeroName(ResolveHeroById(command.TargetId)) + "，已跳过当前跟随命令。";
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return actorName + "未能在时限内追上" + GetPartyName(ResolveMobilePartyById(command.TargetId)) + "，已跳过当前跟随命令。";
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return actorName + "未能在时限内与玩家部队会合，已跳过当前回队命令。";
		}
		return actorName + "的大地图命令时限已到，已跳过当前命令。";
	}

	private static bool TryKeepCommandAliveAfterTimeout(Hero hero, MobileParty party, PartyCommandQueueState state, PartyCommandEntry command, double now)
	{
		if (state == null || command == null)
		{
			return false;
		}
		try
		{
			if (state.ArrivalDay >= 0.0 && (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement) || IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.FollowParty)))
			{
				state.TimeoutDay = -1.0;
				Log("travel timeout ignored after arrival hero=" + (hero?.StringId ?? "") + " kind=" + (command.Kind ?? "") + " arrivalDay=" + state.ArrivalDay.ToString("0.00"));
				return true;
			}
			if (IsKind(command, CommandKind.GoToSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && IsPartyAtSettlement(party, settlement, SettlementArrivalDistance))
				{
					state.TimeoutDay = now + 1.0;
					Log("go timeout deferred because party is already at target hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId);
					return true;
				}
			}
			if (IsKind(command, CommandKind.PatrolSettlement))
			{
				if (IsPartyEngagingAnyTarget(party))
				{
					state.TimeoutDay = now + 1.0;
					Log("patrol timeout deferred while engaging hero=" + (hero?.StringId ?? "") + " " + DescribePartyAi(party));
					return true;
				}
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && IsPartyNearSettlementForPatrol(party, settlement, PatrolLeashDistance))
				{
					state.TimeoutDay = now + 1.0;
					Log("patrol timeout deferred near area hero=" + (hero?.StringId ?? "") + " settlement=" + settlement.StringId + " distance=" + GetDistanceToSettlementForPatrol(party, settlement).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.FollowHero))
			{
				MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
				if (targetParty != null && IsPartyCloseEnoughToStartFollowing(party, targetParty))
				{
					state.TimeoutDay = now + 1.0;
					Log("follow timeout deferred because party is already near target hero=" + (hero?.StringId ?? "") + " target=" + command.TargetId + " distance=" + GetPartyDistance(party, targetParty).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.FollowParty))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				if (targetParty != null && IsPartyCloseEnoughToStartFollowing(party, targetParty))
				{
					state.TimeoutDay = now + 1.0;
					Log("follow party timeout deferred because party is already near target hero=" + (hero?.StringId ?? "") + " targetParty=" + command.TargetId + " distance=" + GetPartyDistance(party, targetParty).ToString("0.0"));
					return true;
				}
			}
			if (IsKind(command, CommandKind.MergeToPlayer) && MobileParty.MainParty != null)
			{
				if (IsPartyNearParty(party, MobileParty.MainParty, PartyArrivalDistance))
				{
					state.TimeoutDay = now + 0.25;
					Log("merge timeout deferred because party is already near player hero=" + (hero?.StringId ?? "") + " distance=" + GetPartyDistance(party, MobileParty.MainParty).ToString("0.0"));
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			Log("timeout recovery failed hero=" + (hero?.StringId ?? "") + " kind=" + (command?.Kind ?? "") + " error=" + ex.Message);
		}
		return false;
	}

	private static string GetStoredActorName(PartyCommandQueueState state, Hero hero = null)
	{
		return GetHeroName(hero ?? ResolveHeroByIdAny(state?.HeroId));
	}

	private static string GetStoredTargetName(PartyCommandQueueState state, PartyCommandEntry command)
	{
		string stored = (state?.ResultTargetName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(stored))
		{
			return stored;
		}
		if (IsSettlementTarget(command))
		{
			return GetSettlementName(ResolveSettlementById(command?.TargetId));
		}
		if (IsKind(command, CommandKind.AttackParty) || IsKind(command, CommandKind.FollowParty))
		{
			return GetPartyName(ResolveMobilePartyById(command?.TargetId));
		}
		return GetHeroName(ResolveHeroByIdAny(command?.TargetId));
	}

	private static string BuildMapEventCasualtySummary(MapEvent mapEvent, BattleSideEnum actorSide, BattleSideEnum targetSide)
	{
		try
		{
			MapEventSide actorEventSide = mapEvent?.GetMapEventSide(actorSide);
			MapEventSide targetEventSide = mapEvent?.GetMapEventSide(targetSide);
			if (actorEventSide == null || targetEventSide == null)
			{
				return "";
			}
			int actorLosses = Math.Max(0, actorEventSide.TroopCasualties);
			int targetLosses = Math.Max(0, targetEventSide.TroopCasualties);
			return " 战斗损失：己方" + actorLosses + "人，敌方" + targetLosses + "人。";
		}
		catch
		{
			return "";
		}
	}

	private static BattleSideEnum GetHeroSideInMapEvent(MapEvent mapEvent, string heroId)
	{
		if (mapEvent == null || string.IsNullOrWhiteSpace(heroId))
		{
			return BattleSideEnum.None;
		}
		if (MapEventSideHasHero(mapEvent.AttackerSide, heroId))
		{
			return BattleSideEnum.Attacker;
		}
		if (MapEventSideHasHero(mapEvent.DefenderSide, heroId))
		{
			return BattleSideEnum.Defender;
		}
		return BattleSideEnum.None;
	}

	private static BattleSideEnum GetPartySideInMapEvent(MapEvent mapEvent, string partyId)
	{
		if (mapEvent == null || string.IsNullOrWhiteSpace(partyId))
		{
			return BattleSideEnum.None;
		}
		if (MapEventSideHasMobileParty(mapEvent.AttackerSide, partyId))
		{
			return BattleSideEnum.Attacker;
		}
		if (MapEventSideHasMobileParty(mapEvent.DefenderSide, partyId))
		{
			return BattleSideEnum.Defender;
		}
		return BattleSideEnum.None;
	}

	private static bool MapEventSideHasHero(MapEventSide side, string heroId)
	{
		if (side?.Parties == null || string.IsNullOrWhiteSpace(heroId))
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				if (PartyBaseMatchesHero(party?.Party, heroId))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool MapEventSideHasMobileParty(MapEventSide side, string partyId)
	{
		if (side?.Parties == null || string.IsNullOrWhiteSpace(partyId))
		{
			return false;
		}
		try
		{
			foreach (MapEventParty party in side.Parties)
			{
				if (PartyBaseMatchesMobileParty(party?.Party, partyId))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static bool IsTargetSettlement(PartyCommandEntry command, Settlement settlement)
	{
		return command != null && settlement != null && IsSettlementTarget(command) && string.Equals((command.TargetId ?? "").Trim(), (settlement.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsVillageLooted(Settlement settlement)
	{
		try
		{
			return settlement?.IsVillage == true && (settlement.IsRaided || settlement.Village?.VillageState == Village.VillageStates.Looted);
		}
		catch
		{
			return false;
		}
	}

	private static bool PartyMatchesHero(MobileParty party, string heroId)
	{
		return !string.IsNullOrWhiteSpace(heroId) && string.Equals(party?.LeaderHero?.StringId, heroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyBaseMatchesHero(PartyBase party, string heroId)
	{
		return !string.IsNullOrWhiteSpace(heroId) && string.Equals(party?.LeaderHero?.StringId, heroId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyBaseMatchesMobileParty(PartyBase party, string partyId)
	{
		return !string.IsNullOrWhiteSpace(partyId) && string.Equals(party?.MobileParty?.StringId, partyId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyMatchesFaction(MobileParty party, string factionId)
	{
		return !string.IsNullOrWhiteSpace(factionId) && string.Equals(SafeFactionId(party?.MapFaction), factionId, StringComparison.OrdinalIgnoreCase);
	}

	private static bool PartyBaseMatchesFaction(PartyBase party, string factionId)
	{
		return !string.IsNullOrWhiteSpace(factionId) && string.Equals(SafeFactionId(party?.MapFaction), factionId, StringComparison.OrdinalIgnoreCase);
	}

	private static string GetQueueEndReasonText(string reason)
	{
		string text = (reason ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || text == "queue_done")
		{
			return "所有命令执行完毕";
		}
		if (text.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "命令时限已到";
		}
		if (text.IndexOf("actor", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "执行者失效";
		}
		if (text.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("settlement", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "目标失效或不可执行";
		}
		if (text.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return "命令无效";
		}
		return text;
	}

	private void AdvanceCommand(Hero hero, MobileParty party, PartyCommandQueueState state, string reason)
	{
		Log("advance hero=" + (hero?.StringId ?? "") + " index=" + state.CurrentIndex + " reason=" + reason);
		AbortCurrentCommandIfNeeded(party, state);
		ResetResultTracking(state);
		state.CurrentIndex++;
		state.Stage = CommandStage.New.ToString();
		state.ArrivalDay = -1.0;
		state.TimeoutDay = -1.0;
		state.EngageCommitted = false;
		state.LastIssuedActionKey = "";
		state.LastStatusMessageKey = "";
		if (state.CurrentIndex >= state.Commands.Count)
		{
			FinishQueue(hero, party, state, "queue_done", appendFact: true);
			return;
		}
		StartCurrentCommand(hero, party, state);
	}

	private void FinishQueue(Hero hero, MobileParty party, PartyCommandQueueState state, string reason, bool appendFact)
	{
		if (party != null)
		{
			AbortCurrentCommandIfNeeded(party, state);
			ReleasePartyAi(party);
		}
		if (!string.IsNullOrWhiteSpace(state?.HeroId))
		{
			lock (_queueLock)
			{
				_queues.Remove(state.HeroId);
			}
		}
		if (appendFact && hero != null)
		{
			LogFact(hero, GetHeroName(hero) + "的大地图命令队列已经结束（" + GetQueueEndReasonText(reason) + "），回归原版行动状态。");
		}
		Log("finish hero=" + (hero?.StringId ?? state?.HeroId ?? "") + " reason=" + reason);
	}

	private static bool TryParseTag(string tag, bool validateTargets, out PartyCommandEntry command, out bool stop)
	{
		command = null;
		stop = false;
		string text = (tag ?? "").Trim();
		const string prefix = "[ACTION:WORLDMAP_ORDER:";
		if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !text.EndsWith("]", StringComparison.Ordinal))
		{
			return false;
		}
		string inner = text.Substring(prefix.Length, text.Length - prefix.Length - 1);
		string[] parts = inner.Split(new[] { ':' }, StringSplitOptions.None).Select(x => (x ?? "").Trim()).ToArray();
		if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
		{
			return false;
		}
		string kind = parts[0].ToUpperInvariant();
		if (kind == "STOP")
		{
			stop = true;
			return true;
		}
		if (kind == "CREATE_COMPANION_PARTY")
		{
			command = new PartyCommandEntry
			{
				Kind = CommandKind.CreateCompanionParty.ToString(),
				Days = 1
			};
			return true;
		}
		if (kind == "MERGE_TO_PLAYER")
		{
			command = new PartyCommandEntry
			{
				Kind = CommandKind.MergeToPlayer.ToString(),
				Days = ParseDays(parts.Length >= 2 ? parts[1] : null)
			};
			return true;
		}
		if (kind == "GO_TO_SETTLEMENT" || kind == "PATROL_SETTLEMENT")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "settlement", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (validateTargets && ResolveSettlementById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = (kind == "GO_TO_SETTLEMENT") ? CommandKind.GoToSettlement.ToString() : CommandKind.PatrolSettlement.ToString(),
				TargetType = "settlement",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null)
			};
			return true;
		}
		if (kind == "FOLLOW")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.FollowParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
					Mode = ""
				};
				return true;
			}
			return false;
		}
		if (kind == "FOLLOW_HERO")
		{
			if (parts.Length < 3 || !string.Equals(parts[1], "hero", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (validateTargets && ResolveHeroById(id) == null)
			{
				return false;
			}
			command = new PartyCommandEntry
			{
				Kind = CommandKind.FollowHero.ToString(),
				TargetType = "hero",
				TargetId = id,
				Days = ParseDays(parts.Length >= 4 ? parts[3] : null),
				Mode = ""
			};
			return true;
		}
		if (kind == "ATTACK")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			return false;
		}
		if (kind == "ATTACK_HERO")
		{
			if (parts.Length < 3)
			{
				return false;
			}
			string targetType = (parts[1] ?? "").Trim().ToLowerInvariant();
			string id = parts[2];
			if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '[', ']', '\r', '\n' }) >= 0)
			{
				return false;
			}
			if (string.Equals(targetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveHeroById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "hero",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "settlement", StringComparison.OrdinalIgnoreCase))
			{
				Settlement settlement = ResolveSettlementById(id);
				if (validateTargets && !IsSupportedAttackSettlement(settlement))
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackHero.ToString(),
					TargetType = "settlement",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, GetDefaultAttackDaysForSettlement(settlement)),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
			if (string.Equals(targetType, "party", StringComparison.OrdinalIgnoreCase))
			{
				if (validateTargets && ResolveMobilePartyById(id) == null)
				{
					return false;
				}
				command = new PartyCommandEntry
				{
					Kind = CommandKind.AttackParty.ToString(),
					TargetType = "party",
					TargetId = id,
					Days = ParseDays(parts.Length >= 4 ? parts[3] : null, DefaultHeroAttackDays),
					Mode = NormalizeAttackMode(parts.Length >= 5 ? parts[4] : "AI")
				};
				return true;
			}
		}
		return false;
	}

	private static string BuildTag(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "";
		}
		if (IsKind(command, CommandKind.GoToSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:GO_TO_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.PatrolSettlement))
		{
			return "[ACTION:WORLDMAP_ORDER:PATROL_SETTLEMENT:settlement:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:hero:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return "[ACTION:WORLDMAP_ORDER:FOLLOW:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			string targetType = IsSettlementTarget(command) ? "settlement" : "hero";
			return "[ACTION:WORLDMAP_ORDER:ATTACK:" + targetType + ":" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			return "[ACTION:WORLDMAP_ORDER:ATTACK:party:" + command.TargetId + ":" + Math.Max(1, command.Days) + ":" + NormalizeAttackMode(command.Mode) + "]";
		}
		if (IsKind(command, CommandKind.MergeToPlayer))
		{
			return "[ACTION:WORLDMAP_ORDER:MERGE_TO_PLAYER:" + Math.Max(1, command.Days) + "]";
		}
		if (IsKind(command, CommandKind.CreateCompanionParty))
		{
			return "[ACTION:WORLDMAP_ORDER:CREATE_COMPANION_PARTY]";
		}
		return "";
	}

	private static int ParseDays(string token)
	{
		return ParseDays(token, 1);
	}

	private static int ParseDays(string token, int defaultDays)
	{
		if (!int.TryParse((token ?? "").Trim(), out int result) || result <= 0)
		{
			return Math.Max(1, defaultDays);
		}
		return result;
	}

	private static string NormalizeAttackMode(string token)
	{
		string text = (token ?? "").Trim();
		if (string.Equals(text, LegacyAttackModeRebellionForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		if (string.Equals(text, AttackModeForce, StringComparison.OrdinalIgnoreCase))
		{
			return AttackModeForce;
		}
		return AttackModeAi;
	}

	private static bool IsForceAttackMode(string mode)
	{
		string normalized = NormalizeAttackMode(mode);
		return string.Equals(normalized, AttackModeForce, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsExecutableCommand(PartyCommandEntry command)
	{
		if (command == null)
		{
			return false;
		}
		command.Days = Math.Max(1, command.Days);
		command.Mode = NormalizeAttackMode(command.Mode);
		if (IsKind(command, CommandKind.CreateCompanionParty) || IsKind(command, CommandKind.MergeToPlayer))
		{
			return true;
		}
		if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
		{
			return string.Equals(command.TargetType, "settlement", StringComparison.OrdinalIgnoreCase) && ResolveSettlementById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.FollowHero))
		{
			return string.Equals(command.TargetType, "hero", StringComparison.OrdinalIgnoreCase) && ResolveHeroById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.FollowParty))
		{
			return string.Equals(command.TargetType, "party", StringComparison.OrdinalIgnoreCase) && ResolveMobilePartyById(command.TargetId) != null;
		}
		if (IsKind(command, CommandKind.AttackHero))
		{
			if (string.Equals(command.TargetType, "hero", StringComparison.OrdinalIgnoreCase))
			{
				return ResolveHeroById(command.TargetId) != null;
			}
			return string.Equals(command.TargetType, "settlement", StringComparison.OrdinalIgnoreCase) && IsSupportedAttackSettlement(ResolveSettlementById(command.TargetId));
		}
		if (IsKind(command, CommandKind.AttackParty))
		{
			return string.Equals(command.TargetType, "party", StringComparison.OrdinalIgnoreCase) && ResolveMobilePartyById(command.TargetId) != null;
		}
		return false;
	}

	private static PartyCommandEntry CloneCommand(PartyCommandEntry command)
	{
		return new PartyCommandEntry
		{
			Kind = command.Kind,
			TargetType = NormalizeTargetType(command),
			TargetId = command.TargetId,
			Days = Math.Max(1, command.Days),
			Mode = NormalizeAttackMode(command.Mode)
		};
	}

	private static string NormalizeTargetType(PartyCommandEntry command)
	{
		if (command == null)
		{
			return "";
		}
		if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement) || IsSettlementTarget(command))
		{
			return "settlement";
		}
		if (IsKind(command, CommandKind.FollowHero) || IsKind(command, CommandKind.AttackHero))
		{
			return "hero";
		}
		if (IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty))
		{
			return "party";
		}
		return (command.TargetType ?? "").Trim();
	}

	private static bool IsKind(PartyCommandEntry command, CommandKind kind)
	{
		return command != null && string.Equals((command.Kind ?? "").Trim(), kind.ToString(), StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSettlementTarget(PartyCommandEntry command)
	{
		return command != null && string.Equals((command.TargetType ?? "").Trim(), "settlement", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSupportedAttackSettlement(Settlement settlement)
	{
		try
		{
			return settlement != null && (settlement.IsTown || settlement.IsCastle || settlement.IsVillage);
		}
		catch
		{
			return false;
		}
	}

	private static int GetDefaultAttackDaysForSettlement(Settlement settlement)
	{
		if (settlement?.IsVillage == true)
		{
			return DefaultRaidAttackDays;
		}
		if (settlement?.IsTown == true || settlement?.IsCastle == true)
		{
			return DefaultSiegeAttackDays;
		}
		return DefaultHeroAttackDays;
	}

	private static CommandStage ParseStage(string value)
	{
		if (Enum.TryParse((value ?? "").Trim(), ignoreCase: true, out CommandStage stage))
		{
			return stage;
		}
		return CommandStage.New;
	}

	private static void NormalizeState(PartyCommandQueueState state)
	{
		if (state == null)
		{
			return;
		}
		state.Commands = (state.Commands ?? new List<PartyCommandEntry>()).Where(x => x != null).ToList();
		state.CurrentIndex = Math.Max(0, state.CurrentIndex);
		if (string.IsNullOrWhiteSpace(state.Stage))
		{
			state.Stage = CommandStage.New.ToString();
		}
		foreach (PartyCommandEntry command in state.Commands)
		{
			if (command != null)
			{
				command.Days = Math.Max(1, command.Days);
				command.Mode = NormalizeAttackMode(command.Mode);
				if (IsKind(command, CommandKind.AttackHero) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "hero";
				}
				if ((IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement)) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "settlement";
				}
				if (IsKind(command, CommandKind.FollowHero) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "hero";
				}
				if ((IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty)) && string.IsNullOrWhiteSpace(command.TargetType))
				{
					command.TargetType = "party";
				}
			}
		}
	}

	private static Settlement ResolveSettlementById(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Settlement.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroById(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.AllAliveHeroes?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Hero ResolveHeroByIdAny(string id)
	{
		string text = (id ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			Hero hero = Hero.Find(text);
			if (hero != null)
			{
				return hero;
			}
		}
		catch
		{
		}
		try
		{
			return Hero.FindFirst(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return ResolveHeroById(text);
		}
	}

	private static MobileParty ResolveTargetHeroParty(string heroId)
	{
		Hero hero = ResolveHeroById(heroId);
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			return null;
		}
		MobileParty party = hero.PartyBelongedTo;
		return IsPartyUsable(party) ? party : null;
	}

	private static MobileParty ResolveMobilePartyById(string id)
	{
		string text = (id ?? "").Trim();
		if (text.StartsWith("party:", StringComparison.OrdinalIgnoreCase))
		{
			text = text.Substring("party:".Length).Trim();
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return MobileParty.All?.FirstOrDefault(x => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

	private static Settlement ResolveTargetHeroShelterSettlement(Hero hero, MobileParty targetParty)
	{
		try
		{
			if (hero == null || hero.IsDead || hero.IsPrisoner)
			{
				return null;
			}
			if (targetParty?.MapEvent != null && !targetParty.MapEvent.IsFinalized)
			{
				return null;
			}
			Settlement settlement = targetParty?.CurrentSettlement ?? hero.CurrentSettlement;
			return settlement;
		}
		catch
		{
			return null;
		}
	}

	private static MobileParty ResolveActorParty(Hero hero, bool allowNonLeaderForRelease = false)
	{
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			return null;
		}
		MobileParty party = (hero == Hero.MainHero) ? MobileParty.MainParty : hero.PartyBelongedTo;
		if (!IsPartyUsable(party))
		{
			return null;
		}
		if (!allowNonLeaderForRelease && party.LeaderHero != hero)
		{
			return null;
		}
		return party;
	}

	private static bool ValidateActor(Hero hero, MobileParty party, out string reason)
	{
		reason = "";
		if (hero == null || hero.IsDead || hero.IsPrisoner)
		{
			reason = "hero_invalid";
			return false;
		}
		if (!IsPartyUsable(party))
		{
			reason = "party_invalid";
			return false;
		}
		if (party.LeaderHero != hero)
		{
			reason = "not_party_leader";
			return false;
		}
		return true;
	}

	private static bool IsPartyUsable(MobileParty party)
	{
		try
		{
			return party != null && party.IsActive && party.Party != null;
		}
		catch
		{
			return false;
		}
	}

	private static void LockPartyAi(MobileParty party)
	{
		try
		{
			party?.Ai?.SetDoNotMakeNewDecisions(true);
		}
		catch
		{
		}
	}

	private static void ReleasePartyAi(MobileParty party)
	{
		try
		{
			party?.Ai?.SetDoNotMakeNewDecisions(false);
		}
		catch
		{
		}
	}

	private static bool IsAiDecisionLockActive(MobileParty party)
	{
		try
		{
			return party?.Ai?.DoNotMakeNewDecisions == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyTrackingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.GoAroundParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEngagingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.EngageParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEngagingAnyTarget(MobileParty party)
	{
		try
		{
			if (party?.MapEvent != null && !party.MapEvent.IsFinalized)
			{
				return true;
			}
			return party != null
				&& (party.DefaultBehavior == AiBehavior.EngageParty
					|| party.ShortTermBehavior == AiBehavior.EngageParty
					|| party.DefaultBehavior == AiBehavior.GoAroundParty
					|| party.ShortTermBehavior == AiBehavior.GoAroundParty)
				&& (party.TargetParty != null || party.Ai?.AiBehaviorPartyBase?.MobileParty != null);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyEscortingTarget(MobileParty party, MobileParty targetParty)
	{
		try
		{
			return party != null && targetParty != null && party.DefaultBehavior == AiBehavior.EscortParty && party.TargetParty == targetParty;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyVisitingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return party != null
				&& settlement != null
				&& ((party.DefaultBehavior == AiBehavior.GoToSettlement && party.TargetSettlement == settlement)
					|| (party.ShortTermBehavior == AiBehavior.GoToSettlement && party.ShortTermTargetSettlement == settlement));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyPatrollingSettlement(MobileParty party, Settlement settlement)
	{
		try
		{
			return party != null
				&& settlement != null
				&& party.DefaultBehavior == AiBehavior.PatrolAroundPoint
				&& party.TargetSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyNearSettlementForPatrol(MobileParty party, Settlement settlement, float distance)
	{
		try
		{
			if (party == null || settlement == null)
			{
				return false;
			}
			if (party.CurrentSettlement == settlement)
			{
				return true;
			}
			if (IsPartyNearPosition(party, settlement.GatePosition, distance))
			{
				return true;
			}
			CampaignVec2 center = GetSettlementMapPosition(settlement);
			return IsPartyNearPosition(party, center, distance);
		}
		catch
		{
			return false;
		}
	}

	private static float GetDistanceToSettlementForPatrol(MobileParty party, Settlement settlement)
	{
		try
		{
			if (party == null || settlement == null)
			{
				return 0f;
			}
			float gateDistance = party.Position.Distance(settlement.GatePosition);
			float centerDistance = party.Position.Distance(GetSettlementMapPosition(settlement));
			return Math.Min(gateDistance, centerDistance);
		}
		catch
		{
			return 0f;
		}
	}

	private static CampaignVec2 GetSettlementMapPosition(Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return CampaignVec2.Zero;
			}
			return new CampaignVec2(settlement.GetPosition2D, settlement.GatePosition.IsOnLand);
		}
		catch
		{
			return settlement?.GatePosition ?? CampaignVec2.Zero;
		}
	}

	private static string DescribePartyAi(MobileParty party)
	{
		try
		{
			return "default=" + party.DefaultBehavior + " short=" + party.ShortTermBehavior + " target=" + (party.TargetParty?.StringId ?? "null") + " locked=" + (party.Ai?.DoNotMakeNewDecisions == true ? "true" : "false");
		}
		catch
		{
			return "";
		}
	}

	private static void PreemptBlockingWorldActivityForCommand(Hero hero, MobileParty party, PartyCommandEntry command, PartyCommandQueueState state, string phase)
	{
		try
		{
			if (!IsPartyUsable(party) || command == null || IsKind(command, CommandKind.CreateCompanionParty))
			{
				return;
			}
			bool changed = false;
			List<string> reasons = new List<string>();
			MapEvent mapEvent = null;
			try
			{
				mapEvent = party.MapEvent;
			}
			catch
			{
				mapEvent = null;
			}
			if (IsPreemptableSettlementMapEventForParty(party, mapEvent) && !IsCommandContinuingCurrentSettlementAttack(party, command, mapEvent?.MapEventSettlement, mapEvent))
			{
				try
				{
					TryFinishPlayerEncounterForMapEvent(mapEvent);
					mapEvent.FinalizeEvent();
					changed = true;
					reasons.Add(GetMapEventPreemptReason(mapEvent));
				}
				catch (Exception ex)
				{
					Log("preempt map event failed party=" + (party.StringId ?? "") + " event=" + GetMapEventPreemptReason(mapEvent) + " error=" + ex.Message);
				}
			}
			Settlement siegeSettlement = GetPartySiegeSettlementSafe(party);
			if (party.SiegeEvent != null && !IsCommandContinuingCurrentSettlementAttack(party, command, siegeSettlement, null))
			{
				try
				{
					party.SiegeEvent.FinalizeSiegeEvent();
					changed = true;
					reasons.Add("当前围城");
				}
				catch (Exception ex)
				{
					Log("preempt siege event failed party=" + (party.StringId ?? "") + " error=" + ex.Message);
				}
			}
			Settlement behaviorSettlement = GetBlockingSettlementBehaviorTarget(party);
			if (behaviorSettlement != null && !IsCommandContinuingCurrentSettlementAttack(party, command, behaviorSettlement, null))
			{
				try
				{
					party.SetMoveModeHold();
					changed = true;
					reasons.Add("当前战略行动");
				}
				catch (Exception ex)
				{
					Log("preempt blocking behavior failed party=" + (party.StringId ?? "") + " error=" + ex.Message);
				}
			}
			if (!changed)
			{
				return;
			}
			string reason = string.Join("、", reasons.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase));
			if (string.IsNullOrWhiteSpace(reason))
			{
				reason = "当前原版行动";
			}
			string key = "preempt:" + phase + ":" + (command.Kind ?? "") + ":" + (command.TargetType ?? "") + ":" + (command.TargetId ?? "");
			NotifyCommandStatus(state, key, GetHeroName(hero) + "放弃" + reason + "，改为执行新的大地图命令。", CommandMessageTone.Progress);
			LogFact(hero, GetHeroName(hero) + "放弃" + reason + "，改为执行新的大地图命令。");
			Log("preempt_activity hero=" + (hero?.StringId ?? "") + " party=" + (party.StringId ?? "") + " reason=" + reason + " command=" + (command.Kind ?? "") + ":" + (command.TargetType ?? "") + ":" + (command.TargetId ?? "") + " phase=" + (phase ?? "") + " " + DescribePartyAi(party));
		}
		catch (Exception ex)
		{
			Log("preempt blocking activity failed hero=" + (hero?.StringId ?? "") + " party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPreemptableSettlementMapEventForParty(MobileParty party, MapEvent mapEvent)
	{
		try
		{
			return IsPartyUsable(party)
				&& mapEvent != null
				&& !mapEvent.IsFinalized
				&& (mapEvent.IsRaid || mapEvent.IsSiegeAssault || mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers)
				&& MapEventSideHasMobileParty(mapEvent.AttackerSide, party.StringId);
		}
		catch
		{
			return false;
		}
	}

	private static bool IsCommandContinuingCurrentSettlementAttack(MobileParty party, PartyCommandEntry command, Settlement settlement, MapEvent mapEvent)
	{
		try
		{
			if (party == null || command == null || settlement == null || !IsTargetSettlement(command, settlement))
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				return (mapEvent != null && mapEvent.IsRaid)
					|| party.DefaultBehavior == AiBehavior.RaidSettlement
					|| party.ShortTermBehavior == AiBehavior.RaidSettlement;
			}
			return (mapEvent != null && mapEvent.IsSiegeAssault)
				|| party.SiegeEvent != null
				|| party.BesiegedSettlement == settlement
				|| party.DefaultBehavior == AiBehavior.BesiegeSettlement
				|| party.ShortTermBehavior == AiBehavior.BesiegeSettlement
				|| party.DefaultBehavior == AiBehavior.AssaultSettlement
				|| party.ShortTermBehavior == AiBehavior.AssaultSettlement;
		}
		catch
		{
			return false;
		}
	}

	private static Settlement GetPartySiegeSettlementSafe(MobileParty party)
	{
		try
		{
			return party?.BesiegedSettlement ?? party?.SiegeEvent?.BesiegedSettlement;
		}
		catch
		{
			return null;
		}
	}

	private static Settlement GetBlockingSettlementBehaviorTarget(MobileParty party)
	{
		try
		{
			if (party == null)
			{
				return null;
			}
			if (party.DefaultBehavior == AiBehavior.RaidSettlement
				|| party.DefaultBehavior == AiBehavior.BesiegeSettlement
				|| party.DefaultBehavior == AiBehavior.AssaultSettlement
				|| party.ShortTermBehavior == AiBehavior.RaidSettlement
				|| party.ShortTermBehavior == AiBehavior.BesiegeSettlement
				|| party.ShortTermBehavior == AiBehavior.AssaultSettlement)
			{
				return party.TargetSettlement ?? party.ShortTermTargetSettlement ?? party.BesiegedSettlement;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string GetMapEventPreemptReason(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null)
			{
				return "当前原版事件";
			}
			if (mapEvent.IsRaid)
			{
				return "当前烧掠";
			}
			if (mapEvent.IsForcingSupplies)
			{
				return "当前强征补给";
			}
			if (mapEvent.IsForcingVolunteers)
			{
				return "当前强征兵员";
			}
			if (mapEvent.IsSiegeAssault)
			{
				return "当前攻城";
			}
		}
		catch
		{
		}
		return "当前原版事件";
	}

	private static void TryFinishPlayerEncounterForMapEvent(MapEvent mapEvent)
	{
		try
		{
			if (mapEvent == null || PlayerEncounterCompat.GetCurrentMapEventSafe() != mapEvent)
			{
				return;
			}
			foreach (MethodInfo method in typeof(PlayerEncounter).GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => string.Equals(x.Name, "Finish", StringComparison.Ordinal)))
			{
				ParameterInfo[] parameters = method.GetParameters();
				if (parameters.Length == 0)
				{
					method.Invoke(null, null);
					return;
				}
				if (parameters.Length == 1 && parameters[0].ParameterType == typeof(bool))
				{
					method.Invoke(null, new object[] { true });
					return;
				}
			}
		}
		catch (Exception ex)
		{
			Log("finish player encounter before preempt failed error=" + ex.Message);
		}
	}

	private static void LeaveArmyIfNeeded(MobileParty party)
	{
		try
		{
			if (party?.Army != null && party.Army.LeaderParty != party)
			{
				party.Army = null;
			}
		}
		catch (Exception ex)
		{
			Log("leave army failed: " + ex.Message);
		}
	}

	private static void SynchronizeArmyObjectiveForCommand(MobileParty party, PartyCommandEntry command)
	{
		try
		{
			if (party?.Army == null || party.Army.LeaderParty != party || command == null)
			{
				return;
			}
			Army army = party.Army;
			IMapPoint oldObject = army.AiBehaviorObject;
			Army.ArmyTypes oldType = army.ArmyType;
			if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null)
				{
					army.ArmyType = Army.ArmyTypes.Defender;
					army.AiBehaviorObject = settlement;
				}
			}
			else if (IsKind(command, CommandKind.AttackHero) && IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null)
				{
					army.ArmyType = settlement.IsVillage ? Army.ArmyTypes.Raider : Army.ArmyTypes.Besieger;
					army.AiBehaviorObject = settlement;
				}
			}
			else
			{
				army.AiBehaviorObject = null;
			}
			if (oldType != army.ArmyType || oldObject != army.AiBehaviorObject)
			{
				Log("army_object_sync leader=" + (party.StringId ?? "") + " type=" + army.ArmyType + " target=" + DescribeMapPointForLog(army.AiBehaviorObject));
			}
		}
		catch (Exception ex)
		{
			Log("army objective sync failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static string DescribeMapPointForLog(IMapPoint mapPoint)
	{
		try
		{
			return mapPoint?.Name?.ToString() ?? "null";
		}
		catch
		{
			return "null";
		}
	}

	private static double ComputeTimeoutDay(MobileParty party, PartyCommandEntry command)
	{
		double now = NowDay();
		try
		{
			float distance = 0f;
			if (IsKind(command, CommandKind.GoToSettlement) || IsKind(command, CommandKind.PatrolSettlement))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && party != null)
				{
					distance = IsKind(command, CommandKind.PatrolSettlement) ? GetDistanceToSettlementForPatrol(party, settlement) : party.Position.Distance(settlement.GatePosition);
				}
			}
			else if (IsKind(command, CommandKind.FollowHero) || (IsKind(command, CommandKind.AttackHero) && !IsSettlementTarget(command)))
			{
				MobileParty targetParty = ResolveTargetHeroParty(command.TargetId);
				if (targetParty != null && party != null)
				{
					distance = party.Position.Distance(targetParty.Position);
				}
				else if (IsKind(command, CommandKind.AttackHero) && party != null)
				{
					Settlement shelter = ResolveTargetHeroShelterSettlement(ResolveHeroById(command.TargetId), targetParty);
					if (shelter != null)
					{
						distance = party.Position.Distance(GetSettlementAttackPosition(shelter));
					}
				}
			}
			else if (IsKind(command, CommandKind.FollowParty) || IsKind(command, CommandKind.AttackParty))
			{
				MobileParty targetParty = ResolveMobilePartyById(command.TargetId);
				if (targetParty != null && party != null)
				{
					distance = party.Position.Distance(targetParty.Position);
				}
				else if (targetParty?.CurrentSettlement != null && party != null)
				{
					distance = party.Position.Distance(GetSettlementAttackPosition(targetParty.CurrentSettlement));
				}
			}
			else if (IsKind(command, CommandKind.AttackHero) && IsSettlementTarget(command))
			{
				Settlement settlement = ResolveSettlementById(command.TargetId);
				if (settlement != null && party != null)
				{
					distance = party.Position.Distance(GetSettlementAttackPosition(settlement));
				}
			}
			else if (IsKind(command, CommandKind.MergeToPlayer) && party != null && MobileParty.MainParty != null)
			{
				distance = party.Position.Distance(MobileParty.MainParty.Position);
			}
			float speed = Math.Max(2.0f, party?.Speed ?? 4.0f);
			double estimatedDays = distance / Math.Max(1.0f, speed * 24.0f);
			double timeout = now + Math.Max(1.5, estimatedDays * 3.0 + 1.0);
			if (IsKind(command, CommandKind.PatrolSettlement))
			{
				timeout = Math.Max(timeout, now + Math.Max(3.0, Math.Max(1, command?.Days ?? 1) + 1.0));
			}
			return timeout;
		}
		catch
		{
			return now + 3.0;
		}
	}

	private static bool IsPartyAtSettlement(MobileParty party, Settlement settlement, float distance)
	{
		try
		{
			return party != null && settlement != null && (party.CurrentSettlement == settlement || IsPartyNearPosition(party, settlement.GatePosition, distance));
		}
		catch
		{
			return false;
		}
	}

	private static CampaignVec2 GetSettlementAttackPosition(Settlement settlement)
	{
		try
		{
			return settlement?.GatePosition ?? CampaignVec2.Zero;
		}
		catch
		{
			return CampaignVec2.Zero;
		}
	}

	private static void MoveTowardSettlementAttackPoint(MobileParty party, Settlement settlement)
	{
		if (party == null || settlement == null)
		{
			return;
		}
		try
		{
			party.SetMoveGoToPoint(GetSettlementAttackPosition(settlement), MobileParty.NavigationType.Default);
		}
		catch (Exception ex)
		{
			Log("move settlement attack point failed party=" + (party.StringId ?? "") + " settlement=" + (settlement.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static void LeaveTargetSettlementIfInside(MobileParty party, Settlement settlement)
	{
		try
		{
			if (party != null && settlement != null && party.CurrentSettlement == settlement)
			{
				LeaveSettlementAction.ApplyForParty(party);
			}
		}
		catch (Exception ex)
		{
			Log("leave target settlement before attack failed party=" + (party?.StringId ?? "") + " settlement=" + (settlement?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPartyCommittedToSettlementAttack(MobileParty party, Settlement settlement)
	{
		try
		{
			if (!IsPartyUsable(party) || settlement == null)
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				if ((party.DefaultBehavior == AiBehavior.RaidSettlement && party.TargetSettlement == settlement) || (party.ShortTermBehavior == AiBehavior.RaidSettlement && party.ShortTermTargetSettlement == settlement))
				{
					return true;
				}
				return party.MapEvent != null && !party.MapEvent.IsFinalized && party.MapEvent.IsRaid && party.MapEvent.MapEventSettlement == settlement;
			}
			if ((party.DefaultBehavior == AiBehavior.BesiegeSettlement && party.TargetSettlement == settlement) || party.BesiegedSettlement == settlement)
			{
				return true;
			}
			return party.SiegeEvent != null && party.SiegeEvent.BesiegedSettlement == settlement;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSameFactionSiege(MobileParty party, Settlement settlement)
	{
		try
		{
			IFaction partyFaction = party?.MapFaction;
			IFaction siegeFaction = settlement?.SiegeEvent?.BesiegerCamp?.MapFaction;
			return partyFaction != null && siegeFaction != null && partyFaction == siegeFaction;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSettlementAttackComplete(MobileParty party, Settlement settlement)
	{
		try
		{
			if (settlement == null)
			{
				return false;
			}
			if (settlement.IsVillage)
			{
				return settlement.IsRaided;
			}
			IFaction partyFaction = party?.MapFaction;
			return partyFaction != null && settlement.MapFaction == partyFaction && !settlement.IsUnderSiege;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsSettlementAttackUnavailableBeforeCommit(Settlement settlement)
	{
		try
		{
			if (settlement?.IsVillage == true)
			{
				return settlement.IsRaided || settlement.IsUnderRaid || settlement.SettlementHitPoints <= 0.001f;
			}
		}
		catch
		{
		}
		return false;
	}

	private static void LogSettlementAttackComplete(Hero hero, Settlement settlement)
	{
		if (hero == null || settlement == null)
		{
			return;
		}
		LogFact(hero, GetHeroName(hero) + (settlement.IsVillage ? "已经完成对" : "已经结束对") + GetSettlementName(settlement) + (settlement.IsVillage ? "的烧掠。" : "的攻击。"));
	}

	private static void AbortCurrentCommandIfNeeded(MobileParty party, PartyCommandQueueState state)
	{
		try
		{
			if (party == null || state == null || state.Commands == null || state.CurrentIndex < 0 || state.CurrentIndex >= state.Commands.Count)
			{
				return;
			}
			PartyCommandEntry command = state.Commands[state.CurrentIndex];
			if (!IsKind(command, CommandKind.AttackHero) && !IsKind(command, CommandKind.AttackParty))
			{
				return;
			}
			bool isShelterWait = !string.IsNullOrWhiteSpace(state.LastIssuedActionKey) && (state.LastIssuedActionKey.StartsWith("wait_hero_shelter:", StringComparison.OrdinalIgnoreCase) || state.LastIssuedActionKey.StartsWith("wait_party_shelter:", StringComparison.OrdinalIgnoreCase));
			if (!IsSettlementTarget(command) && !isShelterWait)
			{
				return;
			}
			if (party.DefaultBehavior == AiBehavior.BesiegeSettlement || party.DefaultBehavior == AiBehavior.RaidSettlement || party.DefaultBehavior == AiBehavior.GoToPoint)
			{
				party.SetMoveModeHold();
			}
		}
		catch (Exception ex)
		{
			Log("abort current worldmap command failed party=" + (party?.StringId ?? "") + " error=" + ex.Message);
		}
	}

	private static bool IsPartyNearPosition(MobileParty party, CampaignVec2 position, float distance)
	{
		try
		{
			if (party == null)
			{
				return false;
			}
			return party.Position.DistanceSquared(position) <= distance * distance;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyNearParty(MobileParty a, MobileParty b, float distance)
	{
		try
		{
			if (a == null || b == null)
			{
				return false;
			}
			if (a.CurrentSettlement != null && a.CurrentSettlement == b.CurrentSettlement)
			{
				return true;
			}
			return a.Position.DistanceSquared(b.Position) <= distance * distance;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsPartyCloseEnoughToStartFollowing(MobileParty party, MobileParty targetParty)
	{
		try
		{
			if (IsPartyNearParty(party, targetParty, FollowArrivalDistance))
			{
				return true;
			}
			return IsPartyEscortingTarget(party, targetParty) && IsPartyNearParty(party, targetParty, FollowLeashDistance);
		}
		catch
		{
			return false;
		}
	}

	private static float GetPartyDistance(MobileParty a, MobileParty b)
	{
		try
		{
			if (a == null || b == null)
			{
				return -1f;
			}
			if (a.CurrentSettlement != null && a.CurrentSettlement == b.CurrentSettlement)
			{
				return 0f;
			}
			return a.Position.Distance(b.Position);
		}
		catch
		{
			return -1f;
		}
	}

	private bool TryOpenCreateCompanionParty(Hero hero, out string message)
	{
		return TryOpenCreateCompanionParty(hero, null, out message);
	}

	private bool TryOpenCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands, appendReturnToPlayer: true);
			if (hero == null || hero == Hero.MainHero || hero.Clan != Clan.PlayerClan)
			{
				message = "只有玩家家族的同伴可以创建部队。";
				return false;
			}
			if (hero.PartyBelongedTo != MobileParty.MainParty)
			{
				message = GetHeroName(hero) + "必须先在玩家队伍中，才能打开原版创建同伴部队界面。";
				return false;
			}
			if (!CanOpenCreateCompanionPartyScreenNow(out string blockedReason))
			{
				QueuePendingCreateCompanionParty(hero, safeFollowUpCommands);
				message = "已记录" + GetHeroName(hero) + "的创建同伴部队请求；" + blockedReason + "，返回大地图后会自动打开分兵界面。";
				return true;
			}
			return OpenCreateCompanionPartyScreen(hero, safeFollowUpCommands, out message);
		}
		catch (Exception ex)
		{
			message = "打开原版创建同伴部队界面失败：" + ex.Message;
			Log("create companion party failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static List<PartyCommandEntry> SanitizeFollowUpCommands(List<PartyCommandEntry> followUpCommands, bool appendReturnToPlayer = false)
	{
		List<PartyCommandEntry> sanitized = (followUpCommands ?? new List<PartyCommandEntry>())
			.Where(command => command != null && !IsKind(command, CommandKind.CreateCompanionParty) && IsExecutableCommand(command))
			.Select(CloneCommand)
			.ToList();
		if (appendReturnToPlayer && sanitized.Count > 0 && !sanitized.Any(command => IsKind(command, CommandKind.MergeToPlayer)))
		{
			sanitized.Add(new PartyCommandEntry
			{
				Kind = CommandKind.MergeToPlayer.ToString(),
				Days = 1
			});
		}
		return sanitized;
	}

	private void QueuePendingCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		lock (_queueLock)
		{
			_pendingCreatePartyRequests[hero.StringId] = new PendingCreateCompanionPartyRequest
			{
				HeroId = hero.StringId,
				FollowUpCommands = SanitizeFollowUpCommands(followUpCommands)
			};
		}
		Log("queued create companion party hero=" + hero.StringId + " followUp=" + (followUpCommands?.Count ?? 0));
	}

	private static bool CanOpenCreateCompanionPartyScreenNow(out string blockedReason)
	{
		blockedReason = "";
		if (Mission.Current != null)
		{
			blockedReason = "当前仍在场景或阅兵中";
			return false;
		}
		if (IsPartyScreenStillActive())
		{
			blockedReason = "当前已有部队界面打开";
			return false;
		}
		if (Game.Current?.GameStateManager == null)
		{
			blockedReason = "当前游戏界面状态尚未就绪";
			return false;
		}
		if (!IsPartyUsable(MobileParty.MainParty))
		{
			blockedReason = "玩家主队当前不可用";
			return false;
		}
		return true;
	}

	private static bool IsPartyScreenStillActive()
	{
		try
		{
			string activeStateName = Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "";
			return activeStateName.IndexOf("PartyState", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private bool OpenCreateCompanionPartyScreen(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands);
			_isOpeningCreateCompanionPartyScreen = true;
			PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(
				hero,
				(leftOwnerParty, leftMemberRoster, leftPrisonRoster, rightOwnerParty, rightMemberRoster, rightPrisonRoster, fromCancel) =>
				{
					_isOpeningCreateCompanionPartyScreen = false;
					OnCreateCompanionPartyScreenClosed(hero.StringId, safeFollowUpCommands, leftMemberRoster, leftPrisonRoster, rightOwnerParty, fromCancel);
				});
			message = "已打开" + GetHeroName(hero) + "的原版创建同伴部队界面。";
			return true;
		}
		catch (Exception ex)
		{
			_isOpeningCreateCompanionPartyScreen = false;
			message = "打开原版创建同伴部队界面失败：" + ex.Message;
			Log("open create companion party screen failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private void OnCreateCompanionPartyScreenClosed(string heroId, List<PartyCommandEntry> followUpCommands, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, bool fromCancel)
	{
		Hero hero = ResolveHeroByIdAny(heroId);
		try
		{
			if (hero == null)
			{
				Log("create companion party closed missing hero=" + (heroId ?? ""));
				return;
			}
			if (fromCancel)
			{
				LogFact(hero, GetHeroName(hero) + "的同伴部队创建已取消，后续大地图命令未执行。");
				return;
			}
			Hero partyHero = FindHeroInRoster(leftMemberRoster) ?? hero;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			if (partyHero.Gold < partyGoldLowerThreshold)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, partyHero, partyGoldLowerThreshold - partyHero.Gold, false);
			}
			MobileParty createdParty = MobilePartyHelper.CreateNewClanMobileParty(partyHero, partyHero.Clan);
			int movedMembers = MoveSelectedTroopsToCreatedParty(createdParty, partyHero, leftMemberRoster, rightOwnerParty);
			int movedPrisoners = MoveSelectedPrisonersToCreatedParty(createdParty, leftPrisonRoster, rightOwnerParty);
			LogFact(partyHero, GetHeroName(partyHero) + "已经创建同伴部队，并接收了" + movedMembers + "名士兵" + (movedPrisoners > 0 ? ("、" + movedPrisoners + "名俘虏") : "") + "。");
			if (followUpCommands != null && followUpCommands.Count > 0)
			{
				if (TryReplaceQueue(partyHero, followUpCommands, out string fact, out string queueMessage))
				{
					if (!string.IsNullOrWhiteSpace(fact))
					{
						MyBehavior.AppendExternalDialogueHistory(partyHero, null, null, fact);
					}
					DisplayCommandMessage(queueMessage, isFailure: false);
				}
				else
				{
					LogFact(partyHero, GetHeroName(partyHero) + "创建同伴部队后无法接续后续大地图命令：" + queueMessage);
				}
			}
		}
		catch (Exception ex)
		{
			Log("create companion party close failed hero=" + (heroId ?? "") + " error=" + ex);
			if (hero != null)
			{
				LogFact(hero, GetHeroName(hero) + "创建同伴部队失败：" + ex.Message);
			}
		}
	}

	private static Hero FindHeroInRoster(TroopRoster roster)
	{
		if (roster == null)
		{
			return null;
		}
		foreach (TroopRosterElement element in roster.GetTroopRoster())
		{
			if (element.Character?.IsHero == true)
			{
				return element.Character.HeroObject;
			}
		}
		return null;
	}

	private static int MoveSelectedTroopsToCreatedParty(MobileParty createdParty, Hero partyHero, TroopRoster leftMemberRoster, PartyBase rightOwnerParty)
	{
		if (!IsPartyUsable(createdParty) || leftMemberRoster == null)
		{
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in leftMemberRoster.GetTroopRoster())
		{
			if (element.Character == null || element.Character == partyHero?.CharacterObject || element.Number <= 0)
			{
				continue;
			}
			createdParty.MemberRoster.Add(element);
			rightOwnerParty?.MemberRoster?.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
			moved += element.Number;
		}
		return moved;
	}

	private static int MoveSelectedPrisonersToCreatedParty(MobileParty createdParty, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty)
	{
		if (!IsPartyUsable(createdParty) || leftPrisonRoster == null)
		{
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in leftPrisonRoster.GetTroopRoster())
		{
			if (element.Character == null || element.Number <= 0)
			{
				continue;
			}
			createdParty.PrisonRoster.Add(element);
			rightOwnerParty?.PrisonRoster?.AddToCounts(element.Character, -element.Number, false, -element.WoundedNumber, -element.Xp, true, -1);
			moved += element.Number;
		}
		return moved;
	}

	private static bool CanMergeToPlayer(Hero hero, MobileParty party, out string reason)
	{
		reason = "";
		if (hero == null || hero == Hero.MainHero || hero.Clan != Clan.PlayerClan)
		{
			reason = "not_player_companion";
			return false;
		}
		if (!IsPartyUsable(party) || party == MobileParty.MainParty || party.LeaderHero != hero)
		{
			reason = "not_independent_companion_party";
			return false;
		}
		if (!IsPartyUsable(MobileParty.MainParty))
		{
			reason = "main_party_invalid";
			return false;
		}
		return true;
	}

	private static int MoveAllMembersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.MemberRoster == null || targetParty?.MemberRoster == null)
		{
			return 0;
		}
		List<Hero> heroes = new List<Hero>();
		for (int i = sourceParty.MemberRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.MemberRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				if (character.HeroObject != null && character.HeroObject != Hero.MainHero)
				{
					heroes.Add(character.HeroObject);
				}
				continue;
			}
			int wounded = Math.Max(0, element.WoundedNumber);
			int xp = Math.Max(0, element.Xp);
			sourceParty.MemberRoster.AddToCounts(character, -count, insertAtFront: false, -wounded, 0, false, -1);
			if (xp > 0)
			{
				sourceParty.MemberRoster.AddXpToTroop(character, -xp);
			}
			targetParty.MemberRoster.AddToCounts(character, count, insertAtFront: false, wounded, 0, false, -1);
			if (xp > 0)
			{
				targetParty.MemberRoster.AddXpToTroop(character, xp);
			}
			moved += count;
		}
		foreach (Hero hero in heroes.Distinct())
		{
			try
			{
				AddHeroToPartyAction.Apply(hero, targetParty, showNotification: false);
				moved += 1;
			}
			catch (Exception ex)
			{
				Log("move member hero failed hero=" + (hero?.StringId ?? "") + " error=" + ex.Message);
			}
		}
		return moved;
	}

	private static int MoveAllPrisonersToMainParty(MobileParty sourceParty)
	{
		int moved = 0;
		MobileParty targetParty = MobileParty.MainParty;
		if (sourceParty?.Party?.PrisonRoster == null || targetParty?.Party == null)
		{
			return 0;
		}
		for (int i = sourceParty.Party.PrisonRoster.Count - 1; i >= 0; i--)
		{
			TroopRosterElement element = sourceParty.Party.PrisonRoster.GetElementCopyAtIndex(i);
			CharacterObject character = element.Character;
			int count = Math.Max(0, element.Number);
			if (character == null || count <= 0)
			{
				continue;
			}
			if (character.IsHero)
			{
				try
				{
					TransferPrisonerAction.Apply(character, sourceParty.Party, targetParty.Party);
					moved += 1;
				}
				catch (Exception ex)
				{
					Log("move prisoner hero failed hero=" + (character.HeroObject?.StringId ?? "") + " error=" + ex.Message);
				}
				continue;
			}
			int xp = Math.Max(0, element.Xp);
			sourceParty.Party.PrisonRoster.AddToCounts(character, -count, insertAtFront: false, 0, 0, false, -1);
			if (xp > 0)
			{
				sourceParty.Party.PrisonRoster.AddXpToTroop(character, -xp);
			}
			targetParty.Party.AddPrisoner(character, count);
			if (xp > 0)
			{
				targetParty.Party.PrisonRoster?.AddXpToTroop(character, xp);
			}
			moved += count;
		}
		return moved;
	}

	private static void TryDestroyEmptyParty(MobileParty party)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty || !party.IsActive)
			{
				return;
			}
			int members = party.MemberRoster?.TotalManCount ?? 0;
			int prisoners = party.Party?.PrisonRoster?.TotalManCount ?? 0;
			if (members <= 0 && prisoners <= 0)
			{
				DestroyPartyAction.Apply((PartyBase)null, party);
			}
		}
		catch (Exception ex)
		{
			Log("destroy empty party failed: " + ex.Message);
		}
	}

	private static double NowDay()
	{
		try
		{
			return CampaignTime.Now.ToDays;
		}
		catch
		{
			return 0.0;
		}
	}

	private static string GetHeroName(Hero hero)
	{
		return (hero?.Name?.ToString() ?? hero?.StringId ?? "NPC").Trim();
	}

	private static string GetSettlementName(Settlement settlement)
	{
		return (settlement?.Name?.ToString() ?? settlement?.StringId ?? "目标定居点").Trim();
	}

	private static string GetPartyName(MobileParty party)
	{
		return (party?.Name?.ToString() ?? party?.StringId ?? "目标部队").Trim();
	}

	private static string SafeFactionId(IFaction faction)
	{
		try
		{
			return (faction?.StringId ?? faction?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

	private static string GetFactionDisplayName(IFaction faction)
	{
		try
		{
			return (faction?.Name?.ToString() ?? faction?.StringId ?? "目标王国").Trim();
		}
		catch
		{
			return "目标王国";
		}
	}

	private static void LogFact(Hero hero, string factText)
	{
		if (hero == null || string.IsNullOrWhiteSpace(factText))
		{
			return;
		}
		string cleanFact = factText.Trim();
		string fact = "[AFEF NPC行为补充] " + cleanFact;
		MyBehavior.AppendExternalDialogueHistory(hero, null, null, fact);
		DisplayCommandMessage(cleanFact, InferCommandMessageTone(cleanFact));
	}

	private static void NotifyCommandStatus(PartyCommandQueueState state, string statusKey, string message, CommandMessageTone tone = CommandMessageTone.Progress)
	{
		if (state == null || string.IsNullOrWhiteSpace(statusKey) || string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		if (string.Equals(state.LastStatusMessageKey, statusKey, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		state.LastStatusMessageKey = statusKey;
		DisplayCommandMessage(message, tone);
	}

	private static string BuildAttackTrackingStatusMessage(Hero actorHero, string targetName, string reason)
	{
		string actorName = GetHeroName(actorHero);
		string safeTargetName = string.IsNullOrWhiteSpace(targetName) ? "目标" : targetName.Trim();
		string safeReason = (reason ?? "").Trim();
		if (safeReason.IndexOf("ai_commit_waiting", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "已经接近" + safeTargetName + "，但战力评估认为当前风险过高，正在等待更好的进攻窗口。";
		}
		if (safeReason.IndexOf("force_commit_blocked", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "已经接近" + safeTargetName + "，但攻击硬条件暂不满足，正在继续跟踪目标。";
		}
		if (safeReason.IndexOf("conditions_lost", StringComparison.OrdinalIgnoreCase) >= 0 || safeReason.IndexOf("target_left", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return actorName + "与" + safeTargetName + "的接战条件暂时丢失，正在重新追击。";
		}
		return actorName + "正在追踪" + safeTargetName + "，准备执行攻击命令。";
	}

	private static void DisplayCommandMessage(string message, bool isFailure)
	{
		DisplayCommandMessage(message, isFailure ? CommandMessageTone.Failure : CommandMessageTone.Success);
	}

	private static void DisplayCommandMessage(string message, CommandMessageTone tone)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message.Trim(), GetCommandMessageColor(tone)));
		}
		catch
		{
		}
	}

	private static Color GetCommandMessageColor(CommandMessageTone tone)
	{
		if (tone == CommandMessageTone.Failure)
		{
			return new Color(1f, 0.45f, 0.25f);
		}
		if (tone == CommandMessageTone.Progress)
		{
			return new Color(1f, 0.9f, 0.25f);
		}
		if (tone == CommandMessageTone.Neutral)
		{
			return new Color(0.7f, 0.85f, 1f);
		}
		return new Color(0.4f, 1f, 0.4f);
	}

	private static CommandMessageTone InferCommandMessageTone(string message)
	{
		string text = (message ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return CommandMessageTone.Neutral;
		}
		if (ContainsAny(text, "失败", "无法", "未能", "失效", "取消", "时限已到", "被击退", "没有", "不能", "不可"))
		{
			return CommandMessageTone.Failure;
		}
		if (ContainsAny(text, "正在", "开始", "发起", "等待", "继续执行", "结果尚未分出", "已记录", "已打开", "准备"))
		{
			return CommandMessageTone.Progress;
		}
		return CommandMessageTone.Success;
	}

	private static bool ContainsAny(string text, params string[] needles)
	{
		if (string.IsNullOrWhiteSpace(text) || needles == null)
		{
			return false;
		}
		foreach (string needle in needles)
		{
			if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void Log(string message)
	{
		try
		{
			Logger.Log(LogSource, message ?? "");
		}
		catch
		{
		}
	}
}
