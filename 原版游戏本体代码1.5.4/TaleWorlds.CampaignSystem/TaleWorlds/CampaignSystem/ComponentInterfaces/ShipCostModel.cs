using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class ShipCostModel : MBGameModel<ShipCostModel>
{
	public abstract float GetShipTradeValue(Ship ship, PartyBase seller, PartyBase buyer);

	public abstract float GetShipRepairCost(Ship ship, PartyBase owner);

	public abstract int GetShipUpgradePieceCost(Ship ship, ShipUpgradePiece piece, PartyBase owner);

	public abstract float GetShipSellingPenalty();
}
