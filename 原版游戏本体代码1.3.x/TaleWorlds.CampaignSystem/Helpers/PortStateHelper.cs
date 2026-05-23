using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers
{
	// Token: 0x02000026 RID: 38
	public static class PortStateHelper
	{
		// Token: 0x06000171 RID: 369 RVA: 0x00010A24 File Offset: 0x0000EC24
		public static void OpenAsTrade(Town town)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				town.Settlement.Party,
				PartyBase.MainParty,
				PortScreenModes.TradeMode
			});
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00010A70 File Offset: 0x0000EC70
		public static void OpenAsLoot(MBReadOnlyList<Ship> lootShips)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				null,
				PartyBase.MainParty,
				lootShips,
				PartyBase.MainParty.Ships,
				PortScreenModes.LootMode
			});
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00010ABC File Offset: 0x0000ECBC
		public static void OpenAsRestricted(Town town, TextObject restrictedReason)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				town.Settlement.Party,
				PartyBase.MainParty,
				PortScreenModes.Restricted
			});
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00010B08 File Offset: 0x0000ED08
		public static void OpenAsStoryMode(Settlement settlement)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				settlement,
				PartyBase.MainParty,
				PortScreenModes.Story
			});
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00010B48 File Offset: 0x0000ED48
		public static void OpenAsManageFleet(MBReadOnlyList<Ship> leftShips)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				null,
				PartyBase.MainParty,
				leftShips,
				PartyBase.MainParty.Ships,
				PortScreenModes.Manage
			});
			GameStateManager.Current.PushState(gameState, 0);
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00010B94 File Offset: 0x0000ED94
		public static void OpenAsManageOtherFleet(PartyBase other, Action onEndAction)
		{
			PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[]
			{
				other,
				PartyBase.MainParty,
				onEndAction,
				PortScreenModes.ManageOther
			});
			GameStateManager.Current.PushState(gameState, 0);
		}
	}
}
