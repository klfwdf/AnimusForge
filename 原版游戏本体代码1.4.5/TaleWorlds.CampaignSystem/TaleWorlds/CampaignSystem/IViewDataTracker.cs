using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem;

public interface IViewDataTracker
{
	bool IsPartyNotificationActive { get; }

	bool IsQuestNotificationActive { get; }

	IReadOnlyList<JournalLog> UnExaminedQuestLogs { get; }

	List<Army> UnExaminedArmies { get; }

	int NumOfKingdomArmyNotifications { get; }

	bool IsCharacterNotificationActive { get; }

	IReadOnlyList<Figurehead> UnexaminedFigureheads { get; }

	void SetInventoryLocks(IEnumerable<string> locks);

	IEnumerable<string> GetInventoryLocks();

	bool GetMapBarExtendedState();

	void SetMapBarExtendedState(bool value);

	void SetPartyTroopLocks(IEnumerable<string> locks);

	void SetPartyPrisonerLocks(IEnumerable<string> locks);

	void SetPartySortType(int sortType);

	void SetIsPartySortAscending(bool isAscending);

	IEnumerable<string> GetPartyTroopLocks();

	IEnumerable<string> GetPartyPrisonerLocks();

	int GetPartySortType();

	bool GetIsPartySortAscending();

	void AddEncyclopediaBookmarkToItem(Concept concept);

	void AddEncyclopediaBookmarkToItem(Kingdom kingdom);

	void AddEncyclopediaBookmarkToItem(Settlement settlement);

	void AddEncyclopediaBookmarkToItem(CharacterObject unit);

	void AddEncyclopediaBookmarkToItem(Hero item);

	void AddEncyclopediaBookmarkToItem(ShipHull shipHull);

	void AddEncyclopediaBookmarkToItem(Clan clan);

	void RemoveEncyclopediaBookmarkFromItem(Hero hero);

	void RemoveEncyclopediaBookmarkFromItem(ShipHull shipHull);

	void RemoveEncyclopediaBookmarkFromItem(Clan clan);

	void RemoveEncyclopediaBookmarkFromItem(Concept concept);

	void RemoveEncyclopediaBookmarkFromItem(Kingdom kingdom);

	void RemoveEncyclopediaBookmarkFromItem(Settlement settlement);

	void RemoveEncyclopediaBookmarkFromItem(CharacterObject unit);

	bool IsEncyclopediaBookmarked(Hero hero);

	bool IsEncyclopediaBookmarked(ShipHull shipHull);

	bool IsEncyclopediaBookmarked(Clan clan);

	bool IsEncyclopediaBookmarked(Concept concept);

	bool IsEncyclopediaBookmarked(Kingdom kingdom);

	bool IsEncyclopediaBookmarked(Settlement settlement);

	bool IsEncyclopediaBookmarked(CharacterObject unit);

	void SetQuestSelection(QuestBase selection);

	QuestBase GetQuestSelection();

	void SetQuestSortTypeSelection(int questSortTypeSelection);

	int GetQuestSortTypeSelection();

	void InventorySetSortPreference(int inventoryMode, int sortOption, int sortState);

	Tuple<int, int> InventoryGetSortPreference(int inventoryMode);

	TextObject GetPartyNotificationText();

	void ClearPartyNotification();

	void UpdatePartyNotification();

	TextObject GetQuestNotificationText();

	void OnQuestLogExamined(JournalLog log);

	void OnArmyExamined(Army army);

	void ClearCharacterNotification();

	TextObject GetCharacterNotificationText();

	MBReadOnlyList<ItemRosterElement> GetPlunderItems();

	void OnFigureheadExamined(Figurehead figurehead);
}
