using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu;

public class GameMenuPlunderItemVM : ViewModel
{
	public readonly EquipmentElement Item;

	private ItemImageIdentifierVM _visual;

	private int _amount;

	[DataSourceProperty]
	public ItemImageIdentifierVM Visual
	{
		get
		{
			return _visual;
		}
		set
		{
			if (value != _visual)
			{
				_visual = value;
				OnPropertyChangedWithValue(value, "Visual");
			}
		}
	}

	[DataSourceProperty]
	public int Amount
	{
		get
		{
			return _amount;
		}
		set
		{
			if (value != _amount)
			{
				_amount = value;
				OnPropertyChangedWithValue(value, "Amount");
			}
		}
	}

	public GameMenuPlunderItemVM(EquipmentElement item, int amount = 1)
	{
		Item = item;
		Amount = amount;
		Visual = new ItemImageIdentifierVM(item.Item);
	}

	public void ExecuteBeginTooltip()
	{
		if (Item.Item != null)
		{
			InformationManager.ShowTooltip(typeof(ItemObject), Item);
		}
	}

	public void ExecuteEndTooltip()
	{
		MBInformationManager.HideInformations();
	}
}
