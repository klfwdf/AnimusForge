using System.Collections.Generic;
using SandBox.Conversation;
using SandBox.GameComponents;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class ClanMemberRolesCampaignBehavior : CampaignBehaviorBase, IMissionPlayerFollowerHandler
{
	private List<Hero> _isFollowingPlayer = new List<Hero>();

	private Agent _gatherOrderedAgent;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, AddDialogs);
		CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
		CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, BeforeMissionOpened);
		CampaignEvents.OnHeroJoinedPartyEvent.AddNonSerializedListener(this, OnHeroJoinedParty);
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
		CampaignEvents.OnHeroGetsBusyEvent.AddNonSerializedListener(this, OnHeroGetsBusy);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isFollowingPlayer", ref _isFollowingPlayer);
	}

	private static void FollowMainAgent()
	{
		DailyBehaviorGroup behaviorGroup = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
		behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
		followAgentBehavior.SetTargetAgent(Agent.Main);
	}

	public bool IsFollowingPlayer(Hero hero)
	{
		return _isFollowingPlayer.Contains(hero);
	}

	private void AddDialogs(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddPlayerLine("clan_member_follow", "hero_main_options", "clan_member_follow_me", "{=blqTMwQT}Follow me.", clan_member_follow_me_on_condition, null);
		campaignGameStarter.AddPlayerLine("clan_member_dont_follow", "hero_main_options", "clan_member_dont_follow_me", "{=LPtWLajd}You can stop following me now. Thanks.", clan_member_dont_follow_me_on_condition, null);
		campaignGameStarter.AddPlayerLine("clan_members_follow", "hero_main_options", "clan_member_gather", "{=PUtbpIFI}Gather all my companions in the settlement and find me.", clan_members_gather_on_condition, null);
		campaignGameStarter.AddPlayerLine("clan_members_dont_follow", "hero_main_options", "clan_members_dont_follow_me", "{=FdwZlCCM}All of you can stop following me and return to what you were doing.", clan_members_gather_end_on_condition, null);
		campaignGameStarter.AddDialogLine("clan_member_gather_clan_members_accept", "clan_member_gather", "close_window", "{=KL8tVq8P}I shall do that.", null, clan_member_gather_on_consequence);
		campaignGameStarter.AddDialogLine("clan_member_follow_accept", "clan_member_follow_me", "close_window", "{=gm3wqjvi}Lead the way.", null, clan_member_follow_me_on_consequence);
		campaignGameStarter.AddDialogLine("clan_member_dont_follow_accept", "clan_member_dont_follow_me", "close_window", "{=ppi6eVos}As you wish.", null, clan_member_dont_follow_me_on_consequence);
		campaignGameStarter.AddDialogLine("clan_members_dont_follow_accept", "clan_members_dont_follow_me", "close_window", "{=ppi6eVos}As you wish.", null, clan_members_dont_follow_me_on_consequence);
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party == MobileParty.MainParty && PlayerEncounter.LocationEncounter != null)
		{
			PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
			_isFollowingPlayer.Clear();
		}
	}

	private void BeforeMissionOpened()
	{
		if (PlayerEncounter.LocationEncounter == null)
		{
			return;
		}
		foreach (Hero item in _isFollowingPlayer)
		{
			if (PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(item.CharacterObject) == null)
			{
				AddClanMembersAsAccompanyingCharacter(item);
			}
		}
	}

	private void OnHeroJoinedParty(Hero hero, MobileParty mobileParty)
	{
		if (hero.Clan == Clan.PlayerClan && mobileParty.IsMainParty && mobileParty.CurrentSettlement != null && PlayerEncounter.LocationEncounter != null && MobileParty.MainParty.IsActive && (mobileParty.CurrentSettlement.IsFortification || mobileParty.CurrentSettlement.IsVillage) && _isFollowingPlayer.Count == 0)
		{
			UpdateAccompanyingCharacters();
		}
	}

	public void RemoveFollowingHero(Hero hero)
	{
		if (_isFollowingPlayer.Contains(hero))
		{
			RemoveAccompanyingHero(hero);
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		_gatherOrderedAgent = null;
	}

	private void OnHeroGetsBusy(Hero hero, HeroGetsBusyReasons heroGetsBusyReason)
	{
		if (heroGetsBusyReason != HeroGetsBusyReasons.BecomeCaravanLeader && heroGetsBusyReason != HeroGetsBusyReasons.BecomeAlleyLeader && heroGetsBusyReason != HeroGetsBusyReasons.SolvesIssue && heroGetsBusyReason != HeroGetsBusyReasons.Traveling)
		{
			return;
		}
		if (Mission.Current != null)
		{
			for (int i = 0; i < Mission.Current.Agents.Count; i++)
			{
				Agent agent = Mission.Current.Agents[i];
				if (agent.IsHuman && agent.Character.IsHero && ((CharacterObject)agent.Character).HeroObject == hero)
				{
					ClearGatherOrderedAgentIfExists(agent);
					if (heroGetsBusyReason == HeroGetsBusyReasons.BecomeAlleyLeader)
					{
						AdjustTheBehaviorsOfTheAgent(agent);
					}
					break;
				}
			}
		}
		if (PlayerEncounter.LocationEncounter != null)
		{
			RemoveAccompanyingHero(hero);
			if (_isFollowingPlayer.Count == 0)
			{
				UpdateAccompanyingCharacters();
			}
		}
	}

	private void ClearGatherOrderedAgentIfExists(Agent agent)
	{
		if (_gatherOrderedAgent == agent)
		{
			_gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().RemoveBehavior<ScriptBehavior>();
			_gatherOrderedAgent = null;
		}
	}

	private void OnNewCompanionAdded(Hero newCompanion)
	{
		Location location = null;
		LocationComplex current = LocationComplex.Current;
		if (current != null)
		{
			foreach (Location listOfLocation in current.GetListOfLocations())
			{
				foreach (LocationCharacter character in listOfLocation.GetCharacterList())
				{
					if (character.Character == newCompanion.CharacterObject)
					{
						location = LocationComplex.Current.GetLocationOfCharacter(character);
						break;
					}
				}
			}
		}
		if (current?.GetLocationWithId("center") != null && location == null)
		{
			AgentData agentData = new AgentData(new PartyAgentOrigin(PartyBase.MainParty, newCompanion.CharacterObject)).Monster(TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(newCompanion.CharacterObject.Race)).NoHorses(noHorses: true);
			current.GetLocationWithId("center").AddCharacter(new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, null, fixedLocation: true, LocationCharacter.CharacterRelations.Friendly, null, useCivilianEquipment: true));
		}
	}

	private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
	{
		if (Campaign.Current.GameMode != CampaignGameMode.Campaign || MobileParty.MainParty.CurrentSettlement == null || LocationComplex.Current == null || (!settlement.IsTown && !settlement.IsCastle && !settlement.IsVillage))
		{
			return;
		}
		if (mobileParty == null && settlement == MobileParty.MainParty.CurrentSettlement && hero.Clan == Clan.PlayerClan)
		{
			if (_isFollowingPlayer.Contains(hero) && hero.PartyBelongedTo == null)
			{
				RemoveAccompanyingHero(hero);
				if (_isFollowingPlayer.Count == 0)
				{
					UpdateAccompanyingCharacters();
				}
			}
		}
		else if (mobileParty == MobileParty.MainParty && MobileParty.MainParty.IsActive)
		{
			UpdateAccompanyingCharacters();
		}
	}

	private bool clan_member_follow_me_on_condition()
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.LocationComplex != null && !Settlement.CurrentSettlement.IsHideout)
		{
			Location location = (Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("village_center") : Settlement.CurrentSettlement.LocationComplex.GetLocationWithId("center"));
			if (Hero.OneToOneConversationHero != null && ConversationMission.OneToOneConversationAgent != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty && CampaignMission.Current?.Location == location && ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
			{
				return !(ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehavior() is FollowAgentBehavior);
			}
			return false;
		}
		return false;
	}

	private bool clan_member_dont_follow_me_on_condition()
	{
		if (ConversationMission.OneToOneConversationAgent != null && Hero.OneToOneConversationHero.Clan == Clan.PlayerClan && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty && ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator != null)
		{
			return ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetActiveBehavior() is FollowAgentBehavior;
		}
		return false;
	}

	private bool clan_members_gather_on_condition()
	{
		if (GameStateManager.Current.ActiveState is MissionState)
		{
			if (_gatherOrderedAgent != null || Settlement.CurrentSettlement == null)
			{
				return false;
			}
			InterruptingBehaviorGroup interruptingBehaviorGroup = ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator?.GetBehaviorGroup<InterruptingBehaviorGroup>();
			if (interruptingBehaviorGroup != null && interruptingBehaviorGroup.IsActive)
			{
				return false;
			}
			Agent oneToOneConversationAgent = ConversationMission.OneToOneConversationAgent;
			CharacterObject oneToOneConversationCharacter = ConversationMission.OneToOneConversationCharacter;
			if (!oneToOneConversationCharacter.IsHero || oneToOneConversationCharacter.HeroObject.Clan != Hero.MainHero.Clan)
			{
				return false;
			}
			foreach (Agent agent in Mission.Current.Agents)
			{
				CharacterObject characterObject = (CharacterObject)agent.Character;
				if (agent.IsHuman && agent != oneToOneConversationAgent && agent != Agent.Main && characterObject.IsHero && characterObject.HeroObject.Clan == Clan.PlayerClan && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty)
				{
					AgentNavigator agentNavigator = agent.GetComponent<CampaignAgentComponent>().AgentNavigator;
					if (agentNavigator != null && !(agentNavigator.GetActiveBehavior() is FollowAgentBehavior))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool clan_members_gather_end_on_condition()
	{
		if (ConversationMission.OneToOneConversationAgent != null && _gatherOrderedAgent == ConversationMission.OneToOneConversationAgent)
		{
			if (ConversationMission.OneToOneConversationAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<InterruptingBehaviorGroup>().IsActive)
			{
				return false;
			}
			return true;
		}
		if (!IsAgentFollowingPlayerAsCompanion(ConversationMission.OneToOneConversationAgent))
		{
			return false;
		}
		foreach (Agent agent in Mission.Current.Agents)
		{
			if (agent != ConversationMission.OneToOneConversationAgent && IsAgentFollowingPlayerAsCompanion(agent))
			{
				return true;
			}
		}
		return false;
	}

	private void clan_member_gather_on_consequence()
	{
		_gatherOrderedAgent = ConversationMission.OneToOneConversationAgent;
		_gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<ScriptBehavior>().IsActive = true;
		ScriptBehavior.AddTargetWithDelegate(_gatherOrderedAgent, SelectTarget, null, OnTargetReached);
		_gatherOrderedAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>().AddBehavior<FollowAgentBehavior>().IsActive = false;
	}

	private void clan_member_dont_follow_me_on_consequence()
	{
		RemoveFollowBehavior(ConversationMission.OneToOneConversationAgent);
	}

	private void clan_members_dont_follow_me_on_consequence()
	{
		foreach (Agent agent in Mission.Current.Agents)
		{
			RemoveFollowBehavior(agent);
		}
	}

	private void RemoveFollowBehavior(Agent agent)
	{
		ClearGatherOrderedAgentIfExists(agent);
		if (IsAgentFollowingPlayerAsCompanion(agent))
		{
			AdjustTheBehaviorsOfTheAgent(agent);
			LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
			RemoveAccompanyingHero(locationCharacter.Character.HeroObject);
		}
	}

	private void AdjustTheBehaviorsOfTheAgent(Agent agent)
	{
		DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		behaviorGroup.RemoveBehavior<FollowAgentBehavior>();
		ScriptBehavior behavior = behaviorGroup.GetBehavior<ScriptBehavior>();
		if (behavior != null)
		{
			behavior.IsActive = true;
		}
		WalkingBehavior walkingBehavior = behaviorGroup.GetBehavior<WalkingBehavior>();
		if (walkingBehavior == null)
		{
			walkingBehavior = behaviorGroup.AddBehavior<WalkingBehavior>();
		}
		walkingBehavior.IsActive = true;
	}

	private void clan_member_follow_me_on_consequence()
	{
		LocationCharacter locationCharacterOfHero = LocationComplex.Current.GetLocationCharacterOfHero(Hero.OneToOneConversationHero);
		if (!IsFollowingPlayer(locationCharacterOfHero.Character.HeroObject))
		{
			_isFollowingPlayer.Add(locationCharacterOfHero.Character.HeroObject);
		}
		AddClanMembersAsAccompanyingCharacter(locationCharacterOfHero.Character.HeroObject, locationCharacterOfHero);
		Campaign.Current.ConversationManager.ConversationEndOneShot += FollowMainAgent;
	}

	private bool SelectTarget(Agent agent, ref Agent targetAgent, ref UsableMachine targetEntity, ref WorldFrame targetFrame, ref float customTargetReachedRangeThreshold, ref float customTargetReachedRotationThreshold)
	{
		if (Agent.Main == null)
		{
			return false;
		}
		Agent agent2 = null;
		float num = float.MaxValue;
		foreach (Agent agent3 in Mission.Current.Agents)
		{
			CharacterObject characterObject = (CharacterObject)agent3.Character;
			CampaignAgentComponent component = agent3.GetComponent<CampaignAgentComponent>();
			if (agent3 == agent || !agent3.IsHuman || !characterObject.IsHero || characterObject.HeroObject.Clan != Clan.PlayerClan || characterObject.HeroObject.PartyBelongedTo != MobileParty.MainParty || component.AgentNavigator == null)
			{
				continue;
			}
			AgentBehavior behavior = agent3.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehavior<FollowAgentBehavior>();
			if (behavior == null || !behavior.IsActive)
			{
				float num2 = agent.Position.DistanceSquared(agent3.Position);
				if (num2 < num)
				{
					num = num2;
					agent2 = agent3;
				}
			}
		}
		if (agent2 != null)
		{
			targetAgent = agent2;
			return true;
		}
		DailyBehaviorGroup behaviorGroup = agent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		FollowAgentBehavior behavior2 = behaviorGroup.GetBehavior<FollowAgentBehavior>();
		behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
		behavior2.IsActive = true;
		behavior2.SetTargetAgent(Agent.Main);
		ScriptBehavior behavior3 = behaviorGroup.GetBehavior<ScriptBehavior>();
		if (behavior3 != null)
		{
			behavior3.IsActive = false;
		}
		WalkingBehavior behavior4 = behaviorGroup.GetBehavior<WalkingBehavior>();
		if (behavior4 != null)
		{
			behavior4.IsActive = false;
		}
		LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(agent);
		if (!IsFollowingPlayer(locationCharacter.Character.HeroObject))
		{
			_isFollowingPlayer.Add(locationCharacter.Character.HeroObject);
		}
		AddClanMembersAsAccompanyingCharacter(locationCharacter.Character.HeroObject, locationCharacter);
		_gatherOrderedAgent = null;
		return false;
	}

	private bool OnTargetReached(Agent agent, ref Agent targetAgent, ref UsableMachine targetEntity, ref WorldFrame targetFrame)
	{
		if (Agent.Main == null)
		{
			return false;
		}
		if (targetAgent == null)
		{
			return true;
		}
		LocationCharacter locationCharacter = LocationComplex.Current.FindCharacter(targetAgent);
		if (locationCharacter == null)
		{
			return true;
		}
		DailyBehaviorGroup behaviorGroup = targetAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.GetBehaviorGroup<DailyBehaviorGroup>();
		FollowAgentBehavior followAgentBehavior = behaviorGroup.AddBehavior<FollowAgentBehavior>();
		behaviorGroup.SetScriptedBehavior<FollowAgentBehavior>();
		followAgentBehavior.SetTargetAgent(Agent.Main);
		if (!IsFollowingPlayer(locationCharacter.Character.HeroObject))
		{
			_isFollowingPlayer.Add(locationCharacter.Character.HeroObject);
			AddClanMembersAsAccompanyingCharacter(locationCharacter.Character.HeroObject, locationCharacter);
		}
		targetAgent = null;
		return true;
	}

	private void UpdateAccompanyingCharacters()
	{
		_isFollowingPlayer.Clear();
		PlayerEncounter.LocationEncounter.RemoveAllAccompanyingCharacters();
		bool flag = false;
		foreach (TroopRosterElement item in MobileParty.MainParty.MemberRoster.GetTroopRoster())
		{
			if (item.Character.IsHero)
			{
				Hero heroObject = item.Character.HeroObject;
				if (heroObject != Hero.MainHero && !heroObject.IsPrisoner && !heroObject.IsWounded && heroObject.Age >= (float)Campaign.Current.Models.AgeModel.HeroComesOfAge && !flag)
				{
					_isFollowingPlayer.Add(heroObject);
					flag = true;
				}
			}
		}
	}

	private void RemoveAccompanyingHero(Hero hero)
	{
		_isFollowingPlayer.Remove(hero);
		PlayerEncounter.LocationEncounter?.RemoveAccompanyingCharacter(hero);
	}

	private bool IsAgentFollowingPlayerAsCompanion(Agent agent)
	{
		CharacterObject characterObject = agent?.Character as CharacterObject;
		CampaignAgentComponent campaignAgentComponent = agent?.GetComponent<CampaignAgentComponent>();
		if (agent != null && agent.IsHuman && characterObject != null && characterObject.IsHero && characterObject.HeroObject.Clan == Clan.PlayerClan && characterObject.HeroObject.PartyBelongedTo == MobileParty.MainParty)
		{
			return campaignAgentComponent.AgentNavigator?.GetActiveBehavior() is FollowAgentBehavior;
		}
		return false;
	}

	private void AddClanMembersAsAccompanyingCharacter(Hero member, LocationCharacter locationCharacter = null)
	{
		CharacterObject characterObject = member.CharacterObject;
		if (characterObject.IsHero && !characterObject.HeroObject.IsWounded && IsFollowingPlayer(member))
		{
			LocationCharacter locationCharacter2 = locationCharacter ?? LocationCharacter.CreateBodyguardHero(characterObject.HeroObject, MobileParty.MainParty, SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior);
			PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(locationCharacter2, isFollowing: true);
			AccompanyingCharacter accompanyingCharacter = PlayerEncounter.LocationEncounter.GetAccompanyingCharacter(characterObject);
			accompanyingCharacter.DisallowEntranceToAllLocations();
			accompanyingCharacter.AllowEntranceToLocations((Location x) => x == LocationComplex.Current.GetLocationWithId("center") || x == LocationComplex.Current.GetLocationWithId("village_center") || x == LocationComplex.Current.GetLocationWithId("tavern"));
		}
	}
}
