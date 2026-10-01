using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Helpers;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal enum NoblePrisonerEscortMode
{
	None,
	TownAftermath,
	SettlementEntry,
	LordsHall,
	WorldMapEncounterMeeting
}

public sealed class NoblePrisonerEscortBehavior : CampaignBehaviorBase
{
	internal const int DefaultSceneLimit = 5;
	internal const int EncounterMeetingLimit = 1;
	internal const int LordPrisonerFormationIndex = 7;
	internal const string ExecuteActionTag = "[ACTION:NOBLE_PRISONER_EXECUTE]";

	private const uint InfoColor = 0xFFDFC16Bu;
	private const uint WarningColor = 0xFFFF6B6Bu;
	private const uint SuccessColor = 0xFF8DDC7Eu;
	private static readonly Regex ExecuteActionTagRegex = new Regex(
		@"\[ACTION:NOBLE_PRISONER_EXECUTE\]",
		RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static TroopRoster _townAftermathProfile;
	private static TroopRoster _settlementEntryProfile;
	private static TroopRoster _lordsHallProfile;
	private static TroopRoster _encounterMeetingProfile;
	private static PendingSelection _pendingSelection;
	private static Mission _activeMission;
	private static bool _lordsHallCommandUiPrimed;
	private static readonly Dictionary<int, EscortedAgentRecord> EscortedAgents = new Dictionary<int, EscortedAgentRecord>();

	public override void RegisterEvents()
	{
		EnsureProfiles();
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_afNoblePrisonerTownAftermath_v1", ref _townAftermathProfile);
		dataStore.SyncData("_afNoblePrisonerSettlementEntry_v1", ref _settlementEntryProfile);
		dataStore.SyncData("_afNoblePrisonerLordsHall_v1", ref _lordsHallProfile);
		dataStore.SyncData("_afNoblePrisonerEncounterMeeting_v1", ref _encounterMeetingProfile);
		EnsureProfiles();
	}

	internal static void OpenConfigFromTerminal()
	{
		EnsureProfiles();
		if (Campaign.Current == null || Mission.Current != null || PartyBase.MainParty?.PrisonRoster == null)
		{
			ShowMessage("【贵族俘虏随行】只能在战役地图上配置。", WarningColor);
			return;
		}

		List<InquiryElement> choices = new List<InquiryElement>
		{
			BuildModeChoice(NoblePrisonerEscortMode.TownAftermath),
			BuildModeChoice(NoblePrisonerEscortMode.SettlementEntry),
			BuildModeChoice(NoblePrisonerEscortMode.LordsHall),
			BuildModeChoice(NoblePrisonerEscortMode.WorldMapEncounterMeeting)
		};
		MultiSelectionInquiryData data = new MultiSelectionInquiryData(
			"【贵族俘虏随行】场景配置",
			"只可选择玩家主队中的英雄单位俘虏：",
			choices,
			isExitShown: true,
			1,
			1,
			"配置",
			"关闭",
			delegate(List<InquiryElement> selected)
			{
				if (selected?.Count > 0 && selected[0].Identifier is NoblePrisonerEscortMode mode)
				{
					OpenProfileSelection(mode);
				}
			},
			null,
			"",
			isSeachAvailable: false);
		MBInformationManager.ShowMultiSelectionInquiry(data, pauseGameActiveState: true);
	}

	internal static bool IsEscortedAgent(Agent agent)
	{
		return agent != null && IsEscortedAgent(agent.Index);
	}

	internal static bool IsEscortedAgent(int agentIndex)
	{
		return agentIndex >= 0
			&& _activeMission != null
			&& Mission.Current == _activeMission
			&& EscortedAgents.ContainsKey(agentIndex);
	}

	internal static IEnumerable<Hero> GetEscortedHeroesForExecution()
	{
		return EscortedAgents.Values
			.Where(record => record?.Hero != null && record.Agent != null && record.Agent.IsActive())
			.Select(record => record.Hero)
			.ToList();
	}

	internal static bool TryGetEscortedAgentForHero(Hero hero, out Agent agent)
	{
		agent = EscortedAgents.Values
			.FirstOrDefault(record => record?.Hero == hero && record.Agent != null && record.Agent.IsActive())
			?.Agent;
		return agent != null;
	}

	internal static bool IsMeetingCombatDespawningAgent(Agent agent)
	{
		return agent != null
			&& EscortedAgents.TryGetValue(agent.Index, out EscortedAgentRecord record)
			&& record.MeetingCombatDespawnStarted;
	}

	private static bool HasLiveLordsHallProfile()
	{
		try
		{
			return ResolveLiveProfile(NoblePrisonerEscortMode.LordsHall, out _, out _).TotalManCount > 0;
		}
		catch
		{
			return false;
		}
	}

	internal static bool ShouldInjectOrderViewsForExternal(Mission mission)
	{
		try
		{
			if (mission == null || mission.IsMissionEnding)
			{
				return false;
			}
			if (ResolveModeForMission(mission) == NoblePrisonerEscortMode.LordsHall)
			{
				return HasLiveLordsHallProfile();
			}
			return ReferenceEquals(_activeMission, mission)
				&& EscortedAgents.Values.Any(record =>
					record?.Mode == NoblePrisonerEscortMode.LordsHall
					&& record.Agent != null
					&& record.Agent.IsActive());
		}
		catch
		{
			return false;
		}
	}

	internal static Team ResolvePlayerCommandTeamForExternal(Mission mission, string source = null)
	{
		try
		{
			mission ??= Mission.Current;
			Agent main = Agent.Main ?? mission?.MainAgent;
			if (mission == null || mission.IsMissionEnding || main == null || !main.IsActive())
			{
				return mission?.PlayerTeam ?? main?.Team;
			}
			Team playerTeam = mission.PlayerTeam ?? main.Team;
			if (playerTeam == null || !playerTeam.IsPlayerGeneral)
			{
				uint color = Hero.MainHero?.MapFaction?.Color ?? 0xFF2020FFu;
				uint color2 = Hero.MainHero?.MapFaction?.Color2 ?? 0xFF101080u;
				try
				{
					playerTeam = mission.Teams.Add(
						BattleSideEnum.Attacker,
						color,
						color2,
						Hero.MainHero?.Clan?.Banner,
						isPlayerGeneral: true,
						isPlayerSergeant: false);
				}
				catch (Exception ex)
				{
					NoblePrisonerEscortLog.Log("Create lords-hall command team failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
					playerTeam = mission.PlayerTeam ?? main.Team;
				}
			}
			if (playerTeam != null)
			{
				mission.PlayerTeam = playerTeam;
				if (main.Team != playerTeam)
				{
					main.SetTeam(playerTeam, sync: true);
				}
			}
			return playerTeam;
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Resolve lords-hall command team failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
			return null;
		}
	}

	internal static bool EnsureCommandUiReadyForExternal(Mission mission, string source)
	{
		try
		{
			mission ??= Mission.Current;
			if (!ShouldInjectOrderViewsForExternal(mission)
				|| mission.Mode == MissionMode.Conversation
				|| mission.Mode == MissionMode.Barter)
			{
				return false;
			}
			Agent main = Agent.Main ?? mission.MainAgent;
			Team playerTeam = ResolvePlayerCommandTeamForExternal(mission, source);
			if (main == null || playerTeam == null)
			{
				return false;
			}
			Formation formation = playerTeam.GetFormation((FormationClass)LordPrisonerFormationIndex);
			if (formation == null)
			{
				return false;
			}
			int commandable = 0;
			foreach (EscortedAgentRecord record in EscortedAgents.Values.ToList())
			{
				Agent agent = record?.Agent;
				if (record?.Mode != NoblePrisonerEscortMode.LordsHall
					|| agent == null
					|| !agent.IsHuman
					|| !agent.IsActive())
				{
					continue;
				}
				if (agent.Team != playerTeam)
				{
					agent.SetTeam(playerTeam, sync: true);
				}
				if (agent.Formation != formation)
				{
					agent.Formation = formation;
				}
				agent.TryAttachToFormation();
				agent.SetShouldCatchUpWithFormation(true);
				agent.UpdateFormationOrders();
				commandable++;
			}
			if (commandable <= 0)
			{
				return false;
			}
			MarkFormationPlayerCommandable(formation, main);
			OrderController controller = playerTeam.PlayerOrderController ?? playerTeam.MasterOrderController;
			if (controller != null && !_lordsHallCommandUiPrimed)
			{
				try
				{
					if (controller.SelectedFormations == null || controller.SelectedFormations.Count == 0)
					{
						controller.SelectFormation(formation);
					}
				}
				catch
				{
				}
				_lordsHallCommandUiPrimed = true;
				NoblePrisonerEscortLog.Log("Lords-hall command UI ready. source=" + (source ?? "N/A")
					+ ", commandable=" + commandable + ", formation=" + (LordPrisonerFormationIndex + 1)
					+ ", controller=true");
			}
			return controller != null;
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Ensure lords-hall command UI failed. source=" + (source ?? "N/A") + ", error=" + ex.Message);
			return false;
		}
	}

	internal static OrderController TryResolveOrderControllerForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			if (!ShouldInjectOrderViewsForExternal(mission)
				|| mission.Mode == MissionMode.Conversation
				|| mission.Mode == MissionMode.Barter)
			{
				return null;
			}
			Team playerTeam = ResolvePlayerCommandTeamForExternal(mission, "resolve_order_controller");
			return playerTeam?.PlayerOrderController ?? playerTeam?.MasterOrderController;
		}
		catch
		{
			return null;
		}
	}

	internal static bool PlayerHasCommandableAgentsForExternal(Mission mission)
	{
		try
		{
			mission ??= Mission.Current;
			if (!ShouldInjectOrderViewsForExternal(mission))
			{
				return false;
			}
			Team playerTeam = ResolvePlayerCommandTeamForExternal(mission, "has_commandable_agents");
			return playerTeam != null && EscortedAgents.Values.Any(record =>
				record?.Mode == NoblePrisonerEscortMode.LordsHall
				&& record.Agent != null
				&& record.Agent.IsActive()
				&& record.Agent.Team == playerTeam
				&& record.Agent.Formation != null);
		}
		catch
		{
			return false;
		}
	}

	internal static bool NativeOrderControllerHasSelectedFormationsForExternal(Mission mission)
	{
		try
		{
			OrderController controller = TryResolveOrderControllerForExternal(mission);
			return controller?.SelectedFormations != null
				&& controller.SelectedFormations.Count > 0
				&& PlayerHasCommandableAgentsForExternal(mission);
		}
		catch
		{
			return false;
		}
	}

	internal static bool TryGetEscortedHero(int agentIndex, out Hero hero, out Agent agent)
	{
		hero = null;
		agent = null;
		if (!IsEscortedAgent(agentIndex) || !EscortedAgents.TryGetValue(agentIndex, out EscortedAgentRecord record))
		{
			return false;
		}
		hero = record.Hero;
		agent = record.Agent;
		return hero != null && agent != null && agent.IsActive();
	}

	internal static string BuildSceneExecutionPromptInstruction(int agentIndex)
	{
		if (!TryGetEscortedHero(agentIndex, out Hero hero, out Agent agent)
			|| hero == null
			|| agent == null)
		{
			return string.Empty;
		}
		return "【贵族俘虏场景处决】当前回应者“" + (hero.Name?.ToString() ?? "俘虏")
			+ "”是玩家带入场景且仍由玩家主队拘押的英雄俘虏。无论当前使用公开范围对话还是 AF 单独对话，只有当前回复直接回应玩家本轮明确要求杀死当前俘虏的命令时，才由 AI 按完整语义在回复末尾输出 "
			+ ExecuteActionTag + "。普通谈话、一般威胁、玩笑、转述、NPC 自主提议、历史内容或含糊表达绝不能输出该标签；该标签只请求原版处刑确认，不代表玩家已经确认。";
	}

	internal static string BuildSceneExecutionPostprocessRule(int agentIndex)
	{
		if (!TryGetEscortedHero(agentIndex, out Hero hero, out Agent agent)
			|| hero == null
			|| agent == null)
		{
			return string.Empty;
		}
		return "仅针对当前直接回应的随行贵族俘虏“"
			+ (hero.Name?.ToString() ?? "俘虏")
			+ "”。由 AI 按玩家本轮发言与该俘虏本轮回复的完整语义判断：只有玩家明确要求杀死当前俘虏，且回复直接承接这项命令时输出。提问、假设、一般威胁、反悔、否定、谈论他人、NPC 自主提议、接力闲聊或历史内容均不得输出。标签只打开原版处刑确认，不能代替玩家最终确认。";
	}

	internal static bool TryProcessSceneExecutionTag(int agentIndex, bool replyIsDirectPlayerResponse, ref string content)
	{
		if (string.IsNullOrWhiteSpace(content) || !ExecuteActionTagRegex.IsMatch(content))
		{
			return false;
		}
		content = ExecuteActionTagRegex.Replace(content, "").Trim();
		if (!replyIsDirectPlayerResponse)
		{
			NoblePrisonerEscortLog.Log("Rejected execution tag outside direct player response. agent=" + agentIndex);
			return false;
		}
		if (!TryGetEscortedHero(agentIndex, out Hero hero, out Agent agent))
		{
			NoblePrisonerEscortLog.Log("Rejected execution tag for non-escorted target. agent=" + agentIndex);
			return false;
		}
		if (!NoblePrisonerExecutionRuntime.TryQueue(hero, agent, out string reason))
		{
			ShowMessage("【贵族俘虏随行】无法处决当前目标。", WarningColor);
			NoblePrisonerEscortLog.Log("Queue execution failed. hero=" + SafeHeroId(hero) + ", reason=" + reason);
			return false;
		}
		NoblePrisonerEscortLog.Log("Queued scene execution confirmation. hero=" + SafeHeroId(hero) + ", agent=" + agentIndex);
		return true;
	}

	internal static void RegisterEscortedAgent(Mission mission, NoblePrisonerEscortMode mode, Hero hero, Agent agent)
	{
		if (mission == null || hero == null || agent == null)
		{
			return;
		}
		if (_activeMission != null && !ReferenceEquals(_activeMission, mission))
		{
			ClearRuntime("mission_replaced");
		}
		_activeMission = mission;
		EscortedAgents[agent.Index] = new EscortedAgentRecord
		{
			Hero = hero,
			Agent = agent,
			Mode = mode
		};
		NoblePrisonerEscortLog.Log("Registered escorted noble prisoner. mode=" + mode + ", hero=" + SafeHeroId(hero) + ", agent=" + agent.Index);
	}

	internal static void MarkMeetingCombatDespawnStarted(Agent agent)
	{
		if (agent != null && EscortedAgents.TryGetValue(agent.Index, out EscortedAgentRecord record))
		{
			record.MeetingCombatDespawnStarted = true;
		}
	}

	internal static void UnregisterEscortedAgent(Agent agent, string source)
	{
		if (agent != null && EscortedAgents.Remove(agent.Index))
		{
			NoblePrisonerEscortLog.Log("Unregistered escorted noble prisoner. agent=" + agent.Index + ", source=" + (source ?? "N/A"));
		}
	}

	internal static void RemoveHeroFromAllProfiles(Hero hero, string source)
	{
		CharacterObject character = hero?.CharacterObject;
		if (character == null)
		{
			return;
		}
		int removed = 0;
		foreach (NoblePrisonerEscortMode mode in SupportedModes)
		{
			TroopRoster roster = GetProfile(mode);
			int index = roster?.FindIndexOfTroop(character) ?? -1;
			if (index >= 0)
			{
				roster.AddToCounts(character, -roster.GetElementCopyAtIndex(index).Number, false, 0, 0, true, -1);
				removed++;
			}
		}
		NoblePrisonerEscortLog.Log("Removed hero from escort profiles. hero=" + SafeHeroId(hero) + ", profiles=" + removed + ", source=" + (source ?? "N/A"));
	}

	internal static NoblePrisonerEscortMode ResolveModeForMission(Mission mission)
	{
		if (mission == null || mission.IsMissionEnding)
		{
			return NoblePrisonerEscortMode.None;
		}
		bool actualWorldMapMeeting = MeetingBattleRuntime.IsMeetingActive
			&& !MeetingBattleRuntime.IsCombatEscalated
			&& LordEncounterBehavior.IsEncounterMeetingMissionActive
			&& mission.GetMissionBehavior<MeetingBattleLockMissionBehavior>() != null;
		if (actualWorldMapMeeting)
		{
			return IsNeutralOrHostileEncounterParty()
				? NoblePrisonerEscortMode.WorldMapEncounterMeeting
				: NoblePrisonerEscortMode.None;
		}
		if (MeetingBattleRuntime.IsMeetingActive || LordEncounterBehavior.IsEncounterMeetingMissionActive)
		{
			return NoblePrisonerEscortMode.None;
		}

		Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.LocationEncounter?.Settlement;
		if (settlement == null || (!settlement.IsTown && !settlement.IsCastle))
		{
			return NoblePrisonerEscortMode.None;
		}
		// Mission ticks run after GCCZ has promoted its pending entry into the active
		// mission state.  Do not use the broader "open or pending" guard here: it
		// intentionally survives into aftermath/menu transitions and could classify a
		// later ordinary settlement visit as a town-aftermath escort scene.
		if (SiegeAiInterventionBehavior.IsOccupationSceneActiveForExternal()
			|| CastleAftermathRuntimeBridge.IsCastleAftermathMission(mission))
		{
			return settlement.IsTown
				? NoblePrisonerEscortMode.TownAftermath
				: NoblePrisonerEscortMode.None;
		}
		string locationId = CampaignMission.Current?.Location?.StringId ?? string.Empty;
		if (string.Equals(locationId, "lordshall", StringComparison.OrdinalIgnoreCase))
		{
			return NoblePrisonerEscortMode.LordsHall;
		}
		if (string.Equals(locationId, "center", StringComparison.OrdinalIgnoreCase))
		{
			return NoblePrisonerEscortMode.SettlementEntry;
		}
		return NoblePrisonerEscortMode.None;
	}

	private static bool IsNeutralOrHostileEncounterParty()
	{
		try
		{
			PartyBase encountered = PlayerEncounter.EncounteredParty;
			Hero meetingTarget = MeetingBattleRuntime.TargetHero;
			IFaction encounteredFaction = encountered?.MapFaction ?? meetingTarget?.MapFaction ?? meetingTarget?.Clan;
			IFaction playerFaction = Clan.PlayerClan?.Kingdom ?? (IFaction)Clan.PlayerClan;
			if (encounteredFaction == null || playerFaction == null)
			{
				return false;
			}
			return !ReferenceEquals(encounteredFaction, playerFaction)
				&& !string.Equals(encounteredFaction.StringId, playerFaction.StringId, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	internal static TroopRoster ResolveLiveProfile(NoblePrisonerEscortMode mode, out int configured, out int unavailable)
	{
		return ResolveProfile(GetProfile(mode), PartyBase.MainParty?.PrisonRoster, GetLimit(mode), out configured, out unavailable);
	}

	private void OnMissionStarted(IMission mission)
	{
		if (mission is not Mission concreteMission || !HasAnyConfiguredProfile())
		{
			return;
		}
		if (concreteMission.GetMissionBehavior<NoblePrisonerEscortMissionBehavior>() == null)
		{
			concreteMission.AddMissionBehavior(new NoblePrisonerEscortMissionBehavior());
			NoblePrisonerEscortLog.Log("Attached mission behavior. scene=" + (concreteMission.SceneName ?? "N/A"));
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (mission is Mission concreteMission)
		{
			NoblePrisonerExecutionRuntime.CancelForMission(concreteMission, "mission_ended");
		}
		ClearRuntime("mission_ended");
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		_townAftermathProfile = TroopRoster.CreateDummyTroopRoster();
		_settlementEntryProfile = TroopRoster.CreateDummyTroopRoster();
		_lordsHallProfile = TroopRoster.CreateDummyTroopRoster();
		_encounterMeetingProfile = TroopRoster.CreateDummyTroopRoster();
		_pendingSelection = null;
		ClearRuntime("new_game");
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		EnsureProfiles();
		ClearRuntime("game_loaded");
	}

	private static void OpenProfileSelection(NoblePrisonerEscortMode mode)
	{
		try
		{
			TroopRoster livePrisoners = PartyBase.MainParty?.PrisonRoster;
			if (livePrisoners == null)
			{
				ShowMessage("【贵族俘虏随行】玩家俘虏栏不可用。", WarningColor);
				return;
			}
			int limit = GetLimit(mode);
			TroopRoster current = ResolveProfile(GetProfile(mode), livePrisoners, limit, out _, out _);
			TroopRoster selectable = BuildSelectableRoster(livePrisoners);
			SubtractRoster(selectable, current);
			_pendingSelection = new PendingSelection { Mode = mode, Limit = limit };
			PartyScreenLogic logic = new PartyScreenLogic();
			PartyScreenLogicInitializationData data = new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = null,
				RightOwnerParty = PartyBase.MainParty,
				LeftMemberRoster = TroopRoster.CreateDummyTroopRoster(),
				LeftPrisonerRoster = selectable,
				RightMemberRoster = TroopRoster.CreateDummyTroopRoster(),
				RightPrisonerRoster = current,
				LeftLeaderHero = null,
				RightLeaderHero = Hero.MainHero,
				LeftPartyMembersSizeLimit = 0,
				LeftPartyPrisonersSizeLimit = Math.Max(selectable.TotalManCount + current.TotalManCount, limit),
				RightPartyMembersSizeLimit = 0,
				RightPartyPrisonersSizeLimit = limit,
				LeftPartyName = new TextObject("玩家主队中的英雄俘虏"),
				RightPartyName = new TextObject(GetModeTitle(mode) + "（上限 " + limit + "）"),
				TroopTransferableDelegate = PrisonerTransferable,
				CanTalkToTroopDelegate = null,
				PartyPresentationDoneButtonDelegate = SelectionDone,
				PartyPresentationDoneButtonConditionDelegate = SelectionDoneCondition,
				PartyPresentationCancelButtonActivateDelegate = null,
				PartyPresentationCancelButtonDelegate = null,
				PartyScreenClosedDelegate = OnSelectionClosed,
				IsDismissMode = true,
				IsTroopUpgradesDisabled = true,
				Header = new TextObject("配置 " + GetModeTitle(mode)),
				TransferHealthiesGetWoundedsFirst = true,
				ShowProgressBar = false,
				MemberTransferState = PartyScreenLogic.TransferState.NotTransferable,
				PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
				AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
				PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
			};
			logic.Initialize(data);
			PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
			state.PartyScreenLogic = logic;
			state.IsDonating = false;
			state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
			Game.Current.GameStateManager.PushState((GameState)(object)state, 0);
			ShowMessage("【贵族俘虏随行】正在配置" + GetModeTitle(mode) + "。", InfoColor);
			NoblePrisonerEscortLog.Log("Opened profile selection. mode=" + mode + ", current=" + current.TotalManCount + ", selectable=" + selectable.TotalManCount);
		}
		catch (Exception ex)
		{
			_pendingSelection = null;
			NoblePrisonerEscortLog.Log("Open profile selection failed. mode=" + mode + ", error=" + ex);
			ShowMessage("【贵族俘虏随行】打开配置界面失败。", WarningColor);
		}
	}

	private static bool PrisonerTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return IsEligibleHeroPrisoner(character);
	}

	private static bool SelectionDone(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty = null, PartyBase rightParty = null)
	{
		return true;
	}

	private static Tuple<bool, TextObject> SelectionDoneCondition(TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, int leftLimitNum, int rightLimitNum)
	{
		int count = rightPrisonerRoster?.TotalManCount ?? 0;
		int limit = _pendingSelection?.Limit ?? DefaultSceneLimit;
		if (count <= limit)
		{
			return new Tuple<bool, TextObject>(true, TextObject.GetEmpty());
		}
		return new Tuple<bool, TextObject>(false, new TextObject("贵族俘虏随行不能超过 " + limit + " 人。"));
	}

	private static void OnSelectionClosed(PartyBase leftOwnerParty, TroopRoster leftMemberRoster, TroopRoster leftPrisonerRoster, PartyBase rightOwnerParty, TroopRoster rightMemberRoster, TroopRoster rightPrisonerRoster, bool fromCancel)
	{
		PendingSelection pending = _pendingSelection;
		_pendingSelection = null;
		if (pending == null || fromCancel)
		{
			return;
		}
		TroopRoster saved = ResolveProfile(rightPrisonerRoster, PartyBase.MainParty?.PrisonRoster, pending.Limit, out _, out _);
		SetProfile(pending.Mode, saved);
		ShowMessage("【贵族俘虏随行】" + GetModeTitle(pending.Mode) + "已保存 " + saved.TotalManCount + "/" + pending.Limit + "。", SuccessColor);
		NoblePrisonerEscortLog.Log("Saved profile. mode=" + pending.Mode + ", count=" + saved.TotalManCount + ", limit=" + pending.Limit);
	}

	private static InquiryElement BuildModeChoice(NoblePrisonerEscortMode mode)
	{
		TroopRoster live = ResolveLiveProfile(mode, out int configured, out int unavailable);
		string hint = "当前可用 " + live.TotalManCount + "/" + GetLimit(mode) + " 名";
		if (unavailable > 0)
		{
			hint += "，另有 " + unavailable + " 名已不在玩家主队俘虏栏";
		}
		return new InquiryElement(mode, GetModeTitle(mode), null, isEnabled: true, hint + "。已配置记录：" + configured + "。只允许英雄俘虏。");
	}

	private static TroopRoster BuildSelectableRoster(TroopRoster source)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		foreach (TroopRosterElement element in Snapshot(source))
		{
			if (element.Number > 0 && IsEligibleHeroPrisoner(element.Character))
			{
				result.AddToCounts(element.Character, 1, false, 0, 0, true, -1);
			}
		}
		return result;
	}

	private static TroopRoster ResolveProfile(TroopRoster configuredRoster, TroopRoster liveRoster, int limit, out int configured, out int unavailable)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		configured = 0;
		unavailable = 0;
		foreach (TroopRosterElement element in Snapshot(configuredRoster))
		{
			if (element.Character == null || element.Number <= 0)
			{
				continue;
			}
			configured++;
			if (result.TotalManCount >= limit || !IsEligibleHeroPrisoner(element.Character) || !RosterContains(liveRoster, element.Character))
			{
				unavailable++;
				continue;
			}
			result.AddToCounts(element.Character, 1, false, 0, 0, true, -1);
		}
		return result;
	}

	private static bool IsEligibleHeroPrisoner(CharacterObject character)
	{
		Hero hero = character?.HeroObject;
		return character?.IsHero == true
			&& hero != null
			&& hero != Hero.MainHero
			&& hero.IsAlive
			&& hero.IsPrisoner
			&& hero.PartyBelongedToAsPrisoner == PartyBase.MainParty;
	}

	private static bool RosterContains(TroopRoster roster, CharacterObject character)
	{
		return roster != null && character != null && roster.FindIndexOfTroop(character) >= 0;
	}

	private static void SubtractRoster(TroopRoster target, TroopRoster subtract)
	{
		foreach (TroopRosterElement element in Snapshot(subtract))
		{
			if (element.Character != null && element.Number > 0)
			{
				target.AddToCounts(element.Character, -element.Number, false, 0, 0, true, -1);
			}
		}
	}

	private static List<TroopRosterElement> Snapshot(TroopRoster roster)
	{
		List<TroopRosterElement> result = new List<TroopRosterElement>();
		if (roster == null)
		{
			return result;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			result.Add(roster.GetElementCopyAtIndex(i));
		}
		return result;
	}

	private static bool HasAnyConfiguredProfile()
	{
		EnsureProfiles();
		return SupportedModes.Any(mode => (GetProfile(mode)?.TotalManCount ?? 0) > 0);
	}

	private static void EnsureProfiles()
	{
		_townAftermathProfile ??= TroopRoster.CreateDummyTroopRoster();
		_settlementEntryProfile ??= TroopRoster.CreateDummyTroopRoster();
		_lordsHallProfile ??= TroopRoster.CreateDummyTroopRoster();
		_encounterMeetingProfile ??= TroopRoster.CreateDummyTroopRoster();
	}

	private static TroopRoster GetProfile(NoblePrisonerEscortMode mode)
	{
		EnsureProfiles();
		return mode switch
		{
			NoblePrisonerEscortMode.TownAftermath => _townAftermathProfile,
			NoblePrisonerEscortMode.SettlementEntry => _settlementEntryProfile,
			NoblePrisonerEscortMode.LordsHall => _lordsHallProfile,
			NoblePrisonerEscortMode.WorldMapEncounterMeeting => _encounterMeetingProfile,
			_ => TroopRoster.CreateDummyTroopRoster()
		};
	}

	private static void SetProfile(NoblePrisonerEscortMode mode, TroopRoster roster)
	{
		TroopRoster safe = ResolveProfile(roster, PartyBase.MainParty?.PrisonRoster, GetLimit(mode), out _, out _);
		switch (mode)
		{
			case NoblePrisonerEscortMode.TownAftermath:
				_townAftermathProfile = safe;
				break;
			case NoblePrisonerEscortMode.SettlementEntry:
				_settlementEntryProfile = safe;
				break;
			case NoblePrisonerEscortMode.LordsHall:
				_lordsHallProfile = safe;
				break;
			case NoblePrisonerEscortMode.WorldMapEncounterMeeting:
				_encounterMeetingProfile = safe;
				break;
		}
	}

	private static int GetLimit(NoblePrisonerEscortMode mode)
	{
		return mode == NoblePrisonerEscortMode.WorldMapEncounterMeeting ? EncounterMeetingLimit : DefaultSceneLimit;
	}

	private static string GetModeTitle(NoblePrisonerEscortMode mode)
	{
		return mode switch
		{
			NoblePrisonerEscortMode.TownAftermath => "城镇攻城处置随行（5名）",
			NoblePrisonerEscortMode.SettlementEntry => "正常城镇/城堡场景随行（5名）",
			NoblePrisonerEscortMode.LordsHall => "城镇/城堡领主大厅随行（5名）",
			NoblePrisonerEscortMode.WorldMapEncounterMeeting => "野外会面场景随行（1名）",
			_ => "贵族俘虏随行"
		};
	}

	private static void ClearRuntime(string source)
	{
		EscortedAgents.Clear();
		_activeMission = null;
		_lordsHallCommandUiPrimed = false;
		NoblePrisonerExecutionRuntime.Reset(source);
		NoblePrisonerEscortLog.Log("Cleared runtime. source=" + (source ?? "N/A"));
	}

	private static void MarkFormationPlayerCommandable(Formation formation, Agent playerOwner)
	{
		try
		{
			if (formation == null)
			{
				return;
			}
			try
			{
				formation.SetControlledByAI(false, false);
			}
			catch
			{
				TrySetFormationProperty(formation, nameof(Formation.IsAIControlled), false);
			}
			TrySetFormationProperty(formation, nameof(Formation.HasPlayerControlledTroop), true);
			if (playerOwner != null && playerOwner.IsActive())
			{
				TrySetFormationProperty(formation, nameof(Formation.PlayerOwner), playerOwner);
			}
		}
		catch (Exception ex)
		{
			NoblePrisonerEscortLog.Log("Mark lords-hall formation commandable failed. error=" + ex.Message);
		}
	}

	private static void TrySetFormationProperty(Formation formation, string propertyName, object value)
	{
		try
		{
			PropertyInfo property = formation?.GetType().GetProperty(
				propertyName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			property?.GetSetMethod(true)?.Invoke(formation, new object[] { value });
		}
		catch
		{
		}
	}

	private static void ShowMessage(string text, uint color)
	{
		InformationManager.DisplayMessage(new InformationMessage(text, Color.FromUint(color)));
	}

	private static string SafeHeroId(Hero hero)
	{
		return hero?.StringId ?? "null";
	}

	private static readonly NoblePrisonerEscortMode[] SupportedModes =
	{
		NoblePrisonerEscortMode.TownAftermath,
		NoblePrisonerEscortMode.SettlementEntry,
		NoblePrisonerEscortMode.LordsHall,
		NoblePrisonerEscortMode.WorldMapEncounterMeeting
	};

	private sealed class PendingSelection
	{
		internal NoblePrisonerEscortMode Mode;
		internal int Limit;
	}

	private sealed class EscortedAgentRecord
	{
		internal Hero Hero;
		internal Agent Agent;
		internal NoblePrisonerEscortMode Mode;
		internal bool MeetingCombatDespawnStarted;
	}
}
