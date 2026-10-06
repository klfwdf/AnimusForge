using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using Storymode.Missions;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("StealthDarkZoneTutorial")]
public class StealthDarkZoneTutorial : TutorialItemBase
{
	public StealthDarkZoneTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.Mission;
	}

	public override bool IsConditionsMetForActivation()
	{
		return SneakIntoTheVillaMissionController.IsStealthTutorialReadyForActivation(SneakIntoTheVillaMissionController.MissionState.DarkZone);
	}

	public override bool IsConditionsMetForCompletion()
	{
		return SneakIntoTheVillaMissionController.IsStealthTutorialReadyForCompletion(SneakIntoTheVillaMissionController.MissionState.DarkZone);
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
