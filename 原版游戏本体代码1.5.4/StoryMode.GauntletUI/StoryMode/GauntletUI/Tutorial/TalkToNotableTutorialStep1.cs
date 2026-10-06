using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("TalkToNotableTutorialStep1")]
public class TalkToNotableTutorialStep1 : TutorialItemBase
{
	private bool _wantedCharacterPopupOpened;

	public TalkToNotableTutorialStep1()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "ApplicableNotable";
		base.MouseRequired = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.CurrentContext == TutorialContexts.MapWindow && TutorialHelper.VillageMenuIsOpen)
		{
			return Settlement.CurrentSettlement.StringId == "village_ES3_2";
		}
		return false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _wantedCharacterPopupOpened;
	}

	public override void OnCharacterPortraitPopUpOpened(CharacterObject obj)
	{
		_wantedCharacterPopupOpened = obj != null && obj.HeroObject?.IsHeadman == true;
	}
}
