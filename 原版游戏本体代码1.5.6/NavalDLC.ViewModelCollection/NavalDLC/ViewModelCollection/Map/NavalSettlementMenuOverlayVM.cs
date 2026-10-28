using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core.ViewModelCollection.Information;

namespace NavalDLC.ViewModelCollection.Map;

[MenuOverlay("SettlementMenuOverlay")]
public class NavalSettlementMenuOverlayVM : SettlementMenuOverlayVM
{
	public NavalSettlementMenuOverlayVM(GameMenu.MenuOverlayType type)
		: base(type)
	{
		base.ShipyardHint = new BasicTooltipViewModel(() => NavalUIHelper.GetShipyardTooltip(_settlement.Town));
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Building building = _settlement.Town?.GetShipyard();
		base.IsShipyardEnabled = building != null;
		base.ShipyardLbl = (base.IsShipyardEnabled ? building.CurrentLevel.ToString() : string.Empty);
	}
}
