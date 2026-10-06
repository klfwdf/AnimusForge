using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Inventory;

public interface IPlayerTradeBehavior
{
	int GetProjectedProfit(ItemRosterElement itemRosterElement, int itemCost);
}
