using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.Conversation.MissionLogics;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class MissionAlleyHandler : MissionLogic
{
	private const float ConstantForInitiatingConversation = 5f;

	private static Vec3 _fightPosition = Vec3.Invalid;

	private Dictionary<Agent, AgentNavigator> _rivalThugAgentsAndAgentNavigators;

	private const int DistanceForEndingAlleyFight = 20;

	private const int GuardAgentSafeZone = 10;

	private static List<Agent> _guardAgents;

	private Dictionary<Alley, bool> _conversationTriggeredAlleys;

	private bool _agentCachesInitialized;

	private MissionFightHandler _missionFightHandler;

	private DisguiseMissionLogic _disguiseMissionLogic;

	public bool CanThugConversationBeTriggered
	{
		get
		{
			if (_disguiseMissionLogic != null)
			{
				return _disguiseMissionLogic.CanCommonAreaFightBeTriggered();
			}
			return true;
		}
	}

	public override void OnMissionTick(float dt)
	{
		if (!_agentCachesInitialized)
		{
			_conversationTriggeredAlleys = new Dictionary<Alley, bool>();
			foreach (Agent agent in base.Mission.Agents)
			{
				if (!agent.IsHuman)
				{
					continue;
				}
				CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
				if (component?.AgentNavigator?.MemberOfAlley != null && component.AgentNavigator.MemberOfAlley.Owner != Hero.MainHero)
				{
					if (!_rivalThugAgentsAndAgentNavigators.ContainsKey(agent))
					{
						_rivalThugAgentsAndAgentNavigators.Add(agent, component.AgentNavigator);
					}
					if (!_conversationTriggeredAlleys.ContainsKey(component.AgentNavigator.MemberOfAlley))
					{
						_conversationTriggeredAlleys.Add(component.AgentNavigator.MemberOfAlley, value: false);
					}
				}
			}
			_agentCachesInitialized = base.Mission.Agents.Count > 0;
		}
		if (Mission.Current.Mode == MissionMode.Battle)
		{
			EndFightIfPlayerIsFarAwayOrNearGuard();
		}
		else if (MBRandom.RandomFloat < dt * 10f && CanThugConversationBeTriggered)
		{
			CheckAndTriggerConversationWithRivalThug();
		}
	}

	private void CheckAndTriggerConversationWithRivalThug()
	{
		if (Campaign.Current.ConversationManager.IsConversationFlowActive || Agent.Main == null)
		{
			return;
		}
		foreach (KeyValuePair<Agent, AgentNavigator> rivalThugAgentsAndAgentNavigator in _rivalThugAgentsAndAgentNavigators)
		{
			if (rivalThugAgentsAndAgentNavigator.Key.IsActive() && _conversationTriggeredAlleys.TryGetValue(rivalThugAgentsAndAgentNavigator.Value.MemberOfAlley, out var value) && !value)
			{
				Agent key = rivalThugAgentsAndAgentNavigator.Key;
				if (key.GetDistanceTo(Agent.Main) < 5f && rivalThugAgentsAndAgentNavigator.Value.CanSeeAgent(Agent.Main))
				{
					Mission.Current.GetMissionBehavior<MissionConversationLogic>().StartConversation(key, setActionsInstantly: false);
					_conversationTriggeredAlleys[rivalThugAgentsAndAgentNavigator.Value.MemberOfAlley] = true;
					break;
				}
			}
		}
	}

	public override void AfterStart()
	{
		_disguiseMissionLogic = Mission.Current.GetMissionBehavior<DisguiseMissionLogic>();
		_guardAgents = new List<Agent>();
		_rivalThugAgentsAndAgentNavigators = new Dictionary<Agent, AgentNavigator>();
		_fightPosition = Vec3.Invalid;
		_missionFightHandler = Mission.Current.GetMissionBehavior<MissionFightHandler>();
	}

	private void EndFightIfPlayerIsFarAwayOrNearGuard()
	{
		if (Agent.Main == null)
		{
			return;
		}
		bool flag = false;
		foreach (Agent guardAgent in _guardAgents)
		{
			if ((Agent.Main.Position - guardAgent.Position).Length <= 10f)
			{
				flag = true;
				break;
			}
		}
		if (_fightPosition != Vec3.Invalid && (Agent.Main.Position - _fightPosition).Length >= 20f)
		{
			flag = true;
		}
		if (flag)
		{
			EndFight();
		}
	}

	private (bool, string) CanPlayerOccupyTheCurrentAlley()
	{
		if (!Settlement.CurrentSettlement.Alleys.All((Alley x) => x.Owner != Hero.MainHero))
		{
			TextObject textObject = new TextObject("{=ribkM9dl}You already own another alley in the settlement.");
			return (false, textObject.ToString());
		}
		if (!Campaign.Current.Models.AlleyModel.GetClanMembersAndAvailabilityDetailsForLeadingAnAlley(CampaignMission.Current.LastVisitedAlley).Any(((Hero, DefaultAlleyModel.AlleyMemberAvailabilityDetail) x) => x.Item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.Available || x.Item2 == DefaultAlleyModel.AlleyMemberAvailabilityDetail.AvailableWithDelay))
		{
			TextObject textObject = new TextObject("{=hnhKJYbx}You don't have any suitable clan members to assign this alley. ({ROGUERY_SKILL} skill {NEEDED_SKILL_LEVEL} or higher, {TRAIT_NAME} trait {MAX_TRAIT_AMOUNT} or lower)");
			textObject.SetTextVariable("ROGUERY_SKILL", DefaultSkills.Roguery.Name);
			textObject.SetTextVariable("NEEDED_SKILL_LEVEL", 30);
			textObject.SetTextVariable("TRAIT_NAME", DefaultTraits.Mercy.Name);
			textObject.SetTextVariable("MAX_TRAIT_AMOUNT", 0);
			return (false, textObject.ToString());
		}
		if (MobileParty.MainParty.MemberRoster.TotalRegulars < Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley)
		{
			TextObject textObject = new TextObject("{=zLnqZdIK}You don't have enough troops to assign this alley. (Needed at least {NEEDED_TROOP_NUMBER})");
			textObject.SetTextVariable("NEEDED_TROOP_NUMBER", Campaign.Current.Models.AlleyModel.MinimumTroopCountInPlayerOwnedAlley);
			return (false, textObject.ToString());
		}
		return (true, null);
	}

	private void EndFight()
	{
		_missionFightHandler.EndFight();
		foreach (Agent guardAgent in _guardAgents)
		{
			guardAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<AlarmedBehaviorGroup>().GetBehavior<FightBehavior>().IsActive = false;
		}
		_guardAgents.Clear();
		Mission.Current.SetMissionMode(MissionMode.StartUp, atStart: false);
	}

	private void OnTakeOverTheAlley()
	{
		AlleyHelper.CreateMultiSelectionInquiryForSelectingClanMemberToAlley(CampaignMission.Current.LastVisitedAlley, OnCompanionSelectedForNewAlley, OnCompanionSelectionCancel);
	}

	private void OnCompanionSelectionCancel(List<InquiryElement> obj)
	{
		OnLeaveItEmpty();
	}

	private void OnCompanionSelectedForNewAlley(List<InquiryElement> companion)
	{
		CharacterObject character = companion.First().Identifier as CharacterObject;
		TroopRoster troopRoster = TroopRoster.CreateDummyTroopRoster();
		troopRoster.AddToCounts(character, 1);
		AlleyHelper.OpenScreenForManagingAlley(isNewAlley: true, troopRoster, OnPartyScreenDoneClicked, new TextObject("{=s8dsW6m0}New Alley"), OnPartyScreenCancel);
	}

	private void OnPartyScreenCancel()
	{
		OnLeaveItEmpty();
	}

	public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
	{
		if (affectedAgent.IsHuman && affectorAgent != null && affectorAgent == Agent.Main && affectorAgent.IsHuman && affectedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			(affectedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>()?.GetBehavior<TalkBehavior>())?.Disable();
			if (!affectedAgent.IsEnemyOf(affectorAgent) && affectedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.MemberOfAlley != null)
			{
				StartCommonAreaBattle(affectedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.MemberOfAlley);
			}
		}
	}

	private bool OnPartyScreenDoneClicked(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
	{
		TeleportHeroAction.ApplyDelayedTeleportToSettlement(leftMemberRoster.GetTroopRoster().Find((TroopRosterElement x) => x.Character.IsHero).Character.HeroObject, MobileParty.MainParty.CurrentSettlement);
		foreach (TroopRosterElement item in leftMemberRoster.GetTroopRoster())
		{
			if (!item.Character.IsHero)
			{
				MobileParty.MainParty.MemberRoster.RemoveTroop(item.Character, item.Number);
			}
		}
		CampaignEventDispatcher.Instance.OnAlleyOccupiedByPlayer(CampaignMission.Current.LastVisitedAlley, leftMemberRoster);
		return true;
	}

	public void StartCommonAreaBattle(Alley alley)
	{
		_guardAgents.Clear();
		_conversationTriggeredAlleys[alley] = true;
		List<Agent> accompanyingAgents = new List<Agent>();
		foreach (Agent agent in Mission.Current.Agents)
		{
			LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
			AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(locationCharacter);
			CharacterObject characterObject = (CharacterObject)agent.Character;
			if (accompanyingCharacter != null && accompanyingCharacter.IsFollowingPlayerAtMissionStart)
			{
				accompanyingAgents.Add(agent);
			}
			else if (characterObject != null && (characterObject.Occupation == Occupation.Guard || characterObject.Occupation == Occupation.Soldier))
			{
				_guardAgents.Add(agent);
			}
		}
		List<Agent> playerSideAgents = Mission.Current.Agents.Where((Agent agent) => agent.IsHuman && agent.Character.IsHero && (agent.IsPlayerControlled || accompanyingAgents.Contains(agent))).ToList();
		List<Agent> opponentSideAgents = Mission.Current.Agents.Where((Agent agent) => agent.IsHuman && agent.GetComponent<CampaignAgentComponent>().AgentNavigator != null && agent.GetComponent<CampaignAgentComponent>().AgentNavigator.MemberOfAlley == alley).ToList();
		_fightPosition = Agent.Main.Position;
		Mission.Current.GetMissionBehavior<MissionFightHandler>().StartCustomFight(playerSideAgents, opponentSideAgents, dropWeapons: false, isItemUseDisabled: false, OnAlleyFightEnd);
	}

	private void OnLeaveItEmpty()
	{
		CampaignEventDispatcher.Instance.OnAlleyClearedByPlayer(CampaignMission.Current.LastVisitedAlley);
	}

	private void OnAlleyFightEnd(bool isPlayerSideWon)
	{
		if (isPlayerSideWon)
		{
			TextObject textObject = new TextObject("{=4QfQBi2k}Alley fight won");
			TextObject textObject2 = new TextObject("{=8SK2BZum}You have cleared an alley which belonged to a gang leader. Now, you can either take it over for your own benefit or leave it empty to help the town. To own an alley, you will need to assign a suitable clan member and some troops to watch over it. This will provide denars to your clan, but also increase your crime rating.");
			TextObject textObject3 = new TextObject("{=qxY2ASqp}Take over the alley");
			InformationManager.ShowInquiry(new InquiryData(negativeText: new TextObject("{=jjEzdO0Y}Leave it empty").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, affirmativeText: textObject3.ToString(), affirmativeAction: OnTakeOverTheAlley, negativeAction: OnLeaveItEmpty, soundEventPath: "", expireTime: 0f, timeoutAction: null, isAffirmativeOptionEnabled: CanPlayerOccupyTheCurrentAlley), pauseGameActiveState: true);
		}
		else if (Agent.Main == null || !Agent.Main.IsActive())
		{
			Mission.Current.NextCheckTimeEndMission = 0f;
			if (!Campaign.Current.IsMainHeroDisguised)
			{
				Campaign.Current.GameMenuManager.SetNextMenu("settlement_player_unconscious");
			}
		}
		_fightPosition = Vec3.Invalid;
	}
}
