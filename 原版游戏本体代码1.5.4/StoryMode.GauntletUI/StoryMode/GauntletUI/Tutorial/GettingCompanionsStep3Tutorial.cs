using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("GettingCompanionsStep3")]
public class GettingCompanionsStep3Tutorial : TutorialItemBase
{
	private bool _startedTalkingWithCompanion;

	public GettingCompanionsStep3Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "OverlayTalkButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _startedTalkingWithCompanion;
	}

	public override void OnPlayerStartTalkFromMenuOverlay(Hero hero)
	{
		_startedTalkingWithCompanion = hero.IsWanderer && !hero.IsPlayerCompanion;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		Location location = LocationComplex.Current?.GetLocationWithId("tavern");
		if (TutorialHelper.PlayerIsInNonEnemyTown && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.BackStreetMenuIsOpen && TutorialHelper.IsCharacterPopUpWindowOpen && Clan.PlayerClan.Companions.Count == 0 && Clan.PlayerClan.CompanionLimit > 0 && TutorialHelper.IsThereAvailableCompanionInLocation(location) == true)
		{
			return Hero.MainHero.Gold > TutorialHelper.MinimumGoldForCompanion;
		}
		return false;
	}
}
