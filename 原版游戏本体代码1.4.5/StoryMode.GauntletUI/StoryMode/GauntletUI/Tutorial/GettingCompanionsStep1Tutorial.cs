using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("GettingCompanionsStep1")]
public class GettingCompanionsStep1Tutorial : TutorialItemBase
{
	private bool _wantedGameMenuOpened;

	public GettingCompanionsStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "town_backstreet";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _wantedGameMenuOpened;
	}

	public override void OnGameMenuOpened(MenuCallbackArgs obj)
	{
		base.OnGameMenuOpened(obj);
		_wantedGameMenuOpened = obj.MenuContext.GameMenu.StringId == "town_backstreet";
	}

	public override bool IsConditionsMetForActivation()
	{
		Location location = LocationComplex.Current?.GetLocationWithId("tavern");
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.PlayerIsInNonEnemyTown && TutorialHelper.TownMenuIsOpen && Clan.PlayerClan.Companions.Count == 0 && Clan.PlayerClan.CompanionLimit > 0 && TutorialHelper.IsThereAvailableCompanionInLocation(location) == true && Hero.MainHero.Gold > TutorialHelper.MinimumGoldForCompanion)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.MapWindow;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}
}
