using System;
using System.Collections.Generic;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.View.GameMenus;

public class NavalMenuViewContext : MenuViewContext
{
	public NavalMenuViewContext(ScreenBase screen, MenuContext menuContext)
		: base(screen, menuContext)
	{
	}

	protected override MenuView CreateNavalTroopSelectionView(TroopRoster fullRoster, TroopRoster initialTroopSelections, List<Ship> eligibleShips, List<Ship> initialShipSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster, List<Ship>> onDone, int minSelectableTroopCount, int minSelectableShipCount, int maxSelectableShipCount, bool anyOtherPartiesOnPlayerSide)
	{
		return AddMenuView<NavalMenuTroopSelectionView>(new object[10] { fullRoster, initialTroopSelections, eligibleShips, initialShipSelections, canChangeStatusOfTroop, onDone, minSelectableTroopCount, minSelectableShipCount, maxSelectableShipCount, anyOtherPartiesOnPlayerSide });
	}
}
