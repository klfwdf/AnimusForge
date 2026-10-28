using Helpers;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("UpgradingTroopsStep3")]
public class UpgradingTroopsStep3Tutorial : TutorialItemBase
{
	private bool _playerUpgradedTroop;

	public UpgradingTroopsStep3Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "UpgradeButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerUpgradedTroop;
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
