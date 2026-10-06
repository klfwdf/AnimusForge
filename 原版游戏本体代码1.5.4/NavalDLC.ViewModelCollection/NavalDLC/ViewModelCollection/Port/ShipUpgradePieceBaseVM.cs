using System;
using TaleWorlds.Core.ViewModelCollection.Generic;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace NavalDLC.ViewModelCollection.Port;

public class ShipUpgradePieceBaseVM : ViewModel
{
	public enum ShipUpgradePieceTier
	{
		Bronze = 1,
		Silver,
		Gold,
		Diamond
	}

	public Action<ShipUpgradePieceBaseVM> _onSelected;

	private ShipUpgradePieceTier _upgradePieceTier = ShipUpgradePieceTier.Bronze;

	protected TextObject _slotHintText;

	private string _identifier;

	private string _name;

	private bool _isSelected;

	private bool _isDisabled;

	private bool _isInspected;

	private bool _isBronzeTier = true;

	private bool _isSilverTier;

	private bool _isGoldTier;

	private bool _isDiamondTier;

	private bool _isUnexamined;

	private bool _isHiddenFromPlayer;

	private int _price;

	private MBBindingList<StringPairItemVM> _properties;

	public ShipUpgradePieceTier UpgradePieceTier
	{
		get
		{
			return _upgradePieceTier;
		}
		set
		{
			if (_upgradePieceTier != value)
			{
				_upgradePieceTier = value;
				IsBronzeTier = _upgradePieceTier == ShipUpgradePieceTier.Bronze;
				IsSilverTier = _upgradePieceTier == ShipUpgradePieceTier.Silver;
				IsGoldTier = _upgradePieceTier == ShipUpgradePieceTier.Gold;
				IsDiamondTier = _upgradePieceTier == ShipUpgradePieceTier.Diamond;
			}
		}
	}

	public bool IsInspectedFromSlot { get; private set; }

	[DataSourceProperty]
	public string Identifier
	{
		get
		{
			return _identifier;
		}
		set
		{
			if (value != _identifier)
			{
				_identifier = value;
				OnPropertyChangedWithValue(value, "Identifier");
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
	public bool IsDisabled
	{
		get
		{
			return _isDisabled;
		}
		set
		{
			if (value != _isDisabled)
			{
				_isDisabled = value;
				OnPropertyChangedWithValue(value, "IsDisabled");
			}
		}
	}

	[DataSourceProperty]
	public bool IsInspected
	{
		get
		{
			return _isInspected;
		}
		set
		{
			if (value != _isInspected)
			{
				_isInspected = value;
				OnPropertyChangedWithValue(value, "IsInspected");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDiamondTier
	{
		get
		{
			return _isDiamondTier;
		}
		set
		{
			if (value != _isDiamondTier)
			{
				_isDiamondTier = value;
				OnPropertyChangedWithValue(value, "IsDiamondTier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsBronzeTier
	{
		get
		{
			return _isBronzeTier;
		}
		set
		{
			if (value != _isBronzeTier)
			{
				_isBronzeTier = value;
				OnPropertyChangedWithValue(value, "IsBronzeTier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsSilverTier
	{
		get
		{
			return _isSilverTier;
		}
		set
		{
			if (value != _isSilverTier)
			{
				_isSilverTier = value;
				OnPropertyChangedWithValue(value, "IsSilverTier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsGoldTier
	{
		get
		{
			return _isGoldTier;
		}
		set
		{
			if (value != _isGoldTier)
			{
				_isGoldTier = value;
				OnPropertyChangedWithValue(value, "IsGoldTier");
			}
		}
	}

	[DataSourceProperty]
	public bool IsUnexamined
	{
		get
		{
			return _isUnexamined;
		}
		set
		{
			if (value != _isUnexamined)
			{
				_isUnexamined = value;
				OnPropertyChangedWithValue(value, "IsUnexamined");
			}
		}
	}

	[DataSourceProperty]
	public bool IsHiddenFromPlayer
	{
		get
		{
			return _isHiddenFromPlayer;
		}
		set
		{
			if (value != _isHiddenFromPlayer)
			{
				_isHiddenFromPlayer = value;
				OnPropertyChangedWithValue(value, "IsHiddenFromPlayer");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<StringPairItemVM> Properties
	{
		get
		{
			return _properties;
		}
		set
		{
			if (value != _properties)
			{
				_properties = value;
				OnPropertyChangedWithValue(value, "Properties");
			}
		}
	}

	[DataSourceProperty]
	public int Price
	{
		get
		{
			return _price;
		}
		set
		{
			if (value != _price)
			{
				_price = value;
				OnPropertyChangedWithValue(value, "Price");
			}
		}
	}

	public static event Action<ShipUpgradePieceBaseVM> OnInspected;

	public ShipUpgradePieceBaseVM(Action<ShipUpgradePieceBaseVM> onSelected)
	{
		_onSelected = onSelected;
		Properties = new MBBindingList<StringPairItemVM>();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		UpdateProperties();
	}

	protected virtual PropertyBasedTooltipVM GetProperties()
	{
		return null;
	}

	private void UpdateProperties()
	{
		Properties.Clear();
		PropertyBasedTooltipVM properties = GetProperties();
		if (properties == null)
		{
			return;
		}
		for (int i = 0; i < properties.TooltipPropertyList.Count; i++)
		{
			TooltipProperty tooltipProperty = properties.TooltipPropertyList[i];
			if (tooltipProperty.PropertyModifier != 4096)
			{
				Properties.Add(new StringPairItemVM(tooltipProperty.DefinitionLabel, tooltipProperty.ValueLabel));
			}
		}
	}

	public void ExecuteSelect()
	{
		_onSelected?.Invoke(this);
	}

	public void ExecuteInspectBegin()
	{
		InspectPiece();
	}

	public virtual void InspectPiece(bool isInspectedFromSlot = false, TextObject slotHintText = null)
	{
		if (IsInspectedFromSlot != isInspectedFromSlot || _slotHintText != slotHintText)
		{
			IsInspectedFromSlot = isInspectedFromSlot;
			_slotHintText = slotHintText;
			UpdateProperties();
		}
		ShipUpgradePieceBaseVM.OnInspected?.Invoke(this);
	}

	public void ExecuteInspectEnd()
	{
		ShipUpgradePieceBaseVM.OnInspected?.Invoke(null);
	}

	public virtual void Update()
	{
	}
}
