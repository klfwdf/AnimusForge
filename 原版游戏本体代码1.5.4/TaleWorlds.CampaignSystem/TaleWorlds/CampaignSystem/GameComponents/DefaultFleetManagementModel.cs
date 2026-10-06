using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultFleetManagementModel : FleetManagementModel
{
	public override int MinimumTroopCountRequiredToSendShips => 0;

	public override bool CanSendShipToPlayerClan(Ship ship, int playerShipsCount, int troopsCountToSend, out TextObject hint)
	{
		hint = TextObject.GetEmpty();
		return false;
	}

	public override bool CanTroopsReturn()
	{
		return false;
	}

	public override CampaignTime GetReturnTimeForTroops(Ship ship)
	{
		return CampaignTime.Never;
	}
}
