using System.Collections.Generic;
using System.Linq;
using SandBox.CampaignBehaviors;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics.Towns;

public class PrisonBreakMissionController : MissionLogic
{
	private const int PrisonerNearThreshold = 5;

	private const int PrisonerSwitchToAlarmedDistance = 3;

	private bool _isFirstPhase;

	private List<CharacterObject> _killedGuardsInTheFirstPhase;

	private readonly CharacterObject _prisonerCharacter;

	private Agent _prisonerAgent;

	private List<Agent> _aliveGuardAgents;

	private PrisonBreakCampaignBehavior _prisonBreakCampaignBehavior;

	private StealthFailCounterMissionLogic _failCounterMissionLogic;

	private bool _isPrisonerFollowing;

	private bool _isPrisonerNear;

	private bool _missionFailedByStealthCounter;

	public PrisonBreakMissionController(CharacterObject prisonerCharacter)
	{
		_prisonerCharacter = prisonerCharacter;
		_isFirstPhase = true;
		_isPrisonerFollowing = false;
		_aliveGuardAgents = new List<Agent>();
		_killedGuardsInTheFirstPhase = new List<CharacterObject>();
		_prisonBreakCampaignBehavior = Campaign.Current.GetCampaignBehavior<PrisonBreakCampaignBehavior>();
	}

	public override void OnCreated()
	{
		base.OnCreated();
		base.Mission.DoesMissionRequireCivilianEquipment = false;
	}

	public override void OnBehaviorInitialize()
	{
		Game.Current.EventManager.RegisterEvent<OnStealthMissionCounterFailedEvent>(OnStealthMissionCounterFailed);
		Game.Current.EventManager.RegisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
		base.Mission.IsAgentInteractionAllowed_AdditionalCondition += IsAgentInteractionAllowed_AdditionalCondition;
	}

	private void OnLocationCharacterAgentSpawned(LocationCharacterAgentSpawnedMissionEvent missionEvent)
	{
		if (missionEvent.LocationCharacter.Character == _prisonerCharacter)
		{
			_prisonerAgent = missionEvent.Agent;
			_prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<WalkingBehavior>();
		}
	}

	public override void AfterStart()
	{
		base.Mission.SetMissionMode(MissionMode.Stealth, atStart: true);
		base.Mission.IsInventoryAccessible = false;
		base.Mission.IsQuestScreenAccessible = false;
		base.Mission.IsKingdomWindowAccessible = false;
		foreach (UsableMachine townPassageProp in base.Mission.GetMissionBehavior<MissionAgentHandler>().TownPassageProps)
		{
			townPassageProp.Deactivate();
		}
		_failCounterMissionLogic = Mission.Current.GetMissionBehavior<StealthFailCounterMissionLogic>();
		_failCounterMissionLogic.FailCounterSeconds = 15f;
		base.Mission.AllowAiTicking = false;
		SandBoxHelpers.MissionHelper.SpawnPlayer(civilianEquipment: false, noHorses: true);
		base.Mission.GetMissionBehavior<MissionAgentHandler>().SpawnLocationCharacters();
		base.Mission.AllowAiTicking = true;
		Agent.Main.SetClothingColor1(4279111698u);
		Agent.Main.SetClothingColor2(4279111698u);
		Agent.Main.UpdateSpawnEquipmentAndRefreshVisuals(Hero.MainHero.StealthEquipment);
		PreparePrisonAgent();
		Agent.Main.Formation = new Formation(Mission.Current.Teams.Player, 0);
		base.Mission.FocusableObjectInformationProvider.AddInfoCallback(GetFocusableObjectInteractionInfoTexts);
		TextObject textObject = new TextObject("{=QYFuj7H7}Find and talk to {PRISONER_NAME}, Do not alert the guards!");
		textObject.SetTextVariable("PRISONER_NAME", _prisonerCharacter.Name);
		MBInformationManager.AddQuickInformation(textObject);
		_aliveGuardAgents = base.Mission.Agents.Where((Agent x) => x.Character is CharacterObject characterObject && (characterObject.Occupation == Occupation.Soldier || characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.PrisonGuard)).ToList();
	}

	private void SwitchPrisonerFollowingState(bool forceFollow = false)
	{
		_isPrisonerFollowing = forceFollow || !_isPrisonerFollowing;
		MBTextManager.SetTextVariable("IS_PRISONER_FOLLOWING", _isPrisonerFollowing ? 1 : 0);
		FollowAgentBehavior behavior = _prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().GetBehavior<FollowAgentBehavior>();
		if (_isPrisonerFollowing)
		{
			_prisonerAgent.SetCrouchMode(set: false);
			behavior.SetTargetAgent(Agent.Main);
			AgentFlag agentFlags = _prisonerAgent.GetAgentFlags();
			_prisonerAgent.SetAgentFlags((AgentFlag)((uint)agentFlags & 0xFFFEFFFFu));
		}
		else
		{
			behavior.SetTargetAgent(null);
			_prisonerAgent.SetCrouchMode(set: true);
		}
		_prisonerAgent.SetAlarmState(Agent.AIStateFlag.None);
	}

	private void CheckPrisonerSwitchToAlarmState()
	{
		foreach (Agent aliveGuardAgent in _aliveGuardAgents)
		{
			if (_prisonerAgent.Position.DistanceSquared(aliveGuardAgent.Position) < 3f && aliveGuardAgent.IsAlarmed())
			{
				AgentFlag agentFlags = _prisonerAgent.GetAgentFlags();
				_prisonerAgent.SetAgentFlags(agentFlags | AgentFlag.CanGetAlarmed);
				_prisonerAgent.SetAlarmState(Agent.AIStateFlag.Alarmed);
			}
		}
	}

	public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
	{
		if (userAgent == Agent.Main && agent == _prisonerAgent && _aliveGuardAgents.All((Agent x) => !x.IsAlarmed()))
		{
			if (_isFirstPhase)
			{
				SpawnPhase2Guards();
				SwitchToPhase2();
				SwitchPrisonerFollowingState();
			}
			else
			{
				SwitchPrisonerFollowingState();
			}
		}
	}

	private void SpawnPhase2Guards()
	{
		Location locationWithId = LocationComplex.Current.GetLocationWithId("prison");
		foreach (CharacterObject item in _killedGuardsInTheFirstPhase)
		{
			_ = item;
			LocationCharacter locationCharacter = _prisonBreakCampaignBehavior.CreatePrisonBreakGuard();
			locationCharacter.SpecialTargetTag = "prison_break_reinforcement_point";
			LocationComplex.Current.ChangeLocation(locationCharacter, null, locationWithId);
			_aliveGuardAgents.Add(base.Mission.Agents.Last());
		}
	}

	private void SwitchToPhase2()
	{
		_isFirstPhase = false;
		MBInformationManager.AddQuickInformation(new TextObject("{=ap5pYDR7}Let's get out of here!"), 0, _prisonerCharacter);
		MBInformationManager.AddQuickInformation(new TextObject("{=S3MaaRQH}Guards know that something is up, be ready to fight!"));
		_prisonerAgent.SetTeam(Mission.Current.PlayerTeam, sync: true);
		DailyBehaviorGroup behaviorGroup = _prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
		behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
		followAgentBehavior.SetTargetAgent(Agent.Main);
		AgentFlag agentFlags = _prisonerAgent.GetAgentFlags();
		_prisonerAgent.SetAgentFlags((AgentFlag)((uint)agentFlags & 0xFFFEFFFFu));
		_prisonerAgent.WieldNextWeapon(Agent.HandIndex.MainHand);
		foreach (Agent aliveGuardAgent in _aliveGuardAgents)
		{
			aliveGuardAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().AddAlarmFactor(2f, aliveGuardAgent.GetWorldPosition());
			aliveGuardAgent.SetAlarmState(Agent.AIStateFlag.PatrollingCautious);
		}
		UpdateDoorPermission();
	}

	public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
	{
		if (userAgent == Agent.Main)
		{
			return otherAgent == _prisonerAgent;
		}
		return false;
	}

	private void GetFocusableObjectInteractionInfoTexts(Agent requesterAgent, IFocusable focusableObject, bool isInteractable, out FocusableObjectInformation focusableObjectInformation)
	{
		focusableObjectInformation = default(FocusableObjectInformation);
		if (requesterAgent.IsMainAgent && focusableObject is Agent agent && agent == _prisonerAgent)
		{
			focusableObjectInformation.PrimaryInteractionText = agent.Character.Name;
			MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			focusableObjectInformation.SecondaryInteractionText = GameTexts.FindText("str_key_action");
			focusableObjectInformation.SecondaryInteractionText.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
			focusableObjectInformation.SecondaryInteractionText.SetTextVariable("ACTION", (!_isFirstPhase) ? GameTexts.FindText("str_ui_prison_break") : GameTexts.FindText("str_ui_prison_break_prisoner_greeting"));
			focusableObjectInformation.IsActive = true;
		}
		else
		{
			focusableObjectInformation.IsActive = false;
		}
	}

	private void PreparePrisonAgent()
	{
		_prisonerAgent.Health = _prisonerAgent.HealthLimit;
		_prisonerAgent.Defensiveness = 2f;
		AgentNavigator agentNavigator = _prisonerAgent.GetComponent<CampaignAgentComponent>().AgentNavigator;
		agentNavigator.RemoveBehaviorGroup<AlarmedBehaviorGroup>();
		agentNavigator.SpecialTargetTag = "sp_prison_break_prisoner";
		ItemObject item = TaleWorlds.Core.Extensions.MinBy(Items.All.Where((ItemObject x) => x.IsCraftedWeapon && x.Type == ItemObject.ItemTypeEnum.OneHandedWeapon && x.WeaponComponent.GetItemType() == ItemObject.ItemTypeEnum.OneHandedWeapon && x.IsCivilian), (ItemObject x) => x.Value);
		MissionWeapon weapon = new MissionWeapon(item, null, _prisonerCharacter.HeroObject.ClanBanner);
		_prisonerAgent.EquipWeaponWithNewEntity(EquipmentIndex.WeaponItemBeginSlot, ref weapon);
		_prisonerAgent.SpawnEquipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.WeaponItemBeginSlot, new EquipmentElement(weapon.Item));
		_prisonerAgent.SetCrouchMode(set: true);
		_prisonerAgent.SetTeam(null, sync: false);
	}

	public override void OnAgentAlarmedStateChanged(Agent agent, Agent.AIStateFlag flag)
	{
		UpdateDoorPermission();
		if (agent == _prisonerAgent && !_prisonerAgent.IsAlarmed())
		{
			AgentFlag agentFlags = _prisonerAgent.GetAgentFlags();
			_prisonerAgent.SetAgentFlags((AgentFlag)((uint)agentFlags & 0xFFFEFFFFu));
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (_prisonerAgent == affectedAgent)
		{
			_prisonerAgent = null;
		}
		if (_aliveGuardAgents.Contains(affectedAgent))
		{
			if (_isFirstPhase)
			{
				_killedGuardsInTheFirstPhase.Add((CharacterObject)affectedAgent.Character);
			}
			_aliveGuardAgents.Remove(affectedAgent);
		}
		UpdateDoorPermission();
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave)
	{
		canLeave = Agent.Main == null || !Agent.Main.IsActive();
		if (!canLeave)
		{
			MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat"));
		}
		return null;
	}

	public void OnStealthMissionCounterFailed(OnStealthMissionCounterFailedEvent obj)
	{
		_missionFailedByStealthCounter = true;
	}

	protected override void OnEndMission()
	{
		Game.Current.EventManager.UnregisterEvent<OnStealthMissionCounterFailedEvent>(OnStealthMissionCounterFailed);
		Game.Current.EventManager.UnregisterEvent<LocationCharacterAgentSpawnedMissionEvent>(OnLocationCharacterAgentSpawned);
		if (PlayerEncounter.LocationEncounter.CharactersAccompanyingPlayer.Any((AccompanyingCharacter x) => x.LocationCharacter.Character == _prisonerCharacter))
		{
			PlayerEncounter.LocationEncounter.RemoveAccompanyingCharacter(_prisonerCharacter.HeroObject);
		}
		if (_missionFailedByStealthCounter)
		{
			GameMenu.SwitchToMenu("settlement_prison_break_fail_player_unconscious");
		}
		else if (Agent.Main == null || !Agent.Main.IsActive())
		{
			GameMenu.SwitchToMenu("settlement_prison_break_fail_player_unconscious");
		}
		else if (_prisonerAgent == null || !_prisonerAgent.IsActive())
		{
			GameMenu.SwitchToMenu("settlement_prison_break_fail_prisoner_unconscious");
		}
		else
		{
			GameMenu.SwitchToMenu("settlement_prison_break_success");
		}
		Campaign.Current.GameMenuManager.NextLocation = null;
		Campaign.Current.GameMenuManager.PreviousLocation = null;
		base.Mission.IsAgentInteractionAllowed_AdditionalCondition -= IsAgentInteractionAllowed_AdditionalCondition;
	}

	public override void OnMissionTick(float dt)
	{
		if (Agent.Main != null && _prisonerAgent != null)
		{
			bool isPrisonerNear = _isPrisonerNear;
			_isPrisonerNear = Agent.Main.VisualPosition.DistanceSquared(_prisonerAgent.VisualPosition) < 25f;
			if (isPrisonerNear != _isPrisonerNear)
			{
				UpdateDoorPermission();
			}
		}
		if (_failCounterMissionLogic != null && !_isFirstPhase)
		{
			Mission.Current.RemoveMissionBehavior(_failCounterMissionLogic);
			_failCounterMissionLogic = null;
		}
		if (_prisonerAgent == null && _aliveGuardAgents.All((Agent x) => x.IsAlarmStateNormal()))
		{
			ShowMissionFailedPopup();
		}
		if (_prisonerAgent != null)
		{
			CheckPrisonerSwitchToAlarmState();
		}
	}

	private void ShowMissionFailedPopup()
	{
		TextObject textObject = new TextObject("{=wQbfWNZO}Mission Failed!");
		TextObject textObject2 = new TextObject("{=KfrybSrr}You made your way out but {PRISONER.NAME} was badly wounded during the escape. You had no choice but to leave {?PRISONER.GENDER}her{?}him{\\?} behind.");
		textObject2.SetCharacterProperties("PRISONER", _prisonerCharacter);
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: null, affirmativeAction: delegate
		{
			Mission.Current.EndMission();
		}, negativeAction: null), Campaign.Current.GameMode == CampaignGameMode.Campaign);
	}

	private void UpdateDoorPermission()
	{
		bool flag = !_isFirstPhase && (_isPrisonerNear || _aliveGuardAgents.Count == 0) && _aliveGuardAgents.All((Agent x) => x.IsAlarmStateNormal());
		foreach (UsableMachine townPassageProp in base.Mission.GetMissionBehavior<MissionAgentHandler>().TownPassageProps)
		{
			if (flag)
			{
				townPassageProp.Activate();
			}
			else
			{
				townPassageProp.Deactivate();
			}
		}
	}

	private bool IsAgentInteractionAllowed_AdditionalCondition()
	{
		return true;
	}
}
