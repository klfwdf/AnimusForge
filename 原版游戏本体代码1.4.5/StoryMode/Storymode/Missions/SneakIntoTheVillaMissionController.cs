using System.Collections.Generic;
using SandBox;
using SandBox.Missions;
using SandBox.Missions.AgentBehaviors;
using SandBox.Objects;
using SandBox.Objects.Usables;
using StoryMode.Quests.TutorialPhase;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Storymode.Missions;

public class SneakIntoTheVillaMissionController : MissionLogic
{
	public enum MissionState
	{
		Start,
		Crouch,
		WalkSlow,
		HideInBushes,
		HideInBushesEnd,
		Distraction,
		DarkZone,
		DarkZoneEnd,
		StealthKill,
		HideCorpse,
		End
	}

	private Dictionary<MissionState, VolumeBox> _volumeBoxes = new Dictionary<MissionState, VolumeBox>();

	private const string FirstDoorId = "doors_before_convo";

	private const string SecondDoorId = "doors_after_convo";

	private const string DistractionAgentSpawnPointId = "sp_agent_distraction";

	private const string StealthKillAgentSpawnPointId = "sp_agent_stealth_kill";

	private const string HeadmanSpawnPoint = "sp_captive";

	private VillagersInNeed _talkToVillagersQuest;

	private MissionTimer _missionEndTimer;

	private Agent _distractionTargetAgent;

	private Agent _stealthKillTargetAgent;

	private bool _isStealthAttackComplete;

	public bool AreVisualsDirty;

	public static SneakIntoTheVillaMissionController Instance { get; private set; }

	public MissionState State { get; private set; }

	public Agent HeadmanAgent { get; private set; }

	public override void OnMissionTick(float dt)
	{
		if (_missionEndTimer != null && _missionEndTimer.Check())
		{
			base.Mission.EndMission();
			_missionEndTimer = null;
		}
		CheckTriggers();
	}

	public override void OnCreated()
	{
		Instance = this;
		base.Mission.DoesMissionRequireCivilianEquipment = true;
		_talkToVillagersQuest = (VillagersInNeed)Campaign.Current.QuestManager.Quests.FirstOrDefaultQ((QuestBase x) => x is VillagersInNeed);
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		base.Mission.IsInventoryAccessible = false;
		base.Mission.IsQuestScreenAccessible = true;
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: false, noHorses: true);
		MBEquipmentRoster mBEquipmentRoster = MBObjectManager.Instance.GetObject<MBEquipmentRoster>("stealth_tutorial_set_player");
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(mBEquipmentRoster.DefaultEquipment);
		SpawnStealthAgents();
		SpawnHeadman();
		InitializeVolumeBoxes();
		base.Mission.GetMissionBehavior<StealthFailCounterMissionLogic>().SetFailTexts(null, new TextObject("{=eJ3iAJ8U}You alerted the bandits. The camp erupts in confusion, but in the darkness you are able to slip away. You watch from a distance as the chaos and noise die down, and you sense that it won’t be long before this ill-disciplined gang relaxes their guard, giving you another chance. When you are ready, you can return to Tevea and try again."));
		Game.Current.EventManager.RegisterEvent<OnStealthMissionCounterFailedEvent>(OnCaughtInStealthZone);
	}

	public static bool IsStealthTutorialReadyForActivation(MissionState missionState)
	{
		if (Mission.Current != null && Instance != null)
		{
			if (missionState != MissionState.Start)
			{
				return missionState <= Instance.State;
			}
			return true;
		}
		return false;
	}

	public static bool IsStealthTutorialReadyForCompletion(MissionState missionState)
	{
		if (Instance != null && missionState < MissionState.End)
		{
			return Instance.State > missionState;
		}
		return false;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		Instance = null;
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<OnStealthMissionCounterFailedEvent>(OnCaughtInStealthZone);
	}

	private void OnCaughtInStealthZone(OnStealthMissionCounterFailedEvent stealthFailedEvent)
	{
		_talkToVillagersQuest.OnRescueMissionFailed();
	}

	private void OnMainAgentIsWounded()
	{
		_talkToVillagersQuest.OnRescueMissionFailed();
		_missionEndTimer = new MissionTimer(2f);
	}

	private void ShowMissionFailedPopup()
	{
		TextObject textObject = new TextObject("{=DM6luo3c}Continue");
		InformationManager.ShowInquiry(new InquiryData(new TextObject("{=wQbfWNZO}Mission Failed!").ToString(), new TextObject("{=45IBacqS}You are knocked to the ground, but in the confusion and darkness you are able to crawl away. You watch from a distance as the chaos and noise in the hideout die down, and you sense that it won’t be long before this ill-disciplined gang relaxes their guard, giving you another chance. When you are ready, you can return to Tevea and try again.").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, textObject.ToString(), null, delegate
		{
			OnMainAgentIsWounded();
		}, null), pauseGameActiveState: true);
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (affectedAgent.IsMainAgent)
		{
			ShowMissionFailedPopup();
		}
	}

	public void OnAfterTalkingToPrisoner()
	{
		GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("doors_before_convo");
		GameEntity gameEntity2 = base.Mission.Scene.FindEntityWithTag("doors_after_convo");
		gameEntity.SetVisibilityExcludeParents(visible: false);
		gameEntity2.SetVisibilityExcludeParents(visible: true);
		List<GameEntity> entities = new List<GameEntity>();
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<Passage>(ref entities);
		foreach (GameEntity item in entities)
		{
			Passage firstScriptOfType = item.GetFirstScriptOfType<Passage>();
			firstScriptOfType.SetEnabled();
			firstScriptOfType.PilotStandingPoint.IsDeactivated = false;
		}
		AreVisualsDirty = true;
	}

	public override void OnObjectUsed(Agent userAgent, UsableMissionObject usedObject)
	{
		if (userAgent.IsMainAgent && usedObject is PassageUsePoint { IsMissionExit: not false })
		{
			_talkToVillagersQuest.OnHeadmanRescued();
		}
	}

	private void SpawnHeadman()
	{
		GameEntity spawnPoint = base.Mission.Scene.FindEntityWithTag("sp_captive");
		HeadmanAgent = SpawnAgent(_talkToVillagersQuest.Headman, spawnPoint, Team.Invalid);
	}

	private void SpawnStealthAgents()
	{
		CharacterObject characterObject = MBObjectManager.Instance.GetObject<CharacterObject>("mountain_bandits_raider");
		List<GameEntity> entities = new List<GameEntity>();
		base.Mission.Scene.GetAllEntitiesWithScriptComponent<DynamicPatrolAreaParent>(ref entities);
		MBActionSet actionSet = MBGlobals.GetActionSet("as_human_hideout_bandit");
		foreach (GameEntity item in entities)
		{
			foreach (GameEntity child in item.GetChildren())
			{
				PatrolPoint firstScriptOfType = child.GetChild(0).GetFirstScriptOfType<PatrolPoint>();
				if (firstScriptOfType.SpawnGroupTag == "stealth_agent")
				{
					Agent agent = SpawnAgent(characterObject, child, base.Mission.PlayerEnemyTeam);
					AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().CreateAgentNavigator();
					SandBoxManager.Instance.AgentBehaviorManager.AddStealthAgentBehaviors(agent);
					AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(actionSet, characterObject.GetStepSize(), hasClippingPlane: false);
					agent.SetActionSet(ref animationSystemData);
					AgentFlag agentFlags = agent.GetAgentFlags();
					agent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
					agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().GetBehavior<PatrolAgentBehavior>().SetDynamicPatrolArea(item);
					if (firstScriptOfType.GameEntity.HasTag("sp_agent_distraction"))
					{
						_distractionTargetAgent = agent;
					}
					if (firstScriptOfType.GameEntity.HasTag("sp_agent_stealth_kill"))
					{
						_stealthKillTargetAgent = agent;
					}
				}
			}
		}
	}

	private void CheckTriggers()
	{
		if (State >= MissionState.End || Agent.Main == null)
		{
			return;
		}
		for (MissionState missionState = State + 1; missionState < MissionState.End; missionState++)
		{
			if (_volumeBoxes[missionState].IsPointIn(Agent.Main.Position))
			{
				State = missionState;
				break;
			}
		}
	}

	public bool IsTargetAgentDistracted()
	{
		if (State == MissionState.Distraction)
		{
			if (_distractionTargetAgent != null && !_distractionTargetAgent.IsCautious())
			{
				return _distractionTargetAgent.IsAlarmed();
			}
			return true;
		}
		return false;
	}

	public bool IsTargetAgentKilled()
	{
		if (State == MissionState.StealthKill)
		{
			if (!_isStealthAttackComplete)
			{
				return _stealthKillTargetAgent == null;
			}
			return true;
		}
		return false;
	}

	public bool IsMainAgentDraggingTargetBody()
	{
		if (_stealthKillTargetAgent != null && _stealthKillTargetAgent.IsAddedAsCorpse() && Agent.Main != null && Agent.Main.IsActive())
		{
			return (Agent.Main.GetScriptedFlags() & Agent.AIScriptedFrameFlags.Drag) == Agent.AIScriptedFrameFlags.Drag;
		}
		return false;
	}

	public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
	{
		if (!_isStealthAttackComplete && _stealthKillTargetAgent != null && collisionData.IsSneakAttack && victim == _stealthKillTargetAgent)
		{
			_isStealthAttackComplete = true;
		}
	}

	private void InitializeVolumeBoxes()
	{
		_volumeBoxes = new Dictionary<MissionState, VolumeBox>();
		_volumeBoxes[MissionState.Crouch] = base.Mission.Scene.FindEntityWithTag("trigger_volume_crouch").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.WalkSlow] = base.Mission.Scene.FindEntityWithTag("trigger_volume_walk_slowly").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.HideInBushes] = base.Mission.Scene.FindEntityWithTag("trigger_volume_stealthbox").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.HideInBushesEnd] = base.Mission.Scene.FindEntityWithTag("end_trigger_stealthbox").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.Distraction] = base.Mission.Scene.FindEntityWithTag("trigger_volume_distraction").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.DarkZone] = base.Mission.Scene.FindEntityWithTag("trigger_volume_dark_zone").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.DarkZoneEnd] = base.Mission.Scene.FindEntityWithTag("end_trigger_darkness").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.StealthKill] = base.Mission.Scene.FindEntityWithTag("trigger_volume_stealth_kill").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.HideCorpse] = base.Mission.Scene.FindEntityWithTag("trigger_volume_hide_corpse").GetFirstScriptOfType<VolumeBox>();
		_volumeBoxes[MissionState.End] = base.Mission.Scene.FindEntityWithTag("trigger_volume_passage").GetFirstScriptOfType<VolumeBox>();
	}

	private Agent SpawnAgent(CharacterObject character, GameEntity spawnPoint, Team team)
	{
		MatrixFrame globalFrame = spawnPoint.GetGlobalFrame();
		AgentBuildData agentBuildData = new AgentBuildData(character).NoHorses(noHorses: true).InitialPosition(in globalFrame.origin).InitialDirection(globalFrame.rotation.f.AsVec2.Normalized())
			.CivilianEquipment(civilianEquipment: true)
			.Team(team)
			.TroopOrigin(new SimpleAgentOrigin(character));
		return Mission.Current.SpawnAgent(agentBuildData);
	}
}
