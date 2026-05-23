using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("ChoosingSkillFocusStep2")]
public class ChoosingSkillFocusStep2Tutorial : TutorialItemBase
{
	private bool _focusAdded;

	public ChoosingSkillFocusStep2Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.TopRight;
		base.HighlightedVisualElementID = "AddFocusButton";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _focusAdded;
	}

	public override void OnFocusAddedByPlayer(FocusAddedByPlayerEvent obj)
	{
		_focusAdded = true;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.CharacterScreen;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (Hero.MainHero.HeroDeveloper.UnspentFocusPoints > 1)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.CharacterScreen;
		}
		return false;
	}
}
