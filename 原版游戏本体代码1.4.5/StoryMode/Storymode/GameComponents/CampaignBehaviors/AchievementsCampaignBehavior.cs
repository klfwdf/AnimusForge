using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox.CampaignBehaviors;
using StoryMode.Quests.ThirdPhase;
using TaleWorlds.AchievementSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace StoryMode.GameComponents.CampaignBehaviors;

public class AchievementsCampaignBehavior : CampaignBehaviorBase
{
	private class AchievementMissionLogic : MissionLogic
	{
		private Action<Agent, Agent> OnAgentRemovedAction;

		private Action<Agent, WeaponComponentData, BoneBodyPartType, int> OnAgentHitAction;

		public AchievementMissionLogic(Action<Agent, Agent> onAgentRemoved, Action<Agent, WeaponComponentData, BoneBodyPartType, int> onAgentHitAction)
		{
			OnAgentRemovedAction = onAgentRemoved;
			OnAgentHitAction = onAgentHitAction;
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			OnAgentRemovedAction?.Invoke(affectedAgent, affectorAgent);
		}

		public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
		{
			OnAgentHitAction?.Invoke(affectorAgent, attackerWeapon, blow.VictimBodyPart, (int)hitDistance);
		}
	}

	private const float SettlementCountStoredInIntegerSet = 30f;

	private const string CreatedKingdomCountStatID = "CreatedKingdomCount";

	private const string ClearedHideoutCountStatID = "ClearedHideoutCount";

	private const string RepelledSiegeAssaultStatID = "RepelledSiegeAssaultCount";

	private const string KingOrQueenKilledInBattleStatID = "KingOrQueenKilledInBattle";

	private const string SuccessfulSiegeCountStatID = "SuccessfulSiegeCount";

	private const string WonTournamentCountStatID = "WonTournamentCount";

	private const string HighestTierSwordCraftedStatID = "HighestTierSwordCrafted";

	private const string SuccessfulBattlesAgainstArmyCountStatID = "SuccessfulBattlesAgainstArmyCount";

	private const string DefeatedArmyWhileAloneCountStatID = "DefeatedArmyWhileAloneCount";

	private const string TotalTradeProfitStatID = "TotalTradeProfit";

	private const string MaxDailyTributeGainStatID = "MaxDailyTributeGain";

	private const string MaxDailyIncomeStatID = "MaxDailyIncome";

	private const string CapturedATownAloneCountStatID = "CapturedATownAloneCount";

	private const string DefeatedTroopCountStatID = "DefeatedTroopCount";

	private const string FarthestHeadStatID = "FarthestHeadShot";

	private const string ButtersInInventoryStatID = "ButtersInInventoryCount";

	private const string ReachedClanTierSixStatID = "ReachedClanTierSix";

	private const string OwnedFortificationCountStatID = "OwnedFortificationCount";

	private const string HasOwnedCaravanAndWorkshopStatID = "HasOwnedCaravanAndWorkshop";

	private const string ExecutedLordWithMinus100RelationStatID = "ExecutedLordRelation100";

	private const string HighestSkillValueStatID = "HighestSkillValue";

	private const string LeaderOfTournamentStatID = "LeaderOfTournament";

	private const string FinishedTutorialStatID = "FinishedTutorial";

	private const string DefeatedSuperiorForceStatID = "DefeatedSuperiorForce";

	private const string BarbarianVictoryStatID = "BarbarianVictory";

	private const string ImperialVictoryStatID = "ImperialVictory";

	private const string AssembledDragonBannerStatID = "AssembledDragonBanner";

	private const string CompletedAllProjectsStatID = "CompletedAllProjects";

	private const string ClansUnderPlayerKingdomCountStatID = "ClansUnderPlayerKingdomCount";

	private const string HearthBreakerStatID = "Hearthbreaker";

	private const string ProposedAndWonAPolicyStatID = "ProposedAndWonAPolicy";

	private const string BestServedColdStatID = "BestServedCold";

	private const string DefeatedRadagosInDUelStatID = "RadagosDefeatedInDuel";

	private const string GreatGrannyStatID = "GreatGranny";

	private const string NumberOfChildrenStatID = "NumberOfChildrenBorn";

	private const string UndercoverStatID = "CompletedAnIssueInHostileTown";

	private const string EnteredEverySettlemenStatID = "EnteredEverySettlement";

	private bool _deactivateAchievements;

	private int _cachedCreatedKingdomCount;

	private int _cachedHideoutClearedCount;

	private int _cachedHighestSkillValue = -1;

	private int _cachedRepelledSiegeAssaultCount;

	private int _cachedCapturedTownAloneCount;

	private int _cachedKingOrQueenKilledInBattle;

	private int _cachedSuccessfulSiegeCount;

	private int _cachedWonTournamentCount;

	private int _cachedSuccessfulBattlesAgainstArmyCount;

	private int _cachedSuccessfulBattlesAgainstArmyAloneCount;

	private int _cachedTotalTradeProfit;

	private int _cachedMaxDailyIncome;

	private int _cachedDefeatedTroopCount;

	private int _cachedFarthestHeadShot;

	private ItemObject _butter;

	private List<Settlement> _orderedSettlementList = new List<Settlement>();

	private int[] _settlementIntegerSetList;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_deactivateAchievements", ref _deactivateAchievements);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.OnCharacterCreationIsOverEvent.AddNonSerializedListener(this, CacheHighestSkillValue);
		CampaignEvents.WorkshopOwnerChangedEvent.AddNonSerializedListener(this, ProgressOwnedWorkshopCount);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, ProgressOwnedCaravanCount);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.KingdomCreatedEvent.AddNonSerializedListener(this, ProgressCreatedKingdomCount);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.BeforeHeroKilledEvent.AddNonSerializedListener(this, OnBeforeHeroKilled);
		CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, ProgressClanTier);
		CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
		CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, ProgressHeroSkillValue);
		CampaignEvents.PlayerInventoryExchangeEvent.AddNonSerializedListener(this, PlayerInventoryExchange);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinish);
		CampaignEvents.SiegeCompletedEvent.AddNonSerializedListener(this, OnSiegeCompleted);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
		CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuildingLevelChanged);
		CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnNewItemCrafted);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.OnPlayerTradeProfitEvent.AddNonSerializedListener(this, ProgressTotalTradeProfit);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, CheckHeroMarriage);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, CheckKingdomDecisionConcluded);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEnter);
		CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
		CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
		CampaignEvents.RulingClanChanged.AddNonSerializedListener(this, OnRulingClanChanged);
		CampaignEvents.OnConfigChangedEvent.AddNonSerializedListener(this, OnConfigChanged);
		CampaignEvents.CollectMetadataEntriesEvent.AddNonSerializedListener(this, CollectMetadataEntries);
		StoryModeEvents.OnStoryModeTutorialEndedEvent.AddNonSerializedListener(this, CheckTutorialFinished);
		StoryModeEvents.OnBannerPieceCollectedEvent.AddNonSerializedListener(this, ProgressAssembledDragonBanner);
	}

	private void OnRulingClanChanged(Kingdom kingdom, Clan oldRulingClan)
	{
		ProgressOwnedFortificationCount();
	}

	private void OnIssueUpdated(IssueBase issueBase, IssueBase.IssueUpdateDetails detail, Hero issueSolver)
	{
		if (issueSolver == Hero.MainHero && !issueBase.IsSolvingWithAlternative && detail == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && issueBase.IssueOwner.MapFaction != null && issueBase.IssueOwner.MapFaction.IsAtWarWith(Hero.MainHero.MapFaction))
		{
			SetStatInternal("CompletedAnIssueInHostileTown", 1);
		}
	}

	private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
		if (hideoutEventComponent.MapEvent.InvolvedParties.Contains(PartyBase.MainParty) && winnerSide == hideoutEventComponent.MapEvent.PlayerSide)
		{
			ProgressHideoutClearedCount();
		}
	}

	private void OnBeforeHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		ProgressKingOrQueenKilledInBattle(victim, killer, detail);
	}

	private void OnConfigChanged()
	{
		if (!CheckAchievementSystemActivity(out var reason))
		{
			DeactivateAchievements(reason);
		}
	}

	private void OnHeroCreated(Hero hero, bool isBornNaturally)
	{
		if (isBornNaturally)
		{
			if (hero.Father == Hero.MainHero || hero.Mother == Hero.MainHero)
			{
				ProgressChildCount();
			}
			CheckGrandparent();
		}
	}

	private void OnGameLoadFinished()
	{
		if (CheckAchievementSystemActivity(out var reason))
		{
			CacheAndInitializeAchievementVariables();
			CacheHighestSkillValue();
		}
		else
		{
			DeactivateAchievements(reason);
		}
	}

	private async void CacheAndInitializeAchievementVariables()
	{
		_butter = MBObjectManager.Instance.GetObject<ItemObject>("butter");
		List<string> list = new List<string>
		{
			"CreatedKingdomCount", "ClearedHideoutCount", "RepelledSiegeAssaultCount", "KingOrQueenKilledInBattle", "SuccessfulSiegeCount", "WonTournamentCount", "SuccessfulBattlesAgainstArmyCount", "DefeatedArmyWhileAloneCount", "TotalTradeProfit", "MaxDailyIncome",
			"CapturedATownAloneCount", "DefeatedTroopCount", "FarthestHeadShot"
		};
		_orderedSettlementList = (from x in Settlement.All
			where x.IsFortification
			orderby x.StringId descending
			select x).ToList();
		int neededIntegerCount = TaleWorlds.Library.MathF.Ceiling((float)_orderedSettlementList.Count / 30f);
		_settlementIntegerSetList = new int[neededIntegerCount];
		for (int num = 0; num < neededIntegerCount; num++)
		{
			list.Add("SettlementSet" + num);
		}
		int[] array = await AchievementManager.GetStats(list.ToArray());
		if (array != null)
		{
			int num2 = 0;
			_cachedCreatedKingdomCount = array[num2++];
			_cachedHideoutClearedCount = array[num2++];
			_cachedRepelledSiegeAssaultCount = array[num2++];
			_cachedKingOrQueenKilledInBattle = array[num2++];
			_cachedSuccessfulSiegeCount = array[num2++];
			_cachedWonTournamentCount = array[num2++];
			_cachedSuccessfulBattlesAgainstArmyCount = array[num2++];
			_cachedSuccessfulBattlesAgainstArmyAloneCount = array[num2++];
			_cachedTotalTradeProfit = array[num2++];
			_cachedMaxDailyIncome = array[num2++];
			_cachedCapturedTownAloneCount = array[num2++];
			_cachedDefeatedTroopCount = array[num2++];
			_cachedFarthestHeadShot = array[num2++];
			for (int num3 = 0; num3 < neededIntegerCount; num3++)
			{
				int num4 = array[num2++];
				if (num4 == -1)
				{
					_settlementIntegerSetList[num3] = 0;
					SetStatInternal("SettlementSet" + num3, 0);
				}
				else
				{
					_settlementIntegerSetList[num3] = num4;
				}
			}
		}
		else
		{
			TextObject reason = new TextObject("{=4wS8eYYe}Achievements are disabled temporarily for this session due to service disconnection.");
			DeactivateAchievements(reason, showMessage: true, temporarily: true);
			Debug.Print("Achievements are disabled because current platform does not support achievements!", 0, Debug.DebugColor.DarkRed);
		}
	}

	private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter starter)
	{
		if (CheckAchievementSystemActivity(out var reason))
		{
			CacheAndInitializeAchievementVariables();
		}
		else
		{
			DeactivateAchievements(reason);
		}
	}

	private void OnDailyTick()
	{
		ProgressDailyTribute();
		ProgressDailyIncome();
	}

	private void OnClanDestroyed(Clan clan)
	{
		ProgressClansUnderKingdomCount();
	}

	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
	{
		ProgressDailyIncome();
		ProgressClansUnderKingdomCount();
		ProgressOwnedFortificationCount();
	}

	private void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
	{
		ProgressHighestTierSwordCrafted(itemObject);
	}

	private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
	{
		ProgressDailyIncome();
		CheckProjectsInSettlement(town);
	}

	private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		ProgressImperialBarbarianVictory(quest, detail);
	}

	private void OnTournamentFinish(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		ProgressTournamentWonCount(winner);
		ProgressTournamentRank(winner);
	}

	private void OnMapEventEnded(MapEvent mapEvent)
	{
		ProgressRepelSiegeAssaultCount(mapEvent);
		CheckDefeatedSuperiorForce(mapEvent);
		ProgressSuccessfulBattlesAgainstArmyCount(mapEvent);
		ProgressSuccessfulBattlesAgainstArmyAloneCount(mapEvent);
	}

	private void OnSiegeCompleted(Settlement siegeSettlement, MobileParty attackerParty, bool isWin, MapEvent.BattleTypes battleType)
	{
		ProgressRepelSiegeAssaultCount(siegeSettlement, isWin);
		ProgressSuccessfulSiegeCount(attackerParty, isWin);
		ProgressCapturedATownAlone(attackerParty, isWin);
	}

	private void PlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
		if (_butter != null)
		{
			int itemNumber = PartyBase.MainParty.ItemRoster.GetItemNumber(_butter);
			if (itemNumber > 0)
			{
				SetStatInternal("ButtersInInventoryCount", itemNumber);
			}
		}
	}

	public bool CheckAchievementSystemActivity(out TextObject reason)
	{
		bool flag = DumpIntegrityCampaignBehavior.IsGameIntegrityAchieved(out reason);
		DumpIntegrityCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<DumpIntegrityCampaignBehavior>();
		if (!(!_deactivateAchievements && behavior != null && flag))
		{
			return MBDebug.IsTestMode();
		}
		return true;
	}

	private void OnSettlementEnter(MobileParty party, Settlement settlement, Hero hero)
	{
		if (party == MobileParty.MainParty && settlement.IsFortification)
		{
			int num = _orderedSettlementList.IndexOf(settlement);
			int num2 = TaleWorlds.Library.MathF.Floor((float)num / 30f);
			int num3 = _settlementIntegerSetList[num2];
			int num4 = 1 << (int)(30f - ((float)num % 30f + 1f));
			int num5 = num3 | num4;
			SetStatInternal("SettlementSet" + num2, num5);
			if (_settlementIntegerSetList[num2] != num5)
			{
				_settlementIntegerSetList[num2] = num5;
				CheckEnteredEverySettlement();
			}
		}
	}

	private void CheckEnteredEverySettlement()
	{
		int num = 0;
		for (int i = 0; i < _settlementIntegerSetList.Length; i++)
		{
			for (int num2 = _settlementIntegerSetList[i]; num2 > 0; num2 >>= 1)
			{
				if (num2 % 2 == 1)
				{
					num++;
				}
			}
		}
		if (num == _orderedSettlementList.Count)
		{
			SetStatInternal("EnteredEverySettlement", 1);
		}
	}

	private void CacheHighestSkillValue()
	{
		int num = 0;
		foreach (SkillObject item in Skills.All)
		{
			int skillValue = Hero.MainHero.GetSkillValue(item);
			if (skillValue > num)
			{
				num = skillValue;
			}
		}
		_cachedHighestSkillValue = num;
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		CheckExecutedLordRelation(victim, killer, detail);
		CheckBestServedCold(victim, killer, detail);
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		ProgressDailyIncome();
		if (settlement.IsFortification)
		{
			ProgressOwnedFortificationCount();
		}
	}

	private void OnMissionStarted(IMission obj)
	{
		AchievementMissionLogic missionBehavior = new AchievementMissionLogic(OnAgentRemoved, OnAgentHit);
		Mission.Current.AddMissionBehavior(missionBehavior);
	}

	private void OnAgentHit(Agent affectorAgent, WeaponComponentData attackerWeapon, BoneBodyPartType victimBoneBodyPartType, int hitDistance)
	{
		if (affectorAgent != null && affectorAgent == Agent.Main && attackerWeapon != null && !attackerWeapon.IsMeleeWeapon && victimBoneBodyPartType == BoneBodyPartType.Head && hitDistance > _cachedFarthestHeadShot)
		{
			SetStatInternal("FarthestHeadShot", hitDistance);
			_cachedFarthestHeadShot = hitDistance;
		}
	}

	private void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent)
	{
		if (affectorAgent != null && affectorAgent == Agent.Main && affectedAgent.IsHuman)
		{
			SetStatInternal("DefeatedTroopCount", ++_cachedDefeatedTroopCount);
		}
	}

	private void ProgressChildCount()
	{
		int num = Hero.MainHero.Children.Count;
		foreach (LogEntry gameActionLog in Campaign.Current.LogEntryHistory.GameActionLogs)
		{
			if (gameActionLog is PlayerCharacterChangedLogEntry playerCharacterChangedLogEntry)
			{
				num += playerCharacterChangedLogEntry.OldPlayerHero.Children.Count;
			}
		}
		SetStatInternal("NumberOfChildrenBorn", num);
	}

	private void CheckGrandparent()
	{
		if (Hero.MainHero.Children.Any((Hero x) => x.Children.Any((Hero y) => y.Children.Any())))
		{
			SetStatInternal("GreatGranny", 1);
		}
	}

	public void OnRadagosDuelWon()
	{
		SetStatInternal("RadagosDefeatedInDuel", 1);
	}

	private void CheckBestServedCold(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
	{
		if (killer != Hero.MainHero || (detail != KillCharacterAction.KillCharacterActionDetail.Executed && detail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent && detail != KillCharacterAction.KillCharacterActionDetail.Murdered && detail != KillCharacterAction.KillCharacterActionDetail.DiedInBattle && detail != KillCharacterAction.KillCharacterActionDetail.WoundedInBattle))
		{
			return;
		}
		foreach (LogEntry gameActionLog in Campaign.Current.LogEntryHistory.GameActionLogs)
		{
			if (gameActionLog is CharacterKilledLogEntry characterKilledLogEntry && characterKilledLogEntry.Killer == victim && characterKilledLogEntry.VictimClan == Clan.PlayerClan)
			{
				SetStatInternal("BestServedCold", 1);
				break;
			}
		}
	}

	private void CheckProposedAndWonPolicy(KingdomDecision decision, DecisionOutcome chosenOutcome)
	{
		if (decision.ProposerClan == Clan.PlayerClan && decision.GetQueriedDecisionOutcome(new MBList<DecisionOutcome> { chosenOutcome }) != null)
		{
			SetStatInternal("ProposedAndWonAPolicy", 1);
		}
	}

	private void CheckKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
	{
		CheckProposedAndWonPolicy(decision, chosenOutcome);
		ProgressOwnedFortificationCount();
		ProgressClansUnderKingdomCount();
	}

	private void CheckHeroMarriage(Hero hero1, Hero hero2, bool showNotification = true)
	{
		if (hero1 != Hero.MainHero && hero2 != Hero.MainHero)
		{
			return;
		}
		Hero hero3 = ((hero1 == Hero.MainHero) ? hero2 : hero1);
		foreach (LogEntry gameActionLog in Campaign.Current.LogEntryHistory.GameActionLogs)
		{
			if (gameActionLog is CharacterKilledLogEntry characterKilledLogEntry && characterKilledLogEntry.Killer == Hero.MainHero && hero3.ExSpouses.Contains(characterKilledLogEntry.Victim))
			{
				SetStatInternal("Hearthbreaker", 1);
			}
		}
	}

	private void ProgressClansUnderKingdomCount()
	{
		if (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.Leader == Hero.MainHero)
		{
			SetStatInternal("ClansUnderPlayerKingdomCount", Clan.PlayerClan.Kingdom.Clans.Count);
		}
	}

	private void ProgressSuccessfulBattlesAgainstArmyCount(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent && mapEvent.Winner == mapEvent.GetMapEventSide(mapEvent.PlayerSide) && mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties.Any((MapEventParty x) => x.Party.MobileParty != null && x.Party.MobileParty.AttachedTo != null))
		{
			SetStatInternal("SuccessfulBattlesAgainstArmyCount", ++_cachedSuccessfulBattlesAgainstArmyCount);
		}
	}

	private void ProgressSuccessfulBattlesAgainstArmyAloneCount(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent && mapEvent.Winner == mapEvent.GetMapEventSide(mapEvent.PlayerSide) && mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties.Any((MapEventParty x) => x.Party.MobileParty != null && x.Party.MobileParty.AttachedTo != null) && mapEvent.GetMapEventSide(mapEvent.PlayerSide).Parties.Count == 1)
		{
			SetStatInternal("DefeatedArmyWhileAloneCount", ++_cachedSuccessfulBattlesAgainstArmyAloneCount);
		}
	}

	private void ProgressDailyTribute()
	{
		IFaction mapFaction = Clan.PlayerClan.MapFaction;
		float num = 1f;
		int num2 = 0;
		if (Clan.PlayerClan.Kingdom != null)
		{
			num = CalculateTributeShareFactor(Clan.PlayerClan);
		}
		foreach (StanceLink stance in FactionHelper.GetStances(mapFaction))
		{
			int dailyTributeToPay = stance.GetDailyTributeToPay(mapFaction);
			if (stance.IsNeutral && dailyTributeToPay < 0)
			{
				int num3 = (int)((float)dailyTributeToPay * num);
				num2 += num3;
			}
		}
		SetStatInternal("MaxDailyTributeGain", TaleWorlds.Library.MathF.Abs(num2));
	}

	private static float CalculateTributeShareFactor(Clan clan)
	{
		Kingdom kingdom = clan.Kingdom;
		int num = kingdom.Fiefs.Sum((Town x) => x.IsCastle ? 1 : 3) + 1 + kingdom.Clans.Count;
		return (float)(clan.Fiefs.Sum((Town x) => x.IsCastle ? 1 : 3) + ((clan == kingdom.RulingClan) ? 1 : 0) + 1) / (float)num;
	}

	private void ProgressDailyIncome()
	{
		int num = (int)Campaign.Current.Models.ClanFinanceModel.CalculateClanIncome(Clan.PlayerClan).ResultNumber;
		if (num > _cachedMaxDailyIncome)
		{
			SetStatInternal("MaxDailyIncome", num);
			_cachedMaxDailyIncome = num;
		}
	}

	private void ProgressTotalTradeProfit(int profit)
	{
		_cachedTotalTradeProfit += profit;
		SetStatInternal("TotalTradeProfit", _cachedTotalTradeProfit);
	}

	private void CheckProjectsInSettlement(Town town)
	{
		if (town.OwnerClan != Clan.PlayerClan)
		{
			return;
		}
		foreach (Settlement item in Clan.PlayerClan.Settlements.Where((Settlement x) => x.IsFortification))
		{
			bool flag = true;
			foreach (Building building in item.Town.Buildings)
			{
				if (building.CurrentLevel != 3 && !building.BuildingType.IsDailyProject)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				SetStatInternal("CompletedAllProjects", 1);
			}
		}
	}

	private void ProgressHighestTierSwordCrafted(ItemObject itemObject)
	{
		WeaponComponentData primaryWeapon = itemObject.WeaponComponent.PrimaryWeapon;
		if (primaryWeapon.WeaponClass == WeaponClass.OneHandedSword || primaryWeapon.WeaponClass == WeaponClass.TwoHandedSword)
		{
			SetStatInternal("HighestTierSwordCrafted", (int)(itemObject.Tier + 1));
		}
	}

	private void ProgressAssembledDragonBanner()
	{
		if (StoryModeManager.Current.MainStoryLine.FirstPhase != null && StoryModeManager.Current.MainStoryLine.FirstPhase.AllPiecesCollected)
		{
			SetStatInternal("AssembledDragonBanner", 1);
		}
	}

	private void ProgressImperialBarbarianVictory(QuestBase quest, QuestBase.QuestCompleteDetails detail)
	{
		if (quest.IsSpecialQuest && quest.GetType() == typeof(DefeatTheConspiracyQuestBehavior.DefeatTheConspiracyQuest))
		{
			if (StoryModeManager.Current.MainStoryLine.MainStoryLineSide == MainStoryLineSide.CreateAntiImperialKingdom || StoryModeManager.Current.MainStoryLine.MainStoryLineSide == MainStoryLineSide.SupportAntiImperialKingdom)
			{
				SetStatInternal("BarbarianVictory", 1);
			}
			else
			{
				SetStatInternal("ImperialVictory", 1);
			}
		}
	}

	private void CheckDefeatedSuperiorForce(MapEvent mapEvent)
	{
		if (mapEvent.IsPlayerMapEvent && mapEvent.Winner == mapEvent.GetMapEventSide(mapEvent.PlayerSide))
		{
			int num = mapEvent.GetMapEventSide(mapEvent.DefeatedSide).Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart);
			int num2 = mapEvent.GetMapEventSide(mapEvent.WinningSide).Parties.Sum((MapEventParty x) => x.HealthyManCountAtStart);
			if (num - num2 >= 500)
			{
				SetStatInternal("DefeatedSuperiorForce", 1);
			}
		}
	}

	private void CheckTutorialFinished()
	{
		if (!StoryModeManager.Current.MainStoryLine.TutorialPhase.IsSkipped)
		{
			SetStatInternal("FinishedTutorial", 1);
		}
	}

	private void ProgressSuccessfulSiegeCount(MobileParty attackerParty, bool isWin)
	{
		if (attackerParty == MobileParty.MainParty && isWin)
		{
			SetStatInternal("SuccessfulSiegeCount", ++_cachedSuccessfulSiegeCount);
		}
	}

	private void ProgressCapturedATownAlone(MobileParty attackerParty, bool isWin)
	{
		if (attackerParty == MobileParty.MainParty && isWin && attackerParty.Army == null)
		{
			SetStatInternal("CapturedATownAloneCount", ++_cachedCapturedTownAloneCount);
		}
	}

	private void ProgressRepelSiegeAssaultCount(Settlement siegeSettlement, bool isWin)
	{
		if (siegeSettlement.OwnerClan == Clan.PlayerClan && !isWin)
		{
			SetStatInternal("RepelledSiegeAssaultCount", ++_cachedRepelledSiegeAssaultCount);
		}
	}

	private void ProgressRepelSiegeAssaultCount(MapEvent mapEvent)
	{
		if (mapEvent.MapEventSettlement != null && mapEvent.MapEventSettlement.OwnerClan == Clan.PlayerClan && mapEvent.EventType == MapEvent.BattleTypes.Siege && mapEvent.BattleState == BattleState.None && PlayerEncounter.Battle != null && PlayerEncounter.CampaignBattleResult != null && PlayerEncounter.CampaignBattleResult.PlayerVictory)
		{
			SetStatInternal("RepelledSiegeAssaultCount", ++_cachedRepelledSiegeAssaultCount);
		}
	}

	private void ProgressTournamentRank(CharacterObject winner)
	{
		if (winner == CharacterObject.PlayerCharacter && Campaign.Current.TournamentManager.GetLeaderboard()[0].Key == Hero.MainHero)
		{
			SetStatInternal("LeaderOfTournament", 1);
		}
	}

	private void ProgressHeroSkillValue(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
	{
		if (hero == Hero.MainHero && _cachedHighestSkillValue > -1)
		{
			int skillValue = hero.GetSkillValue(skill);
			if (skillValue > _cachedHighestSkillValue)
			{
				SetStatInternal("HighestSkillValue", skillValue);
				_cachedHighestSkillValue = skillValue;
			}
		}
	}

	private void ProgressHideoutClearedCount()
	{
		SetStatInternal("ClearedHideoutCount", ++_cachedHideoutClearedCount);
	}

	private void CheckExecutedLordRelation(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
	{
		if (((killer == Hero.MainHero && detail == KillCharacterAction.KillCharacterActionDetail.Executed) || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent) && (int)victim.GetRelationWithPlayer() <= -100)
		{
			SetStatInternal("ExecutedLordRelation100", 1);
		}
	}

	private void ProgressKingOrQueenKilledInBattle(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
	{
		if (killer == Hero.MainHero && victim.IsKingdomLeader && detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			SetStatInternal("KingOrQueenKilledInBattle", ++_cachedKingOrQueenKilledInBattle);
		}
	}

	private void ProgressTournamentWonCount(CharacterObject winner)
	{
		if (winner == CharacterObject.PlayerCharacter)
		{
			SetStatInternal("WonTournamentCount", ++_cachedWonTournamentCount);
		}
	}

	private void ProgressOwnedWorkshopCount(Workshop workshop, Hero oldOwner)
	{
		if (workshop.Owner == Hero.MainHero)
		{
			ProgressHasOwnedCaravanAndWorkshop();
		}
	}

	private void ProgressOwnedCaravanCount(MobileParty party)
	{
		if (party.IsCaravan && party.MapFaction == Hero.MainHero.MapFaction)
		{
			ProgressHasOwnedCaravanAndWorkshop();
		}
	}

	private void ProgressHasOwnedCaravanAndWorkshop()
	{
		if (Hero.MainHero.OwnedWorkshops.Count > 0 && Hero.MainHero.OwnedCaravans.Count > 0)
		{
			SetStatInternal("HasOwnedCaravanAndWorkshop", 1);
		}
	}

	private void ProgressOwnedFortificationCount()
	{
		int num = 0;
		num = ((!Hero.MainHero.IsKingdomLeader) ? Hero.MainHero.Clan.Fiefs.Count : Hero.MainHero.MapFaction.Fiefs.Count);
		SetStatInternal("OwnedFortificationCount", num);
	}

	private void ProgressCreatedKingdomCount(Kingdom kingdom)
	{
		if (kingdom.Leader == Hero.MainHero)
		{
			SetStatInternal("CreatedKingdomCount", ++_cachedCreatedKingdomCount);
		}
	}

	private void ProgressClanTier(Clan clan, bool shouldNotify)
	{
		if (clan == Clan.PlayerClan && clan.Tier == 6)
		{
			SetStatInternal("ReachedClanTierSix", 1);
		}
	}

	public void DeactivateAchievements(TextObject reason = null, bool showMessage = true, bool temporarily = false)
	{
		_deactivateAchievements = !temporarily || _deactivateAchievements;
		CampaignEventDispatcher.Instance.RemoveListeners(this);
		CampaignEvents.CollectMetadataEntriesEvent.AddNonSerializedListener(this, CollectMetadataEntries);
		if (showMessage)
		{
			if (TextObject.IsNullOrEmpty(reason))
			{
				reason = new TextObject("{=Z9mcDuDi}Achievements are disabled!");
			}
			MBInformationManager.AddQuickInformation(reason, 4000);
		}
	}

	private void CollectMetadataEntries(List<KeyValuePair<string, string>> list)
	{
		list.Add(new KeyValuePair<string, string>("AchievementsDisabled", _deactivateAchievements ? "1" : "0"));
	}

	private void SetStatInternal(string statId, int value)
	{
		if (!_deactivateAchievements)
		{
			AchievementManager.SetStat(statId, value);
		}
	}
}
