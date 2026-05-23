using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting.WeaponDesign;

public class CraftingPieceListVM : ViewModel
{
	public CraftingPiece.PieceTypes PieceType;

	private Action<CraftingPiece.PieceTypes, bool> _onSelect;

	private bool _hasNewlyUnlockedPieces;

	private MBBindingList<CraftingPieceVM> _pieces;

	private bool _isSelected;

	private bool _isEnabled;

	private CraftingPieceVM _selectedPiece;

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
	public MBBindingList<CraftingPieceVM> Pieces
	{
		get
		{
			return _pieces;
		}
		set
		{
			if (value != _pieces)
			{
				_pieces = value;
				OnPropertyChangedWithValue(value, "Pieces");
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
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	[DataSourceProperty]
	public CraftingPieceVM SelectedPiece
	{
		get
		{
			return _selectedPiece;
		}
		set
		{
			if (value != _selectedPiece)
			{
				_selectedPiece = value;
				OnPropertyChangedWithValue(value, "SelectedPiece");
			}
		}
	}

	public CraftingPieceListVM(MBBindingList<CraftingPieceVM> pieceList, CraftingPiece.PieceTypes pieceType, Action<CraftingPiece.PieceTypes, bool> onSelect)
	{
		Pieces = pieceList;
		PieceType = pieceType;
		_onSelect = onSelect;
	}

	public void ExecuteSelect()
	{
		_onSelect?.Invoke(PieceType, arg2: true);
		HasNewlyUnlockedPieces = false;
	}

	public void Refresh()
	{
		HasNewlyUnlockedPieces = Pieces.Any((CraftingPieceVM x) => x.IsNewlyUnlocked);
	}
}
