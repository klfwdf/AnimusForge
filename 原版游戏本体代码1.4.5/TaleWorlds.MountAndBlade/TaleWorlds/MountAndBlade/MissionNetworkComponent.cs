using System;
using System.Linq;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class MissionNetworkComponent : MissionNetwork
{
	private float _accumulatedTimeSinceLastTimerSync;

	private const float TimerSyncPeriod = 2f;

	private ChatBox _chatBox;

	public event Action OnMyClientSynchronized;

	public event Action<NetworkCommunicator> OnClientSynchronizedEvent;

	protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
	{
		if (GameNetwork.IsClientOrReplay)
		{
			registerer.RegisterBaseHandler<CreateFreeMountAgent>(HandleServerEventCreateFreeMountAgentEvent);
			registerer.RegisterBaseHandler<CreateAgent>(HandleServerEventCreateAgent);
			registerer.RegisterBaseHandler<SynchronizeAgentSpawnEquipment>(HandleServerEventSynchronizeAgentEquipment);
			registerer.RegisterBaseHandler<CreateAgentVisuals>(HandleServerEventCreateAgentVisuals);
			registerer.RegisterBaseHandler<RemoveAgentVisualsForPeer>(HandleServerEventRemoveAgentVisualsForPeer);
			registerer.RegisterBaseHandler<RemoveAgentVisualsFromIndexForPeer>(HandleServerEventRemoveAgentVisualsFromIndexForPeer);
			registerer.RegisterBaseHandler<ReplaceBotWithPlayer>(HandleServerEventReplaceBotWithPlayer);
			registerer.RegisterBaseHandler<SetWieldedItemIndex>(HandleServerEventSetWieldedItemIndex);
			registerer.RegisterBaseHandler<SetWeaponNetworkData>(HandleServerEventSetWeaponNetworkData);
			registerer.RegisterBaseHandler<SetWeaponAmmoData>(HandleServerEventSetWeaponAmmoData);
			registerer.RegisterBaseHandler<SetWeaponReloadPhase>(HandleServerEventSetWeaponReloadPhase);
			registerer.RegisterBaseHandler<WeaponUsageIndexChangeMessage>(HandleServerEventWeaponUsageIndexChangeMessage);
			registerer.RegisterBaseHandler<StartSwitchingWeaponUsageIndex>(HandleServerEventStartSwitchingWeaponUsageIndex);
			registerer.RegisterBaseHandler<InitializeFormation>(HandleServerEventInitializeFormation);
			registerer.RegisterBaseHandler<SetSpawnedFormationCount>(HandleServerEventSetSpawnedFormationCount);
			registerer.RegisterBaseHandler<AddTeam>(HandleServerEventAddTeam);
			registerer.RegisterBaseHandler<TeamSetIsEnemyOf>(HandleServerEventTeamSetIsEnemyOf);
			registerer.RegisterBaseHandler<AssignFormationToPlayer>(HandleServerEventAssignFormationToPlayer);
			registerer.RegisterBaseHandler<ExistingObjectsBegin>(HandleServerEventExistingObjectsBegin);
			registerer.RegisterBaseHandler<ExistingObjectsEnd>(HandleServerEventExistingObjectsEnd);
			registerer.RegisterBaseHandler<ClearMission>(HandleServerEventClearMission);
			registerer.RegisterBaseHandler<CreateMissionObject>(HandleServerEventCreateMissionObject);
			registerer.RegisterBaseHandler<RemoveMissionObject>(HandleServerEventRemoveMissionObject);
			registerer.RegisterBaseHandler<StopPhysicsAndSetFrameOfMissionObject>(HandleServerEventStopPhysicsAndSetFrameOfMissionObject);
			registerer.RegisterBaseHandler<BurstMissionObjectParticles>(HandleServerEventBurstMissionObjectParticles);
			registerer.RegisterBaseHandler<SetMissionObjectVisibility>(HandleServerEventSetMissionObjectVisibility);
			registerer.RegisterBaseHandler<SetMissionObjectDisabled>(HandleServerEventSetMissionObjectDisabled);
			registerer.RegisterBaseHandler<SetMissionObjectColors>(HandleServerEventSetMissionObjectColors);
			registerer.RegisterBaseHandler<SetMissionObjectFrame>(HandleServerEventSetMissionObjectFrame);
			registerer.RegisterBaseHandler<SetMissionObjectGlobalFrame>(HandleServerEventSetMissionObjectGlobalFrame);
			registerer.RegisterBaseHandler<SetMissionObjectFrameOverTime>(HandleServerEventSetMissionObjectFrameOverTime);
			registerer.RegisterBaseHandler<SetMissionObjectGlobalFrameOverTime>(HandleServerEventSetMissionObjectGlobalFrameOverTime);
			registerer.RegisterBaseHandler<SetMissionObjectAnimationAtChannel>(HandleServerEventSetMissionObjectAnimationAtChannel);
			registerer.RegisterBaseHandler<SetMissionObjectAnimationChannelParameter>(HandleServerEventSetMissionObjectAnimationChannelParameter);
			registerer.RegisterBaseHandler<SetMissionObjectAnimationPaused>(HandleServerEventSetMissionObjectAnimationPaused);
			registerer.RegisterBaseHandler<SetMissionObjectVertexAnimation>(HandleServerEventSetMissionObjectVertexAnimation);
			registerer.RegisterBaseHandler<SetMissionObjectVertexAnimationProgress>(HandleServerEventSetMissionObjectVertexAnimationProgress);
			registerer.RegisterBaseHandler<SetMissionObjectImpulse>(HandleServerEventSetMissionObjectImpulse);
			registerer.RegisterBaseHandler<AddMissionObjectBodyFlags>(HandleServerEventAddMissionObjectBodyFlags);
			registerer.RegisterBaseHandler<RemoveMissionObjectBodyFlags>(HandleServerEventRemoveMissionObjectBodyFlags);
			registerer.RegisterBaseHandler<SetMachineTargetRotation>(HandleServerEventSetMachineTargetRotation);
			registerer.RegisterBaseHandler<SetUsableMissionObjectIsDeactivated>(HandleServerEventSetUsableGameObjectIsDeactivated);
			registerer.RegisterBaseHandler<SetUsableMissionObjectIsDisabledForPlayers>(HandleServerEventSetUsableGameObjectIsDisabledForPlayers);
			registerer.RegisterBaseHandler<SetRangedSiegeWeaponState>(HandleServerEventSetRangedSiegeWeaponState);
			registerer.RegisterBaseHandler<SetRangedSiegeWeaponAmmo>(HandleServerEventSetRangedSiegeWeaponAmmo);
			registerer.RegisterBaseHandler<RangedSiegeWeaponChangeProjectile>(HandleServerEventRangedSiegeWeaponChangeProjectile);
			registerer.RegisterBaseHandler<SetStonePileAmmo>(HandleServerEventSetStonePileAmmo);
			registerer.RegisterBaseHandler<SetSiegeMachineMovementDistance>(HandleServerEventSetSiegeMachineMovementDistance);
			registerer.RegisterBaseHandler<SetSiegeLadderState>(HandleServerEventSetSiegeLadderState);
			registerer.RegisterBaseHandler<SetAgentTargetPositionAndDirection>(HandleServerEventSetAgentTargetPositionAndDirection);
			registerer.RegisterBaseHandler<SetAgentTargetPosition>(HandleServerEventSetAgentTargetPosition);
			registerer.RegisterBaseHandler<ClearAgentTargetFrame>(HandleServerEventClearAgentTargetFrame);
			registerer.RegisterBaseHandler<AgentTeleportToFrame>(HandleServerEventAgentTeleportToFrame);
			registerer.RegisterBaseHandler<SetSiegeTowerGateState>(HandleServerEventSetSiegeTowerGateState);
			registerer.RegisterBaseHandler<SetSiegeTowerHasArrivedAtTarget>(HandleServerEventSetSiegeTowerHasArrivedAtTarget);
			registerer.RegisterBaseHandler<SetBatteringRamHasArrivedAtTarget>(HandleServerEventSetBatteringRamHasArrivedAtTarget);
			registerer.RegisterBaseHandler<SetPeerTeam>(HandleServerEventSetPeerTeam);
			registerer.RegisterBaseHandler<SynchronizeMissionTimeTracker>(HandleServerEventSyncMissionTimer);
			registerer.RegisterBaseHandler<SetAgentPeer>(HandleServerEventSetAgentPeer);
			registerer.RegisterBaseHandler<SetAgentIsPlayer>(HandleServerEventSetAgentIsPlayer);
			registerer.RegisterBaseHandler<SetAgentHealth>(HandleServerEventSetAgentHealth);
			registerer.RegisterBaseHandler<AgentSetTeam>(HandleServerEventAgentSetTeam);
			registerer.RegisterBaseHandler<SetAgentActionSet>(HandleServerEventSetAgentActionSet);
			registerer.RegisterBaseHandler<MakeAgentDead>(HandleServerEventMakeAgentDead);
			registerer.RegisterBaseHandler<AgentSetFormation>(HandleServerEventAgentSetFormation);
			registerer.RegisterBaseHandler<AddPrefabComponentToAgentBone>(HandleServerEventAddPrefabComponentToAgentBone);
			registerer.RegisterBaseHandler<SetAgentPrefabComponentVisibility>(HandleServerEventSetAgentPrefabComponentVisibility);
			registerer.RegisterBaseHandler<UseObject>(HandleServerEventUseObject);
			registerer.RegisterBaseHandler<StopUsingObject>(HandleServerEventStopUsingObject);
			registerer.RegisterBaseHandler<SyncObjectHitpoints>(HandleServerEventHitSynchronizeObjectHitpoints);
			registerer.RegisterBaseHandler<SyncObjectDestructionLevel>(HandleServerEventHitSynchronizeObjectDestructionLevel);
			registerer.RegisterBaseHandler<BurstAllHeavyHitParticles>(HandleServerEventHitBurstAllHeavyHitParticles);
			registerer.RegisterBaseHandler<SynchronizeMissionObject>(HandleServerEventSynchronizeMissionObject);
			registerer.RegisterBaseHandler<SpawnWeaponWithNewEntity>(HandleServerEventSpawnWeaponWithNewEntity);
			registerer.RegisterBaseHandler<AttachWeaponToSpawnedWeapon>(HandleServerEventAttachWeaponToSpawnedWeapon);
			registerer.RegisterBaseHandler<AttachWeaponToAgent>(HandleServerEventAttachWeaponToAgent);
			registerer.RegisterBaseHandler<SpawnWeaponAsDropFromAgent>(HandleServerEventSpawnWeaponAsDropFromAgent);
			registerer.RegisterBaseHandler<SpawnAttachedWeaponOnSpawnedWeapon>(HandleServerEventSpawnAttachedWeaponOnSpawnedWeapon);
			registerer.RegisterBaseHandler<SpawnAttachedWeaponOnCorpse>(HandleServerEventSpawnAttachedWeaponOnCorpse);
			registerer.RegisterBaseHandler<HandleMissileCollisionReaction>(HandleServerEventHandleMissileCollisionReaction);
			registerer.RegisterBaseHandler<RemoveEquippedWeapon>(HandleServerEventRemoveEquippedWeapon);
			registerer.RegisterBaseHandler<BarkAgent>(HandleServerEventBarkAgent);
			registerer.RegisterBaseHandler<EquipWeaponWithNewEntity>(HandleServerEventEquipWeaponWithNewEntity);
			registerer.RegisterBaseHandler<AttachWeaponToWeaponInAgentEquipmentSlot>(HandleServerEventAttachWeaponToWeaponInAgentEquipmentSlot);
			registerer.RegisterBaseHandler<EquipWeaponFromSpawnedItemEntity>(HandleServerEventEquipWeaponFromSpawnedItemEntity);
			registerer.RegisterBaseHandler<CreateMissile>(HandleServerEventCreateMissile);
			registerer.RegisterBaseHandler<CombatLogNetworkMessage>(HandleServerEventAgentHit);
			registerer.RegisterBaseHandler<ConsumeWeaponAmount>(HandleServerEventConsumeWeaponAmount);
			registerer.RegisterBaseHandler<SetAgentOwningMissionPeer>(HandleServerEventSetAgentOwningMissionPeer);
		}
		else if (GameNetwork.IsServer)
		{
			registerer.RegisterBaseHandler<SetFollowedAgent>(HandleClientEventSetFollowedAgent);
			registerer.RegisterBaseHandler<SetMachineRotation>(HandleClientEventSetMachineRotation);
			registerer.RegisterBaseHandler<RequestUseObject>(HandleClientEventRequestUseObject);
			registerer.RegisterBaseHandler<RequestStopUsingObject>(HandleClientEventRequestStopUsingObject);
			registerer.RegisterBaseHandler<ApplyOrder>(HandleClientEventApplyOrder);
			registerer.RegisterBaseHandler<ApplySiegeWeaponOrder>(HandleClientEventApplySiegeWeaponOrder);
			registerer.RegisterBaseHandler<ApplyOrderWithPosition>(HandleClientEventApplyOrderWithPosition);
			registerer.RegisterBaseHandler<ApplyOrderWithFormation>(HandleClientEventApplyOrderWithFormation);
			registerer.RegisterBaseHandler<ApplyOrderWithFormationAndPercentage>(HandleClientEventApplyOrderWithFormationAndPercentage);
			registerer.RegisterBaseHandler<ApplyOrderWithFormationAndNumber>(HandleClientEventApplyOrderWithFormationAndNumber);
			registerer.RegisterBaseHandler<ApplyOrderWithTwoPositions>(HandleClientEventApplyOrderWithTwoPositions);
			registerer.RegisterBaseHandler<ApplyOrderWithMissionObject>(HandleClientEventApplyOrderWithGameEntity);
			registerer.RegisterBaseHandler<ApplyOrderWithAgent>(HandleClientEventApplyOrderWithAgent);
			registerer.RegisterBaseHandler<SelectAllFormations>(HandleClientEventSelectAllFormations);
			registerer.RegisterBaseHandler<SelectAllSiegeWeapons>(HandleClientEventSelectAllSiegeWeapons);
			registerer.RegisterBaseHandler<ClearSelectedFormations>(HandleClientEventClearSelectedFormations);
			registerer.RegisterBaseHandler<SelectFormation>(HandleClientEventSelectFormation);
			registerer.RegisterBaseHandler<SelectSiegeWeapon>(HandleClientEventSelectSiegeWeapon);
			registerer.RegisterBaseHandler<UnselectFormation>(HandleClientEventUnselectFormation);
			registerer.RegisterBaseHandler<UnselectSiegeWeapon>(HandleClientEventUnselectSiegeWeapon);
			registerer.RegisterBaseHandler<DropWeapon>(HandleClientEventDropWeapon);
			registerer.RegisterBaseHandler<TauntSelected>(HandleClientEventCheerSelected);
			registerer.RegisterBaseHandler<BarkSelected>(HandleClientEventBarkSelected);
			registerer.RegisterBaseHandler<AgentVisualsBreakInvulnerability>(HandleClientEventBreakAgentVisualsInvulnerability);
			registerer.RegisterBaseHandler<RequestToSpawnAsBot>(HandleClientEventRequestToSpawnAsBot);
		}
	}

	private Team GetTeamOfPeer(NetworkCommunicator networkPeer)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component.ControlledAgent == null)
		{
			MBDebug.Print("peer.ControlledAgent == null");
			return null;
		}
		Team team = component.ControlledAgent.Team;
		if (team == null)
		{
			MBDebug.Print("peersTeam == null");
		}
		return team;
	}

	private OrderController GetOrderControllerOfPeer(NetworkCommunicator networkPeer)
	{
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		if (teamOfPeer != null)
		{
			return teamOfPeer.GetOrderControllerOf(networkPeer.ControlledAgent);
		}
		MBDebug.Print("peersTeam == null");
		return null;
	}

	private void HandleServerEventSyncMissionTimer(GameNetworkMessage baseMessage)
	{
		SynchronizeMissionTimeTracker synchronizeMissionTimeTracker = (SynchronizeMissionTimeTracker)baseMessage;
		base.Mission.MissionTimeTracker.UpdateSync(synchronizeMissionTimeTracker.CurrentTime);
	}

	private void HandleServerEventSetPeerTeam(GameNetworkMessage baseMessage)
	{
		SetPeerTeam setPeerTeam = (SetPeerTeam)baseMessage;
		MissionPeer component = setPeerTeam.Peer.GetComponent<MissionPeer>();
		component.Team = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(setPeerTeam.TeamIndex);
		if (setPeerTeam.Peer.IsMine)
		{
			base.Mission.PlayerTeam = component.Team;
		}
	}

	private void HandleServerEventCreateFreeMountAgentEvent(GameNetworkMessage baseMessage)
	{
		CreateFreeMountAgent createFreeMountAgent = (CreateFreeMountAgent)baseMessage;
		base.Mission.SpawnMonster(createFreeMountAgent.HorseItem, createFreeMountAgent.HorseHarnessItem, createFreeMountAgent.Position, createFreeMountAgent.Direction.Normalized(), createFreeMountAgent.AgentIndex);
	}

	private void HandleServerEventCreateAgent(GameNetworkMessage baseMessage)
	{
		CreateAgent createAgent = (CreateAgent)baseMessage;
		BasicCharacterObject character = createAgent.Character;
		MissionPeer missionPeer = createAgent.Peer?.GetComponent<MissionPeer>();
		Team teamFromTeamIndex = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(createAgent.TeamIndex);
		AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(createAgent.IsPlayerAgent ? missionPeer : null).Monster(createAgent.Monster).TroopOrigin(new BasicBattleAgentOrigin(character))
			.Equipment(createAgent.SpawnEquipment)
			.EquipmentSeed(createAgent.BodyPropertiesSeed)
			.InitialPosition(createAgent.Position)
			.InitialDirection(createAgent.Direction.Normalized())
			.MissionEquipment(createAgent.MissionEquipment)
			.Team(teamFromTeamIndex)
			.Index(createAgent.AgentIndex)
			.MountIndex(createAgent.MountAgentIndex)
			.IsFemale(createAgent.IsFemale)
			.ClothingColor1(createAgent.ClothingColor1)
			.ClothingColor2(createAgent.ClothingColor2);
		Formation formation = null;
		if (teamFromTeamIndex != null && createAgent.FormationIndex >= 0 && !GameNetwork.IsReplay)
		{
			formation = teamFromTeamIndex.GetFormation((FormationClass)createAgent.FormationIndex);
			agentBuildData.Formation(formation);
		}
		if (createAgent.IsPlayerAgent)
		{
			agentBuildData.BodyProperties(missionPeer.Peer.BodyProperties);
			agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
		}
		else
		{
			agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.BodyPropertyRange.HairTags, character.BodyPropertyRange.BeardTags, character.BodyPropertyRange.TattooTags));
		}
		Banner banner = null;
		if (formation != null)
		{
			if (!string.IsNullOrEmpty(formation.BannerCode))
			{
				banner = ((formation.Banner != null) ? formation.Banner : (formation.Banner = new Banner(formation.BannerCode, teamFromTeamIndex.Color, teamFromTeamIndex.Color2)));
			}
		}
		else if (missionPeer != null)
		{
			banner = new Banner(missionPeer.Peer.BannerCode, teamFromTeamIndex.Color, teamFromTeamIndex.Color2);
		}
		agentBuildData.Banner(banner);
		_ = base.Mission.SpawnAgent(agentBuildData).MountAgent;
	}

	private void HandleServerEventSynchronizeAgentEquipment(GameNetworkMessage baseMessage)
	{
		SynchronizeAgentSpawnEquipment synchronizeAgentSpawnEquipment = (SynchronizeAgentSpawnEquipment)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(synchronizeAgentSpawnEquipment.AgentIndex).UpdateSpawnEquipmentAndRefreshVisuals(synchronizeAgentSpawnEquipment.SpawnEquipment);
	}

	private void HandleServerEventCreateAgentVisuals(GameNetworkMessage baseMessage)
	{
		CreateAgentVisuals createAgentVisuals = (CreateAgentVisuals)baseMessage;
		MissionPeer component = createAgentVisuals.Peer.GetComponent<MissionPeer>();
		_ = component.Team.Side;
		BasicCharacterObject character = createAgentVisuals.Character;
		_ = character.Culture;
		BasicCultureObject attackerCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
		BasicCultureObject defenderCulture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
		MultiplayerBattleColors.MultiplayerCultureColorInfo peerColors = MultiplayerBattleColors.CreateWith(attackerCulture, defenderCulture).GetPeerColors(component);
		AgentBuildData agentBuildData = new AgentBuildData(character).VisualsIndex(createAgentVisuals.VisualsIndex).Equipment(createAgentVisuals.Equipment).EquipmentSeed(createAgentVisuals.BodyPropertiesSeed)
			.IsFemale(createAgentVisuals.IsFemale)
			.ClothingColor1(peerColors.ClothingColor1Uint)
			.ClothingColor2(peerColors.ClothingColor2Uint);
		if (createAgentVisuals.VisualsIndex == 0)
		{
			agentBuildData.BodyProperties(component.Peer.BodyProperties);
		}
		else
		{
			agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, createAgentVisuals.BodyPropertiesSeed, character.BodyPropertyRange.HairTags, character.BodyPropertyRange.BeardTags, character.BodyPropertyRange.TattooTags));
		}
		base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().SpawnAgentVisualsForPeer(component, agentBuildData, createAgentVisuals.SelectedEquipmentSetIndex, isBot: false, createAgentVisuals.TroopCountInFormation);
		if (agentBuildData.AgentVisualsIndex == 0)
		{
			component.HasSpawnedAgentVisuals = true;
			component.EquipmentUpdatingExpired = false;
		}
	}

	private void HandleServerEventRemoveAgentVisualsForPeer(GameNetworkMessage baseMessage)
	{
		MissionPeer component = ((RemoveAgentVisualsForPeer)baseMessage).Peer.GetComponent<MissionPeer>();
		base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component);
		component.HasSpawnedAgentVisuals = false;
	}

	private void HandleServerEventRemoveAgentVisualsFromIndexForPeer(GameNetworkMessage baseMessage)
	{
		((RemoveAgentVisualsFromIndexForPeer)baseMessage).Peer.GetComponent<MissionPeer>();
	}

	private void HandleServerEventReplaceBotWithPlayer(GameNetworkMessage baseMessage)
	{
		ReplaceBotWithPlayer replaceBotWithPlayer = (ReplaceBotWithPlayer)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(replaceBotWithPlayer.BotAgentIndex);
		if (agentFromIndex.Formation != null)
		{
			agentFromIndex.Formation.PlayerOwner = agentFromIndex;
		}
		MissionPeer component = replaceBotWithPlayer.Peer.GetComponent<MissionPeer>();
		agentFromIndex.MissionPeer = replaceBotWithPlayer.Peer.GetComponent<MissionPeer>();
		agentFromIndex.Formation = component.ControlledFormation;
		agentFromIndex.Health = replaceBotWithPlayer.Health;
		if (agentFromIndex.MountAgent != null)
		{
			agentFromIndex.MountAgent.Health = replaceBotWithPlayer.MountHealth;
		}
		if (agentFromIndex.Formation != null)
		{
			agentFromIndex.Team.AssignPlayerAsSergeantOfFormation(component, component.ControlledFormation.FormationIndex);
		}
	}

	private void HandleServerEventSetWieldedItemIndex(GameNetworkMessage baseMessage)
	{
		SetWieldedItemIndex setWieldedItemIndex = (SetWieldedItemIndex)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setWieldedItemIndex.AgentIndex);
		if (agentFromIndex != null)
		{
			agentFromIndex.SetWieldedItemIndexAsClient(setWieldedItemIndex.IsLeftHand ? Agent.HandIndex.OffHand : Agent.HandIndex.MainHand, setWieldedItemIndex.WieldedItemIndex, setWieldedItemIndex.IsWieldedInstantly, setWieldedItemIndex.IsWieldedOnSpawn, setWieldedItemIndex.MainHandCurrentUsageIndex);
			agentFromIndex.UpdateAgentStats();
		}
	}

	private void HandleServerEventSetWeaponNetworkData(GameNetworkMessage baseMessage)
	{
		SetWeaponNetworkData setWeaponNetworkData = (SetWeaponNetworkData)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setWeaponNetworkData.AgentIndex);
		WeaponComponentData weaponComponentData = agentFromIndex.Equipment[setWeaponNetworkData.WeaponEquipmentIndex].Item?.PrimaryWeapon;
		if (weaponComponentData != null)
		{
			if (weaponComponentData.WeaponFlags.HasAnyFlag(WeaponFlags.HasHitPoints))
			{
				agentFromIndex.ChangeWeaponHitPoints(setWeaponNetworkData.WeaponEquipmentIndex, setWeaponNetworkData.DataValue);
			}
			else if (weaponComponentData.IsConsumable)
			{
				agentFromIndex.SetWeaponAmountInSlot(setWeaponNetworkData.WeaponEquipmentIndex, setWeaponNetworkData.DataValue, enforcePrimaryItem: true);
			}
		}
	}

	private void HandleServerEventSetWeaponAmmoData(GameNetworkMessage baseMessage)
	{
		SetWeaponAmmoData setWeaponAmmoData = (SetWeaponAmmoData)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setWeaponAmmoData.AgentIndex);
		if (agentFromIndex.Equipment[setWeaponAmmoData.WeaponEquipmentIndex].CurrentUsageItem.IsRangedWeapon)
		{
			agentFromIndex.SetWeaponAmmoAsClient(setWeaponAmmoData.WeaponEquipmentIndex, setWeaponAmmoData.AmmoEquipmentIndex, setWeaponAmmoData.Ammo);
		}
		else
		{
			Debug.FailedAssert("Invalid item type.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MissionNetworkComponent.cs", "HandleServerEventSetWeaponAmmoData", 468);
		}
	}

	private void HandleServerEventSetWeaponReloadPhase(GameNetworkMessage baseMessage)
	{
		SetWeaponReloadPhase setWeaponReloadPhase = (SetWeaponReloadPhase)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(setWeaponReloadPhase.AgentIndex).SetWeaponReloadPhaseAsClient(setWeaponReloadPhase.EquipmentIndex, setWeaponReloadPhase.ReloadPhase);
	}

	private void HandleServerEventWeaponUsageIndexChangeMessage(GameNetworkMessage baseMessage)
	{
		WeaponUsageIndexChangeMessage weaponUsageIndexChangeMessage = (WeaponUsageIndexChangeMessage)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(weaponUsageIndexChangeMessage.AgentIndex).SetUsageIndexOfWeaponInSlotAsClient(weaponUsageIndexChangeMessage.SlotIndex, weaponUsageIndexChangeMessage.UsageIndex);
	}

	private void HandleServerEventStartSwitchingWeaponUsageIndex(GameNetworkMessage baseMessage)
	{
		StartSwitchingWeaponUsageIndex startSwitchingWeaponUsageIndex = (StartSwitchingWeaponUsageIndex)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(startSwitchingWeaponUsageIndex.AgentIndex).StartSwitchingWeaponUsageIndexAsClient(startSwitchingWeaponUsageIndex.EquipmentIndex, startSwitchingWeaponUsageIndex.UsageIndex, startSwitchingWeaponUsageIndex.CurrentMovementFlagUsageDirection);
	}

	private void HandleServerEventInitializeFormation(GameNetworkMessage baseMessage)
	{
		InitializeFormation initializeFormation = (InitializeFormation)baseMessage;
		Mission.MissionNetworkHelper.GetTeamFromTeamIndex(initializeFormation.TeamIndex).GetFormation((FormationClass)initializeFormation.FormationIndex).BannerCode = initializeFormation.BannerCode;
	}

	private void HandleServerEventSetSpawnedFormationCount(GameNetworkMessage baseMessage)
	{
		SetSpawnedFormationCount setSpawnedFormationCount = (SetSpawnedFormationCount)baseMessage;
		base.Mission.NumOfFormationsSpawnedTeamOne = setSpawnedFormationCount.NumOfFormationsTeamOne;
		base.Mission.NumOfFormationsSpawnedTeamTwo = setSpawnedFormationCount.NumOfFormationsTeamTwo;
	}

	private void HandleServerEventAddTeam(GameNetworkMessage baseMessage)
	{
		AddTeam addTeam = (AddTeam)baseMessage;
		Banner banner = (string.IsNullOrEmpty(addTeam.BannerCode) ? null : new Banner(addTeam.BannerCode, addTeam.Color, addTeam.Color2));
		base.Mission.Teams.Add(addTeam.Side, addTeam.Color, addTeam.Color2, banner, addTeam.IsPlayerGeneral, addTeam.IsPlayerSergeant);
	}

	private void HandleServerEventTeamSetIsEnemyOf(GameNetworkMessage baseMessage)
	{
		TeamSetIsEnemyOf teamSetIsEnemyOf = (TeamSetIsEnemyOf)baseMessage;
		Team teamFromTeamIndex = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(teamSetIsEnemyOf.Team1Index);
		Team teamFromTeamIndex2 = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(teamSetIsEnemyOf.Team2Index);
		teamFromTeamIndex.SetIsEnemyOf(teamFromTeamIndex2, teamSetIsEnemyOf.IsEnemyOf);
	}

	private void HandleServerEventAssignFormationToPlayer(GameNetworkMessage baseMessage)
	{
		AssignFormationToPlayer assignFormationToPlayer = (AssignFormationToPlayer)baseMessage;
		MissionPeer component = assignFormationToPlayer.Peer.GetComponent<MissionPeer>();
		component.Team.AssignPlayerAsSergeantOfFormation(component, assignFormationToPlayer.FormationClass);
	}

	private void HandleServerEventExistingObjectsBegin(GameNetworkMessage baseMessage)
	{
	}

	private void HandleServerEventExistingObjectsEnd(GameNetworkMessage baseMessage)
	{
	}

	private void HandleServerEventClearMission(GameNetworkMessage baseMessage)
	{
		base.Mission.ResetMission();
	}

	private void HandleServerEventCreateMissionObject(GameNetworkMessage baseMessage)
	{
		CreateMissionObject createMissionObject = (CreateMissionObject)baseMessage;
		GameEntity gameEntity = GameEntity.Instantiate(base.Mission.Scene, createMissionObject.Prefab, createMissionObject.Frame);
		MissionObject firstScriptOfType = gameEntity.GetFirstScriptOfType<MissionObject>();
		if (firstScriptOfType == null)
		{
			return;
		}
		firstScriptOfType.Id = createMissionObject.ObjectId;
		int num = 0;
		foreach (GameEntity child in gameEntity.GetChildren())
		{
			MissionObject missionObject = null;
			if ((missionObject = child.GetFirstScriptOfType<MissionObject>()) != null)
			{
				missionObject.Id = createMissionObject.ChildObjectIds[num++];
			}
		}
	}

	private void HandleServerEventRemoveMissionObject(GameNetworkMessage baseMessage)
	{
		RemoveMissionObject message = (RemoveMissionObject)baseMessage;
		base.Mission.MissionObjects.FirstOrDefault((MissionObject mo) => mo.Id == message.ObjectId)?.GameEntity.Remove(82);
	}

	private void HandleServerEventStopPhysicsAndSetFrameOfMissionObject(GameNetworkMessage baseMessage)
	{
		StopPhysicsAndSetFrameOfMissionObject message = (StopPhysicsAndSetFrameOfMissionObject)baseMessage;
		SpawnedItemEntity obj = (SpawnedItemEntity)base.Mission.MissionObjects.FirstOrDefault((MissionObject mo) => mo.Id == message.ObjectId);
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.ParentId);
		obj?.StopPhysicsAndSetFrameForClient(message.Frame, GameEntity.CreateFromWeakEntity(missionObjectFromMissionObjectId?.GameEntity ?? WeakGameEntity.Invalid));
	}

	private void HandleServerEventBurstMissionObjectParticles(GameNetworkMessage baseMessage)
	{
		BurstMissionObjectParticles burstMissionObjectParticles = (BurstMissionObjectParticles)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(burstMissionObjectParticles.MissionObjectId) as SynchedMissionObject).BurstParticlesSynched(burstMissionObjectParticles.DoChildren);
	}

	private void HandleServerEventSetMissionObjectVisibility(GameNetworkMessage baseMessage)
	{
		SetMissionObjectVisibility setMissionObjectVisibility = (SetMissionObjectVisibility)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectVisibility.MissionObjectId).GameEntity.SetVisibilityExcludeParents(setMissionObjectVisibility.Visible);
	}

	private void HandleServerEventSetMissionObjectDisabled(GameNetworkMessage baseMessage)
	{
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(((SetMissionObjectDisabled)baseMessage).MissionObjectId).SetDisabledAndMakeInvisible();
	}

	private void HandleServerEventSetMissionObjectColors(GameNetworkMessage baseMessage)
	{
		SetMissionObjectColors setMissionObjectColors = (SetMissionObjectColors)baseMessage;
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectColors.MissionObjectId) is SynchedMissionObject synchedMissionObject)
		{
			synchedMissionObject.SetTeamColors(setMissionObjectColors.Color, setMissionObjectColors.Color2);
		}
	}

	private void HandleServerEventSetMissionObjectFrame(GameNetworkMessage baseMessage)
	{
		SetMissionObjectFrame setMissionObjectFrame = (SetMissionObjectFrame)baseMessage;
		SynchedMissionObject obj = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectFrame.MissionObjectId) as SynchedMissionObject;
		MatrixFrame frame = setMissionObjectFrame.Frame;
		obj.SetFrameSynched(ref frame, isClient: true);
	}

	private void HandleServerEventSetMissionObjectGlobalFrame(GameNetworkMessage baseMessage)
	{
		SetMissionObjectGlobalFrame setMissionObjectGlobalFrame = (SetMissionObjectGlobalFrame)baseMessage;
		SynchedMissionObject obj = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectGlobalFrame.MissionObjectId) as SynchedMissionObject;
		MatrixFrame frame = setMissionObjectGlobalFrame.Frame;
		obj.SetGlobalFrameSynched(ref frame, isClient: true);
	}

	private void HandleServerEventSetMissionObjectFrameOverTime(GameNetworkMessage baseMessage)
	{
		SetMissionObjectFrameOverTime setMissionObjectFrameOverTime = (SetMissionObjectFrameOverTime)baseMessage;
		SynchedMissionObject obj = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectFrameOverTime.MissionObjectId) as SynchedMissionObject;
		MatrixFrame frame = setMissionObjectFrameOverTime.Frame;
		obj.SetFrameSynchedOverTime(ref frame, setMissionObjectFrameOverTime.Duration, isClient: true);
	}

	private void HandleServerEventSetMissionObjectGlobalFrameOverTime(GameNetworkMessage baseMessage)
	{
		SetMissionObjectGlobalFrameOverTime setMissionObjectGlobalFrameOverTime = (SetMissionObjectGlobalFrameOverTime)baseMessage;
		SynchedMissionObject obj = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectGlobalFrameOverTime.MissionObjectId) as SynchedMissionObject;
		MatrixFrame frame = setMissionObjectGlobalFrameOverTime.Frame;
		obj.SetGlobalFrameSynchedOverTime(ref frame, setMissionObjectGlobalFrameOverTime.Duration, isClient: true);
	}

	private void HandleServerEventSetMissionObjectAnimationAtChannel(GameNetworkMessage baseMessage)
	{
		SetMissionObjectAnimationAtChannel setMissionObjectAnimationAtChannel = (SetMissionObjectAnimationAtChannel)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectAnimationAtChannel.MissionObjectId).GameEntity.Skeleton.SetAnimationAtChannel(setMissionObjectAnimationAtChannel.AnimationIndex, setMissionObjectAnimationAtChannel.ChannelNo, setMissionObjectAnimationAtChannel.AnimationSpeed);
	}

	private void HandleServerEventSetRangedSiegeWeaponAmmo(GameNetworkMessage baseMessage)
	{
		SetRangedSiegeWeaponAmmo setRangedSiegeWeaponAmmo = (SetRangedSiegeWeaponAmmo)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setRangedSiegeWeaponAmmo.RangedSiegeWeaponId) as RangedSiegeWeapon).SetAmmo(setRangedSiegeWeaponAmmo.AmmoCount);
	}

	private void HandleServerEventRangedSiegeWeaponChangeProjectile(GameNetworkMessage baseMessage)
	{
		RangedSiegeWeaponChangeProjectile rangedSiegeWeaponChangeProjectile = (RangedSiegeWeaponChangeProjectile)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(rangedSiegeWeaponChangeProjectile.RangedSiegeWeaponId) as RangedSiegeWeapon).ChangeProjectileEntityClient(rangedSiegeWeaponChangeProjectile.Index);
	}

	private void HandleServerEventSetStonePileAmmo(GameNetworkMessage baseMessage)
	{
		SetStonePileAmmo setStonePileAmmo = (SetStonePileAmmo)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setStonePileAmmo.StonePileId) as StonePile).SetAmmo(setStonePileAmmo.AmmoCount);
	}

	private void HandleServerEventSetRangedSiegeWeaponState(GameNetworkMessage baseMessage)
	{
		SetRangedSiegeWeaponState setRangedSiegeWeaponState = (SetRangedSiegeWeaponState)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setRangedSiegeWeaponState.RangedSiegeWeaponId) as RangedSiegeWeapon).State = setRangedSiegeWeaponState.State;
	}

	private void HandleServerEventSetSiegeLadderState(GameNetworkMessage baseMessage)
	{
		SetSiegeLadderState setSiegeLadderState = (SetSiegeLadderState)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setSiegeLadderState.SiegeLadderId) as SiegeLadder).State = setSiegeLadderState.State;
	}

	private void HandleServerEventSetSiegeTowerGateState(GameNetworkMessage baseMessage)
	{
		SetSiegeTowerGateState setSiegeTowerGateState = (SetSiegeTowerGateState)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setSiegeTowerGateState.SiegeTowerId) as SiegeTower).State = setSiegeTowerGateState.State;
	}

	private void HandleServerEventSetSiegeTowerHasArrivedAtTarget(GameNetworkMessage baseMessage)
	{
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(((SetSiegeTowerHasArrivedAtTarget)baseMessage).SiegeTowerId) as SiegeTower).HasArrivedAtTarget = true;
	}

	private void HandleServerEventSetBatteringRamHasArrivedAtTarget(GameNetworkMessage baseMessage)
	{
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(((SetBatteringRamHasArrivedAtTarget)baseMessage).BatteringRamId) as BatteringRam).HasArrivedAtTarget = true;
	}

	private void HandleServerEventSetSiegeMachineMovementDistance(GameNetworkMessage baseMessage)
	{
		SetSiegeMachineMovementDistance setSiegeMachineMovementDistance = (SetSiegeMachineMovementDistance)baseMessage;
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setSiegeMachineMovementDistance.UsableMachineId) is UsableMachine usableMachine)
		{
			if (usableMachine is SiegeTower)
			{
				((SiegeTower)usableMachine).MovementComponent.SetDistanceTraveledAsClient(setSiegeMachineMovementDistance.Distance);
			}
			else
			{
				((BatteringRam)usableMachine).MovementComponent.SetDistanceTraveledAsClient(setSiegeMachineMovementDistance.Distance);
			}
		}
	}

	private void HandleServerEventSetMissionObjectAnimationChannelParameter(GameNetworkMessage baseMessage)
	{
		SetMissionObjectAnimationChannelParameter setMissionObjectAnimationChannelParameter = (SetMissionObjectAnimationChannelParameter)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectAnimationChannelParameter.MissionObjectId)?.GameEntity.Skeleton.SetAnimationParameterAtChannel(setMissionObjectAnimationChannelParameter.ChannelNo, setMissionObjectAnimationChannelParameter.Parameter);
	}

	private void HandleServerEventSetMissionObjectVertexAnimation(GameNetworkMessage baseMessage)
	{
		SetMissionObjectVertexAnimation setMissionObjectVertexAnimation = (SetMissionObjectVertexAnimation)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectVertexAnimation.MissionObjectId);
		if (missionObjectFromMissionObjectId != null)
		{
			(missionObjectFromMissionObjectId as VertexAnimator).SetAnimationSynched(setMissionObjectVertexAnimation.BeginKey, setMissionObjectVertexAnimation.EndKey, setMissionObjectVertexAnimation.Speed);
		}
	}

	private void HandleServerEventSetMissionObjectVertexAnimationProgress(GameNetworkMessage baseMessage)
	{
		SetMissionObjectVertexAnimationProgress setMissionObjectVertexAnimationProgress = (SetMissionObjectVertexAnimationProgress)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectVertexAnimationProgress.MissionObjectId);
		if (missionObjectFromMissionObjectId != null)
		{
			(missionObjectFromMissionObjectId as VertexAnimator).SetProgressSynched(setMissionObjectVertexAnimationProgress.Progress);
		}
	}

	private void HandleServerEventSetMissionObjectAnimationPaused(GameNetworkMessage baseMessage)
	{
		SetMissionObjectAnimationPaused setMissionObjectAnimationPaused = (SetMissionObjectAnimationPaused)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectAnimationPaused.MissionObjectId);
		if (missionObjectFromMissionObjectId != null)
		{
			if (setMissionObjectAnimationPaused.IsPaused)
			{
				missionObjectFromMissionObjectId.GameEntity.PauseSkeletonAnimation();
			}
			else
			{
				missionObjectFromMissionObjectId.GameEntity.ResumeSkeletonAnimation();
			}
		}
	}

	private void HandleServerEventAddMissionObjectBodyFlags(GameNetworkMessage baseMessage)
	{
		AddMissionObjectBodyFlags addMissionObjectBodyFlags = (AddMissionObjectBodyFlags)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(addMissionObjectBodyFlags.MissionObjectId)?.GameEntity.AddBodyFlags(addMissionObjectBodyFlags.BodyFlags, addMissionObjectBodyFlags.ApplyToChildren);
	}

	private void HandleServerEventRemoveMissionObjectBodyFlags(GameNetworkMessage baseMessage)
	{
		RemoveMissionObjectBodyFlags removeMissionObjectBodyFlags = (RemoveMissionObjectBodyFlags)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(removeMissionObjectBodyFlags.MissionObjectId)?.GameEntity.RemoveBodyFlags(removeMissionObjectBodyFlags.BodyFlags, removeMissionObjectBodyFlags.ApplyToChildren);
	}

	private void HandleServerEventSetMachineTargetRotation(GameNetworkMessage baseMessage)
	{
		SetMachineTargetRotation setMachineTargetRotation = (SetMachineTargetRotation)baseMessage;
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMachineTargetRotation.UsableMachineId) is UsableMachine { PilotAgent: not null } usableMachine)
		{
			((RangedSiegeWeapon)usableMachine).AimAtRotation(setMachineTargetRotation.HorizontalRotation, setMachineTargetRotation.VerticalRotation);
		}
	}

	private void HandleServerEventSetUsableGameObjectIsDeactivated(GameNetworkMessage baseMessage)
	{
		SetUsableMissionObjectIsDeactivated setUsableMissionObjectIsDeactivated = (SetUsableMissionObjectIsDeactivated)baseMessage;
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setUsableMissionObjectIsDeactivated.UsableGameObjectId) is UsableMissionObject usableMissionObject)
		{
			usableMissionObject.IsDeactivated = setUsableMissionObjectIsDeactivated.IsDeactivated;
		}
	}

	private void HandleServerEventSetUsableGameObjectIsDisabledForPlayers(GameNetworkMessage baseMessage)
	{
		SetUsableMissionObjectIsDisabledForPlayers setUsableMissionObjectIsDisabledForPlayers = (SetUsableMissionObjectIsDisabledForPlayers)baseMessage;
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setUsableMissionObjectIsDisabledForPlayers.UsableGameObjectId) is UsableMissionObject usableMissionObject)
		{
			usableMissionObject.IsDisabledForPlayers = setUsableMissionObjectIsDisabledForPlayers.IsDisabledForPlayers;
		}
	}

	private void HandleServerEventSetMissionObjectImpulse(GameNetworkMessage baseMessage)
	{
		SetMissionObjectImpulse setMissionObjectImpulse = (SetMissionObjectImpulse)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMissionObjectImpulse.MissionObjectId);
		if (missionObjectFromMissionObjectId != null)
		{
			Vec3 position = setMissionObjectImpulse.Position;
			missionObjectFromMissionObjectId.GameEntity.ApplyLocalImpulseToDynamicBody(position, setMissionObjectImpulse.Impulse);
		}
	}

	private void HandleServerEventSetAgentTargetPositionAndDirection(GameNetworkMessage baseMessage)
	{
		SetAgentTargetPositionAndDirection obj = (SetAgentTargetPositionAndDirection)baseMessage;
		Vec2 targetPosition = obj.Position;
		Vec3 targetDirection = obj.Direction;
		Mission.MissionNetworkHelper.GetAgentFromIndex(obj.AgentIndex).SetTargetPositionAndDirectionSynched(ref targetPosition, ref targetDirection);
	}

	private void HandleServerEventSetAgentTargetPosition(GameNetworkMessage baseMessage)
	{
		SetAgentTargetPosition obj = (SetAgentTargetPosition)baseMessage;
		Vec2 targetPosition = obj.Position;
		Mission.MissionNetworkHelper.GetAgentFromIndex(obj.AgentIndex).SetTargetPositionSynched(ref targetPosition);
	}

	private void HandleServerEventClearAgentTargetFrame(GameNetworkMessage baseMessage)
	{
		Mission.MissionNetworkHelper.GetAgentFromIndex(((ClearAgentTargetFrame)baseMessage).AgentIndex).ClearTargetFrame();
	}

	private void HandleServerEventAgentTeleportToFrame(GameNetworkMessage baseMessage)
	{
		AgentTeleportToFrame agentTeleportToFrame = (AgentTeleportToFrame)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(agentTeleportToFrame.AgentIndex);
		agentFromIndex.TeleportToPosition(agentTeleportToFrame.Position);
		Vec2 direction = agentTeleportToFrame.Direction.Normalized();
		agentFromIndex.SetMovementDirection(in direction);
		agentFromIndex.LookDirection = direction.ToVec3();
	}

	private void HandleServerEventSetAgentPeer(GameNetworkMessage baseMessage)
	{
		SetAgentPeer setAgentPeer = (SetAgentPeer)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setAgentPeer.AgentIndex, canBeNull: true);
		if (agentFromIndex != null)
		{
			MissionPeer missionPeer = setAgentPeer.Peer?.GetComponent<MissionPeer>();
			agentFromIndex.MissionPeer = missionPeer;
		}
	}

	private void HandleServerEventSetAgentIsPlayer(GameNetworkMessage baseMessage)
	{
		SetAgentIsPlayer setAgentIsPlayer = (SetAgentIsPlayer)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setAgentIsPlayer.AgentIndex);
		if (agentFromIndex.Controller == AgentControllerType.Player != setAgentIsPlayer.IsPlayer)
		{
			if (!agentFromIndex.IsMine)
			{
				agentFromIndex.Controller = AgentControllerType.None;
			}
			else
			{
				agentFromIndex.Controller = AgentControllerType.Player;
			}
		}
	}

	private void HandleServerEventSetAgentHealth(GameNetworkMessage baseMessage)
	{
		SetAgentHealth setAgentHealth = (SetAgentHealth)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(setAgentHealth.AgentIndex).Health = setAgentHealth.Health;
	}

	private void HandleServerEventAgentSetTeam(GameNetworkMessage baseMessage)
	{
		AgentSetTeam obj = (AgentSetTeam)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(obj.AgentIndex);
		MBTeam mBTeamFromTeamIndex = Mission.MissionNetworkHelper.GetMBTeamFromTeamIndex(obj.TeamIndex);
		agentFromIndex.SetTeam(base.Mission.Teams.Find(mBTeamFromTeamIndex), sync: false);
	}

	private void HandleServerEventSetAgentActionSet(GameNetworkMessage baseMessage)
	{
		SetAgentActionSet setAgentActionSet = (SetAgentActionSet)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setAgentActionSet.AgentIndex);
		AnimationSystemData animationSystemData = agentFromIndex.Monster.FillAnimationSystemData(setAgentActionSet.ActionSet, setAgentActionSet.StepSize, hasClippingPlane: false);
		animationSystemData.NumPaces = setAgentActionSet.NumPaces;
		animationSystemData.MonsterUsageSetIndex = setAgentActionSet.MonsterUsageSetIndex;
		animationSystemData.WalkingSpeedLimit = setAgentActionSet.WalkingSpeedLimit;
		animationSystemData.CrouchWalkingSpeedLimit = setAgentActionSet.CrouchWalkingSpeedLimit;
		agentFromIndex.SetActionSet(ref animationSystemData);
	}

	private void HandleServerEventMakeAgentDead(GameNetworkMessage baseMessage)
	{
		MakeAgentDead makeAgentDead = (MakeAgentDead)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(makeAgentDead.AgentIndex).MakeDead(makeAgentDead.IsKilled, makeAgentDead.ActionCodeIndex, makeAgentDead.CorpsesToFadeIndex);
	}

	private void HandleServerEventAddPrefabComponentToAgentBone(GameNetworkMessage baseMessage)
	{
		AddPrefabComponentToAgentBone addPrefabComponentToAgentBone = (AddPrefabComponentToAgentBone)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(addPrefabComponentToAgentBone.AgentIndex).AddSynchedPrefabComponentToBone(addPrefabComponentToAgentBone.PrefabName, addPrefabComponentToAgentBone.BoneIndex);
	}

	private void HandleServerEventSetAgentPrefabComponentVisibility(GameNetworkMessage baseMessage)
	{
		SetAgentPrefabComponentVisibility setAgentPrefabComponentVisibility = (SetAgentPrefabComponentVisibility)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(setAgentPrefabComponentVisibility.AgentIndex).SetSynchedPrefabComponentVisibility(setAgentPrefabComponentVisibility.ComponentIndex, setAgentPrefabComponentVisibility.Visibility);
	}

	private void HandleServerEventAgentSetFormation(GameNetworkMessage baseMessage)
	{
		AgentSetFormation agentSetFormation = (AgentSetFormation)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(agentSetFormation.AgentIndex);
		Team team = agentFromIndex.Team;
		Formation formation = null;
		if (team != null)
		{
			formation = ((agentSetFormation.FormationIndex >= 0) ? team.GetFormation((FormationClass)agentSetFormation.FormationIndex) : null);
		}
		agentFromIndex.Formation = formation;
	}

	private void HandleServerEventUseObject(GameNetworkMessage baseMessage)
	{
		UseObject obj = (UseObject)baseMessage;
		UsableMissionObject usableMissionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(obj.UsableGameObjectId) as UsableMissionObject;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(obj.AgentIndex);
		usableMissionObject?.SetUserForClient(agentFromIndex);
	}

	private void HandleServerEventStopUsingObject(GameNetworkMessage baseMessage)
	{
		StopUsingObject stopUsingObject = (StopUsingObject)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(stopUsingObject.AgentIndex)?.StopUsingGameObject(stopUsingObject.IsSuccessful);
	}

	private void HandleServerEventHitSynchronizeObjectHitpoints(GameNetworkMessage baseMessage)
	{
		SyncObjectHitpoints syncObjectHitpoints = (SyncObjectHitpoints)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(syncObjectHitpoints.MissionObjectId);
		if (missionObjectFromMissionObjectId != null)
		{
			missionObjectFromMissionObjectId.GameEntity.GetFirstScriptOfType<DestructableComponent>().HitPoint = syncObjectHitpoints.Hitpoints;
		}
	}

	private void HandleServerEventHitSynchronizeObjectDestructionLevel(GameNetworkMessage baseMessage)
	{
		SyncObjectDestructionLevel syncObjectDestructionLevel = (SyncObjectDestructionLevel)baseMessage;
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(syncObjectDestructionLevel.MissionObjectId)?.GameEntity.GetFirstScriptOfType<DestructableComponent>().SetDestructionLevel(syncObjectDestructionLevel.DestructionLevel, syncObjectDestructionLevel.ForcedIndex, syncObjectDestructionLevel.BlowMagnitude, syncObjectDestructionLevel.BlowPosition, syncObjectDestructionLevel.BlowDirection);
	}

	private void HandleServerEventHitBurstAllHeavyHitParticles(GameNetworkMessage baseMessage)
	{
		Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(((BurstAllHeavyHitParticles)baseMessage).MissionObjectId)?.GameEntity.GetFirstScriptOfType<DestructableComponent>().BurstHeavyHitParticles();
	}

	private void HandleServerEventSynchronizeMissionObject(GameNetworkMessage baseMessage)
	{
		SynchronizeMissionObject synchronizeMissionObject = (SynchronizeMissionObject)baseMessage;
		SynchedMissionObject obj = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(synchronizeMissionObject.MissionObjectId) as SynchedMissionObject;
		(BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) recordPair = synchronizeMissionObject.RecordPair;
		obj.OnAfterReadFromNetwork(recordPair);
	}

	private void HandleServerEventSpawnWeaponWithNewEntity(GameNetworkMessage baseMessage)
	{
		SpawnWeaponWithNewEntity spawnWeaponWithNewEntity = (SpawnWeaponWithNewEntity)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(spawnWeaponWithNewEntity.ParentMissionObjectId);
		GameEntity gameEntity = base.Mission.SpawnWeaponWithNewEntityAux(spawnWeaponWithNewEntity.Weapon, spawnWeaponWithNewEntity.WeaponSpawnFlags, spawnWeaponWithNewEntity.Frame, spawnWeaponWithNewEntity.ForcedIndex, missionObjectFromMissionObjectId, spawnWeaponWithNewEntity.HasLifeTime, spawnWeaponWithNewEntity.SpawnedOnACorpse);
		if (!spawnWeaponWithNewEntity.IsVisible)
		{
			gameEntity.SetVisibilityExcludeParents(visible: false);
		}
	}

	private void HandleServerEventAttachWeaponToSpawnedWeapon(GameNetworkMessage baseMessage)
	{
		AttachWeaponToSpawnedWeapon attachWeaponToSpawnedWeapon = (AttachWeaponToSpawnedWeapon)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(attachWeaponToSpawnedWeapon.MissionObjectId);
		base.Mission.AttachWeaponWithNewEntityToSpawnedWeapon(attachWeaponToSpawnedWeapon.Weapon, missionObjectFromMissionObjectId as SpawnedItemEntity, attachWeaponToSpawnedWeapon.AttachLocalFrame);
	}

	private void HandleServerEventAttachWeaponToAgent(GameNetworkMessage baseMessage)
	{
		AttachWeaponToAgent attachWeaponToAgent = (AttachWeaponToAgent)baseMessage;
		MatrixFrame attachLocalFrame = attachWeaponToAgent.AttachLocalFrame;
		Mission.MissionNetworkHelper.GetAgentFromIndex(attachWeaponToAgent.AgentIndex).AttachWeaponToBone(attachWeaponToAgent.Weapon, null, attachWeaponToAgent.BoneIndex, ref attachLocalFrame);
	}

	private void HandleServerEventHandleMissileCollisionReaction(GameNetworkMessage baseMessage)
	{
		HandleMissileCollisionReaction handleMissileCollisionReaction = (HandleMissileCollisionReaction)baseMessage;
		MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(handleMissileCollisionReaction.AttachedMissionObjectId);
		base.Mission.HandleMissileCollisionReaction(handleMissileCollisionReaction.MissileIndex, handleMissileCollisionReaction.CollisionReaction, handleMissileCollisionReaction.AttachLocalFrame, handleMissileCollisionReaction.IsAttachedFrameLocal, Mission.MissionNetworkHelper.GetAgentFromIndex(handleMissileCollisionReaction.AttackerAgentIndex, canBeNull: true), Mission.MissionNetworkHelper.GetAgentFromIndex(handleMissileCollisionReaction.AttachedAgentIndex, canBeNull: true), handleMissileCollisionReaction.AttachedToShield, handleMissileCollisionReaction.AttachedBoneIndex, missionObjectFromMissionObjectId, handleMissileCollisionReaction.BounceBackVelocity, handleMissileCollisionReaction.BounceBackAngularVelocity, handleMissileCollisionReaction.ForcedSpawnIndex);
	}

	private void HandleServerEventSpawnWeaponAsDropFromAgent(GameNetworkMessage baseMessage)
	{
		SpawnWeaponAsDropFromAgent spawnWeaponAsDropFromAgent = (SpawnWeaponAsDropFromAgent)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(spawnWeaponAsDropFromAgent.AgentIndex);
		Vec3 globalVelocity = spawnWeaponAsDropFromAgent.Velocity;
		Vec3 globalAngularVelocity = spawnWeaponAsDropFromAgent.AngularVelocity;
		base.Mission.SpawnWeaponAsDropFromAgentAux(agentFromIndex, spawnWeaponAsDropFromAgent.EquipmentIndex, ref globalVelocity, ref globalAngularVelocity, spawnWeaponAsDropFromAgent.WeaponSpawnFlags, spawnWeaponAsDropFromAgent.ForcedIndex);
	}

	private void HandleServerEventSpawnAttachedWeaponOnSpawnedWeapon(GameNetworkMessage baseMessage)
	{
		SpawnAttachedWeaponOnSpawnedWeapon spawnAttachedWeaponOnSpawnedWeapon = (SpawnAttachedWeaponOnSpawnedWeapon)baseMessage;
		SpawnedItemEntity spawnedWeapon = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(spawnAttachedWeaponOnSpawnedWeapon.SpawnedWeaponId) as SpawnedItemEntity;
		base.Mission.SpawnAttachedWeaponOnSpawnedWeapon(spawnedWeapon, spawnAttachedWeaponOnSpawnedWeapon.AttachmentIndex, spawnAttachedWeaponOnSpawnedWeapon.ForcedIndex);
	}

	private void HandleServerEventSpawnAttachedWeaponOnCorpse(GameNetworkMessage baseMessage)
	{
		SpawnAttachedWeaponOnCorpse spawnAttachedWeaponOnCorpse = (SpawnAttachedWeaponOnCorpse)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(spawnAttachedWeaponOnCorpse.AgentIndex);
		base.Mission.SpawnAttachedWeaponOnCorpse(agentFromIndex, spawnAttachedWeaponOnCorpse.AttachedIndex, spawnAttachedWeaponOnCorpse.ForcedIndex);
	}

	private void HandleServerEventRemoveEquippedWeapon(GameNetworkMessage baseMessage)
	{
		RemoveEquippedWeapon removeEquippedWeapon = (RemoveEquippedWeapon)baseMessage;
		Mission.MissionNetworkHelper.GetAgentFromIndex(removeEquippedWeapon.AgentIndex).RemoveEquippedWeapon(removeEquippedWeapon.SlotIndex);
	}

	private void HandleServerEventBarkAgent(GameNetworkMessage baseMessage)
	{
		BarkAgent barkAgent = (BarkAgent)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(barkAgent.AgentIndex);
		agentFromIndex.HandleBark(barkAgent.IndexOfBark);
		if (!_chatBox.IsPlayerMuted(agentFromIndex.MissionPeer.Peer.Id))
		{
			GameTexts.SetVariable("LEFT", agentFromIndex.NameTextObject);
			GameTexts.SetVariable("RIGHT", SkinVoiceManager.VoiceType.MpBarks[barkAgent.IndexOfBark].GetName());
			InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_LEFT_colon_RIGHT_wSpaceAfterColon").ToString(), Color.White, "Bark"));
		}
	}

	private void HandleServerEventEquipWeaponWithNewEntity(GameNetworkMessage baseMessage)
	{
		EquipWeaponWithNewEntity equipWeaponWithNewEntity = (EquipWeaponWithNewEntity)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(equipWeaponWithNewEntity.AgentIndex);
		if (agentFromIndex != null)
		{
			MissionWeapon weapon = equipWeaponWithNewEntity.Weapon;
			agentFromIndex.EquipWeaponWithNewEntity(equipWeaponWithNewEntity.SlotIndex, ref weapon);
		}
	}

	private void HandleServerEventAttachWeaponToWeaponInAgentEquipmentSlot(GameNetworkMessage baseMessage)
	{
		AttachWeaponToWeaponInAgentEquipmentSlot attachWeaponToWeaponInAgentEquipmentSlot = (AttachWeaponToWeaponInAgentEquipmentSlot)baseMessage;
		MatrixFrame attachLocalFrame = attachWeaponToWeaponInAgentEquipmentSlot.AttachLocalFrame;
		Mission.MissionNetworkHelper.GetAgentFromIndex(attachWeaponToWeaponInAgentEquipmentSlot.AgentIndex).AttachWeaponToWeapon(attachWeaponToWeaponInAgentEquipmentSlot.SlotIndex, attachWeaponToWeaponInAgentEquipmentSlot.Weapon, null, ref attachLocalFrame);
	}

	private void HandleServerEventEquipWeaponFromSpawnedItemEntity(GameNetworkMessage baseMessage)
	{
		EquipWeaponFromSpawnedItemEntity equipWeaponFromSpawnedItemEntity = (EquipWeaponFromSpawnedItemEntity)baseMessage;
		SpawnedItemEntity spawnedItemEntity = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(equipWeaponFromSpawnedItemEntity.SpawnedItemEntityId) as SpawnedItemEntity;
		Mission.MissionNetworkHelper.GetAgentFromIndex(equipWeaponFromSpawnedItemEntity.AgentIndex, canBeNull: true)?.EquipWeaponFromSpawnedItemEntity(equipWeaponFromSpawnedItemEntity.SlotIndex, spawnedItemEntity, equipWeaponFromSpawnedItemEntity.RemoveWeapon);
	}

	private void HandleServerEventCreateMissile(GameNetworkMessage baseMessage)
	{
		CreateMissile createMissile = (CreateMissile)baseMessage;
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(createMissile.AgentIndex);
		if (createMissile.WeaponIndex != EquipmentIndex.None)
		{
			Vec3 velocity = createMissile.Direction * createMissile.Speed;
			base.Mission.OnAgentShootMissile(agentFromIndex, createMissile.WeaponIndex, createMissile.Position, velocity, createMissile.Orientation, createMissile.HasRigidBody, createMissile.IsPrimaryWeaponShot, createMissile.MissileIndex);
		}
		else
		{
			MissionObject missionObjectFromMissionObjectId = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(createMissile.MissionObjectToIgnoreId);
			base.Mission.AddCustomMissile(agentFromIndex, createMissile.Weapon, createMissile.Position, createMissile.Direction, createMissile.Orientation, createMissile.Speed, createMissile.Speed, createMissile.HasRigidBody, missionObjectFromMissionObjectId, createMissile.MissileIndex);
		}
	}

	private void HandleServerEventAgentHit(GameNetworkMessage baseMessage)
	{
		CombatLogManager.GenerateCombatLog(Mission.MissionNetworkHelper.GetCombatLogDataForCombatLogNetworkMessage((CombatLogNetworkMessage)baseMessage));
	}

	private void HandleServerEventConsumeWeaponAmount(GameNetworkMessage baseMessage)
	{
		ConsumeWeaponAmount consumeWeaponAmount = (ConsumeWeaponAmount)baseMessage;
		(Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(consumeWeaponAmount.SpawnedItemEntityId) as SpawnedItemEntity).ConsumeWeaponAmount(consumeWeaponAmount.ConsumedAmount);
	}

	private void HandleServerEventSetAgentOwningMissionPeer(GameNetworkMessage baseMessage)
	{
		SetAgentOwningMissionPeer setAgentOwningMissionPeer = (SetAgentOwningMissionPeer)baseMessage;
		Agent agent = Mission.Current.FindAgentWithIndex(setAgentOwningMissionPeer.AgentIndex);
		MissionPeer owningAgentMissionPeer = setAgentOwningMissionPeer.Peer?.GetComponent<MissionPeer>();
		agent.SetOwningAgentMissionPeer(owningAgentMissionPeer);
	}

	private bool HandleClientEventSetFollowedAgent(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		SetFollowedAgent setFollowedAgent = (SetFollowedAgent)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component != null)
		{
			Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(setFollowedAgent.AgentIndex, canBeNull: true);
			component.FollowedAgent = agentFromIndex;
		}
		return true;
	}

	private bool HandleClientEventSetMachineRotation(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		SetMachineRotation setMachineRotation = (SetMachineRotation)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		UsableMachine usableMachine = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(setMachineRotation.UsableMachineId) as UsableMachine;
		if (component.IsControlledAgentActive && usableMachine is RangedSiegeWeapon)
		{
			RangedSiegeWeapon rangedSiegeWeapon = usableMachine as RangedSiegeWeapon;
			if (component.ControlledAgent == rangedSiegeWeapon.PilotAgent && rangedSiegeWeapon.PilotAgent != null)
			{
				rangedSiegeWeapon.AimAtRotation(setMachineRotation.HorizontalRotation, setMachineRotation.VerticalRotation);
			}
		}
		return true;
	}

	private bool HandleClientEventRequestUseObject(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		RequestUseObject requestUseObject = (RequestUseObject)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(requestUseObject.UsableMissionObjectId) is UsableMissionObject usableMissionObject && component.ControlledAgent != null && component.ControlledAgent.IsActive())
		{
			Vec3 position = component.ControlledAgent.Position;
			Vec3 globalPosition = usableMissionObject.InteractionEntity.GlobalPosition;
			float num;
			if (usableMissionObject is StandingPoint)
			{
				num = usableMissionObject.GetUserFrameForAgent(component.ControlledAgent).Origin.AsVec2.Distance(component.ControlledAgent.Position.AsVec2);
			}
			else
			{
				usableMissionObject.InteractionEntity.GetPhysicsMinMax(includeChildren: true, out var bbmin, out var bbmax, returnLocal: false);
				float a = globalPosition.Distance(bbmin);
				float b = globalPosition.Distance(bbmax);
				float num2 = TaleWorlds.Library.MathF.Max(a, b);
				num = globalPosition.Distance(new Vec3(position.x, position.y, position.z + component.ControlledAgent.GetEyeGlobalHeight()));
				num -= num2;
				num = TaleWorlds.Library.MathF.Max(num, 0f);
			}
			if (component.ControlledAgent.CurrentlyUsedGameObject != usableMissionObject && component.ControlledAgent.CanReachAndUseObject(usableMissionObject, num * num * 0.9f * 0.9f) && component.ControlledAgent.ObjectHasVacantPosition(usableMissionObject))
			{
				component.ControlledAgent.UseGameObject(usableMissionObject, requestUseObject.UsedObjectPreferenceIndex);
			}
		}
		return true;
	}

	private bool HandleClientEventRequestStopUsingObject(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (RequestStopUsingObject)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component.ControlledAgent?.CurrentlyUsedGameObject != null)
		{
			component.ControlledAgent.StopUsingGameObject(isSuccessful: false);
		}
		return true;
	}

	private bool HandleClientEventApplyOrder(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrder applyOrder = (ApplyOrder)baseMessage;
		GetOrderControllerOfPeer(networkPeer)?.SetOrder(applyOrder.OrderType);
		return true;
	}

	private bool HandleClientEventApplySiegeWeaponOrder(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplySiegeWeaponOrder applySiegeWeaponOrder = (ApplySiegeWeaponOrder)baseMessage;
		GetOrderControllerOfPeer(networkPeer)?.SiegeWeaponController.SetOrder(applySiegeWeaponOrder.OrderType);
		return true;
	}

	private bool HandleClientEventApplyOrderWithPosition(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithPosition applyOrderWithPosition = (ApplyOrderWithPosition)baseMessage;
		OrderController orderControllerOfPeer = GetOrderControllerOfPeer(networkPeer);
		if (orderControllerOfPeer != null)
		{
			orderControllerOfPeer.SetOrderWithPosition(orderPosition: new WorldPosition(base.Mission.Scene, UIntPtr.Zero, applyOrderWithPosition.Position, hasValidZ: false), orderType: applyOrderWithPosition.OrderType);
		}
		return true;
	}

	private bool HandleClientEventApplyOrderWithFormation(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithFormation message = (ApplyOrderWithFormation)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		OrderController orderController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent);
		Formation formation = teamOfPeer?.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.CountOfUnits > 0 && f.Index == message.FormationIndex);
		if (teamOfPeer != null && orderController != null && formation != null)
		{
			orderController.SetOrderWithFormation(message.OrderType, formation);
		}
		return true;
	}

	private bool HandleClientEventApplyOrderWithFormationAndPercentage(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithFormationAndPercentage message = (ApplyOrderWithFormationAndPercentage)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		OrderController orderController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent);
		Formation formation = teamOfPeer?.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.CountOfUnits > 0 && f.Index == message.FormationIndex);
		float percentage = (float)message.Percentage * 0.01f;
		if (teamOfPeer != null && orderController != null && formation != null)
		{
			orderController.SetOrderWithFormationAndPercentage(message.OrderType, formation, percentage);
		}
		return true;
	}

	private bool HandleClientEventApplyOrderWithFormationAndNumber(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithFormationAndNumber message = (ApplyOrderWithFormationAndNumber)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		OrderController orderController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent);
		Formation formation = teamOfPeer?.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.CountOfUnits > 0 && f.Index == message.FormationIndex);
		int number = message.Number;
		if (teamOfPeer != null && orderController != null && formation != null)
		{
			orderController.SetOrderWithFormationAndNumber(message.OrderType, formation, number);
		}
		return true;
	}

	private bool HandleClientEventApplyOrderWithTwoPositions(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithTwoPositions applyOrderWithTwoPositions = (ApplyOrderWithTwoPositions)baseMessage;
		OrderController orderControllerOfPeer = GetOrderControllerOfPeer(networkPeer);
		if (orderControllerOfPeer != null)
		{
			orderControllerOfPeer.SetOrderWithTwoPositions(position1: new WorldPosition(base.Mission.Scene, UIntPtr.Zero, applyOrderWithTwoPositions.Position1, hasValidZ: false), position2: new WorldPosition(base.Mission.Scene, UIntPtr.Zero, applyOrderWithTwoPositions.Position2, hasValidZ: false), orderType: applyOrderWithTwoPositions.OrderType);
		}
		return true;
	}

	private bool HandleClientEventApplyOrderWithGameEntity(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		IOrderable orderWithOrderableObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(((ApplyOrderWithMissionObject)baseMessage).MissionObjectId) as IOrderable;
		GetOrderControllerOfPeer(networkPeer)?.SetOrderWithOrderableObject(orderWithOrderableObject);
		return true;
	}

	private bool HandleClientEventApplyOrderWithAgent(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		ApplyOrderWithAgent applyOrderWithAgent = (ApplyOrderWithAgent)baseMessage;
		OrderController orderControllerOfPeer = GetOrderControllerOfPeer(networkPeer);
		Agent agentFromIndex = Mission.MissionNetworkHelper.GetAgentFromIndex(applyOrderWithAgent.AgentIndex);
		orderControllerOfPeer?.SetOrderWithAgent(applyOrderWithAgent.OrderType, agentFromIndex);
		return true;
	}

	private bool HandleClientEventSelectAllFormations(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (SelectAllFormations)baseMessage;
		GetOrderControllerOfPeer(networkPeer)?.SelectAllFormations();
		return true;
	}

	private bool HandleClientEventSelectAllSiegeWeapons(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (SelectAllSiegeWeapons)baseMessage;
		GetOrderControllerOfPeer(networkPeer)?.SiegeWeaponController.SelectAll();
		return true;
	}

	private bool HandleClientEventClearSelectedFormations(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (ClearSelectedFormations)baseMessage;
		GetOrderControllerOfPeer(networkPeer)?.ClearSelectedFormations();
		return true;
	}

	private bool HandleClientEventSelectFormation(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		SelectFormation message = (SelectFormation)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		OrderController orderController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent);
		Formation formation = teamOfPeer?.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.Index == message.FormationIndex && f.CountOfUnits > 0);
		if (teamOfPeer != null && orderController != null && formation != null)
		{
			orderController.SelectFormation(formation);
		}
		return true;
	}

	private bool HandleClientEventSelectSiegeWeapon(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		SelectSiegeWeapon selectSiegeWeapon = (SelectSiegeWeapon)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		SiegeWeaponController siegeWeaponController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent).SiegeWeaponController;
		SiegeWeapon siegeWeapon = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(selectSiegeWeapon.SiegeWeaponId) as SiegeWeapon;
		if (teamOfPeer != null && siegeWeaponController != null && siegeWeapon != null)
		{
			siegeWeaponController.Select(siegeWeapon);
		}
		return true;
	}

	private bool HandleClientEventUnselectFormation(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		UnselectFormation message = (UnselectFormation)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		OrderController orderController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent);
		Formation formation = teamOfPeer?.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.CountOfUnits > 0 && f.Index == message.FormationIndex);
		if (teamOfPeer != null && orderController != null && formation != null)
		{
			orderController.DeselectFormation(formation);
		}
		return true;
	}

	private bool HandleClientEventUnselectSiegeWeapon(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		UnselectSiegeWeapon unselectSiegeWeapon = (UnselectSiegeWeapon)baseMessage;
		Team teamOfPeer = GetTeamOfPeer(networkPeer);
		SiegeWeaponController siegeWeaponController = teamOfPeer?.GetOrderControllerOf(networkPeer.ControlledAgent).SiegeWeaponController;
		SiegeWeapon siegeWeapon = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(unselectSiegeWeapon.SiegeWeaponId) as SiegeWeapon;
		if (teamOfPeer != null && siegeWeaponController != null && siegeWeapon != null)
		{
			siegeWeaponController.Deselect(siegeWeapon);
		}
		return true;
	}

	private bool HandleClientEventDropWeapon(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		DropWeapon dropWeapon = (DropWeapon)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component?.ControlledAgent != null && component.ControlledAgent.IsActive())
		{
			component.ControlledAgent.HandleDropWeapon(dropWeapon.IsDefendPressed, dropWeapon.ForcedSlotIndexToDropWeaponFrom);
		}
		return true;
	}

	private bool HandleClientEventCheerSelected(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		TauntSelected tauntSelected = (TauntSelected)baseMessage;
		bool result = false;
		if (networkPeer.ControlledAgent != null)
		{
			networkPeer.ControlledAgent.HandleTaunt(tauntSelected.IndexOfTaunt, isDefaultTaunt: false);
			result = true;
		}
		return result;
	}

	private bool HandleClientEventBarkSelected(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		BarkSelected barkSelected = (BarkSelected)baseMessage;
		bool result = false;
		if (networkPeer.ControlledAgent != null)
		{
			networkPeer.ControlledAgent.HandleBark(barkSelected.IndexOfBark);
			result = true;
		}
		return result;
	}

	private bool HandleClientEventBreakAgentVisualsInvulnerability(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (AgentVisualsBreakInvulnerability)baseMessage;
		if (base.Mission == null || base.Mission.GetMissionBehavior<SpawnComponent>() == null || networkPeer.GetComponent<MissionPeer>() == null)
		{
			return false;
		}
		base.Mission.GetMissionBehavior<SpawnComponent>().SetEarlyAgentVisualsDespawning(networkPeer.GetComponent<MissionPeer>());
		return true;
	}

	private bool HandleClientEventRequestToSpawnAsBot(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
	{
		_ = (RequestToSpawnAsBot)baseMessage;
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return false;
		}
		if (component.HasSpawnTimerExpired)
		{
			component.WantsToSpawnAsBot = true;
		}
		return true;
	}

	private void SendExistingObjectsToPeer(NetworkCommunicator networkPeer)
	{
		MBDebug.Print("Sending all existing objects to peer: " + networkPeer.UserName + " with index: " + networkPeer.Index, 0, Debug.DebugColor.White, 17179869184uL);
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new ExistingObjectsBegin());
		GameNetwork.EndModuleEventAsServer();
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new SynchronizeMissionTimeTracker((float)MissionTime.Now.ToSeconds));
		GameNetwork.EndModuleEventAsServer();
		SendTeamsToPeer(networkPeer);
		SendTeamRelationsToPeer(networkPeer);
		foreach (NetworkCommunicator networkPeersIncludingDisconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
		{
			MissionPeer component = networkPeersIncludingDisconnectedPeer.GetComponent<MissionPeer>();
			if (component != null)
			{
				if (component.Team != null)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new SetPeerTeam(networkPeersIncludingDisconnectedPeer, component.Team.TeamIndex));
					GameNetwork.EndModuleEventAsServer();
				}
				if (component.Culture != null)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new ChangeCulture(component, component.Culture));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
		SendFormationInformation(networkPeer);
		SendAgentsToPeer(networkPeer);
		SendSpawnedMissionObjectsToPeer(networkPeer);
		SynchronizeMissionObjectsToPeer(networkPeer);
		SendMissilesToPeer(networkPeer);
		SendTroopSelectionInformation(networkPeer);
		networkPeer.SendExistingObjects(base.Mission);
		GameNetwork.BeginModuleEventAsServer(networkPeer);
		GameNetwork.WriteMessage(new ExistingObjectsEnd());
		GameNetwork.EndModuleEventAsServer();
	}

	private void SendTroopSelectionInformation(NetworkCommunicator networkPeer)
	{
		foreach (NetworkCommunicator networkPeersIncludingDisconnectedPeer in GameNetwork.NetworkPeersIncludingDisconnectedPeers)
		{
			MissionPeer component = networkPeersIncludingDisconnectedPeer.GetComponent<MissionPeer>();
			if (component != null && component.SelectedTroopIndex != 0)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkPeersIncludingDisconnectedPeer, component.SelectedTroopIndex));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void SendTeamsToPeer(NetworkCommunicator networkPeer)
	{
		foreach (Team team in base.Mission.Teams)
		{
			MBDebug.Print("Syncing a team to peer: " + networkPeer.UserName + " with index: " + networkPeer.Index, 0, Debug.DebugColor.White, 17179869184uL);
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new AddTeam(team.TeamIndex, team.Side, team.Color, team.Color2, (team.Banner != null) ? team.Banner.BannerCode : string.Empty, team.IsPlayerGeneral, team.IsPlayerSergeant));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	private void SendTeamRelationsToPeer(NetworkCommunicator networkPeer)
	{
		int count = base.Mission.Teams.Count;
		for (int i = 0; i < count; i++)
		{
			for (int j = i; j < count; j++)
			{
				Team team = base.Mission.Teams[i];
				Team team2 = base.Mission.Teams[j];
				if (team.IsEnemyOf(team2))
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new TeamSetIsEnemyOf(team.TeamIndex, team2.TeamIndex, isEnemyOf: true));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
	}

	private void SendFormationInformation(NetworkCommunicator networkPeer)
	{
		MBDebug.Print("formations sending begin-", 0, Debug.DebugColor.White, 17179869184uL);
		foreach (Team team in base.Mission.Teams)
		{
			if (!team.IsValid || team.Side == BattleSideEnum.None)
			{
				continue;
			}
			foreach (Formation item in team.FormationsIncludingEmpty)
			{
				if (!string.IsNullOrEmpty(item.BannerCode))
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new InitializeFormation(item, team.TeamIndex, item.BannerCode));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
		if (!networkPeer.IsServerPeer)
		{
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new SetSpawnedFormationCount(base.Mission.NumOfFormationsSpawnedTeamOne, base.Mission.NumOfFormationsSpawnedTeamTwo));
			GameNetwork.EndModuleEventAsServer();
		}
		MBDebug.Print("formations sending end-", 0, Debug.DebugColor.White, 17179869184uL);
	}

	private void SendAgentVisualsToPeer(NetworkCommunicator networkPeer, Team peerTeam)
	{
		MBDebug.Print("agentvisuals sending begin-", 0, Debug.DebugColor.White, 17179869184uL);
		foreach (MissionPeer item in from p in GameNetwork.NetworkPeers
			select p.GetComponent<MissionPeer>() into pr
			where pr != null
			select pr)
		{
			if (item.Team == peerTeam)
			{
				int amountOfAgentVisualsForPeer = item.GetAmountOfAgentVisualsForPeer();
				for (int num = 0; num < amountOfAgentVisualsForPeer; num++)
				{
					PeerVisualsHolder visuals = item.GetVisuals(num);
					IAgentVisual agentVisuals = visuals.AgentVisuals;
					MatrixFrame frame = agentVisuals.GetFrame();
					AgentBuildData agentBuildData = new AgentBuildData(MBObjectManager.Instance.GetObject<BasicCharacterObject>(agentVisuals.GetCharacterObjectID())).MissionPeer(item).Equipment(agentVisuals.GetEquipment()).VisualsIndex(visuals.VisualsIndex)
						.Team(item.Team)
						.InitialPosition(in frame.origin)
						.InitialDirection(frame.rotation.f.AsVec2.Normalized())
						.IsFemale(agentVisuals.GetIsFemale())
						.BodyProperties(agentVisuals.GetBodyProperties());
					networkPeer.GetComponent<MissionPeer>();
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new CreateAgentVisuals(item.GetNetworkPeer(), agentBuildData, item.SelectedTroopIndex));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
		MBDebug.Print("agentvisuals sending end-", 0, Debug.DebugColor.White, 17179869184uL);
	}

	private void SendAgentsToPeer(NetworkCommunicator networkPeer)
	{
		MBDebug.Print("agents sending begin-", 0, Debug.DebugColor.White, 17179869184uL);
		foreach (Agent agent in base.Mission.AllAgents)
		{
			bool isMount = agent.IsMount;
			bool num = agent.IsAddedAsCorpse();
			AgentState state = agent.State;
			if (num || (state != AgentState.Active && ((state != AgentState.Killed && state != AgentState.Unconscious) || (agent.GetAttachedWeaponsCount() <= 0 && (isMount || (agent.GetPrimaryWieldedItemIndex() < EquipmentIndex.WeaponItemBeginSlot && agent.GetOffhandWieldedItemIndex() < EquipmentIndex.WeaponItemBeginSlot)) && !base.Mission.IsAgentInProximityMap(agent))) && !base.Mission.MissilesList.Any((Mission.Missile m) => m.ShooterAgent == agent)))
			{
				continue;
			}
			if (isMount && agent.RiderAgent == null)
			{
				MBDebug.Print("mount sending " + agent.Index, 0, Debug.DebugColor.White, 17179869184uL);
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new CreateFreeMountAgent(agent, agent.Position, agent.GetMovementDirection()));
				GameNetwork.EndModuleEventAsServer();
				agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);
				int attachedWeaponsCount = agent.GetAttachedWeaponsCount();
				for (int num2 = 0; num2 < attachedWeaponsCount; num2++)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new AttachWeaponToAgent(agent.GetAttachedWeapon(num2), agent.Index, agent.GetAttachedWeaponBoneIndex(num2), agent.GetAttachedWeaponFrame(num2)));
					GameNetwork.EndModuleEventAsServer();
				}
				if (!agent.IsActive())
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, state == AgentState.Killed, agent.GetCurrentAction(0)));
					GameNetwork.EndModuleEventAsServer();
				}
			}
			else if (!isMount)
			{
				MBDebug.Print("human sending " + agent.Index, 0, Debug.DebugColor.White, 17179869184uL);
				Agent agent2 = agent.MountAgent;
				if (agent2 != null && agent2.RiderAgent == null)
				{
					agent2 = null;
				}
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new CreateAgent(agent.Index, agent.Character, agent.Monster, agent.SpawnEquipment, agent.Equipment, agent.BodyPropertiesValue, agent.BodyPropertiesSeed, agent.IsFemale, agent.Team?.TeamIndex ?? (-1), agent.Formation?.Index ?? (-1), agent.ClothingColor1, agent.ClothingColor2, agent2?.Index ?? (-1), agent.MountAgent?.SpawnEquipment, agent.MissionPeer != null && agent.OwningAgentMissionPeer == null, agent.Position, agent.GetMovementDirection(), agent.MissionPeer?.GetNetworkPeer() ?? agent.OwningAgentMissionPeer?.GetNetworkPeer()));
				GameNetwork.EndModuleEventAsServer();
				agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);
				agent2?.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);
				for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
				{
					for (int num3 = 0; num3 < agent.Equipment[equipmentIndex].GetAttachedWeaponsCount(); num3++)
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new AttachWeaponToWeaponInAgentEquipmentSlot(agent.Equipment[equipmentIndex].GetAttachedWeapon(num3), agent.Index, equipmentIndex, agent.Equipment[equipmentIndex].GetAttachedWeaponFrame(num3)));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				int attachedWeaponsCount2 = agent.GetAttachedWeaponsCount();
				for (int num4 = 0; num4 < attachedWeaponsCount2; num4++)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new AttachWeaponToAgent(agent.GetAttachedWeapon(num4), agent.Index, agent.GetAttachedWeaponBoneIndex(num4), agent.GetAttachedWeaponFrame(num4)));
					GameNetwork.EndModuleEventAsServer();
				}
				if (agent2 != null)
				{
					attachedWeaponsCount2 = agent2.GetAttachedWeaponsCount();
					for (int num5 = 0; num5 < attachedWeaponsCount2; num5++)
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new AttachWeaponToAgent(agent2.GetAttachedWeapon(num5), agent2.Index, agent2.GetAttachedWeaponBoneIndex(num5), agent2.GetAttachedWeaponFrame(num5)));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				EquipmentIndex primaryWieldedItemIndex = agent.GetPrimaryWieldedItemIndex();
				int mainHandCurUsageIndex = ((primaryWieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[primaryWieldedItemIndex].CurrentUsageIndex : 0);
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, isLeftHand: false, isWieldedInstantly: true, isWieldedOnSpawn: true, primaryWieldedItemIndex, mainHandCurUsageIndex));
				GameNetwork.EndModuleEventAsServer();
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, isLeftHand: true, isWieldedInstantly: true, isWieldedOnSpawn: true, agent.GetOffhandWieldedItemIndex(), mainHandCurUsageIndex));
				GameNetwork.EndModuleEventAsServer();
				MBActionSet actionSet = agent.ActionSet;
				if (actionSet.IsValid)
				{
					AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(actionSet, agent.Character.GetStepSize(), hasClippingPlane: false);
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new SetAgentActionSet(agent.Index, animationSystemData));
					GameNetwork.EndModuleEventAsServer();
					if (!agent.IsActive())
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, state == AgentState.Killed, agent.GetCurrentAction(0)));
						GameNetwork.EndModuleEventAsServer();
					}
				}
				else
				{
					actionSet = MBActionSet.GetActionSet("as_human_warrior");
					AnimationSystemData animationSystemData2 = agent.Monster.FillAnimationSystemData(actionSet, agent.Character.GetStepSize(), hasClippingPlane: false);
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new SetAgentActionSet(agent.Index, animationSystemData2));
					GameNetwork.EndModuleEventAsServer();
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, state == AgentState.Killed, ActionIndexCache.act_death_by_arrow_pelvis));
					GameNetwork.EndModuleEventAsServer();
				}
			}
			else
			{
				MBDebug.Print("agent not sending " + agent.Index, 0, Debug.DebugColor.White, 17179869184uL);
			}
		}
		MBDebug.Print("agents sending end-", 0, Debug.DebugColor.White, 17179869184uL);
	}

	private void SendSpawnedMissionObjectsToPeer(NetworkCommunicator networkPeer)
	{
		foreach (MissionObject missionObject in base.Mission.MissionObjects)
		{
			if (missionObject is SpawnedItemEntity { GameEntity: { Parent: var parent } gameEntity } spawnedItemEntity)
			{
				if (parent.IsValid && gameEntity.Parent.HasScriptOfType<SpawnedItemEntity>())
				{
					continue;
				}
				MissionObject missionObject2 = null;
				if (spawnedItemEntity.GameEntity.Parent.IsValid)
				{
					missionObject2 = gameEntity.Parent.GetFirstScriptOfType<MissionObject>();
				}
				MatrixFrame frame = gameEntity.GetGlobalFrame();
				if (missionObject2 != null)
				{
					frame = missionObject2.GameEntity.GetGlobalFrame().TransformToLocalNonOrthogonal(in frame);
				}
				frame.origin.z = TaleWorlds.Library.MathF.Max(frame.origin.z, CompressionBasic.PositionCompressionInfo.GetMinimumValue() + 1f);
				Mission.WeaponSpawnFlags weaponSpawnFlags = spawnedItemEntity.SpawnFlags;
				if (weaponSpawnFlags.HasAnyFlag(Mission.WeaponSpawnFlags.WithPhysics) && !gameEntity.GetPhysicsState())
				{
					weaponSpawnFlags = (Mission.WeaponSpawnFlags)(((uint)weaponSpawnFlags & 0xFFFFFFF7u) | 0x10);
				}
				bool hasLifeTime = true;
				bool isVisible = !spawnedItemEntity.SpawnedOnACorpse && (!gameEntity.Parent.IsValid || missionObject2 != null);
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SpawnWeaponWithNewEntity(spawnedItemEntity.WeaponCopy, weaponSpawnFlags, spawnedItemEntity.Id.Id, frame, missionObject2?.Id ?? MissionObjectId.Invalid, isVisible, hasLifeTime, spawnedItemEntity.SpawnedOnACorpse));
				GameNetwork.EndModuleEventAsServer();
				for (int i = 0; i < spawnedItemEntity.WeaponCopy.GetAttachedWeaponsCount(); i++)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new AttachWeaponToSpawnedWeapon(spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i), spawnedItemEntity.Id, spawnedItemEntity.WeaponCopy.GetAttachedWeaponFrame(i)));
					GameNetwork.EndModuleEventAsServer();
					if (spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i).Item.ItemFlags.HasAnyFlag(ItemFlags.CanBePickedUpFromCorpse))
					{
						if (!gameEntity.GetChild(i).IsValid)
						{
							Debug.Print("spawnedItemGameEntity child is null. item: " + spawnedItemEntity.WeaponCopy.Item.StringId + " attached item: " + spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i).Item.StringId + " attachment index: " + i);
						}
						else if (gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>() == null)
						{
							Debug.Print("spawnedItemGameEntity child SpawnedItemEntity script is null. item: " + spawnedItemEntity.WeaponCopy.Item.StringId + " attached item: " + spawnedItemEntity.WeaponCopy.GetAttachedWeapon(i).Item.StringId + " attachment index: " + i);
						}
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new SpawnAttachedWeaponOnSpawnedWeapon(spawnedItemEntity.Id, i, gameEntity.GetChild(i).GetFirstScriptOfType<SpawnedItemEntity>().Id.Id));
						GameNetwork.EndModuleEventAsServer();
					}
				}
			}
			else if (missionObject.CreatedAtRuntime)
			{
				Mission.DynamicallyCreatedEntity dynamicallyCreatedEntity = base.Mission.AddedEntitiesInfo.SingleOrDefault((Mission.DynamicallyCreatedEntity x) => x.ObjectId == missionObject.Id);
				if (dynamicallyCreatedEntity != null)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new CreateMissionObject(dynamicallyCreatedEntity.ObjectId, dynamicallyCreatedEntity.Prefab, dynamicallyCreatedEntity.Frame, dynamicallyCreatedEntity.ChildObjectIds));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
	}

	private void SynchronizeMissionObjectsToPeer(NetworkCommunicator networkPeer)
	{
		foreach (MissionObject missionObject in base.Mission.MissionObjects)
		{
			if (missionObject is SynchedMissionObject synchedMissionObject)
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SynchronizeMissionObject(synchedMissionObject));
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}

	private void SendMissilesToPeer(NetworkCommunicator networkPeer)
	{
		foreach (Mission.Missile missiles in base.Mission.MissilesList)
		{
			Vec3 velocity = missiles.GetVelocity();
			float speed = velocity.Normalize();
			Mat3 identity = Mat3.Identity;
			identity.f = velocity;
			identity.Orthonormalize();
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new CreateMissile(missiles.Index, missiles.ShooterAgent.Index, EquipmentIndex.None, missiles.Weapon, missiles.GetPosition(), velocity, speed, identity, missiles.GetHasRigidBody(), missiles.MissionObjectToIgnore?.Id ?? MissionObjectId.Invalid, isPrimaryWeaponShot: false));
			GameNetwork.EndModuleEventAsServer();
		}
	}

	public override void OnPlayerDisconnectedFromServer(NetworkCommunicator networkPeer)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component != null && component.HasSpawnedAgentVisuals)
		{
			base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component);
			component.HasSpawnedAgentVisuals = false;
		}
	}

	protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		if (!networkPeer.IsServerPeer)
		{
			foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
			{
				if (networkPeer2.IsSynchronized || networkPeer2.JustReconnecting)
				{
					networkPeer2.VirtualPlayer.SynchronizeComponentsTo(networkPeer.VirtualPlayer);
				}
			}
			foreach (NetworkCommunicator disconnectedNetworkPeer in GameNetwork.DisconnectedNetworkPeers)
			{
				disconnectedNetworkPeer.VirtualPlayer.SynchronizeComponentsTo(networkPeer.VirtualPlayer);
			}
		}
		MissionPeer missionPeer = networkPeer.AddComponent<MissionPeer>();
		if (networkPeer.JustReconnecting && missionPeer.Team != null)
		{
			MBAPI.IMBPeer.SetTeam(networkPeer.Index, missionPeer.Team.MBTeam.Index);
		}
		missionPeer.JoinTime = DateTime.Now;
	}

	protected override void HandleLateNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		if (!networkPeer.IsServerPeer)
		{
			SendExistingObjectsToPeer(networkPeer);
		}
	}

	protected override void HandleEarlyPlayerDisconnect(NetworkCommunicator networkPeer)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component != null)
		{
			base.Mission?.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component, sync: true);
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(component.GetNetworkPeer()));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			component.HasSpawnedAgentVisuals = false;
		}
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
	}

	protected override void HandlePlayerDisconnect(NetworkCommunicator networkPeer)
	{
		MissionPeer component = networkPeer.GetComponent<MissionPeer>();
		if (component == null)
		{
			return;
		}
		if (component.ControlledAgent != null)
		{
			Agent controlledAgent = component.ControlledAgent;
			Blow b = new Blow(controlledAgent.Index);
			b.WeaponRecord = default(BlowWeaponRecord);
			b.DamageType = DamageTypes.Invalid;
			b.BaseMagnitude = 10000f;
			b.WeaponRecord.WeaponClass = WeaponClass.Undefined;
			b.GlobalPosition = controlledAgent.Position;
			b.DamagedPercentage = 1f;
			controlledAgent.Die(b);
		}
		if (base.Mission.AllAgents != null)
		{
			foreach (Agent allAgent in base.Mission.AllAgents)
			{
				if (allAgent.MissionPeer == component)
				{
					allAgent.MissionPeer = null;
				}
				if (allAgent.OwningAgentMissionPeer == component)
				{
					allAgent.SetOwningAgentMissionPeer(null);
				}
			}
		}
		if (component.ControlledFormation != null)
		{
			component.ControlledFormation.PlayerOwner = null;
		}
	}

	public override void OnAddTeam(Team team)
	{
		base.OnAddTeam(team);
		if (GameNetwork.IsServerOrRecorder)
		{
			MBDebug.Print("----------OnAddTeam-");
			MBDebug.Print("Adding a team and sending it to all clients", 0, Debug.DebugColor.White, 17179869184uL);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new AddTeam(team.TeamIndex, team.Side, team.Color, team.Color2, (team.Banner != null) ? team.Banner.BannerCode : string.Empty, team.IsPlayerGeneral, team.IsPlayerSergeant));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
		else if (team.Side != BattleSideEnum.Attacker && team.Side != BattleSideEnum.Defender && base.Mission.SpectatorTeam == null)
		{
			base.Mission.SpectatorTeam = team;
		}
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_chatBox = Game.Current.GetGameHandler<ChatBox>();
	}

	public override void OnClearScene()
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			MBDebug.Print("I am clearing the scene, and sending this message to all clients", 0, Debug.DebugColor.White, 17179869184uL);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ClearMission());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			_accumulatedTimeSinceLastTimerSync += dt;
			if (_accumulatedTimeSinceLastTimerSync > 2f)
			{
				_accumulatedTimeSinceLastTimerSync -= 2f;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SynchronizeMissionTimeTracker((float)MissionTime.Now.ToSeconds));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			networkPeer.GetComponent<MissionRepresentativeBase>()?.Tick(dt);
			if (GameNetwork.IsServer && !networkPeer.IsServerPeer && !MultiplayerOptions.OptionType.DisableInactivityKick.GetBoolValue())
			{
				networkPeer.GetComponent<MissionPeer>()?.TickInactivityStatus();
			}
		}
	}

	protected override void OnEndMission()
	{
		if (GameNetwork.IsServer)
		{
			foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
			{
				item.ControlledAgent = null;
			}
			foreach (Agent allAgent in base.Mission.AllAgents)
			{
				allAgent.MissionPeer = null;
			}
		}
		base.OnEndMission();
	}

	public void OnPeerSelectedTeam(MissionPeer missionPeer)
	{
		SendAgentVisualsToPeer(missionPeer.GetNetworkPeer(), missionPeer.Team);
	}

	public void OnClientSynchronized(NetworkCommunicator networkPeer)
	{
		this.OnClientSynchronizedEvent?.Invoke(networkPeer);
		if (networkPeer.IsMine)
		{
			this.OnMyClientSynchronized?.Invoke();
		}
	}
}
