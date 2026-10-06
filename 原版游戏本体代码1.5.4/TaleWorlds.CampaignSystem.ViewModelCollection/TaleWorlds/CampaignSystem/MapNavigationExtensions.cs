using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem;

public static class MapNavigationExtensions
{
	public static NavigationPermissionItem GetPermission(this INavigationHandler handler, MapNavigationItemType elementType)
	{
		return GetElement(handler, elementType).Permission;
	}

	public static bool IsActive(this INavigationHandler handler, MapNavigationItemType elementType)
	{
		return GetElement(handler, elementType).IsActive;
	}

	public static void OpenQuests(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.Quest).OpenView();
	}

	public static void OpenQuests(this INavigationHandler handler, QuestBase quest)
	{
		GetElement(handler, MapNavigationItemType.Quest).OpenView(quest);
	}

	public static void OpenQuests(this INavigationHandler handler, IssueBase issue)
	{
		GetElement(handler, MapNavigationItemType.Quest).OpenView(issue);
	}

	public static void OpenQuests(this INavigationHandler handler, JournalLogEntry log)
	{
		GetElement(handler, MapNavigationItemType.Quest).OpenView(log);
	}

	public static void OpenInventory(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.Inventory).OpenView();
	}

	public static void OpenParty(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.Party).OpenView();
	}

	public static void OpenCharacterDeveloper(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.CharacterDeveloper).OpenView();
	}

	public static void OpenCharacterDeveloper(this INavigationHandler handler, Hero hero)
	{
		GetElement(handler, MapNavigationItemType.CharacterDeveloper).OpenView(hero);
	}

	public static void OpenKingdom(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView();
	}

	public static void OpenKingdom(this INavigationHandler handler, Army army)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(army);
	}

	public static void OpenKingdom(this INavigationHandler handler, Settlement settlement)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(settlement);
	}

	public static void OpenKingdom(this INavigationHandler handler, Clan clan)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(clan);
	}

	public static void OpenKingdom(this INavigationHandler handler, PolicyObject policy)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(policy);
	}

	public static void OpenKingdom(this INavigationHandler handler, IFaction faction)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(faction);
	}

	public static void OpenKingdom(this INavigationHandler handler, KingdomDecision decision)
	{
		GetElement(handler, MapNavigationItemType.Kingdom).OpenView(decision);
	}

	public static void OpenClan(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView();
	}

	public static void OpenClan(this INavigationHandler handler, Hero hero)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView(hero);
	}

	public static void OpenClan(this INavigationHandler handler, PartyBase party)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView(party);
	}

	public static void OpenClan(this INavigationHandler handler, Settlement settlement)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView(settlement);
	}

	public static void OpenClan(this INavigationHandler handler, Workshop workshop)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView(workshop);
	}

	public static void OpenClan(this INavigationHandler handler, Alley alley)
	{
		GetElement(handler, MapNavigationItemType.Clan).OpenView(alley);
	}

	public static void OpenEscapeMenu(this INavigationHandler handler)
	{
		GetElement(handler, MapNavigationItemType.EscapeMenu).OpenView();
	}

	private static INavigationElement GetElement(INavigationHandler handler, MapNavigationItemType elementType)
	{
		string text = null;
		switch (elementType)
		{
		case MapNavigationItemType.Party:
			text = "party";
			break;
		case MapNavigationItemType.Inventory:
			text = "inventory";
			break;
		case MapNavigationItemType.Quest:
			text = "quest";
			break;
		case MapNavigationItemType.CharacterDeveloper:
			text = "character_developer";
			break;
		case MapNavigationItemType.Clan:
			text = "clan";
			break;
		case MapNavigationItemType.Kingdom:
			text = "kingdom";
			break;
		case MapNavigationItemType.EscapeMenu:
			text = "escape_menu";
			break;
		}
		if (string.IsNullOrEmpty(text))
		{
			Debug.FailedAssert($"Unable to find navigation item with type: {elementType}", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Map\\MapBar\\MapNavigationExtensions.cs", "GetElement", 181);
			return null;
		}
		return handler.GetElement(text);
	}
}
