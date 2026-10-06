using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class CharacterEquipmentItemVM : ViewModel
{
	private readonly ItemObject _item;

	private string _type;

	private bool _hasItem;

	[DataSourceProperty]
	public string Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public bool HasItem
	{
		get
		{
			return _hasItem;
		}
		set
		{
			if (value != _hasItem)
			{
				_hasItem = value;
				OnPropertyChangedWithValue(value, "HasItem");
			}
		}
	}

	public CharacterEquipmentItemVM(ItemObject item)
	{
		_item = item;
		if (_item == null)
		{
			HasItem = false;
			Type = ItemObject.ItemTypeEnum.Invalid.ToString();
		}
		else
		{
			HasItem = true;
			Type = _item.Type.ToString();
		}
	}

	public virtual void ExecuteBeginHint()
	{
		InformationManager.ShowTooltip(typeof(ItemObject), new EquipmentElement(_item));
	}

	public virtual void ExecuteEndHint()
	{
		MBInformationManager.HideInformations();
	}
}
