using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CrimeTutorial")]
public class CrimeTutorial : TutorialItemBase
{
	private bool _inspectedCrimeValueItem;

	public CrimeTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = "CrimeLabel";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override void OnCrimeValueInspectedInSettlementOverlay(CrimeValueInspectedInSettlementOverlayEvent obj)
	{
		_inspectedCrimeValueItem = true;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.TownMenuIsOpen)
		{
			IFaction mapFaction = Settlement.CurrentSettlement.MapFaction;
			if (mapFaction == null)
			{
				return false;
			}
			return mapFaction.MainHeroCrimeRating > 0f;
		}
		return false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _inspectedCrimeValueItem;
	}
}
