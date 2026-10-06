using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class ItemCollectionElementViewModel : ViewModel
{
	private string _stringId;

	private int _ammo;

	private int _averageUnitCost;

	private string _itemModifierId;

	private string _bannerCode;

	private float _initialPanRotation;

	[DataSourceProperty]
	public string StringId
	{
		get
		{
			return _stringId;
		}
		set
		{
			if (_stringId != value)
			{
				_stringId = value;
				OnPropertyChangedWithValue(value, "StringId");
			}
		}
	}

	[DataSourceProperty]
	public int Ammo
	{
		get
		{
			return _ammo;
		}
		set
		{
			if (_ammo != value)
			{
				_ammo = value;
				OnPropertyChangedWithValue(value, "Ammo");
			}
		}
	}

	[DataSourceProperty]
	public int AverageUnitCost
	{
		get
		{
			return _averageUnitCost;
		}
		set
		{
			if (_averageUnitCost != value)
			{
				_averageUnitCost = value;
				OnPropertyChangedWithValue(value, "AverageUnitCost");
			}
		}
	}

	[DataSourceProperty]
	public string ItemModifierId
	{
		get
		{
			return _itemModifierId;
		}
		set
		{
			if (_itemModifierId != value)
			{
				_itemModifierId = value;
				OnPropertyChangedWithValue(value, "ItemModifierId");
			}
		}
	}

	[DataSourceProperty]
	public string BannerCode
	{
		get
		{
			return _bannerCode;
		}
		set
		{
			if (value != _bannerCode)
			{
				_bannerCode = value;
				OnPropertyChangedWithValue(value, "BannerCode");
			}
		}
	}

	[DataSourceProperty]
	public float InitialPanRotation
	{
		get
		{
			return _initialPanRotation;
		}
		set
		{
			if (value != _initialPanRotation)
			{
				_initialPanRotation = value;
				OnPropertyChangedWithValue(value, "InitialPanRotation");
			}
		}
	}

	public void FillFrom(EquipmentElement item, Banner banner = null)
	{
		StringId = ((item.Item != null) ? item.Item.StringId : "");
		Ammo = ((!item.IsEmpty && item.Item.PrimaryWeapon != null && item.Item.PrimaryWeapon.IsConsumable) ? item.GetModifiedStackCountForUsage(0) : 0);
		AverageUnitCost = item.ItemValue;
		ItemModifierId = ((item.ItemModifier != null) ? item.ItemModifier.StringId : "");
		BannerCode = ((banner != null) ? banner.BannerCode : "");
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		StringId = "";
		ItemModifierId = "";
	}
}
