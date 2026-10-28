using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SceneInformationPopupTypes;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

public abstract class BloodFeudExecutionTutorialBase : TutorialItemBase
{
	private bool _isShown;

	protected BloodFeudExecutionTutorialBase()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = string.Empty;
		base.MouseRequired = true;
	}

	protected abstract bool IsRelevantToBloodFeudState(Hero victimHero);

	private bool IsCurrentSceneNotificationRelevant()
	{
		if (MBInformationManager.GetActiveSceneNotificationData() is HeroExecutionSceneNotificationData { IsPlayerExecutionPrompt: not false, Victim: not null } heroExecutionSceneNotificationData)
		{
			return IsRelevantToBloodFeudState(heroExecutionSceneNotificationData.Victim);
		}
		return false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.SceneNotification;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (IsCurrentSceneNotificationRelevant())
		{
			_isShown = true;
			return true;
		}
		return false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (_isShown)
		{
			return !IsCurrentSceneNotificationRelevant();
		}
		return false;
	}
}
