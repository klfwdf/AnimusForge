using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("RecruitmentTutorialStep1")]
public class RecruitmentStep1Tutorial : TutorialItemBase
{
	private bool _recruitmentOpened;

	public RecruitmentStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "storymode_tutorial_village_recruit";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _recruitmentOpened;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_recruitmentOpened = obj.NewContext == TutorialContexts.RecruitmentWindow;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.PlayerCanRecruit)
		{
			return !Settlement.CurrentSettlement.MapFaction.IsAtWarWith(MobileParty.MainParty.MapFaction);
		}
		return false;
	}
}
