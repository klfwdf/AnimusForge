using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingPieceVM : ViewModel
{
	public WeaponDesignElement CraftingPiece;

	public int Index;

	private readonly Action<CraftingPieceVM> _selectWeaponPiece;

	private bool _isFilteredOut;

	public CraftingPieceImageIdentifierVM _imageIdentifier;

	public int _pieceType = -1;

	public int _tier;

	public bool _isSelected;

	public bool _playerHasPiece;

	private bool _isEmpty;

	public string _tierText;

	private MBBindingList<CraftingItemFlagVM> _itemAttributeIcons;

	private bool _isNewlyUnlocked;

	[DataSourceProperty]
	public bool IsFilteredOut
	{
		get
		{
			return _isFilteredOut;
		}
		set
		{
			if (value != _isFilteredOut)
			{
				_isFilteredOut = value;
				OnPropertyChangedWithValue(value, "IsFilteredOut");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CraftingItemFlagVM> ItemAttributeIcons
	{
		get
		{
			return _itemAttributeIcons;
		}
		set
		{
			if (value != _itemAttributeIcons)
			{
				_itemAttributeIcons = value;
				OnPropertyChangedWithValue(value, "ItemAttributeIcons");
			}
		}
	}

	[DataSourceProperty]
	public bool PlayerHasPiece
	{
		get
		{
			return _playerHasPiece;
		}
		set
		{
			if (_playerHasPiece != value)
			{
				_playerHasPiece = value;
				OnPropertyChangedWithValue(value, "PlayerHasPiece");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEmpty
	{
		get
		{
			return _isEmpty;
		}
		set
		{
			if (_isEmpty != value)
			{
				_isEmpty = value;
				OnPropertyChangedWithValue(value, "IsEmpty");
			}
		}
	}

	[DataSourceProperty]
	public string TierText
	{
		get
		{
			return _tierText;
		}
		set
		{
			if (_tierText != value)
			{
				_tierText = value;
				OnPropertyChangedWithValue(value, "TierText");
			}
		}
	}

	[DataSourceProperty]
	public int Tier
	{
		get
		{
			return _tier;
		}
		set
		{
			if (_tier != value)
			{
				_tier = value;
				OnPropertyChangedWithValue(value, "Tier");
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
			if (_isSelected != value)
			{
				_isSelected = value;
				OnPropertyChangedWithValue(value, "IsSelected");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (_imageIdentifier != value)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public int PieceType
	{
		get
		{
			return _pieceType;
		}
		set
		{
			if (_pieceType != value)
			{
				_pieceType = value;
				OnPropertyChangedWithValue(value, "PieceType");
			}
		}
	}

	[DataSourceProperty]
	public bool IsNewlyUnlocked
	{
		get
		{
			return _isNewlyUnlocked;
		}
		set
		{
			if (value != _isNewlyUnlocked)
			{
				_isNewlyUnlocked = value;
				OnPropertyChangedWithValue(value, "IsNewlyUnlocked");
			}
		}
	}

	public CraftingPieceVM()
	{
		ImageIdentifier = new CraftingPieceImageIdentifierVM(null, string.Empty);
	}

	public CraftingPieceVM(Action<CraftingPieceVM> selectWeaponPart, string templateId, WeaponDesignElement usableCraftingPiece, int pieceType, int index, bool isOpened)
	{
		_selectWeaponPiece = selectWeaponPart;
		CraftingPiece = usableCraftingPiece;
		Tier = usableCraftingPiece.CraftingPiece.PieceTier;
		TierText = Common.ToRoman(Tier);
		ImageIdentifier = new CraftingPieceImageIdentifierVM(usableCraftingPiece.CraftingPiece, templateId);
		PieceType = pieceType;
		Index = index;
		PlayerHasPiece = isOpened;
		ItemAttributeIcons = new MBBindingList<CraftingItemFlagVM>();
		IsEmpty = string.IsNullOrEmpty(CraftingPiece.CraftingPiece.MeshName);
		RefreshFlagIcons();
	}

	public void RefreshFlagIcons()
	{
		ItemAttributeIcons.Clear();
		foreach (Tuple<string, TextObject> itemFlagDetail in CampaignUIHelper.GetItemFlagDetails(CraftingPiece.CraftingPiece.AdditionalItemFlags))
		{
			ItemAttributeIcons.Add(new CraftingItemFlagVM(itemFlagDetail.Item1, itemFlagDetail.Item2, isDisplayed: true));
		}
		foreach (var weaponFlagDetail in CampaignUIHelper.GetWeaponFlagDetails(CraftingPiece.CraftingPiece.AdditionalWeaponFlags))
		{
			ItemAttributeIcons.Add(new CraftingItemFlagVM(weaponFlagDetail.Item1, weaponFlagDetail.Item2, isDisplayed: true));
		}
	}

	public void ExecuteOpenTooltip()
	{
		InformationManager.ShowTooltip(typeof(WeaponDesignElement), CraftingPiece);
	}

	public void ExecuteCloseTooltip()
	{
		MBInformationManager.HideInformations();
	}

	public void ExecuteSelect()
	{
		_selectWeaponPiece(this);
	}
}
