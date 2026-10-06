using System;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

namespace NavalDLC.ViewModelCollection.ClanManagement;

public class NavalClanFiefsVM : ClanFiefsVM
{
	public NavalClanFiefsVM(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
		: base(onRefresh, openCardSelectionPopup)
	{
	}

	protected override ClanSettlementItemVM CreateSettlementItem(Settlement settlement, Action<ClanSettlementItemVM> onSelection, Action onShowSendMembers, ITeleportationCampaignBehavior teleportationBehavior)
	{
		return new NavalClanSettlementItemVM(settlement, onSelection, onShowSendMembers, teleportationBehavior);
	}
}
