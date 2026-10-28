using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class WeaponClassVM : ViewModel
{
	private Action<int> _onSelect;

	private Dictionary<CraftingPiece.PieceTypes, string> _selectedPieces;

	private bool _hasNewlyUnlockedPieces;

	private string _unlockedPiecesLabelText;

	private int _unlockedPiecesCount;

	private string _templateName;

	private bool _isSelected;

	private int _selectionIndex;

	private string _weaponType;

	public int NewlyUnlockedPieceCount { get; set; }

	public CraftingTemplate Template { get; }

	[DataSourceProperty]
	public bool HasNewlyUnlockedPieces
	{
		get
		{
			return _hasNewlyUnlockedPieces;
		}
		set
		{
			if (value != _hasNewlyUnlockedPieces)
			{
				_hasNewlyUnlockedPieces = value;
				OnPropertyChangedWithValue(value, "HasNewlyUnlockedPieces");
			}
		}
	}

	[DataSourceProperty]
	public string UnlockedPiecesLabelText
	{
		get
		{
			return _unlockedPiecesLabelText;
		}
		set
		{
			if (value != _unlockedPiecesLabelText)
			{
				_unlockedPiecesLabelText = value;
				OnPropertyChangedWithValue(value, "UnlockedPiecesLabelText");
			}
		}
	}

	[DataSourceProperty]
	public int UnlockedPiecesCount
	{
		get
		{
			return _unlockedPiecesCount;
		}
		set
		{
			if (value != _unlockedPiecesCount)
			{
				_unlockedPiecesCount = value;
				OnPropertyChangedWithValue(value, "UnlockedPiecesCount");
			}
		}
	}

	[DataSourceProperty]
	public string TemplateName
	{
		get
		{
			return _templateName;
		}
		set
		{
			if (value != _templateName)
			{
				_templateName = value;
				OnPropertyChangedWithValue(value, "TemplateName");
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
	public int SelectionIndex
	{
		get
		{
			return _selectionIndex;
		}
		set
		{
			if (value != _selectionIndex)
			{
				_selectionIndex = value;
				OnPropertyChangedWithValue(value, "SelectionIndex");
			}
		}
	}

	[DataSourceProperty]
	public string WeaponType
	{
		get
		{
			return _weaponType;
		}
		set
		{
			if (value != _weaponType)
			{
				_weaponType = value;
				OnPropertyChangedWithValue(value, "WeaponType");
			}
		}
	}

	public WeaponClassVM(int selectionIndex, CraftingTemplate template, Action<int> onSelect)
	{
		_onSelect = onSelect;
		SelectionIndex = selectionIndex;
		Template = template;
		_selectedPieces = new Dictionary<CraftingPiece.PieceTypes, string>
		{
			{
				CraftingPiece.PieceTypes.Blade,
				null
			},
			{
				CraftingPiece.PieceTypes.Guard,
				null
			},
			{
				CraftingPiece.PieceTypes.Handle,
				null
			},
			{
				CraftingPiece.PieceTypes.Pommel,
				null
			}
		};
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		TemplateName = Template.TemplateName.ToString();
		UnlockedPiecesLabelText = new TextObject("{=OGbskMfz}Unlocked Parts:").ToString();
		WeaponType = Template.StringId;
	}

	public void RegisterSelectedPiece(CraftingPiece.PieceTypes type, string pieceID)
	{
		if (_selectedPieces.TryGetValue(type, out var value) && value != pieceID)
		{
			_selectedPieces[type] = pieceID;
		}
	}

	public string GetSelectedPieceData(CraftingPiece.PieceTypes type)
	{
		if (_selectedPieces.TryGetValue(type, out var value))
		{
			return value;
		}
		return null;
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(SelectionIndex);
	}
}
