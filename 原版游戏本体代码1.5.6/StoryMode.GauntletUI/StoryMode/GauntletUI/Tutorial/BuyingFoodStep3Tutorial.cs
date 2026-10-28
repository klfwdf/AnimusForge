using System.Collections.Generic;
using SandBox.GauntletUI.Tutorial;
using SandBox.ViewModelCollection.Tutorial;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace StoryMode.GauntletUI.Tutorial;

[Tutorial("GetSuppliesTutorialStep3")]
public class BuyingFoodStep3Tutorial : TutorialItemBase
{
	private int _purchasedFoodCount;

	public BuyingFoodStep3Tutorial()
	{
		base.Placement = TutorialItemVM.ItemPlacements.Right;
		base.HighlightedVisualElementID = "TransferButtonOnlyFood";
		base.MouseRequired = true;
	}

	public override bool IsConditionsMetForCompletion()
	{
		return _purchasedFoodCount >= 2;
	}

	public override bool IsConditionsMetForActivation()
	{
		if (TutorialHelper.BuyingFoodBaseConditions)
		{
			return TutorialHelper.CurrentContext == TutorialContexts.InventoryScreen;
		}
		return false;
	}

	public override void OnPlayerInventoryExchange(List<(ItemRosterElement, int)> purchasedItems, List<(ItemRosterElement, int)> soldItems, bool isTrading)
	{
		for (int i = 0; i < purchasedItems.Count; i++)
		{
			(ItemRosterElement, int) tuple = purchasedItems[i];
			if (tuple.Item1.EquipmentElement.Item == DefaultItems.Grain)
			{
				_purchasedFoodCount += tuple.Item1.Amount;
			}
		}
	}

	public override TutorialContexts GetTutorialsRelevantContext()
	{
		return TutorialContexts.InventoryScreen;
	}
}
