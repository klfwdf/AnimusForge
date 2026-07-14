using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Bannerlord adapter for the castle-only member/prisoner selector and prisoner scene agents.
/// Castle policy and limits stay in AnimusForge.SiegeAftermathIntervention.
/// </summary>
internal static class CastleAftermathRuntimeBridge
{
	private static TroopRoster _selectedPrisonerRoster;

	private static readonly HashSet<int> PrisonerAgentIndexes = new HashSet<int>();

	private static readonly HashSet<int> LordPrisonerAgentIndexes = new HashSet<int>();

	internal static int SelectedPrisonerCount => _selectedPrisonerRoster?.TotalManCount ?? 0;

	internal static bool IsCastleAftermathMission(Mission mission)
	{
		try
		{
			return mission?.GetMissionBehavior<CastleAftermathPrisonerCommandMissionBehavior>() != null;
		}
		catch
		{
			return false;
		}
	}

	internal static void Reset(string source)
	{
		_selectedPrisonerRoster = null;
		ClearMissionAgents(source);
		Logger.Log("CastleAftermath", "Reset castle prisoner runtime. Source=" + (source ?? "N/A"));
	}

	internal static void ClearMissionAgents(string source)
	{
		PrisonerAgentIndexes.Clear();
		LordPrisonerAgentIndexes.Clear();
		Logger.Log("CastleAftermath", "Cleared castle prisoner agent registry. Source=" + (source ?? "N/A"));
	}

	internal static bool IsPrisonerAgent(Agent agent)
	{
		return agent != null && PrisonerAgentIndexes.Contains(agent.Index);
	}

	internal static bool IsLordPrisonerAgent(Agent agent)
	{
		return agent != null && LordPrisonerAgentIndexes.Contains(agent.Index);
	}

	internal static void RegisterPrisonerAgent(Agent agent, bool isLord)
	{
		if (agent == null)
		{
			return;
		}

		PrisonerAgentIndexes.Add(agent.Index);
		if (isLord)
		{
			LordPrisonerAgentIndexes.Add(agent.Index);
		}
	}

	internal static TroopRoster GetSelectedPrisonerRosterSnapshot()
	{
		return CloneRoster(_selectedPrisonerRoster, SiegeCastleRosterSelectionProfile.MaxPrisoners);
	}

	internal static void StoreSelectedPrisonerRoster(TroopRoster sourceRoster)
	{
		TroopRoster selected = CloneRoster(sourceRoster, SiegeCastleRosterSelectionProfile.MaxPrisoners);
		_selectedPrisonerRoster = selected.TotalManCount > 0 ? selected : null;
		Logger.Log("CastleAftermath", "Stored castle prisoner selection. Count=" + SelectedPrisonerCount);
	}

	internal static bool TryOpenRosterSelection(
		TroopRoster availableMembers,
		TroopRoster availablePrisoners,
		TroopRoster initialMembers,
		Action<TroopRoster, TroopRoster> onDone,
		Action onCancel)
	{
		try
		{
			if (Game.Current?.GameStateManager == null || MobileParty.MainParty?.Party == null || onDone == null)
			{
				return false;
			}

			TroopRoster selectedMembers = CloneRoster(initialMembers, SiegeCastleRosterSelectionProfile.MaxAlliedTroops);
			TroopRoster selectedPrisoners = TroopRoster.CreateDummyTroopRoster();
			TroopRoster remainingMembers = BuildRemainingRoster(availableMembers, selectedMembers);
			TroopRoster remainingPrisoners = CloneRoster(availablePrisoners, int.MaxValue);

			PartyScreenLogic logic = new PartyScreenLogic();
			PartyScreenLogicInitializationData data = new PartyScreenLogicInitializationData
			{
				LeftOwnerParty = null,
				RightOwnerParty = MobileParty.MainParty.Party,
				LeftMemberRoster = remainingMembers,
				LeftPrisonerRoster = remainingPrisoners,
				RightMemberRoster = selectedMembers,
				RightPrisonerRoster = selectedPrisoners,
				LeftLeaderHero = null,
				RightLeaderHero = PartyBase.MainParty?.LeaderHero,
				LeftPartyMembersSizeLimit = Math.Max(0, remainingMembers.TotalManCount),
				LeftPartyPrisonersSizeLimit = Math.Max(0, remainingPrisoners.TotalManCount),
				RightPartyMembersSizeLimit = SiegeCastleRosterSelectionProfile.MaxAlliedTroops,
				RightPartyPrisonersSizeLimit = SiegeCastleRosterSelectionProfile.MaxPrisoners,
				LeftPartyName = new TextObject(SiegeCastleRosterSelectionProfile.AvailableRosterTitle),
				RightPartyName = new TextObject(SiegeCastleRosterSelectionProfile.SelectedRosterTitle),
				TroopTransferableDelegate = new IsTroopTransferableDelegate(IsCastleSelectionTroopTransferable),
				CanTalkToTroopDelegate = null,
				PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(DoneHandler),
				PartyPresentationDoneButtonConditionDelegate = new PartyPresentationDoneButtonConditionDelegate(DoneCondition),
				PartyPresentationCancelButtonActivateDelegate = null,
				PartyPresentationCancelButtonDelegate = null,
				PartyScreenClosedDelegate = new PartyScreenClosedDelegate(delegate(
					PartyBase leftOwnerParty,
					TroopRoster leftMemberRoster,
					TroopRoster leftPrisonerRoster,
					PartyBase rightOwnerParty,
					TroopRoster rightMemberRoster,
					TroopRoster rightPrisonerRoster,
					bool fromCancel)
				{
					if (fromCancel)
					{
						onCancel?.Invoke();
						return;
					}

					onDone(
						CloneRoster(rightMemberRoster, SiegeCastleRosterSelectionProfile.MaxAlliedTroops),
						CloneRoster(rightPrisonerRoster, SiegeCastleRosterSelectionProfile.MaxPrisoners));
				}),
				IsDismissMode = true,
				IsTroopUpgradesDisabled = true,
				Header = new TextObject(SiegeCastleRosterSelectionProfile.ScreenHeader),
				TransferHealthiesGetWoundedsFirst = true,
				ShowProgressBar = false,
				MemberTransferState = PartyScreenLogic.TransferState.Transferable,
				PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
				AccompanyingTransferState = PartyScreenLogic.TransferState.Transferable,
				PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal
			};

			logic.Initialize(data);
			PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
			state.PartyScreenLogic = logic;
			state.IsDonating = false;
			state.PartyScreenMode = PartyScreenHelper.PartyScreenMode.Normal;
			Game.Current.GameStateManager.PushState((GameState)(object)state, 0);
			Logger.Log("CastleAftermath", "Opened castle member/prisoner selection. AvailableMembers="
				+ remainingMembers.TotalManCount + ", AvailablePrisoners=" + remainingPrisoners.TotalManCount
				+ ", InitialMembers=" + selectedMembers.TotalManCount);
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Open castle member/prisoner selection failed: " + ex);
			return false;
		}
	}

	internal static void AttachMissionBehavior(
		Mission mission,
		Func<Mission, IAgentOriginBase, int, int, FormationClass, Agent> prisonerSpawner = null)
	{
		try
		{
			if (mission == null)
			{
				return;
			}

			CastleAftermathPrisonerCommandMissionBehavior commandBehavior = mission.GetMissionBehavior<CastleAftermathPrisonerCommandMissionBehavior>();
			if (commandBehavior == null)
			{
				commandBehavior = new CastleAftermathPrisonerCommandMissionBehavior(SelectedPrisonerCount);
				mission.AddMissionBehavior(commandBehavior);
			}
			if (mission.GetMissionBehavior<TroopInspectionMissionLogic>() == null)
			{
				mission.AddMissionBehavior(new TroopInspectionMissionLogic(
					GetSelectedPrisonerRosterSnapshot(),
					commandBehavior.RegisterPrisoner,
					commandBehavior.CompleteSpawn,
					commandBehavior.SharedCleanup,
					prisonerSpawner));
			}
			Logger.Log("CastleAftermath", "Attached troop-inspection prisoner and castle command behaviors. Selected=" + SelectedPrisonerCount);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Attach castle prisoner mission behavior failed: " + ex.Message);
		}
	}

	private static bool DoneHandler(
		TroopRoster leftMemberRoster,
		TroopRoster leftPrisonerRoster,
		TroopRoster rightMemberRoster,
		TroopRoster rightPrisonerRoster,
		FlattenedTroopRoster takenPrisonerRoster,
		FlattenedTroopRoster releasedPrisonerRoster,
		bool isForced,
		PartyBase leftParty = null,
		PartyBase rightParty = null)
	{
		return true;
	}

	private static Tuple<bool, TextObject> DoneCondition(
		TroopRoster leftMemberRoster,
		TroopRoster leftPrisonerRoster,
		TroopRoster rightMemberRoster,
		TroopRoster rightPrisonerRoster,
		int leftLimitNum,
		int rightLimitNum)
	{
		int memberCount = rightMemberRoster?.TotalManCount ?? 0;
		int prisonerCount = rightPrisonerRoster?.TotalManCount ?? 0;
		bool allowed = SiegeCastleRosterSelectionProfile.IsWithinLimits(memberCount, prisonerCount);
		return new Tuple<bool, TextObject>(allowed, allowed
			? TextObject.GetEmpty()
			: new TextObject(SiegeCastleRosterSelectionProfile.BuildLimitMessage(memberCount, prisonerCount)));
	}

	private static bool IsCastleSelectionTroopTransferable(
		CharacterObject character,
		PartyScreenLogic.TroopType type,
		PartyScreenLogic.PartyRosterSide side,
		PartyBase leftOwnerParty)
	{
		return character != null
			&& !character.IsPlayerCharacter
			&& !character.IsNotTransferableInPartyScreen
			&& (type == PartyScreenLogic.TroopType.Member || type == PartyScreenLogic.TroopType.Prisoner);
	}

	private static TroopRoster BuildRemainingRoster(TroopRoster available, TroopRoster selected)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (available == null)
		{
			return result;
		}

		foreach (TroopRosterElement element in available.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (character == null || element.Number <= 0)
			{
				continue;
			}

			int remaining = Math.Max(0, element.Number - CountTroop(selected, character));
			if (remaining > 0)
			{
				int wounded = Math.Min(remaining, Math.Max(0, element.WoundedNumber));
				result.AddToCounts(character, remaining, false, wounded, Math.Max(0, element.Xp), true, -1);
			}
		}
		return result;
	}

	private static int CountTroop(TroopRoster roster, CharacterObject character)
	{
		if (roster == null || character == null)
		{
			return 0;
		}

		foreach (TroopRosterElement element in roster.GetTroopRoster())
		{
			if (element.Character == character)
			{
				return Math.Max(0, element.Number);
			}
		}
		return 0;
	}

	private static TroopRoster CloneRoster(TroopRoster source, int maxCount)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (source == null || maxCount <= 0)
		{
			return result;
		}

		int remaining = maxCount;
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (character == null || element.Number <= 0 || remaining <= 0)
			{
				continue;
			}

			int number = Math.Min(remaining, character.IsHero ? 1 : element.Number);
			int wounded = character.IsHero ? 0 : Math.Min(number, Math.Max(0, element.WoundedNumber));
			result.AddToCounts(character, number, false, wounded, Math.Max(0, element.Xp), true, -1);
			remaining -= number;
		}
		return result;
	}
}

internal sealed class CastleAftermathPrisonerCommandMissionBehavior : MissionLogic
{
	private sealed class FormationMovementState
	{
		internal bool Initialized;
		internal bool Moving;
		internal Vec2 LastOrderPosition;
		internal Vec2 TargetOrderPosition;
		internal float MoveStartedAt;
	}

	private const float MoveOrderDeltaSquared = 0.64f;
	private const float MoveArrivalDistanceSquared = 6.25f;
	private const float MoveTimeoutSeconds = 12f;
	private const float PoseRefreshSeconds = 1f;
	private const float MovePollSeconds = 0.2f;

	private readonly int _selectedCount;
	private readonly Dictionary<Agent, bool> _agents = new Dictionary<Agent, bool>();
	private readonly Dictionary<Formation, FormationMovementState> _movementStates = new Dictionary<Formation, FormationMovementState>();
	private readonly HashSet<Agent> _civilianActionSetApplied = new HashSet<Agent>();

	private bool _spawnCompleted;
	private bool _movementInitialized;
	private bool _completionLogged;
	private bool _cleaned;
	private float _nextPoseRefreshTime;
	private float _nextMovePollTime;
	private int _spawnedRegulars;
	private int _spawnedLords;

	internal CastleAftermathPrisonerCommandMissionBehavior(int selectedCount)
	{
		_selectedCount = Math.Max(0, selectedCount);
	}

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	internal void RegisterPrisoner(Agent agent, bool isLord)
	{
		if (agent == null)
		{
			return;
		}

		_agents[agent] = isLord;
		CastleAftermathRuntimeBridge.RegisterPrisonerAgent(agent, isLord);
		SiegeAiInterventionBehavior.EnsureAgentPlayerCommandableForExternal(
			agent,
			SiegeCastleRosterSelectionProfile.PrisonerSpawnCommandSource);
	}

	internal void CompleteSpawn(int selectedCount, int spawnedRegulars, int spawnedLords)
	{
		_spawnedRegulars = Math.Max(0, spawnedRegulars);
		_spawnedLords = Math.Max(0, spawnedLords);
		_spawnCompleted = true;
		Logger.Log("CastleAftermath", "Troop-inspection prisoner spawn callback completed. Selected="
			+ selectedCount + ", Regular=" + _spawnedRegulars + ", Lords=" + _spawnedLords);
	}

	internal void SharedCleanup(string reason)
	{
		Cleanup("shared_" + (reason ?? "unknown"));
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		Mission mission = base.Mission;
		if (!_spawnCompleted || mission == null || mission.IsMissionEnding || mission.Mode == MissionMode.Deployment)
		{
			return;
		}

		if (!_movementInitialized)
		{
			_movementInitialized = true;
			InitializeFormationMovementStates(mission);
			FreezeStationaryPrisoners();
		}

		if (!_completionLogged)
		{
			_completionLogged = true;
			LogCompletion(mission);
		}

		float now = mission.CurrentTime;
		if (now >= _nextMovePollTime)
		{
			_nextMovePollTime = now + MovePollSeconds;
			UpdateFormationMovement(now);
		}
		if (now >= _nextPoseRefreshTime)
		{
			_nextPoseRefreshTime = now + PoseRefreshSeconds;
			RefreshStationaryPrisonerPoses();
		}
	}

	public override void OnRemoveBehavior()
	{
		Cleanup("castle_prisoner_command_behavior_removed");
		base.OnRemoveBehavior();
	}

	protected override void OnEndMission()
	{
		Cleanup("castle_prisoner_command_mission_ended");
		base.OnEndMission();
	}

	private void InitializeFormationMovementStates(Mission mission)
	{
		Team team = mission.PlayerTeam ?? Agent.Main?.Team;
		if (team == null)
		{
			return;
		}

		foreach (int index in new[]
		{
			SiegeCastleRosterSelectionProfile.RegularPrisonerFormationIndex,
			SiegeCastleRosterSelectionProfile.LordPrisonerFormationIndex
		})
		{
			Formation formation = team.GetFormation((FormationClass)index);
			if (formation == null || !_agents.Keys.Any(agent => agent != null && agent.IsActive() && agent.Formation == formation))
			{
				continue;
			}

			try
			{
				formation.SetArrangementOrder(ArrangementOrder.ArrangementOrderLine);
				formation.SetMovementOrder(MovementOrder.MovementOrderMove(formation.CachedMedianPosition));
			}
			catch
			{
			}

			FormationMovementState state = new FormationMovementState();
			if (formation.OrderPositionIsValid)
			{
				state.LastOrderPosition = formation.OrderPosition;
				state.TargetOrderPosition = formation.OrderPosition;
				state.Initialized = true;
			}
			_movementStates[formation] = state;
		}
	}

	private void UpdateFormationMovement(float now)
	{
		foreach (KeyValuePair<Formation, FormationMovementState> pair in _movementStates.ToList())
		{
			Formation formation = pair.Key;
			FormationMovementState state = pair.Value;
			if (formation == null || !formation.OrderPositionIsValid)
			{
				continue;
			}

			Vec2 orderPosition = formation.OrderPosition;
			if (!state.Initialized)
			{
				state.Initialized = true;
				state.LastOrderPosition = orderPosition;
				state.TargetOrderPosition = orderPosition;
				continue;
			}

			if ((orderPosition - state.LastOrderPosition).LengthSquared > MoveOrderDeltaSquared)
			{
				state.LastOrderPosition = orderPosition;
				state.TargetOrderPosition = orderPosition;
				if (!state.Moving)
				{
					state.Moving = true;
					state.MoveStartedAt = now;
					SetFormationPrisonersMoving(formation);
				}
			}

			if (!state.Moving)
			{
				continue;
			}

			bool arrived = CalculateAverageDistanceSquared(formation, state.TargetOrderPosition) <= MoveArrivalDistanceSquared;
			bool timedOut = now - state.MoveStartedAt >= MoveTimeoutSeconds;
			if (arrived || timedOut)
			{
				state.Moving = false;
				FreezeFormationPrisoners(formation);
				Logger.Log("CastleAftermath", "Castle prisoner formation settled. Formation=" + formation.Index
					+ ", Arrived=" + arrived + ", TimedOut=" + timedOut);
			}
		}
	}

	private float CalculateAverageDistanceSquared(Formation formation, Vec2 target)
	{
		float total = 0f;
		int count = 0;
		foreach (Agent agent in _agents.Keys)
		{
			if (agent == null || !agent.IsActive() || agent.Formation != formation)
			{
				continue;
			}
			float dx = agent.Position.x - target.x;
			float dy = agent.Position.y - target.y;
			total += dx * dx + dy * dy;
			count++;
		}
		return count > 0 ? total / count : 0f;
	}

	private void SetFormationPrisonersMoving(Formation formation)
	{
		foreach (Agent agent in _agents.Keys.Where(agent => agent != null && agent.IsActive() && agent.Formation == formation).ToList())
		{
			try
			{
				agent.SetActionChannel(0, ActionIndexCache.act_none, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
				agent.SetMaximumSpeedLimit(1.35f, false);
				agent.SetIsAIPaused(isPaused: false);
				agent.DisableScriptedMovement();
				agent.SetShouldCatchUpWithFormation(true);
				agent.UpdateFormationOrders();
			}
			catch
			{
			}
		}
	}

	private void FreezeStationaryPrisoners()
	{
		foreach (Agent agent in _agents.Keys.ToList())
		{
			ApplyPrisonerPose(agent);
		}
	}

	private void FreezeFormationPrisoners(Formation formation)
	{
		foreach (Agent agent in _agents.Keys.Where(agent => agent != null && agent.IsActive() && agent.Formation == formation).ToList())
		{
			ApplyPrisonerPose(agent);
		}
	}

	private void RefreshStationaryPrisonerPoses()
	{
		foreach (Agent agent in _agents.Keys.ToList())
		{
			if (agent == null || !agent.IsActive())
			{
				continue;
			}
			if (agent.Formation != null
				&& _movementStates.TryGetValue(agent.Formation, out FormationMovementState state)
				&& state.Moving)
			{
				continue;
			}
			ApplyPrisonerPose(agent);
		}
	}

	private void ApplyPrisonerPose(Agent agent)
	{
		if (agent == null || !agent.IsActive())
		{
			return;
		}

		try { agent.SetIsAIPaused(isPaused: true); } catch { }
		try { agent.DisableScriptedMovement(); } catch { }
		try { agent.SetMaximumSpeedLimit(0f, false); } catch { }
		try { agent.SetCrouchMode(false); } catch { }
		TrySetCivilianPrisonerActionSet(agent);

		try
		{
			ActionIndexCache action = ActionIndexCache.act_scared_idle_1;
			if (!MBActionSet.CheckActionAnimationClipExists(agent.ActionSet, action))
			{
				return;
			}
			AnimFlags flags = AnimFlags.anf_disable_alternative_randomization
				| AnimFlags.anf_disable_auto_increment_progress
				| AnimFlags.anf_enforce_all;
			if (agent.SetActionChannel(0, action, true, flags, 0f, 0f, -0.2f, 0.4f, 0.35f, false, -0.2f, 0, true))
			{
				agent.SetCurrentActionProgress(0, 0.35f);
			}
		}
		catch
		{
		}
	}

	private void TrySetCivilianPrisonerActionSet(Agent agent)
	{
		try
		{
			if (agent == null || !agent.IsActive() || _civilianActionSetApplied.Contains(agent) || agent.Monster == null)
			{
				return;
			}
			string actionSetCode = agent.IsFemale ? "as_human_female_villager" : "as_human_villager";
			AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(MBActionSet.GetActionSet(actionSetCode), 1f, false);
			agent.SetActionSet(ref animationSystemData);
			_civilianActionSetApplied.Add(agent);
		}
		catch
		{
		}
	}

	private void LogCompletion(Mission mission)
	{
		int createdCount = _spawnedRegulars + _spawnedLords;
		int activeCount = _agents.Keys.Count(agent => agent != null && agent.IsHuman && agent.IsActive());
		int formedCount = _agents.Keys.Count(agent => agent != null && agent.IsHuman && agent.IsActive() && agent.Formation != null);
		bool commandUiReady = SiegeAiInterventionBehavior.EnsureInterventionCommandUiReadyForExternal(
			mission,
			SiegeCastleRosterSelectionProfile.PrisonerCommandUiRefreshSource);
		Logger.Log("CastleAftermath", "Castle prisoner spawn completed through troop-inspection pipeline. Selected=" + _selectedCount
			+ ", Created=" + createdCount + ", Active=" + activeCount + ", Formed=" + formedCount
			+ ", Regular=" + _spawnedRegulars + ", Lords=" + _spawnedLords
			+ ", MissionAgents=" + (mission.Agents?.Count ?? 0) + ", CommandUiReady=" + commandUiReady);
		AnimusForgeQuickInfo.Show(SiegeCastleRosterSelectionProfile.BuildPrisonerSceneReadyMessage(_selectedCount, activeCount));
	}

	private void Cleanup(string reason)
	{
		if (_cleaned)
		{
			return;
		}
		_cleaned = true;
		_agents.Clear();
		_movementStates.Clear();
		_civilianActionSetApplied.Clear();
		CastleAftermathRuntimeBridge.ClearMissionAgents(reason);
	}
}
