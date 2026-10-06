using Helpers;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper.PerkSelection;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("ChoosingPerkUpgradesStep2")]
public class ChoosingPerkUpgradesStep2Tutorial : TutorialItemBase
{
	private bool _perkPopupOpened;

	public ChoosingPerkUpgradesStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "AvailablePerks";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _perkPopupOpened;
	}

	public override void OnPerkSelectionToggle(PerkSelectionToggleEvent obj)
	{
		_perkPopupOpened = true;
	}

	public override bool IsConditionsMetForActivation()
	{
		if ((TutorialHelper.PlayerIsInAnySettlement || TutorialHelper.PlayerIsSafeOnMap) && PerkHelper.AvailablePerkCountOfHero(Hero.MainHero) > 1)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.CharacterScreen;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.CharacterScreen;
	}
}
