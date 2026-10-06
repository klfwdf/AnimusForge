using System;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

namespace NavalDLC.ViewModelCollection.Kingdom;

public class NavalKingdomSettlementVM : KingdomSettlementVM
{
	public NavalKingdomSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
		: base(forceDecision, onGrantFief)
	{
	}

	protected override KingdomSettlementItemVM CreateSettlementItemVM(Settlement settlement, Action<KingdomSettlementItemVM> onSelect)
	{
		return new NavalKingdomSettlementItemVM(settlement, onSelect);
	}
}
