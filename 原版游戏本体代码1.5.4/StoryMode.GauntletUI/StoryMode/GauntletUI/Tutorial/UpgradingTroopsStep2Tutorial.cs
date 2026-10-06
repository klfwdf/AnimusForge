using Helpers;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("UpgradingTroopsStep2")]
public class UpgradingTroopsStep2Tutorial : TutorialItemBase
{
	private bool _playerUpgradedTroop;

	private bool _playerOpenedUpgradePopup;

	public UpgradingTroopsStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Left;
		base.HighlightedVisualElementID = "UpgradePopupButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (!_playerUpgradedTroop)
		{
			return _playerOpenedUpgradePopup;
		}
		return true;
	}

	public override void OnPlayerToggledUpgradePopup(PlayerToggledUpgradePopupEvent obj)
	{
		if (obj.IsOpened)
		{
			_playerOpenedUpgradePopup = true;
		}
	}

	public override void OnPlayerUpgradeTroop(CharacterObject arg1, CharacterObject arg2, int arg3)
	{
		_playerUpgradedTroop = true;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Hero.MainHero.Gold <= 100 || TutorialHelper.CurrentContext != TutorialContexts.PartyScreen)
		{
			return false;
		}
		PartyState activePartyState = PartyScreenHelper.GetActivePartyState();
		if (activePartyState != null && activePartyState.PartyScreenMode != PartyScreenHelper.PartyScreenMode.Normal)
		{
			return false;
		}
		if (!TutorialHelper.AreTroopUpgradesDisabled)
		{
			return TutorialHelper.PlayerHasAnyUpgradeableTroop;
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.PartyScreen;
	}
}
