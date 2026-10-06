using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using Storymode.Missions;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("StealthHideCorpseTutorial")]
public class StealthHideCorpseTutorial : TutorialItemBase
{
	public StealthHideCorpseTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		return SneakIntoTheVillaMissionController.IsStealthTutorialReadyForActivation(SneakIntoTheVillaMissionController.MissionState.HideCorpse);
	}

	public override bool IsConditionsMetForCompletion()
	{
		if (!SneakIntoTheVillaMissionController.IsStealthTutorialReadyForCompletion(SneakIntoTheVillaMissionController.MissionState.HideCorpse))
		{
			if (SneakIntoTheVillaMissionController.Instance != null)
			{
				return SneakIntoTheVillaMissionController.Instance.IsMainAgentDraggingTargetBody();
			}
			return false;
		}
		return true;
	}

	public override bool IsConditionsMetForVisibility()
	{
		if (base.IsConditionsMetForVisibility())
		{
			return SneakIntoTheVillaMissionController.Instance != null;
		}
		return false;
	}
}
