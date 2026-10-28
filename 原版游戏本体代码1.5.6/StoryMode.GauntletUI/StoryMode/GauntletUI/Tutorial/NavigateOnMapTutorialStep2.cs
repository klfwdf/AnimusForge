using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("NavigateOnMapTutorialStep2")]
public class NavigateOnMapTutorialStep2 : TutorialItemBase
{
	private const string TargetQuestVillage = "village_ES3_2";

	public NavigateOnMapTutorialStep2()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "village_ES3_2";
		base.MouseRequired = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		return TutorialHelper.CurrentContext == TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return MobileParty.MainParty?.TargetSettlement?.StringId == "village_ES3_2";
	}
}
