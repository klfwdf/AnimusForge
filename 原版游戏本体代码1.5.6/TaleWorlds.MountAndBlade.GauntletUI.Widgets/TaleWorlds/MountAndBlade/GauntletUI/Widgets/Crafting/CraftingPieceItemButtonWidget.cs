using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Crafting;

public class CraftingPieceItemButtonWidget : ButtonWidget
{
	private ImageIdentifierWidget _imageIdentifier;

	private bool _playerHasPiece;

	private Brush _hasPieceBrush;

	private Brush _dontHavePieceBrush;

	private Brush _hasPieceMaterialBrush;

	private Brush _dontHavePieceMaterialBrush;

	public ImageIdentifierWidget ImageIdentifier
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
				UpdateMaterialBrush();
			}
		}
	}

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
				UpdateSelfBrush();
				UpdateMaterialBrush();
			}
		}
	}

	public Brush HasPieceBrush
	{
		get
		{
			return _hasPieceBrush;
		}
		set
		{
			if (_hasPieceBrush != value)
			{
				_hasPieceBrush = value;
				UpdateSelfBrush();
			}
		}
	}

	public Brush DontHavePieceBrush
	{
		get
		{
			return _dontHavePieceBrush;
		}
		set
		{
			if (_dontHavePieceBrush != value)
			{
				_dontHavePieceBrush = value;
				UpdateSelfBrush();
			}
		}
	}

	public Brush HasPieceMaterialBrush
	{
		get
		{
			return _hasPieceMaterialBrush;
		}
		set
		{
			if (_hasPieceMaterialBrush != value)
			{
				_hasPieceMaterialBrush = value;
				UpdateMaterialBrush();
			}
		}
	}

	public Brush DontHavePieceMaterialBrush
	{
		get
		{
			return _dontHavePieceMaterialBrush;
		}
		set
		{
			if (_dontHavePieceMaterialBrush != value)
			{
				_dontHavePieceMaterialBrush = value;
				UpdateMaterialBrush();
			}
		}
	}

	public CraftingPieceItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateSelfBrush()
	{
		if (DontHavePieceBrush != null && HasPieceBrush != null)
		{
			base.Brush = (PlayerHasPiece ? HasPieceBrush : DontHavePieceBrush);
		}
	}

	private void UpdateMaterialBrush()
	{
		if (DontHavePieceMaterialBrush != null && HasPieceMaterialBrush != null && ImageIdentifier != null)
		{
			ImageIdentifier.Brush = (PlayerHasPiece ? HasPieceMaterialBrush : DontHavePieceMaterialBrush);
		}
	}
}
