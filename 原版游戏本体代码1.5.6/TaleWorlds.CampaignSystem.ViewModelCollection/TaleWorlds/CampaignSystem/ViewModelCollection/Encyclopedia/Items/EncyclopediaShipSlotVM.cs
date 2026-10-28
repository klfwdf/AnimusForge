using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Items;

public class EncyclopediaShipSlotVM : ViewModel
{
	private string _slotTypeId;

	private string _name;

	private bool _isAvailable;

	[DataSourceProperty]
	public string SlotTypeId
	{
		get
		{
			return _slotTypeId;
		}
		set
		{
			if (value != _slotTypeId)
			{
				_slotTypeId = value;
				OnPropertyChangedWithValue(value, "SlotTypeId");
			}
		}
	}

	[DataSourceProperty]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				OnPropertyChangedWithValue(value, "Name");
			}
		}
	}

	[DataSourceProperty]
	public bool IsAvailable
	{
		get
		{
			return _isAvailable;
		}
		set
		{
			if (value != _isAvailable)
			{
				_isAvailable = value;
				OnPropertyChangedWithValue(value, "IsAvailable");
			}
		}
	}

	public EncyclopediaShipSlotVM(string slotId, bool isAvailable)
	{
		SlotTypeId = slotId;
		IsAvailable = isAvailable;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Name = GameTexts.FindText("str_ship_slot_type", SlotTypeId).ToString();
	}
}
