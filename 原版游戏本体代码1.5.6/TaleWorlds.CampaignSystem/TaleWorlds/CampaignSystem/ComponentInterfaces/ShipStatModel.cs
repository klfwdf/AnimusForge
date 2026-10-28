using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ShipStatModel : MBGameModel<ShipStatModel>
{
	public abstract float GetShipFlagshipScore(Ship ship);
}
