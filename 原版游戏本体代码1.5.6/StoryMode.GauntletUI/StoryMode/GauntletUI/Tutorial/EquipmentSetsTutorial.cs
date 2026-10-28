using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("EquipmentSets")]
public class EquipmentSetsTutorial : TutorialItemBase
{
	private bool _playerFilteredToDifferentEquipment;

	public EquipmentSetsTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "EquipmentSetFilters";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _playerFilteredToDifferentEquipment;
	}

	public override void OnInventoryEquipmentTypeChange(InventoryEquipmentTypeChangedEvent obj)
	{
		_playerFilteredToDifferentEquipment = !obj.IsCurrentlyWarSet;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.InventoryScreen;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialPhase.Instance.IsCompleted)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.InventoryScreen;
		}
		return false;
	}
}
