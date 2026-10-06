using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Tutorial;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("CraftingOrdersTutorial")]
public class CraftingOrdersTutorial : TutorialItemBase
{
	private bool _craftingCategorySelectionOpened;

	private bool _craftingOrderSelectionOpened;

	private bool _craftingOrderResultOpened;

	private bool _craftingOrderTabOpened;

	public CraftingOrdersTutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Top;
		base.HighlightedVisualElementID = "CraftingOrdersButton";
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

	public override void OnCraftingOrderTabOpened(CraftingOrderTabOpenedEvent obj)
	{
		_craftingOrderTabOpened = obj.IsOpen;
		if (_craftingOrderTabOpened)
		{
			base.HighlightedVisualElementID = "OrderSelectionButton";
		}
		else
		{
			base.HighlightedVisualElementID = "CraftingOrdersButton";
		}
		Game.Current?.EventManager.TriggerEvent(new TutorialNotificationElementChangeEvent(base.HighlightedVisualElementID));
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
		if (!_craftingCategorySelectionOpened && !_craftingOrderResultOpened)
		{
			return TutorialHelper.IsCurrentTownHaveDoableCraftingOrder;
		}
		return false;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _craftingOrderSelectionOpened;
	}
}
