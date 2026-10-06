using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

namespace NavalDLC.ViewModelCollection.ClanManagement;

public class NavalClanManagementVM : ClanManagementVM
{
	public NavalClanManagementVM(Action onClose, Action<Hero> showHeroOnMap, Action<Hero> openPartyAsManage, Action openBannerEditor)
		: base(onClose, showHeroOnMap, openPartyAsManage, openBannerEditor)
	{
	}

	protected override ClanFiefsVM CreateFiefsDataSource(Action onRefresh, Action<ClanCardSelectionInfo> openCardSelectionPopup)
	{
		return new NavalClanFiefsVM(onRefresh, openCardSelectionPopup);
	}
}
