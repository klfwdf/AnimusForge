using Helpers;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("ChoosingPerkUpgradesStep1")]
public class ChoosingPerkUpgradesStep1Tutorial : TutorialItemBase
{
	private bool _contextChangedToCharacterScreen;

	public ChoosingPerkUpgradesStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "character_developer";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _contextChangedToCharacterScreen;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if ((TutorialHelper.PlayerIsInAnySettlement || TutorialHelper.PlayerIsSafeOnMap) && PerkHelper.AvailablePerkCountOfHero(Hero.MainHero) > 1)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.MapWindow;
		}
		return false;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_contextChangedToCharacterScreen = obj.NewContext == TutorialContexts.CharacterScreen;
	}
}
