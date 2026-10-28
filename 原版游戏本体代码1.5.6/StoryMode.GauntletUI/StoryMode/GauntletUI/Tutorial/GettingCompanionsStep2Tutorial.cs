using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("GettingCompanionsStep2")]
public class GettingCompanionsStep2Tutorial : TutorialItemBase
{
	private bool _wantedCharacterPopupOpened;

	public GettingCompanionsStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "ApplicapleCompanion";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _wantedCharacterPopupOpened;
	}

	public override void OnCharacterPortraitPopUpOpened(CharacterObject obj)
	{
		_wantedCharacterPopupOpened = obj != null && obj.IsHero && obj.HeroObject.IsWanderer;
	}

	public override bool IsConditionsMetForActivation()
	{
		Location location = LocationComplex.Current?.GetLocationWithId("tavern");
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.PlayerIsInNonEnemyTown && TutorialHelper.BackStreetMenuIsOpen && Clan.PlayerClan.Companions.Count == 0 && Clan.PlayerClan.CompanionLimit > 0 && TutorialHelper.IsThereAvailableCompanionInLocation(location) == true)
		{
			return Hero.MainHero.Gold > TutorialHelper.MinimumGoldForCompanion;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}
}
