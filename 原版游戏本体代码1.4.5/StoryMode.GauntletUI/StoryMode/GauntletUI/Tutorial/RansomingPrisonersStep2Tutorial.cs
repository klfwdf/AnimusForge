using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("RansomingPrisonersStep2")]
public class RansomingPrisonersStep2Tutorial : TutorialItemBase
{
	private bool _sellPrisonersOptionsSelected;

	public RansomingPrisonersStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "sell_all_prisoners";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _sellPrisonersOptionsSelected;
	}

	public override void OnGameMenuOptionSelected(GameMenuOption obj)
	{
		_sellPrisonersOptionsSelected = obj.IdString == "sell_all_prisoners";
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.PlayerIsInNonEnemyTown && TutorialHelper.BackStreetMenuIsOpen && !Hero.MainHero.IsPrisoner)
		{
			return MobileParty.MainParty.PrisonRoster.TotalManCount > 0;
		}
		return false;
	}
}
