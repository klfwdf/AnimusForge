using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace SandBox.CampaignBehaviors;

public class StatisticsCampaignBehavior : CampaignBehaviorBase, IStatisticsCampaignBehavior, ICampaignBehavior
{
	private class StatisticsMissionLogic : MissionLogic
	{
		private readonly StatisticsCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<StatisticsCampaignBehavior>();

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (behavior != null)
			{
				behavior.OnAgentRemoved(affectedAgent, affectorAgent);
			}
		}
	}

	private int _highestTournamentRank;

	private int _numberOfTournamentWins;

	private int _numberOfChildrenBorn;

	private int _numberOfPrisonersRecruited;

	private int _numberOfTroopsRecruited;

	private int _numberOfClansDefected;

	private int _numberOfIssuesSolved;

	private int _totalInfluenceEarned;

	private int _totalCrimeRatingGained;

	private ulong _totalTimePlayedInSeconds;

	private int _numberOfbattlesWon;

	private int _numberOfbattlesLost;

	private int _largestBattleWonAsLeader;

	private int _largestArmyFormedByPlayer;

	private int _numberOfEnemyClansDestroyed;

	private int _numberOfHeroesKilledInBattle;

	private int _numberOfTroopsKnockedOrKilledAsParty;

	private int _numberOfTroopsKnockedOrKilledByPlayer;

	private int _numberOfHeroPrisonersTaken;

	private int _numberOfTroopPrisonersTaken;

	private int _numberOfTownsCaptured;

	private int _numberOfHideoutsCleared;

	private int _numberOfCastlesCaptured;

	private int _numberOfVillagesRaided;

	private CampaignTime _timeSpentAsPrisoner;

	private ulong _totalDenarsEarned;

	private ulong _denarsEarnedFromCaravans;

	private ulong _denarsEarnedFromWorkshops;

	private ulong _denarsEarnedFromRansoms;

	private ulong _denarsEarnedFromTaxes;

	private ulong _denarsEarnedFromTributes;

	private ulong _denarsPaidAsTributes;

	private int _numberOfCraftingPartsUnlocked;

	private int _numberOfWeaponsCrafted;

	private int _numberOfCraftingOrdersCompleted;

	private (string, int) _mostExpensiveItemCrafted = (null, 0);

	private int _numberOfCompanionsHired;

	private Dictionary<Hero, (int, int)> _companionData = new Dictionary<Hero, (int, int)>();

	private int _lastPlayerBattleSize;

	private DateTime _lastGameplayTimeCheck;

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_highestTournamentRank", ref _highestTournamentRank);
		dataStore.SyncData("_numberOfTournamentWins", ref _numberOfTournamentWins);
		dataStore.SyncData("_numberOfChildrenBorn", ref _numberOfChildrenBorn);
		dataStore.SyncData("_numberOfPrisonersRecruited", ref _numberOfPrisonersRecruited);
		dataStore.SyncData("_numberOfTroopsRecruited", ref _numberOfTroopsRecruited);
		dataStore.SyncData("_numberOfClansDefected", ref _numberOfClansDefected);
		dataStore.SyncData("_numberOfIssuesSolved", ref _numberOfIssuesSolved);
		dataStore.SyncData("_totalInfluenceEarned", ref _totalInfluenceEarned);
		dataStore.SyncData("_totalCrimeRatingGained", ref _totalCrimeRatingGained);
		dataStore.SyncData("_totalTimePlayedInSeconds", ref _totalTimePlayedInSeconds);
		dataStore.SyncData("_numberOfbattlesWon", ref _numberOfbattlesWon);
		dataStore.SyncData("_numberOfbattlesLost", ref _numberOfbattlesLost);
		dataStore.SyncData("_largestBattleWonAsLeader", ref _largestBattleWonAsLeader);
		dataStore.SyncData("_largestArmyFormedByPlayer", ref _largestArmyFormedByPlayer);
		dataStore.SyncData("_numberOfEnemyClansDestroyed", ref _numberOfEnemyClansDestroyed);
		dataStore.SyncData("_numberOfHeroesKilledInBattle", ref _numberOfHeroesKilledInBattle);
		dataStore.SyncData("_numberOfTroopsKnockedOrKilledAsParty", ref _numberOfTroopsKnockedOrKilledAsParty);
		dataStore.SyncData("_numberOfTroopsKnockedOrKilledByPlayer", ref _numberOfTroopsKnockedOrKilledByPlayer);
		dataStore.SyncData("_numberOfHeroPrisonersTaken", ref _numberOfHeroPrisonersTaken);
		dataStore.SyncData("_numberOfTroopPrisonersTaken", ref _numberOfTroopPrisonersTaken);
		dataStore.SyncData("_numberOfTownsCaptured", ref _numberOfTownsCaptured);
		dataStore.SyncData("_numberOfHideoutsCleared", ref _numberOfHideoutsCleared);
		dataStore.SyncData("_numberOfCastlesCaptured", ref _numberOfCastlesCaptured);
		dataStore.SyncData("_numberOfVillagesRaided", ref _numberOfVillagesRaided);
		dataStore.SyncData("_timeSpentAsPrisoner", ref _timeSpentAsPrisoner);
		dataStore.SyncData("_totalDenarsEarned", ref _totalDenarsEarned);
		dataStore.SyncData("_denarsEarnedFromCaravans", ref _denarsEarnedFromCaravans);
		dataStore.SyncData("_denarsEarnedFromWorkshops", ref _denarsEarnedFromWorkshops);
		dataStore.SyncData("_denarsEarnedFromRansoms", ref _denarsEarnedFromRansoms);
		dataStore.SyncData("_denarsEarnedFromTaxes", ref _denarsEarnedFromTaxes);
		dataStore.SyncData("_denarsEarnedFromTributes", ref _denarsEarnedFromTributes);
		dataStore.SyncData("_denarsPaidAsTributes", ref _denarsPaidAsTributes);
		dataStore.SyncData("_numberOfCraftingPartsUnlocked", ref _numberOfCraftingPartsUnlocked);
		dataStore.SyncData("_numberOfWeaponsCrafted", ref _numberOfWeaponsCrafted);
		dataStore.SyncData("_numberOfCraftingOrdersCompleted", ref _numberOfCraftingOrdersCompleted);
		dataStore.SyncData("_mostExpensiveItemCrafted", ref _mostExpensiveItemCrafted);
		dataStore.SyncData("_numberOfCompanionsHired", ref _numberOfCompanionsHired);
		dataStore.SyncData("_companionData", ref _companionData);
		dataStore.SyncData("_lastPlayerBattleSize", ref _lastPlayerBattleSize);
	}

	public override void RegisterEvents()
	{
		CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
		CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
		CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
		CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, OnClanInfluenceChanged);
		CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, OnAfterSessionLaunched);
		CampaignEvents.CrimeRatingChanged.AddNonSerializedListener(this, OnCrimeRatingChanged);
		CampaignEvents.OnMainPartyPrisonerRecruitedEvent.AddNonSerializedListener(this, OnMainPartyPrisonerRecruited);
		CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, OnUnitRecruited);
		CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, OnBeforeSave);
		CampaignEvents.CraftingPartUnlockedEvent.AddNonSerializedListener(this, OnCraftingPartUnlocked);
		CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, OnNewItemCrafted);
		CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, OnNewCompanionAdded);
		CampaignEvents.HeroOrPartyTradedGold.AddNonSerializedListener(this, OnHeroOrPartyTradedGold);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnd);
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, OnPartyAttachedAnotherParty);
		CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
		CampaignEvents.OnPrisonerTakenEvent.AddNonSerializedListener(this, OnPrisonersTaken);
		CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
		CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, OnHideoutBattleCompleted);
		CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
		CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
		CampaignEvents.OnPlayerPartyKnockedOrKilledTroopEvent.AddNonSerializedListener(this, OnPlayerPartyKnockedOrKilledTroop);
		CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, OnMissionStarted);
		CampaignEvents.OnPlayerEarnedGoldFromAssetEvent.AddNonSerializedListener(this, OnPlayerEarnedGoldFromAsset);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
	}

	private void OnBeforeSave()
	{
		UpdateTotalTimePlayedInSeconds();
	}

	private void OnAfterSessionLaunched(CampaignGameStarter starter)
	{
		_lastGameplayTimeCheck = DateTime.Now;
		if (_highestTournamentRank == 0)
		{
			_highestTournamentRank = Campaign.Current.TournamentManager.GetLeaderBoardRank(Hero.MainHero);
		}
	}

	public void OnDefectionPersuasionSucess()
	{
		_numberOfClansDefected++;
	}

	private void OnUnitRecruited(CharacterObject character, int amount)
	{
		_numberOfTroopsRecruited += amount;
	}

	private void OnMainPartyPrisonerRecruited(FlattenedTroopRoster flattenedTroopRoster)
	{
		_numberOfPrisonersRecruited += flattenedTroopRoster.CountQ();
	}

	private void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
	{
		if (deltaCrimeAmount > 0f)
		{
			_totalCrimeRatingGained += (int)deltaCrimeAmount;
		}
	}

	private void OnClanInfluenceChanged(Clan clan, float change)
	{
		if (change > 0f && clan == Clan.PlayerClan)
		{
			_totalInfluenceEarned += (int)change;
		}
	}

	private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		if (winner.HeroObject == Hero.MainHero)
		{
			_numberOfTournamentWins++;
			int leaderBoardRank = Campaign.Current.TournamentManager.GetLeaderBoardRank(Hero.MainHero);
			if (leaderBoardRank < _highestTournamentRank)
			{
				_highestTournamentRank = leaderBoardRank;
			}
		}
	}

	private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
	{
		if (details != IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess && details != IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest && details != IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal)
		{
			return;
		}
		_numberOfIssuesSolved++;
		if (issueSolver != null && issueSolver.IsPlayerCompanion)
		{
			if (_companionData.ContainsKey(issueSolver))
			{
				_companionData[issueSolver] = (_companionData[issueSolver].Item1 + 1, _companionData[issueSolver].Item2);
			}
			else
			{
				_companionData.Add(issueSolver, (1, 0));
			}
		}
	}

	private void OnHeroCreated(Hero hero, bool isBornNaturally = false)
	{
		if (hero.Mother == Hero.MainHero || hero.Father == Hero.MainHero)
		{
			_numberOfChildrenBorn++;
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
	{
		if (killer != null && killer.PartyBelongedTo == MobileParty.MainParty && detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
		{
			_numberOfHeroesKilledInBattle++;
		}
	}

	private void OnMissionStarted(IMission mission)
	{
		StatisticsMissionLogic missionBehavior = new StatisticsMissionLogic();
		Mission.Current.AddMissionBehavior(missionBehavior);
	}

	private void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent)
	{
		if (affectorAgent == null)
		{
			return;
		}
		if (affectorAgent == Agent.Main)
		{
			_numberOfTroopsKnockedOrKilledByPlayer++;
		}
		else if (affectorAgent.IsPlayerTroop)
		{
			_numberOfTroopsKnockedOrKilledAsParty++;
		}
		else if (affectorAgent.IsHero)
		{
			Hero heroObject = (affectorAgent.Character as CharacterObject).HeroObject;
			if (heroObject.IsPlayerCompanion)
			{
				if (_companionData.ContainsKey(heroObject))
				{
					_companionData[heroObject] = (_companionData[heroObject].Item1, _companionData[heroObject].Item2 + 1);
				}
				else
				{
					_companionData.Add(heroObject, (0, 1));
				}
			}
		}
		if (affectedAgent.IsHero && affectedAgent.State == AgentState.Killed)
		{
			_numberOfHeroesKilledInBattle++;
		}
	}

	private void OnPlayerPartyKnockedOrKilledTroop(CharacterObject troop)
	{
		_numberOfTroopsKnockedOrKilledAsParty++;
	}

	private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
	{
		if (prisoner == Hero.MainHero)
		{
			_timeSpentAsPrisoner += CampaignTime.Now - PlayerCaptivity.CaptivityStartTime;
		}
	}

	private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
	{
		if (mapEvent.IsPlayerMapEvent)
		{
			_lastPlayerBattleSize = mapEvent.AttackerSide.TroopCount + mapEvent.DefenderSide.TroopCount;
		}
	}

	private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent, HideoutEventComponent.HideoutBattleEndState battleEndState)
	{
		if (hideoutEventComponent.MapEvent.PlayerSide == winnerSide)
		{
			_numberOfHideoutsCleared++;
		}
	}

	private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEventComponent)
	{
		if (raidEventComponent.MapEvent.HasWinner && raidEventComponent.MapEvent.PlayerSide == winnerSide)
		{
			_numberOfVillagesRaided++;
		}
	}

	private void OnPrisonersTaken(FlattenedTroopRoster troopRoster)
	{
		_numberOfTroopPrisonersTaken += troopRoster.CountQ();
	}

	private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
	{
		if (capturer == PartyBase.MainParty)
		{
			_numberOfHeroPrisonersTaken++;
		}
	}

	private void OnArmyCreated(Army army)
	{
		if (army.LeaderParty == MobileParty.MainParty && _largestArmyFormedByPlayer < army.TotalManCount)
		{
			_largestArmyFormedByPlayer = army.TotalManCount;
		}
	}

	private void OnPartyAttachedAnotherParty(MobileParty mobileParty)
	{
		if (mobileParty.Army == MobileParty.MainParty.Army && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty && _largestArmyFormedByPlayer < MobileParty.MainParty.Army.TotalManCount)
		{
			_largestArmyFormedByPlayer = MobileParty.MainParty.Army.TotalManCount;
		}
	}

	private void OnClanDestroyed(Clan clan)
	{
		if (clan.MapFaction.IsAtWarWith(Clan.PlayerClan.MapFaction))
		{
			_numberOfEnemyClansDestroyed++;
		}
	}

	private void OnMapEventEnd(MapEvent mapEvent)
	{
		if (!mapEvent.IsPlayerMapEvent || !mapEvent.HasWinner)
		{
			return;
		}
		if (mapEvent.WinningSide == mapEvent.PlayerSide)
		{
			_numberOfbattlesWon++;
			if (mapEvent.IsSiegeAssault && !mapEvent.IsPlayerSergeant() && mapEvent.MapEventSettlement != null)
			{
				if (mapEvent.MapEventSettlement.IsTown)
				{
					_numberOfTownsCaptured++;
				}
				else if (mapEvent.MapEventSettlement.IsCastle)
				{
					_numberOfCastlesCaptured++;
				}
			}
			if (_largestBattleWonAsLeader < _lastPlayerBattleSize && !mapEvent.IsPlayerSergeant())
			{
				_largestBattleWonAsLeader = _lastPlayerBattleSize;
			}
		}
		else
		{
			_numberOfbattlesLost++;
		}
	}

	private void OnHeroOrPartyTradedGold((Hero, PartyBase) giver, (Hero, PartyBase) recipient, (int, string) goldAmount, bool showNotification)
	{
		if (recipient.Item1 == Hero.MainHero || recipient.Item2 == PartyBase.MainParty)
		{
			_totalDenarsEarned += (ulong)goldAmount.Item1;
		}
	}

	public void OnPlayerAcceptedRansomOffer(int ransomPrice)
	{
		_denarsEarnedFromRansoms += (ulong)ransomPrice;
	}

	private void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType assetType, int amount)
	{
		switch (assetType)
		{
		case DefaultClanFinanceModel.AssetIncomeType.Workshop:
			_denarsEarnedFromWorkshops += (ulong)amount;
			break;
		case DefaultClanFinanceModel.AssetIncomeType.Caravan:
			_denarsEarnedFromCaravans += (ulong)amount;
			break;
		case DefaultClanFinanceModel.AssetIncomeType.Taxes:
			_denarsEarnedFromTaxes += (ulong)amount;
			break;
		case DefaultClanFinanceModel.AssetIncomeType.TributesEarned:
			_denarsEarnedFromTributes += (ulong)amount;
			break;
		}
	}

	private void OnNewCompanionAdded(Hero hero)
	{
		_numberOfCompanionsHired++;
	}

	private void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
	{
		_numberOfWeaponsCrafted++;
		if (isCraftingOrderItem)
		{
			_numberOfCraftingOrdersCompleted++;
		}
		if (_mostExpensiveItemCrafted.Item2 == 0 || _mostExpensiveItemCrafted.Item2 < itemObject.Value)
		{
			_mostExpensiveItemCrafted.Item1 = itemObject.Name.ToString();
			_mostExpensiveItemCrafted.Item2 = itemObject.Value;
		}
	}

	private void OnCraftingPartUnlocked(CraftingPiece craftingPiece)
	{
		_numberOfCraftingPartsUnlocked++;
	}

	public (string name, int value) GetCompanionWithMostKills()
	{
		if (_companionData.IsEmpty())
		{
			return (name: null, value: 0);
		}
		KeyValuePair<Hero, (int, int)> keyValuePair = _companionData.MaxBy((KeyValuePair<Hero, (int, int)> kvp) => kvp.Value.Item2);
		return (name: keyValuePair.Key.Name.ToString(), value: keyValuePair.Value.Item2);
	}

	public (string name, int value) GetCompanionWithMostIssuesSolved()
	{
		if (_companionData.IsEmpty())
		{
			return (name: null, value: 0);
		}
		KeyValuePair<Hero, (int, int)> keyValuePair = _companionData.MaxBy((KeyValuePair<Hero, (int, int)> kvp) => kvp.Value.Item1);
		return (name: keyValuePair.Key.Name.ToString(), value: keyValuePair.Value.Item1);
	}

	public int GetHighestTournamentRank()
	{
		return _highestTournamentRank;
	}

	public int GetNumberOfTournamentWins()
	{
		return _numberOfTournamentWins;
	}

	public int GetNumberOfChildrenBorn()
	{
		return _numberOfChildrenBorn;
	}

	public int GetNumberOfPrisonersRecruited()
	{
		return _numberOfPrisonersRecruited;
	}

	public int GetNumberOfTroopsRecruited()
	{
		return _numberOfTroopsRecruited;
	}

	public int GetNumberOfClansDefected()
	{
		return _numberOfClansDefected;
	}

	public int GetNumberOfIssuesSolved()
	{
		return _numberOfIssuesSolved;
	}

	public int GetTotalInfluenceEarned()
	{
		return _totalInfluenceEarned;
	}

	public int GetTotalCrimeRatingGained()
	{
		return _totalCrimeRatingGained;
	}

	public int GetNumberOfBattlesWon()
	{
		return _numberOfbattlesWon;
	}

	public int GetNumberOfBattlesLost()
	{
		return _numberOfbattlesLost;
	}

	public int GetLargestBattleWonAsLeader()
	{
		return _largestBattleWonAsLeader;
	}

	public int GetLargestArmyFormedByPlayer()
	{
		return _largestArmyFormedByPlayer;
	}

	public int GetNumberOfEnemyClansDestroyed()
	{
		return _numberOfEnemyClansDestroyed;
	}

	public int GetNumberOfHeroesKilledInBattle()
	{
		return _numberOfHeroesKilledInBattle;
	}

	public int GetNumberOfTroopsKnockedOrKilledAsParty()
	{
		return _numberOfTroopsKnockedOrKilledAsParty;
	}

	public int GetNumberOfTroopsKnockedOrKilledByPlayer()
	{
		return _numberOfTroopsKnockedOrKilledByPlayer;
	}

	public int GetNumberOfHeroPrisonersTaken()
	{
		return _numberOfHeroPrisonersTaken;
	}

	public int GetNumberOfTroopPrisonersTaken()
	{
		return _numberOfTroopPrisonersTaken;
	}

	public int GetNumberOfTownsCaptured()
	{
		return _numberOfTownsCaptured;
	}

	public int GetNumberOfHideoutsCleared()
	{
		return _numberOfHideoutsCleared;
	}

	public int GetNumberOfCastlesCaptured()
	{
		return _numberOfCastlesCaptured;
	}

	public int GetNumberOfVillagesRaided()
	{
		return _numberOfVillagesRaided;
	}

	public int GetNumberOfCraftingPartsUnlocked()
	{
		return _numberOfCraftingPartsUnlocked;
	}

	public int GetNumberOfWeaponsCrafted()
	{
		return _numberOfWeaponsCrafted;
	}

	public int GetNumberOfCraftingOrdersCompleted()
	{
		return _numberOfCraftingOrdersCompleted;
	}

	public int GetNumberOfCompanionsHired()
	{
		return _numberOfCompanionsHired;
	}

	public CampaignTime GetTimeSpentAsPrisoner()
	{
		return _timeSpentAsPrisoner;
	}

	public ulong GetTotalTimePlayedInSeconds()
	{
		UpdateTotalTimePlayedInSeconds();
		return _totalTimePlayedInSeconds;
	}

	public ulong GetTotalDenarsEarned()
	{
		return _totalDenarsEarned;
	}

	public ulong GetDenarsEarnedFromCaravans()
	{
		return _denarsEarnedFromCaravans;
	}

	public ulong GetDenarsEarnedFromWorkshops()
	{
		return _denarsEarnedFromWorkshops;
	}

	public ulong GetDenarsEarnedFromRansoms()
	{
		return _denarsEarnedFromRansoms;
	}

	public ulong GetDenarsEarnedFromTaxes()
	{
		return _denarsEarnedFromTaxes;
	}

	public ulong GetDenarsEarnedFromTributes()
	{
		return _denarsEarnedFromTributes;
	}

	public ulong GetDenarsPaidAsTributes()
	{
		return _denarsPaidAsTributes;
	}

	public CampaignTime GetTotalTimePlayed()
	{
		return CampaignTime.Now - Campaign.Current.Models.CampaignTimeModel.CampaignStartTime;
	}

	public (string, int) GetMostExpensiveItemCrafted()
	{
		return _mostExpensiveItemCrafted;
	}

	private void UpdateTotalTimePlayedInSeconds()
	{
		double totalSeconds = (DateTime.Now - _lastGameplayTimeCheck).TotalSeconds;
		if (totalSeconds > 9.999999747378752E-06)
		{
			_totalTimePlayedInSeconds += (ulong)totalSeconds;
			_lastGameplayTimeCheck = DateTime.Now;
		}
	}
}
