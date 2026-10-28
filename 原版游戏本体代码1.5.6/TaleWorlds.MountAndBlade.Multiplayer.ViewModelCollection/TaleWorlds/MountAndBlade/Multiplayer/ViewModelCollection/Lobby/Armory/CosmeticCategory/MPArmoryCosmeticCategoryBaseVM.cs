using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Diamond.Cosmetics;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticItem;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.Lobby.Armory.CosmeticCategory;

public abstract class MPArmoryCosmeticCategoryBaseVM : ViewModel
{
	public readonly CosmeticsManager.CosmeticType CosmeticType;

	private string _cosmeticTypeName;

	private string _cosmeticCategoryName;

	private bool _isSelected;

	private MBBindingList<MPArmoryCosmeticItemBaseVM> _availableCosmetics;

	[DataSourceProperty]
	public string CosmeticTypeName
	{
		get
		{
			return _cosmeticTypeName;
		}
		set
		{
			if (value != _cosmeticTypeName)
			{
				_cosmeticTypeName = value;
				OnPropertyChangedWithValue(value, "CosmeticTypeName");
			}
		}
	}

	[DataSourceProperty]
	public string CosmeticCategoryName
	{
		get
		{
			return _cosmeticCategoryName;
		}
		set
		{
			if (value != _cosmeticCategoryName)
			{
				_cosmeticCategoryName = value;
				OnPropertyChangedWithValue(value, "CosmeticCategoryName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSelected
	{
		get
		{
			return _isSelected;
		}
		set
		{
			if (value != _isSelected)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<MPArmoryCosmeticItemBaseVM> AvailableCosmetics
	{
		get
		{
			return _availableCosmetics;
		}
		set
		{
			if (value != _availableCosmetics)
			{
				_availableCosmetics = value;
				OnPropertyChangedWithValue(value, "AvailableCosmetics");
			}
		}
	}

	public MPArmoryCosmeticCategoryBaseVM(CosmeticsManager.CosmeticType cosmeticType)
	{
		AvailableCosmetics = new MBBindingList<MPArmoryCosmeticItemBaseVM>();
		CosmeticType = cosmeticType;
		CosmeticTypeName = cosmeticType.ToString();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		AvailableCosmetics.ApplyActionOnAllItems(delegate(MPArmoryCosmeticItemBaseVM c)
		{
			c.RefreshValues();
		});
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		AvailableCosmetics.ApplyActionOnAllItems(delegate(MPArmoryCosmeticItemBaseVM c)
		{
			c.OnFinalize();
		});
	}

	protected abstract void ExecuteSelectCategory();

	public void Sort(MPArmoryCosmeticsVM.CosmeticItemComparer comparer)
	{
		AvailableCosmetics.Sort(comparer);
	}
}
