using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class PartyTradeModel : MBGameModel<PartyTradeModel>
{
	public abstract int CaravanTransactionHighestValueItemCount { get; }

	public abstract float GetTradePenaltyFactor(MobileParty party);
}
