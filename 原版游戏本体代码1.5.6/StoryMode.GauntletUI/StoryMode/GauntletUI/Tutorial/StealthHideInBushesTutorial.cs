using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using Storymode.Missions;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("StealthHideInBushesTutorial")]
public class StealthHideInBushesTutorial : TutorialItemBase
{
	public StealthHideInBushesTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		return SneakIntoTheVillaMissionController.IsStealthTutorialReadyForActivation(SneakIntoTheVillaMissionController.MissionState.HideInBushes);
	}

	public override bool IsConditionsMetForCompletion()
	{
		return SneakIntoTheVillaMissionController.IsStealthTutorialReadyForCompletion(SneakIntoTheVillaMissionController.MissionState.HideInBushes);
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
