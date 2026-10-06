using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class FleetManagementModel : MBGameModel<FleetManagementModel>
{
	public abstract int MinimumTroopCountRequiredToSendShips { get; }

	public abstract bool CanTroopsReturn();

	public abstract CampaignTime GetReturnTimeForTroops(Ship ship);

	public abstract bool CanSendShipToPlayerClan(Ship ship, int playerShipsCount, int troopsCountToSend, out TextObject hint);
}
