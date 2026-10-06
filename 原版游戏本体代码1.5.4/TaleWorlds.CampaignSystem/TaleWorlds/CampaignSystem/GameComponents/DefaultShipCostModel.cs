using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultShipCostModel : ShipCostModel
{
	public override float GetShipTradeValue(Ship ship, PartyBase seller, PartyBase buyer)
	{
		return 0f;
	}

	public override float GetShipRepairCost(Ship ship, PartyBase owner)
	{
		return 0f;
	}

	public override int GetShipUpgradePieceCost(Ship ship, ShipUpgradePiece piece, PartyBase owner)
	{
		return 0;
	}

	public override float GetShipSellingPenalty()
	{
		return 0f;
	}
}
