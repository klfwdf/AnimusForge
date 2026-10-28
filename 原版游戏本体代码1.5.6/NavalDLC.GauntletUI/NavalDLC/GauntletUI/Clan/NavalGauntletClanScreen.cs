using NavalDLC.ViewModelCollection.ClanManagement;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement;
using TaleWorlds.MountAndBlade.View.Screens;

namespace NavalDLC.GauntletUI.Clan;

[GameStateScreen(typeof(ClanState))]
public class NavalGauntletClanScreen : GauntletClanScreen
{
	public NavalGauntletClanScreen(ClanState clanState)
		: base(clanState)
	{
	}

	protected override ClanManagementVM CreateDataSource()
	{
		return new NavalClanManagementVM(base.CloseClanScreen, base.ShowHeroOnMap, base.OpenPartyScreenForNewClanParty, base.OpenBannerEditorWithPlayerClan);
	}
}
