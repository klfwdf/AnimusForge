using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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

namespace SandBox.CampaignBehaviors
{
	// Token: 0x020000DC RID: 220
	public class StatisticsCampaignBehavior : CampaignBehaviorBase, IStatisticsCampaignBehavior, ICampaignBehavior
	{
		// Token: 0x06000A71 RID: 2673 RVA: 0x0004F14C File Offset: 0x0004D34C
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<int>("_highestTournamentRank", ref this._highestTournamentRank);
			dataStore.SyncData<int>("_numberOfTournamentWins", ref this._numberOfTournamentWins);
			dataStore.SyncData<int>("_numberOfChildrenBorn", ref this._numberOfChildrenBorn);
			dataStore.SyncData<int>("_numberOfPrisonersRecruited", ref this._numberOfPrisonersRecruited);
			dataStore.SyncData<int>("_numberOfTroopsRecruited", ref this._numberOfTroopsRecruited);
			dataStore.SyncData<int>("_numberOfClansDefected", ref this._numberOfClansDefected);
			dataStore.SyncData<int>("_numberOfIssuesSolved", ref this._numberOfIssuesSolved);
			dataStore.SyncData<int>("_totalInfluenceEarned", ref this._totalInfluenceEarned);
			dataStore.SyncData<int>("_totalCrimeRatingGained", ref this._totalCrimeRatingGained);
			dataStore.SyncData<ulong>("_totalTimePlayedInSeconds", ref this._totalTimePlayedInSeconds);
			dataStore.SyncData<int>("_numberOfbattlesWon", ref this._numberOfbattlesWon);
			dataStore.SyncData<int>("_numberOfbattlesLost", ref this._numberOfbattlesLost);
			dataStore.SyncData<int>("_largestBattleWonAsLeader", ref this._largestBattleWonAsLeader);
			dataStore.SyncData<int>("_largestArmyFormedByPlayer", ref this._largestArmyFormedByPlayer);
			dataStore.SyncData<int>("_numberOfEnemyClansDestroyed", ref this._numberOfEnemyClansDestroyed);
			dataStore.SyncData<int>("_numberOfHeroesKilledInBattle", ref this._numberOfHeroesKilledInBattle);
			dataStore.SyncData<int>("_numberOfTroopsKnockedOrKilledAsParty", ref this._numberOfTroopsKnockedOrKilledAsParty);
			dataStore.SyncData<int>("_numberOfTroopsKnockedOrKilledByPlayer", ref this._numberOfTroopsKnockedOrKilledByPlayer);
			dataStore.SyncData<int>("_numberOfHeroPrisonersTaken", ref this._numberOfHeroPrisonersTaken);
			dataStore.SyncData<int>("_numberOfTroopPrisonersTaken", ref this._numberOfTroopPrisonersTaken);
			dataStore.SyncData<int>("_numberOfTownsCaptured", ref this._numberOfTownsCaptured);
			dataStore.SyncData<int>("_numberOfHideoutsCleared", ref this._numberOfHideoutsCleared);
			dataStore.SyncData<int>("_numberOfCastlesCaptured", ref this._numberOfCastlesCaptured);
			dataStore.SyncData<int>("_numberOfVillagesRaided", ref this._numberOfVillagesRaided);
			dataStore.SyncData<CampaignTime>("_timeSpentAsPrisoner", ref this._timeSpentAsPrisoner);
			dataStore.SyncData<ulong>("_totalDenarsEarned", ref this._totalDenarsEarned);
			dataStore.SyncData<ulong>("_denarsEarnedFromCaravans", ref this._denarsEarnedFromCaravans);
			dataStore.SyncData<ulong>("_denarsEarnedFromWorkshops", ref this._denarsEarnedFromWorkshops);
			dataStore.SyncData<ulong>("_denarsEarnedFromRansoms", ref this._denarsEarnedFromRansoms);
			dataStore.SyncData<ulong>("_denarsEarnedFromTaxes", ref this._denarsEarnedFromTaxes);
			dataStore.SyncData<ulong>("_denarsEarnedFromTributes", ref this._denarsEarnedFromTributes);
			dataStore.SyncData<ulong>("_denarsPaidAsTributes", ref this._denarsPaidAsTributes);
			dataStore.SyncData<int>("_numberOfCraftingPartsUnlocked", ref this._numberOfCraftingPartsUnlocked);
			dataStore.SyncData<int>("_numberOfWeaponsCrafted", ref this._numberOfWeaponsCrafted);
			dataStore.SyncData<int>("_numberOfCraftingOrdersCompleted", ref this._numberOfCraftingOrdersCompleted);
			dataStore.SyncData<ValueTuple<string, int>>("_mostExpensiveItemCrafted", ref this._mostExpensiveItemCrafted);
			dataStore.SyncData<int>("_numberOfCompanionsHired", ref this._numberOfCompanionsHired);
			dataStore.SyncData<Dictionary<Hero, ValueTuple<int, int>>>("_companionData", ref this._companionData);
			dataStore.SyncData<int>("_lastPlayerBattleSize", ref this._lastPlayerBattleSize);
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x0004F418 File Offset: 0x0004D618
		public override void RegisterEvents()
		{
			CampaignEvents.HeroCreated.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroCreated));
			CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, new Action<IssueBase, IssueBase.IssueUpdateDetails, Hero>(this.OnIssueUpdated));
			CampaignEvents.TournamentFinished.AddNonSerializedListener(this, new Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>(this.OnTournamentFinished));
			CampaignEvents.OnClanInfluenceChangedEvent.AddNonSerializedListener(this, new Action<Clan, float>(this.OnClanInfluenceChanged));
			CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnAfterSessionLaunched));
			CampaignEvents.CrimeRatingChanged.AddNonSerializedListener(this, new Action<IFaction, float>(this.OnCrimeRatingChanged));
			CampaignEvents.OnMainPartyPrisonerRecruitedEvent.AddNonSerializedListener(this, new Action<FlattenedTroopRoster>(this.OnMainPartyPrisonerRecruited));
			CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, new Action<CharacterObject, int>(this.OnUnitRecruited));
			CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, new Action(this.OnBeforeSave));
			CampaignEvents.CraftingPartUnlockedEvent.AddNonSerializedListener(this, new Action<CraftingPiece>(this.OnCraftingPartUnlocked));
			CampaignEvents.OnNewItemCraftedEvent.AddNonSerializedListener(this, new Action<ItemObject, ItemModifier, bool>(this.OnNewItemCrafted));
			CampaignEvents.NewCompanionAdded.AddNonSerializedListener(this, new Action<Hero>(this.OnNewCompanionAdded));
			CampaignEvents.HeroOrPartyTradedGold.AddNonSerializedListener(this, new Action<ValueTuple<Hero, PartyBase>, ValueTuple<Hero, PartyBase>, ValueTuple<int, string>, bool>(this.OnHeroOrPartyTradedGold));
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(this.OnMapEventEnd));
			CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, new Action<Clan>(this.OnClanDestroyed));
			CampaignEvents.PartyAttachedAnotherParty.AddNonSerializedListener(this, new Action<MobileParty>(this.OnPartyAttachedAnotherParty));
			CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnArmyCreated));
			CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, new Action<PartyBase, Hero>(this.OnHeroPrisonerTaken));
			CampaignEvents.OnPrisonerTakenEvent.AddNonSerializedListener(this, new Action<FlattenedTroopRoster>(this.OnPrisonersTaken));
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
			CampaignEvents.OnHideoutBattleCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, HideoutEventComponent>(this.OnHideoutBattleCompleted));
			CampaignEvents.MapEventStarted.AddNonSerializedListener(this, new Action<MapEvent, PartyBase, PartyBase>(this.OnMapEventStarted));
			CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, new Action<Hero, PartyBase, IFaction, EndCaptivityDetail, bool>(this.OnHeroPrisonerReleased));
			CampaignEvents.OnPlayerPartyKnockedOrKilledTroopEvent.AddNonSerializedListener(this, new Action<CharacterObject>(this.OnPlayerPartyKnockedOrKilledTroop));
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionStarted));
			CampaignEvents.OnPlayerEarnedGoldFromAssetEvent.AddNonSerializedListener(this, new Action<DefaultClanFinanceModel.AssetIncomeType, int>(this.OnPlayerEarnedGoldFromAsset));
			CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, new Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>(this.OnHeroKilled));
		}

		// Token: 0x06000A73 RID: 2675 RVA: 0x0004F692 File Offset: 0x0004D892
		private void OnBeforeSave()
		{
			this.UpdateTotalTimePlayedInSeconds();
		}

		// Token: 0x06000A74 RID: 2676 RVA: 0x0004F69A File Offset: 0x0004D89A
		private void OnAfterSessionLaunched(CampaignGameStarter starter)
		{
			this._lastGameplayTimeCheck = DateTime.Now;
			if (this._highestTournamentRank == 0)
			{
				this._highestTournamentRank = Campaign.Current.TournamentManager.GetLeaderBoardRank(Hero.MainHero);
			}
		}

		// Token: 0x06000A75 RID: 2677 RVA: 0x0004F6C9 File Offset: 0x0004D8C9
		public void OnDefectionPersuasionSucess()
		{
			this._numberOfClansDefected++;
		}

		// Token: 0x06000A76 RID: 2678 RVA: 0x0004F6D9 File Offset: 0x0004D8D9
		private void OnUnitRecruited(CharacterObject character, int amount)
		{
			this._numberOfTroopsRecruited += amount;
		}

		// Token: 0x06000A77 RID: 2679 RVA: 0x0004F6E9 File Offset: 0x0004D8E9
		private void OnMainPartyPrisonerRecruited(FlattenedTroopRoster flattenedTroopRoster)
		{
			this._numberOfPrisonersRecruited += flattenedTroopRoster.CountQ<FlattenedTroopRosterElement>();
		}

		// Token: 0x06000A78 RID: 2680 RVA: 0x0004F6FE File Offset: 0x0004D8FE
		private void OnCrimeRatingChanged(IFaction kingdom, float deltaCrimeAmount)
		{
			if (deltaCrimeAmount > 0f)
			{
				this._totalCrimeRatingGained += (int)deltaCrimeAmount;
			}
		}

		// Token: 0x06000A79 RID: 2681 RVA: 0x0004F717 File Offset: 0x0004D917
		private void OnClanInfluenceChanged(Clan clan, float change)
		{
			if (change > 0f && clan == Clan.PlayerClan)
			{
				this._totalInfluenceEarned += (int)change;
			}
		}

		// Token: 0x06000A7A RID: 2682 RVA: 0x0004F738 File Offset: 0x0004D938
		private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			if (winner.HeroObject == Hero.MainHero)
			{
				this._numberOfTournamentWins++;
				int leaderBoardRank = Campaign.Current.TournamentManager.GetLeaderBoardRank(Hero.MainHero);
				if (leaderBoardRank < this._highestTournamentRank)
				{
					this._highestTournamentRank = leaderBoardRank;
				}
			}
		}

		// Token: 0x06000A7B RID: 2683 RVA: 0x0004F788 File Offset: 0x0004D988
		private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver = null)
		{
			if (details == IssueBase.IssueUpdateDetails.IssueFinishedWithSuccess || details == IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest || details == IssueBase.IssueUpdateDetails.IssueFinishedWithBetrayal)
			{
				this._numberOfIssuesSolved++;
				if (issueSolver != null && issueSolver.IsPlayerCompanion)
				{
					if (this._companionData.ContainsKey(issueSolver))
					{
						this._companionData[issueSolver] = new ValueTuple<int, int>(this._companionData[issueSolver].Item1 + 1, this._companionData[issueSolver].Item2);
						return;
					}
					this._companionData.Add(issueSolver, new ValueTuple<int, int>(1, 0));
				}
			}
		}

		// Token: 0x06000A7C RID: 2684 RVA: 0x0004F811 File Offset: 0x0004DA11
		private void OnHeroCreated(Hero hero, bool isBornNaturally = false)
		{
			if (hero.Mother == Hero.MainHero || hero.Father == Hero.MainHero)
			{
				this._numberOfChildrenBorn++;
			}
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x0004F83B File Offset: 0x0004DA3B
		private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification = true)
		{
			if (killer != null && killer.PartyBelongedTo == MobileParty.MainParty && detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle)
			{
				this._numberOfHeroesKilledInBattle++;
			}
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x0004F860 File Offset: 0x0004DA60
		private void OnMissionStarted(IMission mission)
		{
			StatisticsCampaignBehavior.StatisticsMissionLogic missionBehavior = new StatisticsCampaignBehavior.StatisticsMissionLogic();
			Mission.Current.AddMissionBehavior(missionBehavior);
		}

		// Token: 0x06000A7F RID: 2687 RVA: 0x0004F880 File Offset: 0x0004DA80
		private void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent)
		{
			if (affectorAgent != null)
			{
				if (affectorAgent == Agent.Main)
				{
					this._numberOfTroopsKnockedOrKilledByPlayer++;
				}
				else if (affectorAgent.IsPlayerTroop)
				{
					this._numberOfTroopsKnockedOrKilledAsParty++;
				}
				else if (affectorAgent.IsHero)
				{
					Hero heroObject = (affectorAgent.Character as CharacterObject).HeroObject;
					if (heroObject.IsPlayerCompanion)
					{
						if (this._companionData.ContainsKey(heroObject))
						{
							this._companionData[heroObject] = new ValueTuple<int, int>(this._companionData[heroObject].Item1, this._companionData[heroObject].Item2 + 1);
						}
						else
						{
							this._companionData.Add(heroObject, new ValueTuple<int, int>(0, 1));
						}
					}
				}
				if (affectedAgent.IsHero && affectedAgent.State == AgentState.Killed)
				{
					this._numberOfHeroesKilledInBattle++;
				}
			}
		}

		// Token: 0x06000A80 RID: 2688 RVA: 0x0004F95E File Offset: 0x0004DB5E
		private void OnPlayerPartyKnockedOrKilledTroop(CharacterObject troop)
		{
			this._numberOfTroopsKnockedOrKilledAsParty++;
		}

		// Token: 0x06000A81 RID: 2689 RVA: 0x0004F96E File Offset: 0x0004DB6E
		private void OnHeroPrisonerReleased(Hero prisoner, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
		{
			if (prisoner == Hero.MainHero)
			{
				this._timeSpentAsPrisoner += CampaignTime.Now - PlayerCaptivity.CaptivityStartTime;
			}
		}

		// Token: 0x06000A82 RID: 2690 RVA: 0x0004F998 File Offset: 0x0004DB98
		private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
		{
			if (mapEvent.IsPlayerMapEvent)
			{
				this._lastPlayerBattleSize = mapEvent.AttackerSide.TroopCount + mapEvent.DefenderSide.TroopCount;
			}
		}

		// Token: 0x06000A83 RID: 2691 RVA: 0x0004F9BF File Offset: 0x0004DBBF
		private void OnHideoutBattleCompleted(BattleSideEnum winnerSide, HideoutEventComponent hideoutEventComponent)
		{
			if (hideoutEventComponent.MapEvent.PlayerSide == winnerSide)
			{
				this._numberOfHideoutsCleared++;
			}
		}

		// Token: 0x06000A84 RID: 2692 RVA: 0x0004F9DD File Offset: 0x0004DBDD
		private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEventComponent)
		{
			if (raidEventComponent.MapEvent.HasWinner && raidEventComponent.MapEvent.PlayerSide == winnerSide)
			{
				this._numberOfVillagesRaided++;
			}
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x0004FA08 File Offset: 0x0004DC08
		private void OnPrisonersTaken(FlattenedTroopRoster troopRoster)
		{
			this._numberOfTroopPrisonersTaken += troopRoster.CountQ<FlattenedTroopRosterElement>();
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x0004FA1D File Offset: 0x0004DC1D
		private void OnHeroPrisonerTaken(PartyBase capturer, Hero prisoner)
		{
			if (capturer == PartyBase.MainParty)
			{
				this._numberOfHeroPrisonersTaken++;
			}
		}

		// Token: 0x06000A87 RID: 2695 RVA: 0x0004FA35 File Offset: 0x0004DC35
		private void OnArmyCreated(Army army)
		{
			if (army.LeaderParty == MobileParty.MainParty && this._largestArmyFormedByPlayer < army.TotalManCount)
			{
				this._largestArmyFormedByPlayer = army.TotalManCount;
			}
		}

		// Token: 0x06000A88 RID: 2696 RVA: 0x0004FA60 File Offset: 0x0004DC60
		private void OnPartyAttachedAnotherParty(MobileParty mobileParty)
		{
			if (mobileParty.Army == MobileParty.MainParty.Army && MobileParty.MainParty.Army.LeaderParty == MobileParty.MainParty && this._largestArmyFormedByPlayer < MobileParty.MainParty.Army.TotalManCount)
			{
				this._largestArmyFormedByPlayer = MobileParty.MainParty.Army.TotalManCount;
			}
		}

		// Token: 0x06000A89 RID: 2697 RVA: 0x0004FAC1 File Offset: 0x0004DCC1
		private void OnClanDestroyed(Clan clan)
		{
			if (clan.IsAtWarWith(Clan.PlayerClan))
			{
				this._numberOfEnemyClansDestroyed++;
			}
		}

		// Token: 0x06000A8A RID: 2698 RVA: 0x0004FAE0 File Offset: 0x0004DCE0
		private void OnMapEventEnd(MapEvent mapEvent)
		{
			if (mapEvent.IsPlayerMapEvent && mapEvent.HasWinner)
			{
				if (mapEvent.WinningSide == mapEvent.PlayerSide)
				{
					this._numberOfbattlesWon++;
					if (mapEvent.IsSiegeAssault && !mapEvent.IsPlayerSergeant() && mapEvent.MapEventSettlement != null)
					{
						if (mapEvent.MapEventSettlement.IsTown)
						{
							this._numberOfTownsCaptured++;
						}
						else if (mapEvent.MapEventSettlement.IsCastle)
						{
							this._numberOfCastlesCaptured++;
						}
					}
					if (this._largestBattleWonAsLeader < this._lastPlayerBattleSize && !mapEvent.IsPlayerSergeant())
					{
						this._largestBattleWonAsLeader = this._lastPlayerBattleSize;
						return;
					}
				}
				else
				{
					this._numberOfbattlesLost++;
				}
			}
		}

		// Token: 0x06000A8B RID: 2699 RVA: 0x0004FBA3 File Offset: 0x0004DDA3
		private void OnHeroOrPartyTradedGold(ValueTuple<Hero, PartyBase> giver, ValueTuple<Hero, PartyBase> recipient, ValueTuple<int, string> goldAmount, bool showNotification)
		{
			if (recipient.Item1 == Hero.MainHero || recipient.Item2 == PartyBase.MainParty)
			{
				this._totalDenarsEarned += (ulong)((long)goldAmount.Item1);
			}
		}

		// Token: 0x06000A8C RID: 2700 RVA: 0x0004FBD3 File Offset: 0x0004DDD3
		public void OnPlayerAcceptedRansomOffer(int ransomPrice)
		{
			this._denarsEarnedFromRansoms += (ulong)((long)ransomPrice);
		}

		// Token: 0x06000A8D RID: 2701 RVA: 0x0004FBE4 File Offset: 0x0004DDE4
		private void OnPlayerEarnedGoldFromAsset(DefaultClanFinanceModel.AssetIncomeType assetType, int amount)
		{
			switch (assetType)
			{
			case DefaultClanFinanceModel.AssetIncomeType.Workshop:
				this._denarsEarnedFromWorkshops += (ulong)((long)amount);
				return;
			case DefaultClanFinanceModel.AssetIncomeType.Caravan:
				this._denarsEarnedFromCaravans += (ulong)((long)amount);
				return;
			case DefaultClanFinanceModel.AssetIncomeType.Taxes:
				this._denarsEarnedFromTaxes += (ulong)((long)amount);
				return;
			case DefaultClanFinanceModel.AssetIncomeType.TributesEarned:
				this._denarsEarnedFromTributes += (ulong)((long)amount);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000A8E RID: 2702 RVA: 0x0004FC47 File Offset: 0x0004DE47
		private void OnNewCompanionAdded(Hero hero)
		{
			this._numberOfCompanionsHired++;
		}

		// Token: 0x06000A8F RID: 2703 RVA: 0x0004FC58 File Offset: 0x0004DE58
		private void OnNewItemCrafted(ItemObject itemObject, ItemModifier overriddenItemModifier, bool isCraftingOrderItem)
		{
			this._numberOfWeaponsCrafted++;
			if (isCraftingOrderItem)
			{
				this._numberOfCraftingOrdersCompleted++;
			}
			if (this._mostExpensiveItemCrafted.Item2 == 0 || this._mostExpensiveItemCrafted.Item2 < itemObject.Value)
			{
				this._mostExpensiveItemCrafted.Item1 = itemObject.Name.ToString();
				this._mostExpensiveItemCrafted.Item2 = itemObject.Value;
			}
		}

		// Token: 0x06000A90 RID: 2704 RVA: 0x0004FCCB File Offset: 0x0004DECB
		private void OnCraftingPartUnlocked(CraftingPiece craftingPiece)
		{
			this._numberOfCraftingPartsUnlocked++;
		}

		// Token: 0x06000A91 RID: 2705 RVA: 0x0004FCDC File Offset: 0x0004DEDC
		[return: TupleElementNames(new string[] { "name", "value" })]
		public ValueTuple<string, int> GetCompanionWithMostKills()
		{
			if (this._companionData.IsEmpty<KeyValuePair<Hero, ValueTuple<int, int>>>())
			{
				return new ValueTuple<string, int>(null, 0);
			}
			KeyValuePair<Hero, ValueTuple<int, int>> keyValuePair = this._companionData.MaxBy((KeyValuePair<Hero, ValueTuple<int, int>> kvp) => kvp.Value.Item2);
			return new ValueTuple<string, int>(keyValuePair.Key.Name.ToString(), keyValuePair.Value.Item2);
		}

		// Token: 0x06000A92 RID: 2706 RVA: 0x0004FD4C File Offset: 0x0004DF4C
		[return: TupleElementNames(new string[] { "name", "value" })]
		public ValueTuple<string, int> GetCompanionWithMostIssuesSolved()
		{
			if (this._companionData.IsEmpty<KeyValuePair<Hero, ValueTuple<int, int>>>())
			{
				return new ValueTuple<string, int>(null, 0);
			}
			KeyValuePair<Hero, ValueTuple<int, int>> keyValuePair = this._companionData.MaxBy((KeyValuePair<Hero, ValueTuple<int, int>> kvp) => kvp.Value.Item1);
			return new ValueTuple<string, int>(keyValuePair.Key.Name.ToString(), keyValuePair.Value.Item1);
		}

		// Token: 0x06000A93 RID: 2707 RVA: 0x0004FDBB File Offset: 0x0004DFBB
		public int GetHighestTournamentRank()
		{
			return this._highestTournamentRank;
		}

		// Token: 0x06000A94 RID: 2708 RVA: 0x0004FDC3 File Offset: 0x0004DFC3
		public int GetNumberOfTournamentWins()
		{
			return this._numberOfTournamentWins;
		}

		// Token: 0x06000A95 RID: 2709 RVA: 0x0004FDCB File Offset: 0x0004DFCB
		public int GetNumberOfChildrenBorn()
		{
			return this._numberOfChildrenBorn;
		}

		// Token: 0x06000A96 RID: 2710 RVA: 0x0004FDD3 File Offset: 0x0004DFD3
		public int GetNumberOfPrisonersRecruited()
		{
			return this._numberOfPrisonersRecruited;
		}

		// Token: 0x06000A97 RID: 2711 RVA: 0x0004FDDB File Offset: 0x0004DFDB
		public int GetNumberOfTroopsRecruited()
		{
			return this._numberOfTroopsRecruited;
		}

		// Token: 0x06000A98 RID: 2712 RVA: 0x0004FDE3 File Offset: 0x0004DFE3
		public int GetNumberOfClansDefected()
		{
			return this._numberOfClansDefected;
		}

		// Token: 0x06000A99 RID: 2713 RVA: 0x0004FDEB File Offset: 0x0004DFEB
		public int GetNumberOfIssuesSolved()
		{
			return this._numberOfIssuesSolved;
		}

		// Token: 0x06000A9A RID: 2714 RVA: 0x0004FDF3 File Offset: 0x0004DFF3
		public int GetTotalInfluenceEarned()
		{
			return this._totalInfluenceEarned;
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x0004FDFB File Offset: 0x0004DFFB
		public int GetTotalCrimeRatingGained()
		{
			return this._totalCrimeRatingGained;
		}

		// Token: 0x06000A9C RID: 2716 RVA: 0x0004FE03 File Offset: 0x0004E003
		public int GetNumberOfBattlesWon()
		{
			return this._numberOfbattlesWon;
		}

		// Token: 0x06000A9D RID: 2717 RVA: 0x0004FE0B File Offset: 0x0004E00B
		public int GetNumberOfBattlesLost()
		{
			return this._numberOfbattlesLost;
		}

		// Token: 0x06000A9E RID: 2718 RVA: 0x0004FE13 File Offset: 0x0004E013
		public int GetLargestBattleWonAsLeader()
		{
			return this._largestBattleWonAsLeader;
		}

		// Token: 0x06000A9F RID: 2719 RVA: 0x0004FE1B File Offset: 0x0004E01B
		public int GetLargestArmyFormedByPlayer()
		{
			return this._largestArmyFormedByPlayer;
		}

		// Token: 0x06000AA0 RID: 2720 RVA: 0x0004FE23 File Offset: 0x0004E023
		public int GetNumberOfEnemyClansDestroyed()
		{
			return this._numberOfEnemyClansDestroyed;
		}

		// Token: 0x06000AA1 RID: 2721 RVA: 0x0004FE2B File Offset: 0x0004E02B
		public int GetNumberOfHeroesKilledInBattle()
		{
			return this._numberOfHeroesKilledInBattle;
		}

		// Token: 0x06000AA2 RID: 2722 RVA: 0x0004FE33 File Offset: 0x0004E033
		public int GetNumberOfTroopsKnockedOrKilledAsParty()
		{
			return this._numberOfTroopsKnockedOrKilledAsParty;
		}

		// Token: 0x06000AA3 RID: 2723 RVA: 0x0004FE3B File Offset: 0x0004E03B
		public int GetNumberOfTroopsKnockedOrKilledByPlayer()
		{
			return this._numberOfTroopsKnockedOrKilledByPlayer;
		}

		// Token: 0x06000AA4 RID: 2724 RVA: 0x0004FE43 File Offset: 0x0004E043
		public int GetNumberOfHeroPrisonersTaken()
		{
			return this._numberOfHeroPrisonersTaken;
		}

		// Token: 0x06000AA5 RID: 2725 RVA: 0x0004FE4B File Offset: 0x0004E04B
		public int GetNumberOfTroopPrisonersTaken()
		{
			return this._numberOfTroopPrisonersTaken;
		}

		// Token: 0x06000AA6 RID: 2726 RVA: 0x0004FE53 File Offset: 0x0004E053
		public int GetNumberOfTownsCaptured()
		{
			return this._numberOfTownsCaptured;
		}

		// Token: 0x06000AA7 RID: 2727 RVA: 0x0004FE5B File Offset: 0x0004E05B
		public int GetNumberOfHideoutsCleared()
		{
			return this._numberOfHideoutsCleared;
		}

		// Token: 0x06000AA8 RID: 2728 RVA: 0x0004FE63 File Offset: 0x0004E063
		public int GetNumberOfCastlesCaptured()
		{
			return this._numberOfCastlesCaptured;
		}

		// Token: 0x06000AA9 RID: 2729 RVA: 0x0004FE6B File Offset: 0x0004E06B
		public int GetNumberOfVillagesRaided()
		{
			return this._numberOfVillagesRaided;
		}

		// Token: 0x06000AAA RID: 2730 RVA: 0x0004FE73 File Offset: 0x0004E073
		public int GetNumberOfCraftingPartsUnlocked()
		{
			return this._numberOfCraftingPartsUnlocked;
		}

		// Token: 0x06000AAB RID: 2731 RVA: 0x0004FE7B File Offset: 0x0004E07B
		public int GetNumberOfWeaponsCrafted()
		{
			return this._numberOfWeaponsCrafted;
		}

		// Token: 0x06000AAC RID: 2732 RVA: 0x0004FE83 File Offset: 0x0004E083
		public int GetNumberOfCraftingOrdersCompleted()
		{
			return this._numberOfCraftingOrdersCompleted;
		}

		// Token: 0x06000AAD RID: 2733 RVA: 0x0004FE8B File Offset: 0x0004E08B
		public int GetNumberOfCompanionsHired()
		{
			return this._numberOfCompanionsHired;
		}

		// Token: 0x06000AAE RID: 2734 RVA: 0x0004FE93 File Offset: 0x0004E093
		public CampaignTime GetTimeSpentAsPrisoner()
		{
			return this._timeSpentAsPrisoner;
		}

		// Token: 0x06000AAF RID: 2735 RVA: 0x0004FE9B File Offset: 0x0004E09B
		public ulong GetTotalTimePlayedInSeconds()
		{
			this.UpdateTotalTimePlayedInSeconds();
			return this._totalTimePlayedInSeconds;
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x0004FEA9 File Offset: 0x0004E0A9
		public ulong GetTotalDenarsEarned()
		{
			return this._totalDenarsEarned;
		}

		// Token: 0x06000AB1 RID: 2737 RVA: 0x0004FEB1 File Offset: 0x0004E0B1
		public ulong GetDenarsEarnedFromCaravans()
		{
			return this._denarsEarnedFromCaravans;
		}

		// Token: 0x06000AB2 RID: 2738 RVA: 0x0004FEB9 File Offset: 0x0004E0B9
		public ulong GetDenarsEarnedFromWorkshops()
		{
			return this._denarsEarnedFromWorkshops;
		}

		// Token: 0x06000AB3 RID: 2739 RVA: 0x0004FEC1 File Offset: 0x0004E0C1
		public ulong GetDenarsEarnedFromRansoms()
		{
			return this._denarsEarnedFromRansoms;
		}

		// Token: 0x06000AB4 RID: 2740 RVA: 0x0004FEC9 File Offset: 0x0004E0C9
		public ulong GetDenarsEarnedFromTaxes()
		{
			return this._denarsEarnedFromTaxes;
		}

		// Token: 0x06000AB5 RID: 2741 RVA: 0x0004FED1 File Offset: 0x0004E0D1
		public ulong GetDenarsEarnedFromTributes()
		{
			return this._denarsEarnedFromTributes;
		}

		// Token: 0x06000AB6 RID: 2742 RVA: 0x0004FED9 File Offset: 0x0004E0D9
		public ulong GetDenarsPaidAsTributes()
		{
			return this._denarsPaidAsTributes;
		}

		// Token: 0x06000AB7 RID: 2743 RVA: 0x0004FEE1 File Offset: 0x0004E0E1
		public CampaignTime GetTotalTimePlayed()
		{
			return CampaignTime.Now - Campaign.Current.Models.CampaignTimeModel.CampaignStartTime;
		}

		// Token: 0x06000AB8 RID: 2744 RVA: 0x0004FF01 File Offset: 0x0004E101
		public ValueTuple<string, int> GetMostExpensiveItemCrafted()
		{
			return this._mostExpensiveItemCrafted;
		}

		// Token: 0x06000AB9 RID: 2745 RVA: 0x0004FF0C File Offset: 0x0004E10C
		private void UpdateTotalTimePlayedInSeconds()
		{
			double totalSeconds = (DateTime.Now - this._lastGameplayTimeCheck).TotalSeconds;
			if (totalSeconds > 9.999999747378752E-06)
			{
				this._totalTimePlayedInSeconds += (ulong)totalSeconds;
				this._lastGameplayTimeCheck = DateTime.Now;
			}
		}

		// Token: 0x04000492 RID: 1170
		private int _highestTournamentRank;

		// Token: 0x04000493 RID: 1171
		private int _numberOfTournamentWins;

		// Token: 0x04000494 RID: 1172
		private int _numberOfChildrenBorn;

		// Token: 0x04000495 RID: 1173
		private int _numberOfPrisonersRecruited;

		// Token: 0x04000496 RID: 1174
		private int _numberOfTroopsRecruited;

		// Token: 0x04000497 RID: 1175
		private int _numberOfClansDefected;

		// Token: 0x04000498 RID: 1176
		private int _numberOfIssuesSolved;

		// Token: 0x04000499 RID: 1177
		private int _totalInfluenceEarned;

		// Token: 0x0400049A RID: 1178
		private int _totalCrimeRatingGained;

		// Token: 0x0400049B RID: 1179
		private ulong _totalTimePlayedInSeconds;

		// Token: 0x0400049C RID: 1180
		private int _numberOfbattlesWon;

		// Token: 0x0400049D RID: 1181
		private int _numberOfbattlesLost;

		// Token: 0x0400049E RID: 1182
		private int _largestBattleWonAsLeader;

		// Token: 0x0400049F RID: 1183
		private int _largestArmyFormedByPlayer;

		// Token: 0x040004A0 RID: 1184
		private int _numberOfEnemyClansDestroyed;

		// Token: 0x040004A1 RID: 1185
		private int _numberOfHeroesKilledInBattle;

		// Token: 0x040004A2 RID: 1186
		private int _numberOfTroopsKnockedOrKilledAsParty;

		// Token: 0x040004A3 RID: 1187
		private int _numberOfTroopsKnockedOrKilledByPlayer;

		// Token: 0x040004A4 RID: 1188
		private int _numberOfHeroPrisonersTaken;

		// Token: 0x040004A5 RID: 1189
		private int _numberOfTroopPrisonersTaken;

		// Token: 0x040004A6 RID: 1190
		private int _numberOfTownsCaptured;

		// Token: 0x040004A7 RID: 1191
		private int _numberOfHideoutsCleared;

		// Token: 0x040004A8 RID: 1192
		private int _numberOfCastlesCaptured;

		// Token: 0x040004A9 RID: 1193
		private int _numberOfVillagesRaided;

		// Token: 0x040004AA RID: 1194
		private CampaignTime _timeSpentAsPrisoner;

		// Token: 0x040004AB RID: 1195
		private ulong _totalDenarsEarned;

		// Token: 0x040004AC RID: 1196
		private ulong _denarsEarnedFromCaravans;

		// Token: 0x040004AD RID: 1197
		private ulong _denarsEarnedFromWorkshops;

		// Token: 0x040004AE RID: 1198
		private ulong _denarsEarnedFromRansoms;

		// Token: 0x040004AF RID: 1199
		private ulong _denarsEarnedFromTaxes;

		// Token: 0x040004B0 RID: 1200
		private ulong _denarsEarnedFromTributes;

		// Token: 0x040004B1 RID: 1201
		private ulong _denarsPaidAsTributes;

		// Token: 0x040004B2 RID: 1202
		private int _numberOfCraftingPartsUnlocked;

		// Token: 0x040004B3 RID: 1203
		private int _numberOfWeaponsCrafted;

		// Token: 0x040004B4 RID: 1204
		private int _numberOfCraftingOrdersCompleted;

		// Token: 0x040004B5 RID: 1205
		private ValueTuple<string, int> _mostExpensiveItemCrafted = new ValueTuple<string, int>(null, 0);

		// Token: 0x040004B6 RID: 1206
		private int _numberOfCompanionsHired;

		// Token: 0x040004B7 RID: 1207
		private Dictionary<Hero, ValueTuple<int, int>> _companionData = new Dictionary<Hero, ValueTuple<int, int>>();

		// Token: 0x040004B8 RID: 1208
		private int _lastPlayerBattleSize;

		// Token: 0x040004B9 RID: 1209
		private DateTime _lastGameplayTimeCheck;

		// Token: 0x02000201 RID: 513
		private class StatisticsMissionLogic : MissionLogic
		{
			// Token: 0x060013A3 RID: 5027 RVA: 0x00077BE0 File Offset: 0x00075DE0
			public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
			{
				if (this.behavior != null)
				{
					this.behavior.OnAgentRemoved(affectedAgent, affectorAgent);
				}
			}

			// Token: 0x0400093C RID: 2364
			private readonly StatisticsCampaignBehavior behavior = Campaign.Current.CampaignBehaviorManager.GetBehavior<StatisticsCampaignBehavior>();
		}
	}
}
