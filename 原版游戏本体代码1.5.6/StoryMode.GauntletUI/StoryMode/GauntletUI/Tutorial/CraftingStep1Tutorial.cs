using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CraftingStep1Tutorial")]
public class CraftingStep1Tutorial : TutorialItemBase
{
	private bool _craftingCategorySelectionOpened;

	private bool _craftingOrderSelectionOpened;

	private bool _craftingOrderResultOpened;

	public CraftingStep1Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = "FreeModeClassSelectionButton";
		base.MouseRequired = false;
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.CraftingScreen;
	}

	public override void OnCraftingWeaponClassSelectionOpened(CraftingWeaponClassSelectionOpenedEvent obj)
	{
		_craftingCategorySelectionOpened = obj.IsOpen;
	}

	public override void OnCraftingOrderSelectionOpened(CraftingOrderSelectionOpenedEvent obj)
	{
		_craftingOrderSelectionOpened = obj.IsOpen;
	}

	public override void OnCraftingOnWeaponResultPopupOpened(CraftingWeaponResultPopupToggledEvent obj)
	{
		_craftingOrderResultOpened = obj.IsOpen;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (!_craftingOrderSelectionOpened)
		{
			return !_craftingOrderResultOpened;
		}
		return false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _craftingCategorySelectionOpened;
	}
}
