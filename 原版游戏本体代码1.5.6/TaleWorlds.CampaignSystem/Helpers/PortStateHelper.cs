using System;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Helpers;

public static class PortStateHelper
{
	public static void OpenAsTrade(Town town)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[3]
		{
			town.Settlement.Party,
			PartyBase.MainParty,
			PortScreenModes.TradeMode
		});
		GameStateManager.Current.PushState(gameState);
	}

	public static void OpenAsLoot(MBReadOnlyList<Ship> lootShips, Action onEndAction = null)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[6]
		{
			null,
			PartyBase.MainParty,
			lootShips,
			PartyBase.MainParty.Ships,
			onEndAction,
			PortScreenModes.LootMode
		});
		GameStateManager.Current.PushState(gameState);
	}

	public static void OpenAsRestricted(Town town, TextObject restrictedReason)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[3]
		{
			town.Settlement.Party,
			PartyBase.MainParty,
			PortScreenModes.Restricted
		});
		GameStateManager.Current.PushState(gameState);
	}

	public static void OpenAsStoryMode(Settlement settlement)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[3]
		{
			settlement,
			PartyBase.MainParty,
			PortScreenModes.Story
		});
		GameStateManager.Current.PushState(gameState);
	}

	public static void OpenAsManageFleet(MBReadOnlyList<Ship> leftShips)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[5]
		{
			null,
			PartyBase.MainParty,
			leftShips,
			PartyBase.MainParty.Ships,
			PortScreenModes.Manage
		});
		GameStateManager.Current.PushState(gameState);
	}

	public static void OpenAsManageOtherFleet(PartyBase other, Action onEndAction)
	{
		PortState gameState = GameStateManager.Current.CreateState<PortState>(new object[4]
		{
			other,
			PartyBase.MainParty,
			onEndAction,
			PortScreenModes.ManageOther
		});
		GameStateManager.Current.PushState(gameState);
	}
}
