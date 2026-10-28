using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class VillageTradeModel : MBGameModel<VillageTradeModel>
{
	public abstract float TradeBoundDistanceLimitAsDays(MobileParty.NavigationType navigationType);

	public abstract Settlement GetTradeBoundToAssignForVillage(Village village);
}
