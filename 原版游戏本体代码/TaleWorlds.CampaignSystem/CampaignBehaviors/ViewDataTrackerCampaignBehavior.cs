using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
	// Token: 0x0200044B RID: 1099
	public class ViewDataTrackerCampaignBehavior : CampaignBehaviorBase, IViewDataTracker
	{
		// Token: 0x06004607 RID: 17927 RVA: 0x0015CF40 File Offset: 0x0015B140
		public ViewDataTrackerCampaignBehavior()
		{
			this._inventoryItemLocks = new List<string>();
			this._partyPrisonerLocks = new List<string>();
			this._partyTroopLocks = new List<string>();
			this._encyclopediaBookmarkedClans = new List<Clan>();
			this._encyclopediaBookmarkedConcepts = new List<Concept>();
			this._encyclopediaBookmarkedHeroes = new List<Hero>();
			this._encyclopediaBookmarkedShips = new List<ShipHull>();
			this._encyclopediaBookmarkedKingdoms = new List<Kingdom>();
			this._encyclopediaBookmarkedSettlements = new List<Settlement>();
			this._encyclopediaBookmarkedUnits = new List<CharacterObject>();
			this._inventorySortPreferences = new Dictionary<int, Tuple<int, int>>();
			this._plunderItems = new List<ItemRosterElement>();
			this._unexaminedFigureheads = new List<Figurehead>();
		}

		// Token: 0x17000E30 RID: 3632
		// (get) Token: 0x06004608 RID: 17928 RVA: 0x0015D041 File Offset: 0x0015B241
		// (set) Token: 0x06004609 RID: 17929 RVA: 0x0015D049 File Offset: 0x0015B249
		public bool IsPartyNotificationActive { get; private set; }

		// Token: 0x0600460A RID: 17930 RVA: 0x0015D052 File Offset: 0x0015B252
		public TextObject GetPartyNotificationText()
		{
			return this._recruitNotificationText.CopyTextObject().SetTextVariable("NUMBER", this._numOfRecruitablePrisoners);
		}

		// Token: 0x0600460B RID: 17931 RVA: 0x0015D06F File Offset: 0x0015B26F
		public void ClearPartyNotification()
		{
			this.IsPartyNotificationActive = false;
			this._numOfRecruitablePrisoners = 0;
		}

		// Token: 0x0600460C RID: 17932 RVA: 0x0015D07F File Offset: 0x0015B27F
		public void UpdatePartyNotification()
		{
			this.UpdatePrisonerRecruitValue();
		}

		// Token: 0x0600460D RID: 17933 RVA: 0x0015D088 File Offset: 0x0015B288
		private void UpdatePrisonerRecruitValue()
		{
			Dictionary<CharacterObject, int> dictionary = new Dictionary<CharacterObject, int>();
			foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.PrisonRoster.GetTroopRoster())
			{
				int num = Campaign.Current.Models.PrisonerRecruitmentCalculationModel.CalculateRecruitableNumber(PartyBase.MainParty, troopRosterElement.Character);
				int num2;
				if (this._examinedPrisonerCharacterList.TryGetValue(troopRosterElement.Character, out num2))
				{
					if (num2 != num)
					{
						this._examinedPrisonerCharacterList[troopRosterElement.Character] = num;
						if (num2 < num)
						{
							this.IsPartyNotificationActive = true;
							this._numOfRecruitablePrisoners += num - num2;
						}
					}
				}
				else
				{
					this._examinedPrisonerCharacterList.Add(troopRosterElement.Character, num);
					if (num > 0)
					{
						this.IsPartyNotificationActive = true;
						this._numOfRecruitablePrisoners += num;
					}
				}
				dictionary.Add(troopRosterElement.Character, num);
			}
			this._examinedPrisonerCharacterList = dictionary;
		}

		// Token: 0x17000E31 RID: 3633
		// (get) Token: 0x0600460E RID: 17934 RVA: 0x0015D194 File Offset: 0x0015B394
		public bool IsQuestNotificationActive
		{
			get
			{
				return this.UnExaminedQuestLogs.Count > 0;
			}
		}

		// Token: 0x17000E32 RID: 3634
		// (get) Token: 0x0600460F RID: 17935 RVA: 0x0015D1A4 File Offset: 0x0015B3A4
		public IReadOnlyList<JournalLog> UnExaminedQuestLogs
		{
			get
			{
				if (this._isUnExaminedQuestLogJournalEntriesDirty)
				{
					this.UpdateJournalLogEntries();
				}
				for (int i = this._unExaminedQuestLogs.Count - 1; i >= 0; i--)
				{
					JournalLogEntry journalLogEntry = this._unExaminedQuestLogJournalEntries[this._unExaminedQuestLogs[i]];
					CampaignTime campaignTime = journalLogEntry.KeepInHistoryTime + journalLogEntry.GameTime;
					if (!journalLogEntry.IsValid() || campaignTime.IsPast)
					{
						this._unExaminedQuestLogJournalEntries.Remove(this._unExaminedQuestLogs[i]);
						this._unExaminedQuestLogs.RemoveAt(i);
					}
				}
				if (this._unExaminedQuestLogsReadOnly == null || this._unExaminedQuestLogs.Count != this._unExaminedQuestLogsReadOnly.Count)
				{
					this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
				}
				return this._unExaminedQuestLogsReadOnly;
			}
		}

		// Token: 0x06004610 RID: 17936 RVA: 0x0015D26D File Offset: 0x0015B46D
		public TextObject GetQuestNotificationText()
		{
			return this._questNotificationText.CopyTextObject().SetTextVariable("NUMBER", this.UnExaminedQuestLogs.Count);
		}

		// Token: 0x06004611 RID: 17937 RVA: 0x0015D290 File Offset: 0x0015B490
		public void OnQuestLogExamined(JournalLog log)
		{
			if (this._unExaminedQuestLogs.Contains(log))
			{
				this._unExaminedQuestLogs.Remove(log);
				this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
				IEnumerable<JournalLog> entries = this._unExaminedQuestLogJournalEntries[log].GetEntries();
				if (this._unExaminedQuestLogs.All((JournalLog x) => !entries.Contains(x)))
				{
					this._unExaminedQuestLogJournalEntries.Remove(log);
				}
			}
		}

		// Token: 0x06004612 RID: 17938 RVA: 0x0015D30C File Offset: 0x0015B50C
		private void OnQuestLogAdded(QuestBase obj, bool hideInformation)
		{
			this._unExaminedQuestLogs.Add(obj.JournalEntries[obj.JournalEntries.Count - 1]);
			this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
			this._isUnExaminedQuestLogJournalEntriesDirty = true;
		}

		// Token: 0x06004613 RID: 17939 RVA: 0x0015D349 File Offset: 0x0015B549
		private void OnIssueLogAdded(IssueBase obj, bool hideInformation)
		{
			this._unExaminedQuestLogs.Add(obj.JournalEntries[obj.JournalEntries.Count - 1]);
			this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
			this._isUnExaminedQuestLogJournalEntriesDirty = true;
		}

		// Token: 0x06004614 RID: 17940 RVA: 0x0015D388 File Offset: 0x0015B588
		private void UpdateJournalLogEntries()
		{
			this._unExaminedQuestLogJournalEntries.Clear();
			JournalLogEntry[] source = Campaign.Current.LogEntryHistory.GetGameActionLogs<JournalLogEntry>((JournalLogEntry x) => true).ToArray<JournalLogEntry>();
			for (int i = this._unExaminedQuestLogs.Count - 1; i >= 0; i--)
			{
				JournalLog unExaminedQuestLog = this._unExaminedQuestLogs[i];
				JournalLogEntry journalLogEntry = source.FirstOrDefault((JournalLogEntry x) => x.GetEntries().Contains(unExaminedQuestLog));
				if (journalLogEntry == null)
				{
					this._unExaminedQuestLogs.RemoveAt(i);
				}
				else
				{
					this._unExaminedQuestLogJournalEntries.Add(unExaminedQuestLog, journalLogEntry);
				}
			}
			if (this._unExaminedQuestLogs.Count != this._unExaminedQuestLogsReadOnly.Count)
			{
				this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
			}
			this._isUnExaminedQuestLogJournalEntriesDirty = false;
		}

		// Token: 0x17000E33 RID: 3635
		// (get) Token: 0x06004615 RID: 17941 RVA: 0x0015D46A File Offset: 0x0015B66A
		public List<Army> UnExaminedArmies
		{
			get
			{
				return this._unExaminedArmies;
			}
		}

		// Token: 0x17000E34 RID: 3636
		// (get) Token: 0x06004616 RID: 17942 RVA: 0x0015D472 File Offset: 0x0015B672
		public int NumOfKingdomArmyNotifications
		{
			get
			{
				return this.UnExaminedArmies.Count;
			}
		}

		// Token: 0x06004617 RID: 17943 RVA: 0x0015D47F File Offset: 0x0015B67F
		public void OnArmyExamined(Army army)
		{
			this._unExaminedArmies.Remove(army);
		}

		// Token: 0x06004618 RID: 17944 RVA: 0x0015D490 File Offset: 0x0015B690
		private void OnArmyDispersed(Army arg1, Army.ArmyDispersionReason arg2, bool isPlayersArmy)
		{
			Army item;
			if (isPlayersArmy && (item = this._unExaminedArmies.SingleOrDefault((Army a) => a == arg1)) != null)
			{
				this._unExaminedArmies.Remove(item);
			}
		}

		// Token: 0x06004619 RID: 17945 RVA: 0x0015D4D5 File Offset: 0x0015B6D5
		private void OnNewArmyCreated(Army army)
		{
			if (army.Kingdom == Hero.MainHero.MapFaction && army.LeaderParty != MobileParty.MainParty)
			{
				this._unExaminedArmies.Add(army);
			}
		}

		// Token: 0x17000E35 RID: 3637
		// (get) Token: 0x0600461A RID: 17946 RVA: 0x0015D502 File Offset: 0x0015B702
		public bool IsCharacterNotificationActive
		{
			get
			{
				return this._isCharacterNotificationActive;
			}
		}

		// Token: 0x0600461B RID: 17947 RVA: 0x0015D50A File Offset: 0x0015B70A
		public void ClearCharacterNotification()
		{
			this._isCharacterNotificationActive = false;
			this._numOfPerks = 0;
		}

		// Token: 0x0600461C RID: 17948 RVA: 0x0015D51A File Offset: 0x0015B71A
		public TextObject GetCharacterNotificationText()
		{
			return this._characterNotificationText.CopyTextObject().SetTextVariable("NUMBER", this._numOfPerks);
		}

		// Token: 0x0600461D RID: 17949 RVA: 0x0015D537 File Offset: 0x0015B737
		private void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
		{
			if ((hero == Hero.MainHero || hero.Clan == Clan.PlayerClan) && PerkHelper.AvailablePerkCountOfHero(hero) > 0)
			{
				this._isCharacterNotificationActive = shouldNotify;
				this._numOfPerks++;
			}
		}

		// Token: 0x0600461E RID: 17950 RVA: 0x0015D56D File Offset: 0x0015B76D
		private void OnHeroLevelledUp(Hero hero, bool shouldNotify)
		{
			if (hero == Hero.MainHero)
			{
				this._isCharacterNotificationActive = shouldNotify;
			}
		}

		// Token: 0x0600461F RID: 17951 RVA: 0x0015D57E File Offset: 0x0015B77E
		private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
		{
			this._unExaminedQuestLogsReadOnly = this._unExaminedQuestLogs.AsReadOnly();
			this.UpdatePartyNotification();
			this.UpdatePrisonerRecruitValue();
			this.UpdateJournalLogEntries();
		}

		// Token: 0x06004620 RID: 17952 RVA: 0x0015D5A3 File Offset: 0x0015B7A3
		public bool GetMapBarExtendedState()
		{
			return this._isMapBarExtended;
		}

		// Token: 0x06004621 RID: 17953 RVA: 0x0015D5AB File Offset: 0x0015B7AB
		public void SetMapBarExtendedState(bool isExtended)
		{
			this._isMapBarExtended = isExtended;
		}

		// Token: 0x06004622 RID: 17954 RVA: 0x0015D5B4 File Offset: 0x0015B7B4
		public void SetInventoryLocks(IEnumerable<string> locks)
		{
			this._inventoryItemLocks = locks.ToList<string>();
		}

		// Token: 0x06004623 RID: 17955 RVA: 0x0015D5C2 File Offset: 0x0015B7C2
		public IEnumerable<string> GetInventoryLocks()
		{
			return this._inventoryItemLocks;
		}

		// Token: 0x06004624 RID: 17956 RVA: 0x0015D5CA File Offset: 0x0015B7CA
		public void InventorySetSortPreference(int inventoryMode, int sortOption, int sortState)
		{
			this._inventorySortPreferences[inventoryMode] = new Tuple<int, int>(sortOption, sortState);
		}

		// Token: 0x06004625 RID: 17957 RVA: 0x0015D5E0 File Offset: 0x0015B7E0
		public Tuple<int, int> InventoryGetSortPreference(int inventoryMode)
		{
			Tuple<int, int> result;
			if (this._inventorySortPreferences.TryGetValue(inventoryMode, out result))
			{
				return result;
			}
			return new Tuple<int, int>(0, 0);
		}

		// Token: 0x06004626 RID: 17958 RVA: 0x0015D606 File Offset: 0x0015B806
		public void SetPartyTroopLocks(IEnumerable<string> locks)
		{
			this._partyTroopLocks = locks.ToList<string>();
		}

		// Token: 0x06004627 RID: 17959 RVA: 0x0015D614 File Offset: 0x0015B814
		public void SetPartyPrisonerLocks(IEnumerable<string> locks)
		{
			this._partyPrisonerLocks = locks.ToList<string>();
		}

		// Token: 0x06004628 RID: 17960 RVA: 0x0015D622 File Offset: 0x0015B822
		public void SetPartySortType(int sortType)
		{
			this._partySortType = sortType;
		}

		// Token: 0x06004629 RID: 17961 RVA: 0x0015D62B File Offset: 0x0015B82B
		public void SetIsPartySortAscending(bool isAscending)
		{
			this._isPartySortAscending = isAscending;
		}

		// Token: 0x0600462A RID: 17962 RVA: 0x0015D634 File Offset: 0x0015B834
		public IEnumerable<string> GetPartyTroopLocks()
		{
			return this._partyTroopLocks;
		}

		// Token: 0x0600462B RID: 17963 RVA: 0x0015D63C File Offset: 0x0015B83C
		public IEnumerable<string> GetPartyPrisonerLocks()
		{
			return this._partyPrisonerLocks;
		}

		// Token: 0x0600462C RID: 17964 RVA: 0x0015D644 File Offset: 0x0015B844
		public int GetPartySortType()
		{
			return this._partySortType;
		}

		// Token: 0x0600462D RID: 17965 RVA: 0x0015D64C File Offset: 0x0015B84C
		public bool GetIsPartySortAscending()
		{
			return this._isPartySortAscending;
		}

		// Token: 0x0600462E RID: 17966 RVA: 0x0015D654 File Offset: 0x0015B854
		public void AddEncyclopediaBookmarkToItem(Hero item)
		{
			this._encyclopediaBookmarkedHeroes.Add(item);
		}

		// Token: 0x0600462F RID: 17967 RVA: 0x0015D662 File Offset: 0x0015B862
		public void AddEncyclopediaBookmarkToItem(ShipHull shipHull)
		{
			this._encyclopediaBookmarkedShips.Add(shipHull);
		}

		// Token: 0x06004630 RID: 17968 RVA: 0x0015D670 File Offset: 0x0015B870
		public void AddEncyclopediaBookmarkToItem(Clan clan)
		{
			this._encyclopediaBookmarkedClans.Add(clan);
		}

		// Token: 0x06004631 RID: 17969 RVA: 0x0015D67E File Offset: 0x0015B87E
		public void AddEncyclopediaBookmarkToItem(Concept concept)
		{
			this._encyclopediaBookmarkedConcepts.Add(concept);
		}

		// Token: 0x06004632 RID: 17970 RVA: 0x0015D68C File Offset: 0x0015B88C
		public void AddEncyclopediaBookmarkToItem(Kingdom kingdom)
		{
			this._encyclopediaBookmarkedKingdoms.Add(kingdom);
		}

		// Token: 0x06004633 RID: 17971 RVA: 0x0015D69A File Offset: 0x0015B89A
		public void AddEncyclopediaBookmarkToItem(Settlement settlement)
		{
			this._encyclopediaBookmarkedSettlements.Add(settlement);
		}

		// Token: 0x06004634 RID: 17972 RVA: 0x0015D6A8 File Offset: 0x0015B8A8
		public void AddEncyclopediaBookmarkToItem(CharacterObject unit)
		{
			this._encyclopediaBookmarkedUnits.Add(unit);
		}

		// Token: 0x06004635 RID: 17973 RVA: 0x0015D6B6 File Offset: 0x0015B8B6
		public void RemoveEncyclopediaBookmarkFromItem(Hero hero)
		{
			this._encyclopediaBookmarkedHeroes.Remove(hero);
		}

		// Token: 0x06004636 RID: 17974 RVA: 0x0015D6C5 File Offset: 0x0015B8C5
		public void RemoveEncyclopediaBookmarkFromItem(ShipHull shipHull)
		{
			this._encyclopediaBookmarkedShips.Remove(shipHull);
		}

		// Token: 0x06004637 RID: 17975 RVA: 0x0015D6D4 File Offset: 0x0015B8D4
		public void RemoveEncyclopediaBookmarkFromItem(Clan clan)
		{
			this._encyclopediaBookmarkedClans.Remove(clan);
		}

		// Token: 0x06004638 RID: 17976 RVA: 0x0015D6E3 File Offset: 0x0015B8E3
		public void RemoveEncyclopediaBookmarkFromItem(Concept concept)
		{
			this._encyclopediaBookmarkedConcepts.Remove(concept);
		}

		// Token: 0x06004639 RID: 17977 RVA: 0x0015D6F2 File Offset: 0x0015B8F2
		public void RemoveEncyclopediaBookmarkFromItem(Kingdom kingdom)
		{
			this._encyclopediaBookmarkedKingdoms.Remove(kingdom);
		}

		// Token: 0x0600463A RID: 17978 RVA: 0x0015D701 File Offset: 0x0015B901
		public void RemoveEncyclopediaBookmarkFromItem(Settlement settlement)
		{
			this._encyclopediaBookmarkedSettlements.Remove(settlement);
		}

		// Token: 0x0600463B RID: 17979 RVA: 0x0015D710 File Offset: 0x0015B910
		public void RemoveEncyclopediaBookmarkFromItem(CharacterObject unit)
		{
			this._encyclopediaBookmarkedUnits.Remove(unit);
		}

		// Token: 0x0600463C RID: 17980 RVA: 0x0015D71F File Offset: 0x0015B91F
		public bool IsEncyclopediaBookmarked(Hero hero)
		{
			return this._encyclopediaBookmarkedHeroes.Contains(hero);
		}

		// Token: 0x0600463D RID: 17981 RVA: 0x0015D72D File Offset: 0x0015B92D
		public bool IsEncyclopediaBookmarked(ShipHull shipHull)
		{
			return this._encyclopediaBookmarkedShips.Contains(shipHull);
		}

		// Token: 0x0600463E RID: 17982 RVA: 0x0015D73B File Offset: 0x0015B93B
		public bool IsEncyclopediaBookmarked(Clan clan)
		{
			return this._encyclopediaBookmarkedClans.Contains(clan);
		}

		// Token: 0x0600463F RID: 17983 RVA: 0x0015D749 File Offset: 0x0015B949
		public bool IsEncyclopediaBookmarked(Concept concept)
		{
			return this._encyclopediaBookmarkedConcepts.Contains(concept);
		}

		// Token: 0x06004640 RID: 17984 RVA: 0x0015D757 File Offset: 0x0015B957
		public bool IsEncyclopediaBookmarked(Kingdom kingdom)
		{
			return this._encyclopediaBookmarkedKingdoms.Contains(kingdom);
		}

		// Token: 0x06004641 RID: 17985 RVA: 0x0015D765 File Offset: 0x0015B965
		public bool IsEncyclopediaBookmarked(Settlement settlement)
		{
			return this._encyclopediaBookmarkedSettlements.Contains(settlement);
		}

		// Token: 0x06004642 RID: 17986 RVA: 0x0015D773 File Offset: 0x0015B973
		public bool IsEncyclopediaBookmarked(CharacterObject unit)
		{
			return this._encyclopediaBookmarkedUnits.Contains(unit);
		}

		// Token: 0x06004643 RID: 17987 RVA: 0x0015D781 File Offset: 0x0015B981
		public void SetQuestSelection(QuestBase selection)
		{
			this._questSelection = selection;
		}

		// Token: 0x06004644 RID: 17988 RVA: 0x0015D78A File Offset: 0x0015B98A
		public QuestBase GetQuestSelection()
		{
			return this._questSelection;
		}

		// Token: 0x06004645 RID: 17989 RVA: 0x0015D792 File Offset: 0x0015B992
		public MBReadOnlyList<ItemRosterElement> GetPlunderItems()
		{
			return new MBReadOnlyList<ItemRosterElement>(this._plunderItems);
		}

		// Token: 0x17000E36 RID: 3638
		// (get) Token: 0x06004646 RID: 17990 RVA: 0x0015D79F File Offset: 0x0015B99F
		public IReadOnlyList<Figurehead> UnexaminedFigureheads
		{
			get
			{
				return this._unexaminedFigureheads;
			}
		}

		// Token: 0x06004647 RID: 17991 RVA: 0x0015D7A7 File Offset: 0x0015B9A7
		private void OnFigureheadUnlocked(Figurehead newFigurehead)
		{
			this._unexaminedFigureheads.Add(newFigurehead);
		}

		// Token: 0x06004648 RID: 17992 RVA: 0x0015D7B5 File Offset: 0x0015B9B5
		public void OnFigureheadExamined(Figurehead figurehead)
		{
			this._unexaminedFigureheads.Remove(figurehead);
		}

		// Token: 0x06004649 RID: 17993 RVA: 0x0015D7C4 File Offset: 0x0015B9C4
		public override void RegisterEvents()
		{
			CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, new Action<Hero, SkillObject, int, bool>(this.OnHeroGainedSkill));
			CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, new Action<Hero, bool>(this.OnHeroLevelledUp));
			CampaignEvents.ArmyCreated.AddNonSerializedListener(this, new Action<Army>(this.OnNewArmyCreated));
			CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, new Action<Army, Army.ArmyDispersionReason, bool>(this.OnArmyDispersed));
			CampaignEvents.QuestLogAddedEvent.AddNonSerializedListener(this, new Action<QuestBase, bool>(this.OnQuestLogAdded));
			CampaignEvents.IssueLogAddedEvent.AddNonSerializedListener(this, new Action<IssueBase, bool>(this.OnIssueLogAdded));
			CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
			CampaignEvents.ItemsLooted.AddNonSerializedListener(this, new Action<MobileParty, ItemRoster>(this.OnPlayerPlunderedItems));
			CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, new Action<BattleSideEnum, RaidEventComponent>(this.OnRaidCompleted));
			CampaignEvents.OnFigureheadUnlockedEvent.AddNonSerializedListener(this, new Action<Figurehead>(this.OnFigureheadUnlocked));
		}

		// Token: 0x0600464A RID: 17994 RVA: 0x0015D8B7 File Offset: 0x0015BAB7
		private void OnRaidCompleted(BattleSideEnum winnerSide, RaidEventComponent raidEvent)
		{
			if (raidEvent.IsPlayerMapEvent)
			{
				this._plunderItems.Clear();
			}
		}

		// Token: 0x0600464B RID: 17995 RVA: 0x0015D8CC File Offset: 0x0015BACC
		private void OnPlayerPlunderedItems(MobileParty mobileParty, ItemRoster items)
		{
			if (mobileParty == MobileParty.MainParty)
			{
				for (int i = 0; i < items.Count; i++)
				{
					ItemRosterElement item = items[i];
					bool flag = false;
					for (int j = 0; j < this._plunderItems.Count; j++)
					{
						if (this._plunderItems[j].EquipmentElement.IsEqualTo(item.EquipmentElement))
						{
							ItemRosterElement value = this._plunderItems[j];
							value.Amount += item.Amount;
							this._plunderItems[j] = value;
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						this._plunderItems.Add(item);
					}
				}
			}
		}

		// Token: 0x0600464C RID: 17996 RVA: 0x0015D986 File Offset: 0x0015BB86
		public void SetQuestSortTypeSelection(int questSortTypeSelection)
		{
			this._questSortTypeSelection = questSortTypeSelection;
		}

		// Token: 0x0600464D RID: 17997 RVA: 0x0015D98F File Offset: 0x0015BB8F
		public int GetQuestSortTypeSelection()
		{
			return this._questSortTypeSelection;
		}

		// Token: 0x0600464E RID: 17998 RVA: 0x0015D998 File Offset: 0x0015BB98
		public override void SyncData(IDataStore dataStore)
		{
			dataStore.SyncData<bool>("_isMapBarExtended", ref this._isMapBarExtended);
			dataStore.SyncData<List<string>>("_inventoryItemLocks", ref this._inventoryItemLocks);
			dataStore.SyncData<Dictionary<int, Tuple<int, int>>>("_inventorySortPreferences", ref this._inventorySortPreferences);
			dataStore.SyncData<int>("_partySortType", ref this._partySortType);
			dataStore.SyncData<bool>("_isPartySortAscending", ref this._isPartySortAscending);
			dataStore.SyncData<List<string>>("_partyTroopLocks", ref this._partyTroopLocks);
			dataStore.SyncData<List<string>>("_partyPrisonerLocks", ref this._partyPrisonerLocks);
			dataStore.SyncData<List<Hero>>("_encyclopediaBookmarkedHeroes", ref this._encyclopediaBookmarkedHeroes);
			dataStore.SyncData<List<ShipHull>>("_encyclopediaBookmarkedShips", ref this._encyclopediaBookmarkedShips);
			dataStore.SyncData<List<Clan>>("_encyclopediaBookmarkedClans", ref this._encyclopediaBookmarkedClans);
			dataStore.SyncData<List<Concept>>("_encyclopediaBookmarkedConcepts", ref this._encyclopediaBookmarkedConcepts);
			dataStore.SyncData<List<Kingdom>>("_encyclopediaBookmarkedKingdoms", ref this._encyclopediaBookmarkedKingdoms);
			dataStore.SyncData<List<Settlement>>("_encyclopediaBookmarkedSettlements", ref this._encyclopediaBookmarkedSettlements);
			dataStore.SyncData<List<CharacterObject>>("_encyclopediaBookmarkedUnits", ref this._encyclopediaBookmarkedUnits);
			dataStore.SyncData<QuestBase>("_questSelection", ref this._questSelection);
			dataStore.SyncData<List<JournalLog>>("_unExaminedQuestLogs", ref this._unExaminedQuestLogs);
			dataStore.SyncData<List<Army>>("_unExaminedArmies", ref this._unExaminedArmies);
			dataStore.SyncData<bool>("_isCharacterNotificationActive", ref this._isCharacterNotificationActive);
			dataStore.SyncData<int>("_numOfPerks", ref this._numOfPerks);
			dataStore.SyncData<Dictionary<CharacterObject, int>>("_examinedPrisonerCharacterList", ref this._examinedPrisonerCharacterList);
			dataStore.SyncData<List<ItemRosterElement>>("_plunderItems", ref this._plunderItems);
			dataStore.SyncData<List<Figurehead>>("_unexaminedFigureheads", ref this._unexaminedFigureheads);
		}

		// Token: 0x0400138F RID: 5007
		private readonly TextObject _characterNotificationText = new TextObject("{=rlqjkZ9Q}You have {NUMBER} new perks available for selection.", null);

		// Token: 0x04001390 RID: 5008
		private readonly TextObject _questNotificationText = new TextObject("{=FAIYN0vN}You have {NUMBER} new updates to your quests.", null);

		// Token: 0x04001391 RID: 5009
		private readonly TextObject _recruitNotificationText = new TextObject("{=PJMbfSPJ}You have {NUMBER} new prisoners to recruit.", null);

		// Token: 0x04001393 RID: 5011
		private Dictionary<CharacterObject, int> _examinedPrisonerCharacterList = new Dictionary<CharacterObject, int>();

		// Token: 0x04001394 RID: 5012
		private int _numOfRecruitablePrisoners;

		// Token: 0x04001395 RID: 5013
		private List<JournalLog> _unExaminedQuestLogs = new List<JournalLog>();

		// Token: 0x04001396 RID: 5014
		private IReadOnlyList<JournalLog> _unExaminedQuestLogsReadOnly;

		// Token: 0x04001397 RID: 5015
		private readonly Dictionary<JournalLog, JournalLogEntry> _unExaminedQuestLogJournalEntries = new Dictionary<JournalLog, JournalLogEntry>();

		// Token: 0x04001398 RID: 5016
		private bool _isUnExaminedQuestLogJournalEntriesDirty;

		// Token: 0x04001399 RID: 5017
		private List<Army> _unExaminedArmies = new List<Army>();

		// Token: 0x0400139A RID: 5018
		private bool _isCharacterNotificationActive;

		// Token: 0x0400139B RID: 5019
		private int _numOfPerks;

		// Token: 0x0400139C RID: 5020
		private bool _isMapBarExtended;

		// Token: 0x0400139D RID: 5021
		private List<string> _inventoryItemLocks;

		// Token: 0x0400139E RID: 5022
		[SaveableField(21)]
		private Dictionary<int, Tuple<int, int>> _inventorySortPreferences;

		// Token: 0x0400139F RID: 5023
		private int _partySortType;

		// Token: 0x040013A0 RID: 5024
		private bool _isPartySortAscending;

		// Token: 0x040013A1 RID: 5025
		private List<string> _partyTroopLocks;

		// Token: 0x040013A2 RID: 5026
		private List<string> _partyPrisonerLocks;

		// Token: 0x040013A3 RID: 5027
		private List<Hero> _encyclopediaBookmarkedHeroes;

		// Token: 0x040013A4 RID: 5028
		private List<ShipHull> _encyclopediaBookmarkedShips;

		// Token: 0x040013A5 RID: 5029
		private List<Clan> _encyclopediaBookmarkedClans;

		// Token: 0x040013A6 RID: 5030
		private List<Concept> _encyclopediaBookmarkedConcepts;

		// Token: 0x040013A7 RID: 5031
		private List<Kingdom> _encyclopediaBookmarkedKingdoms;

		// Token: 0x040013A8 RID: 5032
		private List<Settlement> _encyclopediaBookmarkedSettlements;

		// Token: 0x040013A9 RID: 5033
		private List<CharacterObject> _encyclopediaBookmarkedUnits;

		// Token: 0x040013AA RID: 5034
		private QuestBase _questSelection;

		// Token: 0x040013AB RID: 5035
		[SaveableField(51)]
		private int _questSortTypeSelection;

		// Token: 0x040013AC RID: 5036
		private List<ItemRosterElement> _plunderItems;

		// Token: 0x040013AD RID: 5037
		private List<Figurehead> _unexaminedFigureheads;
	}
}
