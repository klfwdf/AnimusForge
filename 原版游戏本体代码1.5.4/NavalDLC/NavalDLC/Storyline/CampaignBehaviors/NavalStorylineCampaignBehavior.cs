using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Helpers;
using NavalDLC.Storyline.Quests;
using SandBox;
using SandBox.GameComponents;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using StoryMode;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace NavalDLC.Storyline.CampaignBehaviors;

public class NavalStorylineCampaignBehavior : CampaignBehaviorBase
{
	private const int RansomGoldCost = 10000;

	private bool _isNavalStorylineActive;

	private NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes _activeMissionType = NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.None;

	private bool _isNavalStorylineCanceled;

	private TroopRoster _troops = TroopRoster.CreateDummyTroopRoster();

	private TroopRoster _prisoners = TroopRoster.CreateDummyTroopRoster();

	private List<Ship> _ships = new List<Ship>();

	private bool _inquiryFired;

	private AnchorPoint _cachedAnchor;

	private NavalStorylineData.NavalStorylineStage _lastCompletedStorylineStage = NavalStorylineData.NavalStorylineStage.None;

	private bool _isFirstReturnToOstican = true;

	private bool _isTutorialSkipped;

	private CampaignTime _sisterReturnTime = CampaignTime.Zero;

	private bool _removeCrimeHandler;

	private int _foodStage = 1;

	private NavalStorylineData.NavalStorylineCheckpoint _lastSavedCheckpoint;

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		if (NavalStorylineData.Gunnar.IsDisabled || NavalStorylineData.Gunnar.IsNotSpawned)
		{
			NavalStorylineData.Gunnar.ChangeState(Hero.CharacterStates.Active);
		}
		if (NavalStorylineData.Gunnar.PartyBelongedTo == null && NavalStorylineData.Gunnar.StayingInSettlement == null)
		{
			EnterSettlementAction.ApplyForCharacterOnly(NavalStorylineData.Gunnar, NavalStorylineData.HomeSettlement);
		}
	}

	public override void RegisterEvents()
	{
		if (!_isNavalStorylineCanceled)
		{
			CampaignEvents.TickEvent.AddNonSerializedListener(this, Tick);
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
			CampaignEvents.OnHeirSelectionOverEvent.AddNonSerializedListener(this, OnHeirSelectionOver);
			CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
			CampaignEvents.CanHeroBecomePrisonerEvent.AddNonSerializedListener(this, CanHeroBecomePrisoner);
			CampaignEvents.CanHaveCampaignIssuesEvent.AddNonSerializedListener(this, CanHaveCampaignIssues);
			CampaignEvents.OnMobilePartyNavigationStateChangedEvent.AddNonSerializedListener(this, OnMobilePartyNavigationStateChanged);
			CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnSettlementLeft);
			CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTick);
			CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
			CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreated);
			CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
			NavalDLCEvents.OnNavalStorylineTutorialSkippedEvent.AddNonSerializedListener(this, OnNavalStorylineSkipped);
			NavalDLCEvents.OnNavalStorylineCanceledEvent.AddNonSerializedListener(this, OnNavalStorylineCanceled);
			CampaignEvents.AfterMissionStarted.AddNonSerializedListener(this, AfterMissionStarted);
			CampaignEvents.MissionTickEvent.AddNonSerializedListener(this, MissionTickEvent);
			CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
			CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
			CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, OnHeroComesOfAge);
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
			CampaignEvents.IsSettlementBusyEvent.AddNonSerializedListener(this, IsSettlementBusy);
		}
	}

	private void IsSettlementBusy(Settlement settlement, object asker, ref int priority)
	{
		if (asker != this && settlement == NavalStorylineData.HomeSettlement && !NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5))
		{
			priority = Math.Max(priority, 400);
		}
	}

	private void OnMissionEnded(IMission mission)
	{
		if (_isNavalStorylineActive && _activeMissionType != NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.None)
		{
			_activeMissionType = NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes.None;
		}
	}

	private void HourlyTick()
	{
		if (MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.SiegeEvent == null && Hero.MainHero.IsActive && !MobileParty.MainParty.IsInNavalAutoTravel && IsWaitingForSistersReturn() && _sisterReturnTime.IsPast)
		{
			ShowSisterPopUp();
			_sisterReturnTime = CampaignTime.Zero;
		}
	}

	private void ShowSisterPopUp()
	{
		TextObject textObject = new TextObject("{=FdXpi6Ql}Word from Gunnar");
		TextObject textObject2 = new TextObject("{=9AjMDDOJ}You receive a message from Gunnar urging you to hurry back to Ostican. He has found and ransomed your sister.");
		InformationManager.ShowInquiry(new InquiryData(affirmativeText: new TextObject("{=DM6luo3c}Continue").ToString(), titleText: textObject.ToString(), text: textObject2.ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: false, negativeText: string.Empty, affirmativeAction: OnSisterRansomed, negativeAction: null), pauseGameActiveState: true);
	}

	private void OnSisterRansomed()
	{
		NavalStorylineData.Gunnar.ChangeState(Hero.CharacterStates.Active);
		TeleportHeroAction.ApplyImmediateTeleportToSettlement(NavalStorylineData.Gunnar, NavalStorylineData.HomeSettlement);
		_lastCompletedStorylineStage = NavalStorylineData.NavalStorylineStage.Act3SpeakToGunnarAndSister;
		NavalDLCEvents.Instance.OnSisterRansomed();
	}

	private bool IsSister(IAgent agent)
	{
		return agent.Character == StoryModeHeroes.LittleSister.CharacterObject;
	}

	private bool IsGunnar(IAgent agent)
	{
		return agent.Character == NavalStorylineData.Gunnar.CharacterObject;
	}

	private bool IsPlayer(IAgent agent)
	{
		return agent.Character == Hero.MainHero.CharacterObject;
	}

	private void OnHeroComesOfAge(Hero hero)
	{
		if (hero == StoryModeHeroes.LittleSister && NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5))
		{
			StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(hero);
		}
	}

	private void OnGameLoadFinished()
	{
		if (!MBSaveLoad.IsUpdatingGameVersion)
		{
			return;
		}
		if (StoryModeHeroes.LittleSister.IsAlive && MBSaveLoad.IsUpdatingGameVersion && MBSaveLoad.LastLoadedGameVersion < ApplicationVersion.FromString("v1.3.15.109185"))
		{
			AgingCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<AgingCampaignBehavior>();
			FieldInfo field = typeof(AgingCampaignBehavior).GetField("_heroesYoungerThanHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic);
			Dictionary<Hero, int> dictionary = ((campaignBehavior != null) ? ((Dictionary<Hero, int>)field.GetValue(campaignBehavior)) : null);
			bool flag = NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5);
			if (StoryModeHeroes.LittleSister.Age < (float)Campaign.Current.Models.AgeModel.HeroComesOfAge)
			{
				if (!StoryModeHeroes.LittleSister.IsDisabled && !StoryModeHeroes.LittleSister.IsNotSpawned)
				{
					if (flag)
					{
						StoryModeHeroes.LittleSister.ChangeState(Hero.CharacterStates.NotSpawned);
					}
					else
					{
						DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
					}
				}
				if (!StoryModeHeroes.LittleSister.IsDisabled && dictionary != null && !dictionary.ContainsKey(StoryModeHeroes.LittleSister))
				{
					dictionary.Add(StoryModeHeroes.LittleSister, (int)StoryModeHeroes.LittleSister.Age);
					field.SetValue(campaignBehavior, dictionary);
				}
			}
			else if (flag)
			{
				if (dictionary != null && dictionary.ContainsKey(StoryModeHeroes.LittleSister))
				{
					dictionary.Remove(StoryModeHeroes.LittleSister);
				}
				CheckPlayerSiblingsEducationStages(StoryModeHeroes.LittleSister);
				CheckStoryModeHeroStateAndUpdateIfNeeded(StoryModeHeroes.LittleSister);
				StoryModeHelpers.SetPlayerSiblingsSkillsIfNeeded(StoryModeHeroes.LittleSister);
			}
			else if (!StoryModeHeroes.LittleSister.IsDisabled)
			{
				DisableHeroAction.Apply(StoryModeHeroes.LittleSister);
				if (StoryModeHeroes.LittleSister.GovernorOf != null)
				{
					ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleSister);
				}
			}
			CheckAndUpdateGovernorStatusOfStoryModeHero(StoryModeHeroes.LittleSister);
		}
		if (MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.3.15")))
		{
			if (NavalStorylineData.GetStorylineStage() >= NavalStorylineData.NavalStorylineStage.Act3Quest5 && !NavalStorylineData.Gunnar.IsDead)
			{
				Settlement currentSettlement = NavalStorylineData.Gunnar.CurrentSettlement;
				Village village = Village.All.FirstOrDefault((Village x) => x.Settlement.StringId == "village_N1_2");
				if (village != null && currentSettlement != village.Settlement)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(NavalStorylineData.Gunnar, village.Settlement);
				}
			}
			NavalStorylineData.SetHeroText(NavalStorylineData.Gunnar);
			NavalStorylineData.SetHeroText(NavalStorylineData.Purig);
			NavalStorylineData.SetHeroText(NavalStorylineData.Lahar);
			NavalStorylineData.SetHeroText(NavalStorylineData.EmiraAlFahda);
			NavalStorylineData.SetHeroText(NavalStorylineData.Prusas);
			NavalStorylineData.SetHeroText(NavalStorylineData.Bjolgur);
			if (NavalStorylineData.Purig.IsDead)
			{
				SetOnPurigKilledTexts();
			}
		}
		if (!MBSaveLoad.LastLoadedGameVersion.IsOlderThan(ApplicationVersion.FromString("v1.4.0")) || !_isNavalStorylineActive)
		{
			return;
		}
		for (int num = Campaign.Current.QuestManager.Quests.Count - 1; num >= 0; num--)
		{
			QuestBase questBase = Campaign.Current.QuestManager.Quests[num];
			if (NavalStorylineData.ShouldIssueQuestCancelled(questBase))
			{
				questBase.CompleteQuestWithCancel(GameTexts.FindText("str_quest_cancellation_due_to_naval_storyline"));
			}
		}
	}

	private void CheckPlayerSiblingsEducationStages(Hero hero)
	{
		EducationCampaignBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<EducationCampaignBehavior>();
		if (campaignBehavior != null)
		{
			Type typeFromHandle = typeof(EducationCampaignBehavior);
			if (((Dictionary<Hero, short>)typeFromHandle.GetField("_previousEducations", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(campaignBehavior)).ContainsKey(hero) || !IsHeroAttributesInitialized(hero))
			{
				typeFromHandle.GetMethod("OnHeroComesOfAge", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(campaignBehavior, new object[1] { hero });
			}
		}
	}

	private void CheckStoryModeHeroStateAndUpdateIfNeeded(Hero hero)
	{
		if (hero.IsNotSpawned || hero.IsDisabled)
		{
			Settlement settlementToSpawnForPlayerRelative = GetSettlementToSpawnForPlayerRelative(hero);
			if (hero.BornSettlement == null)
			{
				hero.BornSettlement = settlementToSpawnForPlayerRelative;
			}
			TeleportHeroAction.ApplyImmediateTeleportToSettlement(hero, settlementToSpawnForPlayerRelative);
			if (!hero.IsActive)
			{
				hero.ChangeState(Hero.CharacterStates.Active);
			}
		}
		if (hero.Clan == null)
		{
			hero.Clan = Clan.PlayerClan;
			if (!hero.IsFugitive)
			{
				MakeHeroFugitiveAction.Apply(hero);
			}
		}
	}

	private void CheckAndUpdateGovernorStatusOfStoryModeHero(Hero hero)
	{
		if (hero.GovernorOf != null && hero.CurrentSettlement != hero.GovernorOf.Settlement)
		{
			Debug.FailedAssert("Last governor check might be unnecessary, check this case", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\NavalDLC\\Storyline\\CampaignBehaviors\\NavalStorylineCampaignBehavior.cs", "CheckAndUpdateGovernorStatusOfStoryModeHero", 370);
			ChangeGovernorAction.RemoveGovernorOf(StoryModeHeroes.LittleSister);
		}
	}

	private bool IsHeroAttributesInitialized(Hero hero)
	{
		if (hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Vigor) == 0 && hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Control) == 0 && hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Endurance) == 0 && hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Cunning) == 0 && hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Social) == 0)
		{
			return hero.CharacterAttributes.GetPropertyValue(DefaultCharacterAttributes.Intelligence) != 0;
		}
		return true;
	}

	private Settlement GetSettlementToSpawnForPlayerRelative(Hero hero)
	{
		if (hero.GovernorOf != null)
		{
			return hero.GovernorOf.Settlement;
		}
		if (!hero.HomeSettlement.OwnerClan.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			return hero.HomeSettlement;
		}
		if (!Clan.PlayerClan.MapFaction.Settlements.IsEmpty())
		{
			return Clan.PlayerClan.MapFaction.Settlements.GetRandomElement();
		}
		foreach (Settlement item in Settlement.All)
		{
			if (!item.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
			{
				return item;
			}
		}
		return Village.All.GetRandomElement().Settlement;
	}

	private void MissionTickEvent(float dt)
	{
		if (_removeCrimeHandler)
		{
			RemoveCrimeHandler(Mission.Current);
			_removeCrimeHandler = false;
		}
	}

	private void AfterMissionStarted(IMission mission)
	{
		if (_isNavalStorylineActive && LocationComplex.Current != null && Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && Campaign.Current.Models.CrimeModel.IsPlayerCrimeRatingSevere(Settlement.CurrentSettlement.MapFaction))
		{
			_removeCrimeHandler = true;
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (victim == NavalStorylineData.Purig)
		{
			SetOnPurigKilledTexts();
		}
	}

	private void SetOnPurigKilledTexts()
	{
		TextObject textObject = new TextObject("{=QPn1OTcd}Purig was a Nord warrior who fought in the rebellion against Volbjorn, first ruler of the Nordvyg. Following the king's victory he joined with other defeated rebels to form the Sea Hounds, a pirate confederation that terrorized the northern seas of Calradia, but he was defeated and slain by {PLAYER.NAME}.");
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject);
		NavalStorylineData.Purig.EncyclopediaText = textObject;
		TextObject textObject2 = new TextObject("{=8NxBfsY1}Gunnar of Lagshofn is a Nord warrior from the island of Beinland. He won a reputation for courage fighting in a rebellion against Volbjorn, first king of the Nordvyg, but after the rebels' defeat he made his peace with the victors. He then joined with {PLAYER.NAME} to vanquish the Sea Hounds, a pirate confederation led by Purig that had terrorized the northern seas of Calradia.");
		StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter, textObject2);
		NavalStorylineData.Gunnar.EncyclopediaText = textObject2;
	}

	private void OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail detail)
	{
		_isNavalStorylineCanceled = true;
		CampaignEventDispatcher.Instance.RemoveListeners(this);
	}

	private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == MobileParty.MainParty && NavalStorylineData.Gunnar.StayingInSettlement == settlement && settlement.StringId.Equals("village_N1_2"))
		{
			Monster monsterWithSuffix = TaleWorlds.Core.FaceGen.GetMonsterWithSuffix(NavalStorylineData.Gunnar.CharacterObject.Race, "_settlement");
			(string, Monster) tuple = (ActionSetCode.GenerateActionSetNameWithSuffix(monsterWithSuffix, NavalStorylineData.Gunnar.CharacterObject.IsFemale, "_lord"), monsterWithSuffix);
			uint color = (uint)(((int?)NavalStorylineData.Gunnar.MapFaction?.Color) ?? (-3357781));
			uint color2 = (uint)(((int?)NavalStorylineData.Gunnar.MapFaction?.Color) ?? (-3357781));
			AgentData agentData = new AgentData(new SimpleAgentOrigin(NavalStorylineData.Gunnar.CharacterObject)).Monster(tuple.Item2).NoHorses(noHorses: true).ClothingColor1(color)
				.ClothingColor2(color2);
			LocationComplex.Current.GetLocationWithId("village_center").AddCharacter(new LocationCharacter(agentData, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "sp_notable", fixedLocation: true, LocationCharacter.CharacterRelations.Friendly, tuple.Item1, useCivilianEquipment: true));
		}
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails details)
	{
		if (!(quest is NavalStorylineQuestBase { WillProgressStoryline: not false } navalStorylineQuestBase))
		{
			return;
		}
		if (details == QuestBase.QuestCompleteDetails.Cancel)
		{
			ChangeNavalStorylineActivity(activity: false);
			return;
		}
		if (navalStorylineQuestBase.Stage < NavalStorylineData.NavalStorylineStage.Act3Quest5)
		{
			new ReturnToBaseQuest("naval_storyline_return_to_base", NavalStorylineData.Gunnar).StartQuest();
		}
		_lastCompletedStorylineStage = navalStorylineQuestBase.Stage;
	}

	private void CanHeroBecomePrisoner(Hero hero, ref bool result)
	{
		if (_isNavalStorylineActive && NavalStorylineData.IsNavalStorylineHero(hero))
		{
			result = false;
		}
	}

	private void OnSettlementLeft(MobileParty party, Settlement settlement)
	{
		if (party == MobileParty.MainParty && _isNavalStorylineActive)
		{
			Campaign.Current.SaveHandler.ForceAutoSave();
			if (!MobileParty.MainParty.IsCurrentlyAtSea)
			{
				ChangeNavalStorylineActivity(activity: false);
			}
		}
	}

	private void OnMobilePartyNavigationStateChanged(MobileParty mobileParty)
	{
		if (_isNavalStorylineActive && mobileParty.IsMainParty && !mobileParty.IsCurrentlyAtSea && PlayerEncounter.EncounterSettlement == null)
		{
			ChangeNavalStorylineActivity(activity: false);
		}
	}

	private void OnHeirSelectionOver(Hero hero)
	{
		if (_isNavalStorylineActive)
		{
			ChangeNavalStorylineActivity(activity: false);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		AddGameMenus(starter);
		AddDialogues();
	}

	private void AddDialogues()
	{
		AddGunnarSeaDefaultConversations();
		AddGunnarTownDefaultConversations();
		AddGunnarRansomConversations();
		AddGunnarSisterRansomConversations();
		AddGunnarStorylineActivationNotPossibleConversation();
		AddBjolgurDefaultConversations();
		AddLaharDefaultConversations();
	}

	private void AddGunnarSeaDefaultConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 200).NpcLine("{=0zTShzbi}Keep an eye on the horizon, and look for sails.").Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && Settlement.CurrentSettlement == null && MobileParty.MainParty.IsCurrentlyAtSea && Hero.OneToOneConversationHero.PartyBelongedTo == MobileParty.MainParty)
			.CloseDialog());
	}

	private void AddGunnarTownDefaultConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 200).NpcLine("{=Si6F4bdz}I'm waiting for more news. Soon, I may have more to tell you.").Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && Settlement.CurrentSettlement != null)
			.CloseDialog());
	}

	private void AddGunnarStorylineActivationNotPossibleConversation()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 30000).NpcLine("{=njVdva7h}This isn't the right time to pursue our war against the Sea Hounds, but believe me, I am not about to abandon it.").Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && !NavalStorylineData.IsStorylineActivationPossible())
			.PlayerLine("{=KrsZJv1e}I shall return, hopefully under better circumstances.")
			.CloseDialog());
	}

	private void AddGunnarRansomConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("gunnar_ransom_sister", 1200).NpcLine("{=F94IaWhk}[ib:demure][if:convo_grave]Ah... So be it. I understand why you must put your sister's safety above other considerations. I know people who can pass a message to the Sea Hounds, and I can make inquiries about a ransom, if you like.").ClickableCondition(CanRansomSister)
			.GenerateToken(out var token)
			.BeginPlayerOptions()
			.PlayerOption("{=JIoiP1Is}Do that.")
			.GotoDialogState(token)
			.PlayerOption("{=NvCbw6VY}No. I will not pay money to pirates.")
			.NpcLine("{=BpQZOVIp}[ib:confident][if:convo_approving]I am glad that you see things that way.")
			.CloseDialog()
			.EndPlayerOptions()
			.NpcLine("{=R9byg5mp}[ib:closed]Very well. But I should warn you... By now, I am sure, the Sea Hounds know your name. You are building a bit of a reputation. I doubt that they’d give up your sister as cheaply as they would some common captive. If you left me {GOLD}{GOLD_ICON} denars, I'm sure that would suffice, but that kind of money may be hard to come by.", null, null, token)
			.Condition(delegate
			{
				MBTextManager.SetTextVariable("GOLD", 10000);
				MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
				return true;
			})
			.BeginPlayerOptions()
			.PlayerOption("{=rFaOeL2M}Here - this is {GOLD}{GOLD_ICON} denars. Take it, and make your inquiries.")
			.ClickableCondition(DoesPlayerHaveEnoughGoldToRansomSister)
			.NpcLine("{=nSWqf79K}Right... I will send word when I know more.")
			.Consequence(RequestRansomSister)
			.CloseDialog()
			.PlayerOption("{=UgWdVbxn}Somehow, I will raise the money.")
			.NpcLine("{=S5cioFLJ}[ib:normal]Very well. If you are able to raise the money, and still wish to proceed with the ransom, then let me know. I owe you my life, and I am always ready to help you in whatever course you choose.")
			.CloseDialog()
			.PlayerOption("{=OONSEXb2}I will never pay that kind of money to brigands.")
			.NpcLine("{=BpQZOVIp}[ib:confident][if:convo_approving]I am glad that you see things that way.")
			.CloseDialog()
			.EndPlayerOptions());
	}

	private void AddLaharDefaultConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 200).NpcLine("{=TAiPuK1n}When you're at sea, you long to be on shore. When you're on shore, shuffling about and waiting for things to be made ready, you'd give anything to be back at sea, running fast before the wind. That's always how it is.").Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Lahar)
			.CloseDialog());
	}

	private void AddGunnarSisterRansomConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 1200).NpcLine("{=Gkmt02Zo}{PLAYER.NAME}... I have someone who is eager to see you again!").Condition(delegate
		{
			int num;
			if (Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement)
			{
				num = ((_lastCompletedStorylineStage == NavalStorylineData.NavalStorylineStage.Act3SpeakToGunnarAndSister) ? 1 : 0);
				if (num != 0)
				{
					StringHelpers.SetCharacterProperties("PLAYER", CharacterObject.PlayerCharacter);
				}
			}
			else
			{
				num = 0;
			}
			return (byte)num != 0;
		})
			.Consequence(delegate
			{
				SpawnSister(GetSisterTeleportPosition());
			})
			.NpcLine("{=MmGO1qT4}{?PLAYER.GENDER}[if:convo_astonished]Sister{?}Brother{\\?}! Is that you? Heaven's mercy! I had all but given up hope, and then they told me that you had arranged for my ransom. Thank you, from the bottom of my heart, thank you!", IsSister, IsPlayer)
			.GenerateToken(out var token)
			.GenerateToken(out var token2)
			.BeginPlayerOptions()
			.PlayerOption("{=ZYtba6KE}My sister... Of course I could not leave you in the hands of those cruel men.", IsSister)
			.GotoDialogState(token)
			.PlayerOption("{=5Drb4hBh}A small price to pay for your safety. Well, maybe not that small...", IsSister)
			.GotoDialogState(token)
			.EndPlayerOptions()
			.NpcLine("{=QunHWmAo}[ib:demure][if:convo_grateful]That terrible night... Father, mother... Those vile slavers, dragging me from port to port. I won’t speak of it now. But Gunnar says that you have risen in the world, that our fortunes have changed. Know that I am ready to do my part, for our family and our future...", IsSister, IsPlayer, token)
			.NpcLine("{=LZSRoGTN}[if:convo_grave][ib:confident]{PLAYER.NAME}... I am glad to have helped you reunite your family, and I hope it repays part of my debt to you. But now I must take my leave. I have unfinished business with the Sea Hounds, and with Purig in particular. I do not think we shall meet again.", IsGunnar, IsPlayer)
			.BeginPlayerOptions()
			.PlayerOption("{=I2Ab1kzZ}Good hunting, Gunnar. Give that bastard Purig one from me.", IsGunnar)
			.GotoDialogState(token2)
			.PlayerOption("{=agCqAQuA}It seems a bit of a doomed errand, but good luck anyway.", IsGunnar)
			.GotoDialogState(token2)
			.EndPlayerOptions()
			.NpcLine("{=2g2FhKb5}[ib:normal]Farewell.", null, null, token2)
			.Consequence(EndStorylineByRansom)
			.CloseDialog());
	}

	private void SpawnSister(Vec3 spawnPosition)
	{
		Agent agent = Mission.Current.Agents.FirstOrDefault((Agent x) => IsSister(x));
		if (agent == null)
		{
			AgentBuildData agentBuildData = new AgentBuildData(StoryModeHeroes.LittleSister.CharacterObject);
			agentBuildData.TroopOrigin(new SimpleAgentOrigin(agentBuildData.AgentCharacter));
			agentBuildData.InitialPosition(in spawnPosition);
			agentBuildData.InitialDirection(-Agent.Main.LookDirection.AsVec2.Normalized());
			agentBuildData.NoHorses(noHorses: true);
			agentBuildData.CivilianEquipment(civilianEquipment: true);
			agent = Mission.Current.SpawnAgent(agentBuildData);
		}
		Campaign.Current.ConversationManager.AddConversationAgents(new MBList<IAgent> { agent }, setActionsInstantly: true);
		RemoveWalkingBehavior(StoryModeHeroes.LittleSister.CharacterObject);
	}

	private void RemoveWalkingBehavior(CharacterObject character)
	{
		Agent agent = Mission.Current.Agents.FirstOrDefault((Agent x) => x.Character == character);
		CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
		agent.ClearTargetFrame();
		component.AgentNavigator?.GetBehaviorGroup<DailyBehaviorGroup>()?.RemoveBehavior<WalkingBehavior>();
	}

	private Vec3 GetSisterTeleportPosition()
	{
		Vec3 position = Mission.Current.GetRandomPositionAroundPoint(Agent.Main.Position + Agent.Main.LookRotation.s * 3f, 1f, 1.5f);
		int num = 20;
		while (Mission.Current.Scene.GetNavigationMeshForPosition(in position) == UIntPtr.Zero && num > 0)
		{
			position = Mission.Current.GetRandomPositionAroundPoint(Agent.Main.Position + Agent.Main.LookRotation.s * 3f, 1f, 1.5f);
			num--;
		}
		return position;
	}

	private void EndStorylineByRansom()
	{
		NavalDLCEvents.Instance.OnNavalStorylineCanceled(NavalStorylineData.StorylineCancelDetail.ByRansom);
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.EndMission();
		};
		LocationComplex.Current?.RemoveCharacterIfExists(NavalStorylineData.Gunnar);
		DisableHeroAction.Apply(NavalStorylineData.Gunnar);
		NavalDLCHelpers.AddSisterToClan();
	}

	private bool DoesPlayerHaveEnoughGoldToRansomSister(out TextObject tooltip)
	{
		bool num = Hero.MainHero.Gold >= 10000;
		if (!num)
		{
			tooltip = new TextObject("{=d0kbtGYn}You don't have enough gold.");
		}
		else
		{
			tooltip = TextObject.GetEmpty();
		}
		MBTextManager.SetTextVariable("GOLD", 10000);
		MBTextManager.SetTextVariable("GOLD_ICON", "{=!}<img src=\"General\\Icons\\Coin@2x\" extend=\"6\">");
		return num;
	}

	private void RequestRansomSister()
	{
		if (!Campaign.Current.QuestManager.IsThereActiveQuestWithType(typeof(ScourgeoftheSeasQuest)))
		{
			new ScourgeoftheSeasQuest().StartQuest();
		}
		GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, 10000);
		_sisterReturnTime = CampaignTime.WeeksFromNow(3f);
		LocationComplex.Current?.RemoveCharacterIfExists(NavalStorylineData.Gunnar);
		DisableHeroAction.Apply(NavalStorylineData.Gunnar);
		Campaign.Current.ConversationManager.ConversationEndOneShot += delegate
		{
			Mission.Current.EndMission();
		};
		NavalDLCEvents.Instance.OnSisterRansomRequested();
	}

	private bool CanRansomSister(out TextObject tooltip)
	{
		tooltip = TextObject.GetEmpty();
		if (Hero.OneToOneConversationHero == NavalStorylineData.Gunnar && Settlement.CurrentSettlement == NavalStorylineData.HomeSettlement && !NavalStorylineData.IsNavalStorylineCanceled())
		{
			return !NavalStorylineData.IsNavalStoryLineActive();
		}
		return false;
	}

	private void AddBjolgurDefaultConversations()
	{
		Campaign.Current.ConversationManager.AddDialogFlow(DialogFlow.CreateDialogFlow("start", 200).NpcLine("{=fzhTwmvM}A battle at sea is a fine thing. Cowards have nowhere to run, and the fish do your cleaning-up for you.").Condition(() => Hero.OneToOneConversationHero == NavalStorylineData.Bjolgur)
			.CloseDialog());
	}

	private void home_settlement_encounter_init(MenuCallbackArgs args)
	{
		TextObject text = new TextObject("{=lqy3wHWi}You have returned to Ostican harbor. Gunnar takes his leave to see if any new information about the Sea Hounds has arrived, or any new allies to join you in your fight. He tells you to look for him in the harbor when you are ready to proceed.");
		if (_isFirstReturnToOstican)
		{
			text = new TextObject("{=7UmbvMKi}You return to Ostican harbor, and tie your ship up at the pier. Besides the Vlandian traders and fishing vessels lies a small Nordic longship. Gunnar tells you that some of his comrades have responded to his call to hunt the Sea Hounds. He tells you he needs to dictate a letter to some others, and asks you to meet him later in the port.");
		}
		MBTextManager.SetTextVariable("MENU_TEXT", text);
	}

	private void leave_on_consequence(MenuCallbackArgs args)
	{
		if (_isFirstReturnToOstican)
		{
			_isFirstReturnToOstican = false;
		}
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement;
		if (settlement == null)
		{
			GameMenu.ExitToLast();
		}
		else
		{
			GameMenu.SwitchToMenu((MobileParty.MainParty.HasNavalNavigationCapability && MobileParty.MainParty.Anchor.IsAtSettlement(settlement)) ? "naval_town_outside" : Campaign.Current.Models.EncounterGameMenuModel.GetEncounterMenu(PartyBase.MainParty, settlement.Party, out var _, out var _));
		}
	}

	private bool leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void AddGameMenus(CampaignGameStarter campaignGameStarter)
	{
		campaignGameStarter.AddGameMenu("naval_storyline_encounter_blocking", "{=LptlZGpR}The seas are rough, and it is difficult to bring your ship within hailing distance. Gunnar urges you not to waste time here, as you are in some haste.", virtual_encounter_init);
		campaignGameStarter.AddGameMenuOption("naval_storyline_encounter_blocking", "continue", "{=3sRdGQou}Leave", leave_on_condition, virtual_encounter_end_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("naval_storyline_outside_town", "{MENU_TEXT}", home_settlement_encounter_init);
		campaignGameStarter.AddGameMenuOption("naval_storyline_outside_town", "talk_to_gunnar", "{=fJP8DJcB}Talk to Gunnar in port", talk_to_gunnar_on_condition, talk_to_gunnar_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_outside_town", "continue", "{=8nP7PcCQ}Continue the story later", leave_on_condition, leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("naval_storyline_encounter_meeting", "{=!}.", game_menu_naval_storyline_encounter_meeting_on_init);
		campaignGameStarter.AddGameMenu("naval_storyline_encounter", "{=!}{ENCOUNTER_TEXT}", game_menu_naval_storyline_encounter_on_init, GameMenu.MenuOverlayType.Encounter);
		campaignGameStarter.AddGameMenuOption("naval_storyline_encounter", "attack", "{=zxMOqlhs}Attack", game_menu_naval_storyline_encounter_attack_on_condition, game_menu_naval_storyline_encounter_attack_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_encounter", "leave", "{=2YYRyrOO}Leave...", game_menu_naval_storyline_encounter_leave_on_condition, game_menu_naval_storyline_encounter_leave_on_consequence, isLeave: true);
		campaignGameStarter.AddGameMenu("naval_storyline_join_encounter", "{=jKWJpIES}{JOIN_ENCOUNTER_TEXT}. You decide to...", game_menu_join_naval_storyline_encounter_on_init);
		campaignGameStarter.AddGameMenuOption("naval_storyline_join_encounter", "join_encounter_help_attackers", "{=h3yEHb4U}Help {ATTACKER}.", game_menu_join_naval_storyline_encounter_help_attackers_on_condition, game_menu_join_naval_storyline_encounter_help_attackers_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_join_encounter", "join_encounter_help_defenders", "{=FwIgakj8}Help {DEFENDER}.", game_menu_join_naval_storyline_encounter_help_defenders_on_condition, game_menu_join_naval_storyline_encounter_help_defenders_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_storyline_join_encounter", "join_encounter_leave", "{=!}{LEAVE_TEXT}", game_menu_join_naval_storyline_encounter_leave_no_army_on_condition, game_menu_join_naval_storyline_encounter_leave_on_condition, isLeave: true);
		campaignGameStarter.AddGameMenuOption("town_outside", "contact_gunnar", "{=KStpUvo2}Hail Gunnar's contact for entry", talk_to_gunnar_town_outside_on_condition, contact_gunnar_on_consequence);
		campaignGameStarter.AddGameMenuOption("naval_town_outside", "contact_gunnar", "{=KStpUvo2}Hail Gunnar's contact for entry", talk_to_gunnar_town_outside_on_condition, contact_gunnar_on_consequence);
		campaignGameStarter.AddGameMenu("talk_to_gunnar_restricted", "{=sNybnI5O}Gunnar's contact snuck you into the town and lead you to him.", talk_to_gunnar_restricted_on_init);
		campaignGameStarter.AddGameMenuOption("talk_to_gunnar_restricted", "talk_to_gunnar_restricted_continue", "{=DM6luo3c}Continue", talk_to_gunnar_restricted_continue, talk_to_gunnar_on_consequence);
		campaignGameStarter.AddGameMenuOption("talk_to_gunnar_restricted", "leave", "{=3sRdGQou}Leave", null, contact_gunnar_leave_on_consequence);
	}

	private void talk_to_gunnar_restricted_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(Settlement.CurrentSettlement.SettlementComponent.WaitMeshName);
	}

	private bool talk_to_gunnar_town_outside_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Continue;
		NavalStorylineData.NavalStorylineStage storylineStage = NavalStorylineData.GetStorylineStage();
		Settlement currentSettlement = Settlement.CurrentSettlement;
		LocationComplex current = LocationComplex.Current;
		if (storylineStage >= NavalStorylineData.NavalStorylineStage.Act1 && storylineStage != NavalStorylineData.NavalStorylineStage.Act3Quest5 && !NavalStorylineData.IsMainPartyAllowed() && current != null && current.GetListOfCharacters().AnyQ((LocationCharacter t) => t.Character != null && t.Character.IsHero && t.Character.HeroObject == NavalStorylineData.Gunnar) && currentSettlement != null && !currentSettlement.IsUnderSiege)
		{
			return currentSettlement == NavalStorylineData.HomeSettlement;
		}
		return false;
	}

	private bool talk_to_gunnar_restricted_continue(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Conversation;
		return true;
	}

	private void contact_gunnar_on_consequence(MenuCallbackArgs args)
	{
		GameMenu.SwitchToMenu("talk_to_gunnar_restricted");
	}

	private void contact_gunnar_leave_on_consequence(MenuCallbackArgs args)
	{
		Settlement settlement = Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement;
		GameMenu.SwitchToMenu((MobileParty.MainParty.HasNavalNavigationCapability && MobileParty.MainParty.Anchor.IsAtSettlement(settlement)) ? "naval_town_outside" : Campaign.Current.Models.EncounterGameMenuModel.GetEncounterMenu(PartyBase.MainParty, settlement.Party, out var _, out var _));
	}

	private void game_menu_naval_storyline_encounter_meeting_on_init(MenuCallbackArgs args)
	{
		if (PlayerEncounter.Current != null && ((PlayerEncounter.Battle != null && PlayerEncounter.Battle.AttackerSide.LeaderParty != PartyBase.MainParty && PlayerEncounter.Battle.DefenderSide.LeaderParty != PartyBase.MainParty) || PlayerEncounter.MeetingDone))
		{
			if (PlayerEncounter.LeaveEncounter)
			{
				PlayerEncounter.Finish();
				return;
			}
			if (PlayerEncounter.Battle == null)
			{
				PlayerEncounter.StartBattle();
			}
			if (PlayerEncounter.BattleChallenge)
			{
				GameMenu.SwitchToMenu("duel_starter_menu");
				return;
			}
			MBTextManager.SetTextVariable("ENCOUNTER_TEXT", GameTexts.FindText("str_you_have_encountered_PARTY"));
			GameMenu.SwitchToMenu("naval_storyline_encounter");
		}
		else
		{
			PlayerEncounter.DoMeeting();
		}
	}

	private void game_menu_naval_storyline_encounter_on_init(MenuCallbackArgs args)
	{
		args.MenuContext.SetPanelSound("event:/ui/panels/battle/slide_in");
		if (PlayerEncounter.Battle == null)
		{
			if (MobileParty.MainParty.MapEvent != null)
			{
				PlayerEncounter.Init();
			}
			else
			{
				PlayerEncounter.StartBattle();
			}
		}
		PlayerEncounter.Update();
		if (PlayerEncounter.Current == null)
		{
			Campaign.Current.SaveHandler.SignalAutoSave();
		}
	}

	private bool game_menu_naval_storyline_encounter_attack_on_condition(MenuCallbackArgs args)
	{
		MenuCallbackArgs args2 = new MenuCallbackArgs(args.MapState, TextObject.GetEmpty());
		CampaignBattleResult campaignBattleResult = PlayerEncounter.CampaignBattleResult;
		if (campaignBattleResult != null && !campaignBattleResult.PlayerVictory && Hero.MainHero.IsWounded && !PlayerEncounter.PlayerSurrender)
		{
			PlayerEncounter.PlayerSurrender = true;
			PlayerEncounter.Update();
			return false;
		}
		if (MenuHelper.EncounterOrderAttackCondition(args2) && Hero.MainHero.HitPoints < Hero.MainHero.WoundedHealthLimit + 1)
		{
			Hero.MainHero.HitPoints = Hero.MainHero.WoundedHealthLimit + 1;
		}
		MenuHelper.CheckEnemyAttackableHonorably(args);
		return MenuHelper.EncounterAttackCondition(args);
	}

	private void game_menu_naval_storyline_encounter_attack_on_consequence(MenuCallbackArgs args)
	{
		MenuHelper.EncounterAttackConsequence(args);
	}

	private bool game_menu_naval_storyline_encounter_leave_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		return true;
	}

	private void game_menu_naval_storyline_encounter_leave_on_consequence(MenuCallbackArgs args)
	{
		MenuHelper.EncounterLeaveConsequence();
	}

	private void game_menu_join_naval_storyline_encounter_on_init(MenuCallbackArgs args)
	{
		MapEvent encounteredBattle = PlayerEncounter.EncounteredBattle;
		PartyBase leaderParty = encounteredBattle.GetLeaderParty(BattleSideEnum.Attacker);
		PartyBase leaderParty2 = encounteredBattle.GetLeaderParty(BattleSideEnum.Defender);
		if (leaderParty.IsMobile && leaderParty.MobileParty.Army != null)
		{
			MBTextManager.SetTextVariable("ATTACKER", leaderParty.MobileParty.ArmyName);
		}
		else
		{
			MBTextManager.SetTextVariable("ATTACKER", leaderParty.Name);
		}
		if (leaderParty2.IsMobile && leaderParty2.MobileParty.Army != null)
		{
			MBTextManager.SetTextVariable("DEFENDER", leaderParty2.MobileParty.ArmyName);
		}
		else
		{
			MBTextManager.SetTextVariable("DEFENDER", leaderParty2.Name);
		}
		MBTextManager.SetTextVariable("JOIN_ENCOUNTER_TEXT", GameTexts.FindText("str_come_across_battle"));
	}

	private void game_menu_join_naval_storyline_encounter_leave_on_condition(MenuCallbackArgs args)
	{
		PlayerEncounter.Finish();
	}

	private bool game_menu_join_naval_storyline_encounter_help_attackers_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
		return PlayerEncounter.EncounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Attacker);
	}

	private void game_menu_join_naval_storyline_encounter_help_attackers_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.JoinBattle(BattleSideEnum.Attacker);
		GameMenu.SwitchToMenu("naval_storyline_encounter");
	}

	private bool game_menu_join_naval_storyline_encounter_help_defenders_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.DefendAction;
		return PlayerEncounter.EncounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Defender);
	}

	private void game_menu_join_naval_storyline_encounter_help_defenders_on_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.JoinBattle(BattleSideEnum.Defender);
		GameMenu.ActivateGameMenu("naval_storyline_encounter");
	}

	private bool game_menu_join_naval_storyline_encounter_leave_no_army_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Leave;
		MBTextManager.SetTextVariable("LEAVE_TEXT", "{=ebUwP3Q3}Don't get involved.");
		return true;
	}

	[GameMenuInitializationHandler("naval_storyline_encounter")]
	private static void game_menu_naval_storyline_encounter_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
	}

	[GameMenuInitializationHandler("naval_storyline_encounter_meeting")]
	private static void game_menu_naval_storyline_encounter_meeting_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName("encounter_naval");
	}

	[GameMenuInitializationHandler("naval_storyline_join_encounter")]
	private static void game_menu_naval_storyline_join_encounter_on_init_background(MenuCallbackArgs args)
	{
		string encounterCultureBackgroundMesh = MenuHelper.GetEncounterCultureBackgroundMesh(PlayerEncounter.EncounteredParty.MapFaction.Culture);
		args.MenuContext.SetBackgroundMeshName(encounterCultureBackgroundMesh);
	}

	private void talk_to_gunnar_on_consequence(MenuCallbackArgs args)
	{
		leave_on_consequence(args);
		Mission mission = null;
		if (LocationComplex.Current != null && PlayerEncounter.LocationEncounter != null)
		{
			mission = (Mission)PlayerEncounter.LocationEncounter.CreateAndOpenMissionController(LocationComplex.Current.GetLocationWithId("port"), null, NavalStorylineData.Gunnar.CharacterObject);
		}
		else
		{
			Location locationWithId = NavalStorylineData.HomeSettlement.LocationComplex.GetLocationWithId("port");
			mission = (Mission)CampaignMission.OpenConversationMission(new ConversationCharacterData(CharacterObject.PlayerCharacter, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true), new ConversationCharacterData(NavalStorylineData.Gunnar.CharacterObject, PartyBase.MainParty, noHorse: true, noWeapon: true, spawnAfterFight: false, isCivilianEquipmentRequiredForLeader: false, isCivilianEquipmentRequiredForBodyGuardCharacters: false, noBodyguards: true), locationWithId.GetSceneName(NavalStorylineData.HomeSettlement.Town.GetWallLevel()));
		}
		RemoveCrimeHandler(mission);
	}

	[GameMenuInitializationHandler("naval_storyline_encounter_blocking")]
	private static void naval_storyline_encounter_meeting_blocking_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(SettlementHelper.FindNearestHideoutToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.All).WaitMeshName);
	}

	private void RemoveCrimeHandler(Mission mission)
	{
		MissionCrimeHandler missionBehavior = mission.GetMissionBehavior<MissionCrimeHandler>();
		if (missionBehavior != null)
		{
			mission.RemoveMissionBehavior(missionBehavior);
		}
	}

	[GameMenuInitializationHandler("naval_storyline_outside_town")]
	private static void naval_storyline_outside_town_on_init_background(MenuCallbackArgs args)
	{
		args.MenuContext.SetBackgroundMeshName(NavalStorylineData.HomeSettlement.SettlementComponent.WaitMeshName);
	}

	private bool talk_to_gunnar_on_condition(MenuCallbackArgs args)
	{
		args.optionLeaveType = GameMenuOption.LeaveType.Mission;
		return true;
	}

	private void virtual_encounter_end_consequence(MenuCallbackArgs args)
	{
		PlayerEncounter.Finish();
	}

	private void virtual_encounter_init(MenuCallbackArgs args)
	{
	}

	private void CanHaveCampaignIssues(Hero hero, ref bool result)
	{
		if (NavalStorylineData.IsNavalStorylineHero(hero))
		{
			result = false;
		}
	}

	private void OnNavalStorylineSkipped()
	{
		_lastCompletedStorylineStage = NavalStorylineData.NavalStorylineStage.Act2;
		_isTutorialSkipped = true;
	}

	private void Tick(float dt)
	{
		if (!_inquiryFired && !MobileParty.MainParty.IsInNavalAutoTravel && _isNavalStorylineActive && MobileParty.MainParty.IsCurrentlyAtSea && MobileParty.MainParty.IsTransitionInProgress)
		{
			InformationManager.ShowInquiry(new InquiryData(new TextObject("{=461jcc87}Leaving Story Mode").ToString(), new TextObject("{=dV92VE8i}When you leave story mode, you will be returned to Ostican. You can speak to Gunnar in port to try again later. Do you wish to continue?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), OnAcceptDeactivatingNavalStoryline, OnRejectDeactivatingNavalStoryline), pauseGameActiveState: true);
		}
	}

	private void OnAcceptDeactivatingNavalStoryline()
	{
		_inquiryFired = true;
	}

	private void OnRejectDeactivatingNavalStoryline()
	{
		MobileParty.MainParty.SetMoveModeHold();
		MobileParty.MainParty.CancelNavigationTransition();
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_isActive", ref _isNavalStorylineActive);
		dataStore.SyncData("_troops", ref _troops);
		dataStore.SyncData("_ships", ref _ships);
		dataStore.SyncData("_prisoners", ref _prisoners);
		dataStore.SyncData("_inquiryFired", ref _inquiryFired);
		dataStore.SyncData("_cachedAnchor", ref _cachedAnchor);
		dataStore.SyncData("_storylineStage", ref _lastCompletedStorylineStage);
		dataStore.SyncData("_isNavalStorylineCanceled", ref _isNavalStorylineCanceled);
		dataStore.SyncData("_isFirstReturnToOstican", ref _isFirstReturnToOstican);
		dataStore.SyncData("_isTutorialSkipped", ref _isTutorialSkipped);
		dataStore.SyncData("_foodStage", ref _foodStage);
		dataStore.SyncData("_sisterReturnTime", ref _sisterReturnTime);
		dataStore.SyncData("_lastSavedCheckpoint", ref _lastSavedCheckpoint);
	}

	public bool IsNavalStorylineActive()
	{
		return _isNavalStorylineActive;
	}

	private void CanHeroDie(Hero hero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
	{
		if (!_isNavalStorylineCanceled && NavalStorylineData.IsNavalStorylineHero(hero) && !NavalStorylineData.HasCompletedLast(NavalStorylineData.NavalStorylineStage.Act3Quest5))
		{
			result = false;
		}
	}

	public NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes GetNavalStorylineSetPieceBattleMissionType()
	{
		return _activeMissionType;
	}

	public void SetNavalStorylineSetPieceBattleMissionType(NavalStorylineData.NavalStorylineSetPieceBattleMissionTypes missionType)
	{
		_activeMissionType = missionType;
	}

	public NavalStorylineData.NavalStorylineStage GetNavalStorylineStage()
	{
		return _lastCompletedStorylineStage;
	}

	public bool GetIsNavalStorylineCanceled()
	{
		return _isNavalStorylineCanceled;
	}

	public bool IsTutorialSkipped()
	{
		return _isTutorialSkipped;
	}

	public void OnCheckpointReached(NavalStorylineData.NavalStorylineCheckpoint checkpoint)
	{
		if (checkpoint != _lastSavedCheckpoint)
		{
			_lastSavedCheckpoint = checkpoint;
			Campaign.Current.SaveHandler.ForceAutoSave();
		}
	}

	public void ChangeNavalStorylineActivity(bool activity)
	{
		if (_isNavalStorylineActive != activity)
		{
			_isNavalStorylineActive = activity;
			OnActivityChanged(_isNavalStorylineActive);
		}
	}

	private void OnActivityChanged(bool newState)
	{
		_inquiryFired = false;
		if (newState)
		{
			CacheTroopsAndShips();
		}
		else
		{
			ClearRosters();
			GetTroopsAndShipsFromCache();
			NavalStorylineData.TeleportMainHeroAndGunnarBackToBase();
		}
		MobileParty.MainParty.MemberRoster.UpdateVersion();
		NavalDLCEvents.Instance.OnNavalStorylineActivityChanged(newState);
	}

	public bool IsWaitingForSistersReturn()
	{
		return _sisterReturnTime != CampaignTime.Zero;
	}

	public void GiveProvisionsToPlayer()
	{
		int num = (int)(_lastCompletedStorylineStage + 1);
		if (_foodStage < num)
		{
			GiveProvisionsToPlayerInternal();
			_foodStage = (int)(_lastCompletedStorylineStage + 1);
		}
	}

	private void GiveProvisionsToPlayerInternal()
	{
		float num = ((_lastCompletedStorylineStage == NavalStorylineData.NavalStorylineStage.Act3Quest2) ? 7f : 5.5f);
		float num2 = num * TaleWorlds.Library.MathF.Abs(MobileParty.MainParty.FoodChange);
		if (num2 > 0f)
		{
			ItemRosterElement itemRosterElement = new ItemRosterElement(DefaultItems.Grain, (int)(num2 / 2f));
			MobileParty.MainParty.ItemRoster.Add(itemRosterElement);
			ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>("fish");
			if (itemObject != null)
			{
				ItemRosterElement itemRosterElement2 = new ItemRosterElement(itemObject, (int)(num2 / 2f));
				MobileParty.MainParty.ItemRoster.Add(itemRosterElement2);
			}
		}
		int num3 = (int)((float)MobileParty.MainParty.TotalWage * num);
		num3 = (int)(Math.Round((float)num3 / 100f) * 100.0);
		GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, num3);
		InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=wJefidrb}Gunnar has secured some provisions for the journey.").ToString(), new Color(0f, 1f, 0f)));
	}

	private void GetTroopsAndShipsFromCache()
	{
		MBList<TroopRosterElement> troopRoster = _troops.GetTroopRoster();
		for (int num = troopRoster.Count - 1; num >= 0; num--)
		{
			TroopRosterElement troopRosterElement = troopRoster[num];
			if (troopRosterElement.Character.IsHero)
			{
				troopRosterElement.Character.HeroObject.ChangeState(Hero.CharacterStates.Active);
			}
			MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, insertAtFront: false, troopRosterElement.WoundedNumber, troopRosterElement.Xp);
		}
		MBList<TroopRosterElement> troopRoster2 = _prisoners.GetTroopRoster();
		for (int num2 = troopRoster2.Count - 1; num2 >= 0; num2--)
		{
			TroopRosterElement troopRosterElement2 = troopRoster2[num2];
			if (troopRosterElement2.Character.IsHero)
			{
				troopRosterElement2.Character.HeroObject.ChangeState(Hero.CharacterStates.Prisoner);
			}
			MobileParty.MainParty.PrisonRoster.AddToCounts(troopRosterElement2.Character, troopRosterElement2.Number, insertAtFront: false, troopRosterElement2.WoundedNumber);
		}
		_troops.Clear();
		_prisoners.Clear();
		for (int num3 = _ships.Count - 1; num3 >= 0; num3--)
		{
			ChangeShipOwnerAction.ApplyByGivingBackShipsToPlayer(_ships[num3]);
		}
		if (_cachedAnchor != null)
		{
			MobileParty.MainParty.SetAnchor(_cachedAnchor);
			_cachedAnchor = null;
		}
		else
		{
			MobileParty.MainParty.Anchor.ResetPosition();
		}
		_ships.Clear();
	}

	private void ClearRosters()
	{
		MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
		for (int num = troopRoster.Count - 1; num >= 0; num--)
		{
			TroopRosterElement troopRosterElement = troopRoster[num];
			if (troopRosterElement.Character != CharacterObject.PlayerCharacter)
			{
				MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -troopRosterElement.Number, insertAtFront: false, -troopRosterElement.WoundedNumber);
			}
			if (troopRosterElement.Character.IsHero)
			{
				foreach (IMissionPlayerFollowerHandler behavior in Campaign.Current.CampaignBehaviorManager.GetBehaviors<IMissionPlayerFollowerHandler>())
				{
					behavior.RemoveFollowingHero(troopRosterElement.Character.HeroObject);
				}
			}
		}
		MBList<TroopRosterElement> troopRoster2 = MobileParty.MainParty.PrisonRoster.GetTroopRoster();
		for (int num2 = troopRoster2.Count - 1; num2 >= 0; num2--)
		{
			TroopRosterElement troopRosterElement2 = troopRoster2[num2];
			MobileParty.MainParty.PrisonRoster.AddToCounts(troopRosterElement2.Character, -troopRosterElement2.Number, insertAtFront: false, -troopRosterElement2.WoundedNumber);
		}
		for (int num3 = PartyBase.MainParty.Ships.Count - 1; num3 >= 0; num3--)
		{
			DestroyShipAction.Apply(PartyBase.MainParty.Ships[num3]);
		}
	}

	private void CacheTroopsAndShips()
	{
		MBList<TroopRosterElement> troopRoster = MobileParty.MainParty.MemberRoster.GetTroopRoster();
		for (int num = troopRoster.Count - 1; num >= 0; num--)
		{
			TroopRosterElement troopRosterElement = troopRoster[num];
			if (troopRosterElement.Character != CharacterObject.PlayerCharacter)
			{
				_troops.Add(troopRosterElement);
				if (troopRosterElement.Character.IsHero)
				{
					troopRosterElement.Character.HeroObject.ChangeState(Hero.CharacterStates.Disabled);
				}
				MobileParty.MainParty.MemberRoster.AddToCountsAtIndex(num, -troopRosterElement.Number, -troopRosterElement.WoundedNumber);
			}
		}
		MBList<TroopRosterElement> troopRoster2 = MobileParty.MainParty.PrisonRoster.GetTroopRoster();
		for (int num2 = troopRoster2.Count - 1; num2 >= 0; num2--)
		{
			TroopRosterElement troopRosterElement2 = troopRoster2[num2];
			_prisoners.Add(troopRosterElement2);
			if (troopRosterElement2.Character.IsHero)
			{
				troopRosterElement2.Character.HeroObject.ChangeState(Hero.CharacterStates.Disabled);
			}
			MobileParty.MainParty.PrisonRoster.AddToCountsAtIndex(num2, -troopRosterElement2.Number, -troopRosterElement2.WoundedNumber);
		}
		_cachedAnchor = (MobileParty.MainParty.Anchor.IsValid ? new AnchorPoint(MobileParty.MainParty.Anchor) : null);
		for (int num3 = MobileParty.MainParty.Ships.Count - 1; num3 >= 0; num3--)
		{
			Ship ship = MobileParty.MainParty.Ships[num3];
			ChangeShipOwnerAction.ApplyByTemporarilyRemovingShipsFromPlayer(ship);
			_ships.Add(ship);
		}
		MobileParty.MainParty.Anchor.ResetPosition();
	}
}
