using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("GetSuppliesTutorialStep1")]
public class BuyingFoodStep1Tutorial : TutorialItemBase
{
	private bool _contextChangedToInventory;

	public BuyingFoodStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "storymode_tutorial_village_buy";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _contextChangedToInventory;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!TutorialHelper.IsCharacterPopUpWindowOpen && TutorialHelper.BuyingFoodBaseConditions)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.MapWindow;
		}
		return false;
	}

	public override void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		_contextChangedToInventory = obj.NewContext == TutorialContexts.InventoryScreen;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.MapWindow;
	}
}
