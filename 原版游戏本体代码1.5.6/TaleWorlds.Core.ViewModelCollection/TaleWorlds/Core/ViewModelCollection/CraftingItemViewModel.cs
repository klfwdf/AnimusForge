using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection;

public class CraftingItemViewModel : ViewModel
{
	private string _usedPieces;

	private int _weaponClass;

	[DataSourceProperty]
	public string UsedPieces
	{
		get
		{
			return _usedPieces;
		}
		set
		{
			_usedPieces = value;
			OnPropertyChangedWithValue(value, "UsedPieces");
		}
	}

	[DataSourceProperty]
	public int WeaponClass
	{
		get
		{
			return _weaponClass;
		}
		set
		{
			if (value != _weaponClass)
			{
				_weaponClass = value;
				OnPropertyChangedWithValue(value, "WeaponClass");
			}
		}
	}

	public WeaponClass GetWeaponClass()
	{
		return (WeaponClass)WeaponClass;
	}

	public void SetCraftingData(WeaponClass weaponClass, WeaponDesignElement[] craftingPieces)
	{
		WeaponClass = (int)weaponClass;
	}

	public CraftingItemViewModel()
	{
		WeaponClass = -1;
	}
}
