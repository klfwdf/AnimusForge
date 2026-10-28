using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("RansomingPrisonersStep1")]
public class RansomingPrisonersStep1Tutorial : TutorialItemBase
{
	private bool _wantedGameMenuOpened;

	public RansomingPrisonersStep1Tutorial()
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
		_wantedGameMenuOpened = obj.MenuContext.GameMenu.StringId == "town_backstreet";
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.PlayerIsInNonEnemyTown && TutorialHelper.TownMenuIsOpen && !Hero.MainHero.IsPrisoner)
		{
			return MobileParty.MainParty.PrisonRoster.TotalManCount > 0;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}
}
