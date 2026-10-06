using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Settlements;

namespace NavalDLC.ViewModelCollection.Kingdom;

public class NavalKingdomManagementVM : KingdomManagementVM
{
	public NavalKingdomManagementVM(Action onClose, Action onManageArmy, Action<Army> onShowArmyOnMap)
		: base(onClose, onManageArmy, onShowArmyOnMap)
	{
	}

	protected override KingdomSettlementVM CreateSettlementVM(Action<KingdomDecision> forceDecision, Action<Settlement> onGrantFief)
	{
		return new NavalKingdomSettlementVM(forceDecision, onGrantFief);
	}
}
