using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200009B RID: 155
	public interface IViewDataTracker
	{
		// Token: 0x060012C4 RID: 4804
		void SetInventoryLocks(IEnumerable<string> locks);

		// Token: 0x060012C5 RID: 4805
		IEnumerable<string> GetInventoryLocks();

		// Token: 0x060012C6 RID: 4806
		bool GetMapBarExtendedState();

		// Token: 0x060012C7 RID: 4807
		void SetMapBarExtendedState(bool value);

		// Token: 0x060012C8 RID: 4808
		void SetPartyTroopLocks(IEnumerable<string> locks);

		// Token: 0x060012C9 RID: 4809
		void SetPartyPrisonerLocks(IEnumerable<string> locks);

		// Token: 0x060012CA RID: 4810
		void SetPartySortType(int sortType);

		// Token: 0x060012CB RID: 4811
		void SetIsPartySortAscending(bool isAscending);

		// Token: 0x060012CC RID: 4812
		IEnumerable<string> GetPartyTroopLocks();

		// Token: 0x060012CD RID: 4813
		IEnumerable<string> GetPartyPrisonerLocks();

		// Token: 0x060012CE RID: 4814
		int GetPartySortType();

		// Token: 0x060012CF RID: 4815
		bool GetIsPartySortAscending();

		// Token: 0x060012D0 RID: 4816
		void AddEncyclopediaBookmarkToItem(Concept concept);

		// Token: 0x060012D1 RID: 4817
		void AddEncyclopediaBookmarkToItem(Kingdom kingdom);

		// Token: 0x060012D2 RID: 4818
		void AddEncyclopediaBookmarkToItem(Settlement settlement);

		// Token: 0x060012D3 RID: 4819
		void AddEncyclopediaBookmarkToItem(CharacterObject unit);

		// Token: 0x060012D4 RID: 4820
		void AddEncyclopediaBookmarkToItem(Hero item);

		// Token: 0x060012D5 RID: 4821
		void AddEncyclopediaBookmarkToItem(ShipHull shipHull);

		// Token: 0x060012D6 RID: 4822
		void AddEncyclopediaBookmarkToItem(Clan clan);

		// Token: 0x060012D7 RID: 4823
		void RemoveEncyclopediaBookmarkFromItem(Hero hero);

		// Token: 0x060012D8 RID: 4824
		void RemoveEncyclopediaBookmarkFromItem(ShipHull shipHull);

		// Token: 0x060012D9 RID: 4825
		void RemoveEncyclopediaBookmarkFromItem(Clan clan);

		// Token: 0x060012DA RID: 4826
		void RemoveEncyclopediaBookmarkFromItem(Concept concept);

		// Token: 0x060012DB RID: 4827
		void RemoveEncyclopediaBookmarkFromItem(Kingdom kingdom);

		// Token: 0x060012DC RID: 4828
		void RemoveEncyclopediaBookmarkFromItem(Settlement settlement);

		// Token: 0x060012DD RID: 4829
		void RemoveEncyclopediaBookmarkFromItem(CharacterObject unit);

		// Token: 0x060012DE RID: 4830
		bool IsEncyclopediaBookmarked(Hero hero);

		// Token: 0x060012DF RID: 4831
		bool IsEncyclopediaBookmarked(ShipHull shipHull);

		// Token: 0x060012E0 RID: 4832
		bool IsEncyclopediaBookmarked(Clan clan);

		// Token: 0x060012E1 RID: 4833
		bool IsEncyclopediaBookmarked(Concept concept);

		// Token: 0x060012E2 RID: 4834
		bool IsEncyclopediaBookmarked(Kingdom kingdom);

		// Token: 0x060012E3 RID: 4835
		bool IsEncyclopediaBookmarked(Settlement settlement);

		// Token: 0x060012E4 RID: 4836
		bool IsEncyclopediaBookmarked(CharacterObject unit);

		// Token: 0x060012E5 RID: 4837
		void SetQuestSelection(QuestBase selection);

		// Token: 0x060012E6 RID: 4838
		QuestBase GetQuestSelection();

		// Token: 0x060012E7 RID: 4839
		void SetQuestSortTypeSelection(int questSortTypeSelection);

		// Token: 0x060012E8 RID: 4840
		int GetQuestSortTypeSelection();

		// Token: 0x060012E9 RID: 4841
		void InventorySetSortPreference(int inventoryMode, int sortOption, int sortState);

		// Token: 0x060012EA RID: 4842
		Tuple<int, int> InventoryGetSortPreference(int inventoryMode);

		// Token: 0x17000517 RID: 1303
		// (get) Token: 0x060012EB RID: 4843
		bool IsPartyNotificationActive { get; }

		// Token: 0x060012EC RID: 4844
		TextObject GetPartyNotificationText();

		// Token: 0x060012ED RID: 4845
		void ClearPartyNotification();

		// Token: 0x060012EE RID: 4846
		void UpdatePartyNotification();

		// Token: 0x17000518 RID: 1304
		// (get) Token: 0x060012EF RID: 4847
		bool IsQuestNotificationActive { get; }

		// Token: 0x17000519 RID: 1305
		// (get) Token: 0x060012F0 RID: 4848
		IReadOnlyList<JournalLog> UnExaminedQuestLogs { get; }

		// Token: 0x060012F1 RID: 4849
		TextObject GetQuestNotificationText();

		// Token: 0x060012F2 RID: 4850
		void OnQuestLogExamined(JournalLog log);

		// Token: 0x1700051A RID: 1306
		// (get) Token: 0x060012F3 RID: 4851
		List<Army> UnExaminedArmies { get; }

		// Token: 0x1700051B RID: 1307
		// (get) Token: 0x060012F4 RID: 4852
		int NumOfKingdomArmyNotifications { get; }

		// Token: 0x060012F5 RID: 4853
		void OnArmyExamined(Army army);

		// Token: 0x1700051C RID: 1308
		// (get) Token: 0x060012F6 RID: 4854
		bool IsCharacterNotificationActive { get; }

		// Token: 0x060012F7 RID: 4855
		void ClearCharacterNotification();

		// Token: 0x060012F8 RID: 4856
		TextObject GetCharacterNotificationText();

		// Token: 0x060012F9 RID: 4857
		MBReadOnlyList<ItemRosterElement> GetPlunderItems();

		// Token: 0x1700051D RID: 1309
		// (get) Token: 0x060012FA RID: 4858
		IReadOnlyList<Figurehead> UnexaminedFigureheads { get; }

		// Token: 0x060012FB RID: 4859
		void OnFigureheadExamined(Figurehead figurehead);
	}
}
