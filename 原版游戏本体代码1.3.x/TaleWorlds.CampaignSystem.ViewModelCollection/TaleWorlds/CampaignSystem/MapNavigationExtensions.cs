using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000005 RID: 5
	public static class MapNavigationExtensions
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		public static NavigationPermissionItem GetPermission(this INavigationHandler handler, MapNavigationItemType elementType)
		{
			return MapNavigationExtensions.GetElement(handler, elementType).Permission;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002066 File Offset: 0x00000266
		public static bool IsActive(this INavigationHandler handler, MapNavigationItemType elementType)
		{
			return MapNavigationExtensions.GetElement(handler, elementType).IsActive;
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002074 File Offset: 0x00000274
		public static void OpenQuests(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Quest).OpenView();
		}

		// Token: 0x06000006 RID: 6 RVA: 0x00002082 File Offset: 0x00000282
		public static void OpenQuests(this INavigationHandler handler, QuestBase quest)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Quest).OpenView(new object[] { quest });
		}

		// Token: 0x06000007 RID: 7 RVA: 0x0000209A File Offset: 0x0000029A
		public static void OpenQuests(this INavigationHandler handler, IssueBase issue)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Quest).OpenView(new object[] { issue });
		}

		// Token: 0x06000008 RID: 8 RVA: 0x000020B2 File Offset: 0x000002B2
		public static void OpenQuests(this INavigationHandler handler, JournalLogEntry log)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Quest).OpenView(new object[] { log });
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000020CA File Offset: 0x000002CA
		public static void OpenInventory(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Inventory).OpenView();
		}

		// Token: 0x0600000A RID: 10 RVA: 0x000020D8 File Offset: 0x000002D8
		public static void OpenParty(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Party).OpenView();
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000020E6 File Offset: 0x000002E6
		public static void OpenCharacterDeveloper(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.CharacterDeveloper).OpenView();
		}

		// Token: 0x0600000C RID: 12 RVA: 0x000020F4 File Offset: 0x000002F4
		public static void OpenCharacterDeveloper(this INavigationHandler handler, Hero hero)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.CharacterDeveloper).OpenView(new object[] { hero });
		}

		// Token: 0x0600000D RID: 13 RVA: 0x0000210C File Offset: 0x0000030C
		public static void OpenKingdom(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView();
		}

		// Token: 0x0600000E RID: 14 RVA: 0x0000211A File Offset: 0x0000031A
		public static void OpenKingdom(this INavigationHandler handler, Army army)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { army });
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002132 File Offset: 0x00000332
		public static void OpenKingdom(this INavigationHandler handler, Settlement settlement)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { settlement });
		}

		// Token: 0x06000010 RID: 16 RVA: 0x0000214A File Offset: 0x0000034A
		public static void OpenKingdom(this INavigationHandler handler, Clan clan)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { clan });
		}

		// Token: 0x06000011 RID: 17 RVA: 0x00002162 File Offset: 0x00000362
		public static void OpenKingdom(this INavigationHandler handler, PolicyObject policy)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { policy });
		}

		// Token: 0x06000012 RID: 18 RVA: 0x0000217A File Offset: 0x0000037A
		public static void OpenKingdom(this INavigationHandler handler, IFaction faction)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { faction });
		}

		// Token: 0x06000013 RID: 19 RVA: 0x00002192 File Offset: 0x00000392
		public static void OpenKingdom(this INavigationHandler handler, KingdomDecision decision)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Kingdom).OpenView(new object[] { decision });
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000021AA File Offset: 0x000003AA
		public static void OpenClan(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x000021B8 File Offset: 0x000003B8
		public static void OpenClan(this INavigationHandler handler, Hero hero)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView(new object[] { hero });
		}

		// Token: 0x06000016 RID: 22 RVA: 0x000021D0 File Offset: 0x000003D0
		public static void OpenClan(this INavigationHandler handler, PartyBase party)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView(new object[] { party });
		}

		// Token: 0x06000017 RID: 23 RVA: 0x000021E8 File Offset: 0x000003E8
		public static void OpenClan(this INavigationHandler handler, Settlement settlement)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView(new object[] { settlement });
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002200 File Offset: 0x00000400
		public static void OpenClan(this INavigationHandler handler, Workshop workshop)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView(new object[] { workshop });
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002218 File Offset: 0x00000418
		public static void OpenClan(this INavigationHandler handler, Alley alley)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.Clan).OpenView(new object[] { alley });
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002230 File Offset: 0x00000430
		public static void OpenEscapeMenu(this INavigationHandler handler)
		{
			MapNavigationExtensions.GetElement(handler, MapNavigationItemType.EscapeMenu).OpenView();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002240 File Offset: 0x00000440
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
				Debug.FailedAssert(string.Format("Unable to find navigation item with type: {0}", elementType), "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.CampaignSystem.ViewModelCollection\\Map\\MapBar\\MapNavigationExtensions.cs", "GetElement", 181);
				return null;
			}
			return handler.GetElement(text);
		}
	}
}
