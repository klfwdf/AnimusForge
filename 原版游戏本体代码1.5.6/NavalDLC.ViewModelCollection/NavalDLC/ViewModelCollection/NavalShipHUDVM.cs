using TaleWorlds.Library;

namespace NavalDLC.ViewModelCollection;

public class NavalShipHUDVM : ViewModel
{
	private bool _isControllingShip;

	private float _shipHealth;

	private float _maxShipHealth;

	[DataSourceProperty]
	public bool IsControllingShip
	{
		get
		{
			return _isControllingShip;
		}
		set
		{
			if (value != _isControllingShip)
			{
				_isControllingShip = value;
				OnPropertyChangedWithValue(value, "IsControllingShip");
			}
		}
	}

	[DataSourceProperty]
	public float ShipHealth
	{
		get
		{
			return _shipHealth;
		}
		set
		{
			if (value != _shipHealth)
			{
				_shipHealth = value;
				OnPropertyChangedWithValue(value, "ShipHealth");
			}
		}
	}

	[DataSourceProperty]
	public float MaxShipHealth
	{
		get
		{
			return _maxShipHealth;
		}
		set
		{
			if (value != _maxShipHealth)
			{
				_maxShipHealth = value;
				OnPropertyChangedWithValue(value, "MaxShipHealth");
			}
		}
	}
}
