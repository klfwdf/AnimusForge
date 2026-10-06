using System;
using System.Collections.Generic;
using Helpers;
using SandBox.Conversation;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class CampaignMissionComponent : MissionLogic, ICampaignMission
{
	private class AgentConversationState
	{
		private StackArray.StackArray2Bool _actionAtChannelModified;

		public Agent Agent { get; private set; }

		public AgentConversationState(Agent agent)
		{
			Agent = agent;
			_actionAtChannelModified = default(StackArray.StackArray2Bool);
			_actionAtChannelModified[0] = false;
			_actionAtChannelModified[1] = false;
		}

		public bool IsChannelModified(int channelNo)
		{
			return _actionAtChannelModified[channelNo];
		}

		public void SetChannelModified(int channelNo)
		{
			_actionAtChannelModified[channelNo] = true;
		}
	}

	private MissionState _state;

	private SoundEvent _soundEvent;

	private Agent _currentAgent;

	private bool _isMainAgentAnimationSet;

	private readonly Dictionary<Agent, int> _agentSoundEvents = new Dictionary<Agent, int>();

	private readonly List<AgentConversationState> _conversationAgents = new List<AgentConversationState>();

	public GameState State => _state;

	public IMissionTroopSupplier AgentSupplier { get; set; }

	public Location Location { get; set; }

	public Alley LastVisitedAlley { get; set; }

	MissionMode ICampaignMission.Mode => base.Mission.Mode;

	void ICampaignMission.SetMissionMode(MissionMode newMode, bool atStart)
	{
		base.Mission.SetMissionMode(newMode, atStart);
	}

	public override void OnAgentCreated(Agent agent)
	{
		base.OnAgentCreated(agent);
		agent.AddComponent(new CampaignAgentComponent(agent));
		CharacterObject characterObject = (CharacterObject)agent.Character;
		if (characterObject?.HeroObject != null && characterObject.HeroObject.IsPlayerCompanion)
		{
			agent.AgentRole = new TextObject("{=kPTp6TPT}({AGENT_ROLE})");
			agent.AgentRole.SetTextVariable("AGENT_ROLE", GameTexts.FindText("str_companion"));
		}
	}

	public override void OnPreDisplayMissionTick(float dt)
	{
		base.OnPreDisplayMissionTick(dt);
		if (_soundEvent != null && !_soundEvent.IsPlaying())
		{
			RemovePreviousAgentsSoundEvent();
			_soundEvent.Stop();
			_soundEvent = null;
		}
	}

	public override void OnMissionTick(float dt)
	{
		base.OnMissionTick(dt);
		if (Campaign.Current != null)
		{
			CampaignEventDispatcher.Instance.MissionTick(dt);
		}
	}

	protected override void OnObjectDisabled(DestructableComponent missionObject)
	{
		SiegeWeapon firstScriptOfType = missionObject.GameEntity.GetFirstScriptOfType<SiegeWeapon>();
		if (firstScriptOfType != null && Campaign.Current != null && Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			CampaignSiegeStateHandler missionBehavior = Mission.Current.GetMissionBehavior<CampaignSiegeStateHandler>();
			if (missionBehavior != null && missionBehavior.IsSallyOut)
			{
				ISiegeEventSide siegeEventSide = missionBehavior.Settlement.SiegeEvent.GetSiegeEventSide(firstScriptOfType.Side);
				siegeEventSide.SiegeEvent.BreakSiegeEngine(siegeEventSide, firstScriptOfType.GetSiegeEngineType());
			}
		}
		base.OnObjectDisabled(missionObject);
	}

	public override void EarlyStart()
	{
		_state = Game.Current.GameStateManager.ActiveState as MissionState;
	}

	public override void OnCreated()
	{
		CampaignMission.Current = this;
		_isMainAgentAnimationSet = false;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		CampaignEventDispatcher.Instance.OnMissionStarted(base.Mission);
	}

	public override void AfterStart()
	{
		base.AfterStart();
		CampaignEventDispatcher.Instance.OnAfterMissionStarted(base.Mission);
	}

	private static void SimulateRunningAwayAgents()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			PartyBase ownerParty = agent.GetComponent<CampaignAgentComponent>().OwnerParty;
			if (ownerParty != null && !agent.IsHero && agent.IsRunningAway && MBRandom.RandomFloat < 0.5f)
			{
				CharacterObject character = (CharacterObject)agent.Character;
				ownerParty.MemberRoster.AddToCounts(character, -1);
			}
		}
	}

	public override void OnMissionResultReady(MissionResult missionResult)
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign && PlayerEncounter.IsActive && PlayerEncounter.Battle != null)
		{
			if (missionResult.PlayerVictory)
			{
				PlayerEncounter.SetPlayerVictorious();
			}
			else if (missionResult.BattleState == BattleState.DefenderPullBack)
			{
				PlayerEncounter.SetPlayerSiegeContinueWithDefenderPullBack();
			}
			PlayerEncounter.CampaignBattleResult = CampaignBattleResult.GetResult(base.Mission.MissionResult?.BattleState ?? BattleState.None, missionResult.EnemyRetreated);
		}
	}

	protected override void OnEndMission()
	{
		if (Campaign.Current.GameMode == CampaignGameMode.Campaign)
		{
			if (PlayerEncounter.Battle != null && (PlayerEncounter.Battle.IsSiegeAssault || PlayerEncounter.Battle.IsSiegeAmbush) && (Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege || Mission.Current.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut))
			{
				Mission.Current.GetMissionBehavior<MissionSiegeEnginesLogic>().GetMissionSiegeWeapons(out var defenderSiegeWeapons, out var attackerSiegeWeapons);
				PlayerEncounter.Battle.GetLeaderParty(BattleSideEnum.Attacker).SiegeEvent.SetSiegeEngineStatesAfterSiegeMission(attackerSiegeWeapons, defenderSiegeWeapons);
			}
			if (_soundEvent != null)
			{
				RemovePreviousAgentsSoundEvent();
				_soundEvent.Stop();
				_soundEvent = null;
			}
		}
		CampaignEventDispatcher.Instance.OnMissionEnded(base.Mission);
		CampaignMission.Current = null;
	}

	void ICampaignMission.OnCloseEncounterMenu()
	{
		if (base.Mission.Mode == MissionMode.Conversation)
		{
			Campaign.Current.ConversationManager.EndConversation();
			if (Game.Current.GameStateManager.ActiveState is MissionState)
			{
				Game.Current.GameStateManager.PopState();
			}
		}
	}

	bool ICampaignMission.AgentLookingAtAgent(IAgent agent1, IAgent agent2)
	{
		return base.Mission.AgentLookingAtAgent((Agent)agent1, (Agent)agent2);
	}

	void ICampaignMission.OnCharacterLocationChanged(LocationCharacter locationCharacter, Location fromLocation, Location toLocation)
	{
		MissionAgentHandler missionBehavior = base.Mission.GetMissionBehavior<MissionAgentHandler>();
		if (toLocation == null)
		{
			missionBehavior.FadeoutExitingLocationCharacter(locationCharacter);
		}
		else
		{
			missionBehavior.SpawnEnteringLocationCharacter(locationCharacter, fromLocation);
		}
	}

	void ICampaignMission.OnProcessSentence()
	{
	}

	void ICampaignMission.OnConversationContinue()
	{
	}

	bool ICampaignMission.CheckIfAgentCanFollow(IAgent agent)
	{
		AgentNavigator agentNavigator = ((Agent)agent).GetComponent<CampaignAgentComponent>().AgentNavigator;
		if (agentNavigator != null)
		{
			DailyBehaviorGroup behaviorGroup = agentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup != null)
			{
				return behaviorGroup.GetBehavior<FollowAgentBehavior>() == null;
			}
			return false;
		}
		return false;
	}

	void ICampaignMission.AddAgentFollowing(IAgent agent)
	{
		Agent agent2 = (Agent)agent;
		if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			DailyBehaviorGroup behaviorGroup = agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			behaviorGroup.AddBehavior<FollowAgentBehavior>();
			behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
		}
	}

	bool ICampaignMission.CheckIfAgentCanUnFollow(IAgent agent)
	{
		Agent agent2 = (Agent)agent;
		if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			DailyBehaviorGroup behaviorGroup = agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
			if (behaviorGroup != null)
			{
				return behaviorGroup.GetBehavior<FollowAgentBehavior>() != null;
			}
			return false;
		}
		return false;
	}

	void ICampaignMission.RemoveAgentFollowing(IAgent agent)
	{
		Agent agent2 = (Agent)agent;
		if (agent2.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			agent2.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<FollowAgentBehavior>();
		}
	}

	void ICampaignMission.EndMission()
	{
		base.Mission.EndMission();
	}

	private string GetIdleAnimationId(Agent agent, string selectedId, bool startingConversation)
	{
		switch (agent.GetCurrentActionType(0))
		{
		case Agent.ActionCodeType.Sit:
			return "sit";
		case Agent.ActionCodeType.SitOnTheFloor:
			return "sit_floor";
		case Agent.ActionCodeType.SitOnAThrone:
			return "sit_throne";
		default:
			if (agent.MountAgent != null)
			{
				(string, ConversationAnimData) animDataForRiderAndMountAgents = GetAnimDataForRiderAndMountAgents(agent);
				SetMountAgentAnimation(agent.MountAgent, animDataForRiderAndMountAgents.Item2, startingConversation);
				return animDataForRiderAndMountAgents.Item1;
			}
			if (agent == Agent.Main)
			{
				return "normal";
			}
			if (startingConversation)
			{
				CharacterObject character = (CharacterObject)agent.Character;
				PartyBase ownerParty = agent.GetComponent<CampaignAgentComponent>().OwnerParty;
				return CharacterHelper.GetStandingBodyIdle(character, ownerParty);
			}
			return selectedId;
		}
	}

	private (string, ConversationAnimData) GetAnimDataForRiderAndMountAgents(Agent riderAgent)
	{
		bool flag = false;
		string item = "";
		bool flag2 = false;
		ConversationAnimData item2 = null;
		foreach (KeyValuePair<string, ConversationAnimData> conversationAnim in Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims)
		{
			if (conversationAnim.Value != null)
			{
				if (conversationAnim.Value.FamilyType == riderAgent.MountAgent.Monster.FamilyType)
				{
					item2 = conversationAnim.Value;
					flag2 = true;
				}
				else if (conversationAnim.Value.FamilyType == riderAgent.Monster.FamilyType && conversationAnim.Value.MountFamilyType == riderAgent.MountAgent.Monster.FamilyType)
				{
					item = conversationAnim.Key;
					flag = true;
				}
				if (flag2 && flag)
				{
					break;
				}
			}
		}
		return (item, item2);
	}

	private int GetActionChannelNoForConversation(Agent agent)
	{
		if (agent.IsSitting())
		{
			return 0;
		}
		if (agent.MountAgent != null)
		{
			return 1;
		}
		return 0;
	}

	private void SetMountAgentAnimation(IAgent agent, ConversationAnimData mountAnimData, bool startingConversation)
	{
		Agent agent2 = (Agent)agent;
		if (mountAnimData != null)
		{
			if (startingConversation)
			{
				_conversationAgents.Add(new AgentConversationState(agent2));
			}
			SetConversationAgentActionAtChannel(agent2, string.IsNullOrEmpty(mountAnimData.IdleAnimStart) ? ActionIndexCache.Create(mountAnimData.IdleAnimLoop) : ActionIndexCache.Create(mountAnimData.IdleAnimStart), GetActionChannelNoForConversation(agent2), setInstantly: false, forceFaceMorphRestart: false);
		}
	}

	void ICampaignMission.OnConversationStart(IAgent iAgent, bool setActionsInstantly)
	{
		((Agent)iAgent).AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		Agent.Main.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		if (!_isMainAgentAnimationSet)
		{
			_isMainAgentAnimationSet = true;
			StartConversationAnimations(Agent.Main, setActionsInstantly);
		}
		StartConversationAnimations(iAgent, setActionsInstantly);
	}

	private void StartConversationAnimations(IAgent iAgent, bool setActionsInstantly)
	{
		Agent agent = (Agent)iAgent;
		_conversationAgents.Add(new AgentConversationState(agent));
		string idleAnimationId = GetIdleAnimationId(agent, "", startingConversation: true);
		string defaultFaceIdle = CharacterHelper.GetDefaultFaceIdle((CharacterObject)agent.Character);
		int actionChannelNoForConversation = GetActionChannelNoForConversation(agent);
		if (Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleAnimationId, out var value))
		{
			SetConversationAgentActionAtChannel(agent, string.IsNullOrEmpty(value.IdleAnimStart) ? ActionIndexCache.Create(value.IdleAnimLoop) : ActionIndexCache.Create(value.IdleAnimStart), actionChannelNoForConversation, setActionsInstantly, forceFaceMorphRestart: false);
			SetFaceIdle(agent, defaultFaceIdle);
		}
		if (agent.IsUsingGameObject)
		{
			agent.CurrentlyUsedGameObject.OnUserConversationStart();
		}
	}

	private void EndConversationAnimations(IAgent iAgent, bool ignorePriority = false)
	{
		Agent agent = (Agent)iAgent;
		if (agent.IsHuman)
		{
			agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", loop: false);
			agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, "", loop: false);
			if (agent.HasMount)
			{
				EndConversationAnimations(agent.MountAgent, ignorePriority: true);
			}
		}
		int num = -1;
		int count = _conversationAgents.Count;
		for (int i = 0; i < count; i++)
		{
			AgentConversationState agentConversationState = _conversationAgents[i];
			if (agentConversationState.Agent != agent)
			{
				continue;
			}
			for (int j = 0; j < 2; j++)
			{
				if (agentConversationState.IsChannelModified(j))
				{
					agent.SetActionChannel(j, in ActionIndexCache.act_none, ignorePriority, (AnimFlags)Math.Min(agent.GetCurrentActionPriority(j), 73));
				}
			}
			if (agent.IsUsingGameObject)
			{
				agent.CurrentlyUsedGameObject.OnUserConversationEnd();
			}
			num = i;
			break;
		}
		if (num != -1)
		{
			_conversationAgents.RemoveAt(num);
		}
	}

	void ICampaignMission.OnConversationPlay(string idleActionId, string idleFaceAnimId, string reactionId, string reactionFaceAnimId, string soundPath)
	{
		_currentAgent = (Agent)Campaign.Current.ConversationManager.SpeakerAgent;
		RemovePreviousAgentsSoundEvent();
		StopPreviousSound();
		string idleAnimationId = GetIdleAnimationId(_currentAgent, idleActionId, startingConversation: false);
		if (!string.IsNullOrEmpty(idleAnimationId) && Campaign.Current.ConversationManager.ConversationAnimationManager.ConversationAnims.TryGetValue(idleAnimationId, out var value))
		{
			if (!string.IsNullOrEmpty(reactionId))
			{
				SetConversationAgentActionAtChannel(_currentAgent, ActionIndexCache.Create(value.Reactions[reactionId]), 0, setInstantly: false, forceFaceMorphRestart: true);
			}
			else
			{
				ActionIndexCache action = (string.IsNullOrEmpty(value.IdleAnimStart) ? ActionIndexCache.Create(value.IdleAnimLoop) : ActionIndexCache.Create(value.IdleAnimStart));
				SetConversationAgentActionAtChannel(_currentAgent, in action, GetActionChannelNoForConversation(_currentAgent), setInstantly: false, forceFaceMorphRestart: false);
			}
		}
		if (!string.IsNullOrEmpty(reactionFaceAnimId))
		{
			_currentAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, reactionFaceAnimId, loop: false);
		}
		else if (!string.IsNullOrEmpty(idleFaceAnimId))
		{
			SetFaceIdle(_currentAgent, idleFaceAnimId);
		}
		else
		{
			_currentAgent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, "", loop: false);
		}
		if (!string.IsNullOrEmpty(soundPath))
		{
			PlayConversationSoundEvent(soundPath);
		}
	}

	private string GetRhubarbXmlPathFromSoundPath(string soundPath)
	{
		return soundPath[..soundPath.LastIndexOf('.')] + ".xml";
	}

	public void PlayConversationSoundEvent(string soundPath)
	{
		Vec3 position = ConversationMission.CurrentSpeakerAgent.Position;
		Debug.Print("Conversation sound playing: " + soundPath + ", position: " + position, 5);
		_soundEvent = SoundEvent.CreateEventFromExternalFile("event:/Extra/voiceover", soundPath, Mission.Current.Scene, is3d: false, isBlocking: false);
		_soundEvent.SetPosition(position);
		_soundEvent.Play();
		int soundId = _soundEvent.GetSoundId();
		_agentSoundEvents.Add(_currentAgent, soundId);
		string rhubarbXmlPathFromSoundPath = GetRhubarbXmlPathFromSoundPath(soundPath);
		_currentAgent.AgentVisuals.StartRhubarbRecord(rhubarbXmlPathFromSoundPath, soundId);
	}

	private void StopPreviousSound()
	{
		if (_soundEvent != null)
		{
			_soundEvent.Stop();
			_soundEvent = null;
		}
	}

	private void RemovePreviousAgentsSoundEvent()
	{
		if (_soundEvent == null || !_agentSoundEvents.ContainsValue(_soundEvent.GetSoundId()))
		{
			return;
		}
		Agent agent = null;
		foreach (KeyValuePair<Agent, int> agentSoundEvent in _agentSoundEvents)
		{
			if (agentSoundEvent.Value == _soundEvent.GetSoundId())
			{
				agent = agentSoundEvent.Key;
			}
		}
		_agentSoundEvents.Remove(agent);
		agent.AgentVisuals.StartRhubarbRecord("", -1);
	}

	void ICampaignMission.OnConversationEnd(IAgent iAgent)
	{
		Agent agent = (Agent)iAgent;
		agent.ResetLookAgent();
		agent.DisableLookToPointOfInterest();
		Agent.Main.ResetLookAgent();
		Agent.Main.DisableLookToPointOfInterest();
		if (Settlement.CurrentSettlement != null && !base.Mission.HasMissionBehavior<ConversationMissionLogic>())
		{
			agent.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
			Agent.Main.AgentVisuals.SetAgentLodZeroOrMax(makeZero: true);
		}
		if (_soundEvent != null)
		{
			RemovePreviousAgentsSoundEvent();
			_soundEvent.Stop();
		}
		if (_isMainAgentAnimationSet)
		{
			_isMainAgentAnimationSet = false;
			EndConversationAnimations(Agent.Main);
		}
		EndConversationAnimations(iAgent);
		_soundEvent = null;
	}

	private void SetFaceIdle(Agent agent, string idleFaceAnimId)
	{
		agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.Mid, idleFaceAnimId, loop: true);
	}

	private void SetConversationAgentActionAtChannel(Agent agent, in ActionIndexCache action, int channelNo, bool setInstantly, bool forceFaceMorphRestart)
	{
		agent.SetActionChannel(channelNo, in action, ignorePriority: false, (AnimFlags)0uL, 0f, 1f, setInstantly ? 0f : (-0.2f), 0.4f, 0f, useLinearSmoothing: false, -0.2f, 0, forceFaceMorphRestart);
		int count = _conversationAgents.Count;
		for (int i = 0; i < count; i++)
		{
			if (_conversationAgents[i].Agent == agent)
			{
				_conversationAgents[i].SetChannelModified(channelNo);
				break;
			}
		}
	}

	public void FadeOutCharacter(CharacterObject characterObject)
	{
		foreach (Agent agent in base.Mission.Agents)
		{
			if (agent.Character != null && agent.Character == characterObject)
			{
				agent.FadeOut(hideInstantly: true, hideMount: true);
				break;
			}
		}
	}

	public void OnGameStateChanged()
	{
		RemovePreviousAgentsSoundEvent();
		StopPreviousSound();
	}
}
