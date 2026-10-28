using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.BannerBuilder;

public class BannerBuilderLayerVM : ViewModel
{
	private static Action<BannerBuilderLayerVM> _onSelection;

	private static Action<BannerBuilderLayerVM> _onDeletion;

	private static Action<int, Action<BannerBuilderColorItemVM>> _onColorSelection;

	private static Action _refresh;

	private int _iconID;

	private string _iconIDAsString;

	private Color _color1;

	private Color _color2;

	private string _color1AsStr;

	private string _color2AsStr;

	private bool _isSelected;

	private bool _canDeleteLayer;

	private bool _isLayerPattern;

	private bool _isDrawStrokeActive;

	private bool _isMirrorActive;

	private int _editableAreaSize;

	private int _totalAreaSize;

	private int _layerIndex;

	private float _rotationValue;

	private Vec2 _positionValue;

	private Vec2 _sizeValue;

	public BannerData Data { get; private set; }

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
	public bool CanDeleteLayer
	{
		get
		{
			return _canDeleteLayer;
		}
		set
		{
			if (value != _canDeleteLayer)
			{
				_canDeleteLayer = value;
				OnPropertyChangedWithValue(value, "CanDeleteLayer");
			}
		}
	}

	[DataSourceProperty]
	public bool IsLayerPattern
	{
		get
		{
			return _isLayerPattern;
		}
		set
		{
			if (value != _isLayerPattern)
			{
				_isLayerPattern = value;
				OnPropertyChangedWithValue(value, "IsLayerPattern");
			}
		}
	}

	[DataSourceProperty]
	public bool IsDrawStrokeActive
	{
		get
		{
			return _isDrawStrokeActive;
		}
		set
		{
			if (value != _isDrawStrokeActive)
			{
				_isDrawStrokeActive = value;
				OnPropertyChangedWithValue(value, "IsDrawStrokeActive");
				Data.DrawStroke = value;
				ExecuteUpdateBanner();
			}
		}
	}

	[DataSourceProperty]
	public bool IsMirrorActive
	{
		get
		{
			return _isMirrorActive;
		}
		set
		{
			if (value != _isMirrorActive)
			{
				_isMirrorActive = value;
				OnPropertyChangedWithValue(value, "IsMirrorActive");
				Data.Mirror = value;
				ExecuteUpdateBanner();
			}
		}
	}

	[DataSourceProperty]
	public float RotationValue
	{
		get
		{
			return _rotationValue;
		}
		set
		{
			if (value != _rotationValue)
			{
				_rotationValue = value;
				Data.RotationValue = value;
				OnPropertyChangedWithValue(value, "RotationValue");
				OnPropertyChanged("RotationValue360");
			}
		}
	}

	[DataSourceProperty]
	public int RotationValue360
	{
		get
		{
			return (int)(_rotationValue * 360f);
		}
		set
		{
			if (value != (int)(_rotationValue * 360f))
			{
				RotationValue = (float)value / 360f;
				OnPropertyChangedWithValue(value, "RotationValue360");
			}
		}
	}

	[DataSourceProperty]
	public int IconID
	{
		get
		{
			return _iconID;
		}
		set
		{
			if (value != _iconID)
			{
				_iconID = value;
				OnPropertyChangedWithValue(value, "IconID");
			}
		}
	}

	[DataSourceProperty]
	public int LayerIndex
	{
		get
		{
			return _layerIndex;
		}
		set
		{
			if (value != _layerIndex)
			{
				_layerIndex = value;
				OnPropertyChangedWithValue(value, "LayerIndex");
			}
		}
	}

	[DataSourceProperty]
	public int EditableAreaSize
	{
		get
		{
			return _editableAreaSize;
		}
		set
		{
			if (value != _editableAreaSize)
			{
				_editableAreaSize = value;
				OnPropertyChangedWithValue(value, "EditableAreaSize");
			}
		}
	}

	[DataSourceProperty]
	public int TotalAreaSize
	{
		get
		{
			return _totalAreaSize;
		}
		set
		{
			if (value != _totalAreaSize)
			{
				_totalAreaSize = value;
				OnPropertyChangedWithValue(value, "TotalAreaSize");
			}
		}
	}

	[DataSourceProperty]
	public string IconIDAsString
	{
		get
		{
			return _iconIDAsString;
		}
		set
		{
			if (value != _iconIDAsString)
			{
				_iconIDAsString = value;
				OnPropertyChangedWithValue(value, "IconIDAsString");
			}
		}
	}

	[DataSourceProperty]
	public Color Color1
	{
		get
		{
			return _color1;
		}
		set
		{
			if (value != _color1)
			{
				_color1 = value;
				OnPropertyChangedWithValue(value, "Color1");
				Color1AsStr = value.ToString();
			}
		}
	}

	[DataSourceProperty]
	public Color Color2
	{
		get
		{
			return _color2;
		}
		set
		{
			if (value != _color2)
			{
				_color2 = value;
				OnPropertyChangedWithValue(value, "Color2");
				Color2AsStr = value.ToString();
			}
		}
	}

	[DataSourceProperty]
	public string Color1AsStr
	{
		get
		{
			return _color1AsStr;
		}
		set
		{
			if (value != _color1AsStr)
			{
				_color1AsStr = value;
				OnPropertyChangedWithValue(value, "Color1AsStr");
			}
		}
	}

	[DataSourceProperty]
	public string Color2AsStr
	{
		get
		{
			return _color2AsStr;
		}
		set
		{
			if (value != _color2AsStr)
			{
				_color2AsStr = value;
				OnPropertyChangedWithValue(value, "Color2AsStr");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 PositionValue
	{
		get
		{
			return _positionValue;
		}
		set
		{
			if (_positionValue != value)
			{
				_positionValue = value;
				OnPropertyChangedWithValue(value, "PositionValue");
				OnPropertyChanged("PositionValueX");
				OnPropertyChanged("PositionValueY");
				Data.Position = value;
			}
		}
	}

	[DataSourceProperty]
	public float PositionValueX
	{
		get
		{
			return (float)Math.Round(_positionValue.X);
		}
		set
		{
			value = (float)Math.Round(value);
			if (value != _positionValue.X)
			{
				PositionValue = new Vec2(value, _positionValue.Y);
				Data.Position = _positionValue;
				OnPropertyChangedWithValue(value, "PositionValueX");
			}
		}
	}

	[DataSourceProperty]
	public float PositionValueY
	{
		get
		{
			return (float)Math.Round(_positionValue.Y);
		}
		set
		{
			value = (float)Math.Round(value);
			if (value != _positionValue.Y)
			{
				PositionValue = new Vec2(_positionValue.X, value);
				Data.Position = _positionValue;
				OnPropertyChangedWithValue(value, "PositionValueY");
			}
		}
	}

	[DataSourceProperty]
	public Vec2 SizeValue
	{
		get
		{
			return _sizeValue;
		}
		set
		{
			if (_sizeValue != value)
			{
				_sizeValue = value;
				OnPropertyChangedWithValue(value, "SizeValue");
				OnPropertyChanged("SizeValueX");
				OnPropertyChanged("SizeValueY");
				Data.Size = value;
			}
		}
	}

	[DataSourceProperty]
	public float SizeValueX
	{
		get
		{
			return (float)Math.Round(_sizeValue.X);
		}
		set
		{
			value = (float)Math.Round(value);
			if (value != _sizeValue.X)
			{
				SizeValue = new Vec2(value, _sizeValue.Y);
				Data.Size = _sizeValue;
				OnPropertyChangedWithValue(value, "SizeValueX");
			}
		}
	}

	[DataSourceProperty]
	public float SizeValueY
	{
		get
		{
			return (float)Math.Round(_sizeValue.Y);
		}
		set
		{
			value = (float)Math.Round(value);
			if (value != _sizeValue.Y)
			{
				SizeValue = new Vec2(_sizeValue.X, value);
				Data.Size = _sizeValue;
				OnPropertyChangedWithValue(value, "SizeValueY");
			}
		}
	}

	public BannerBuilderLayerVM(BannerData data, int layerIndex)
	{
		Data = data;
		LayerIndex = layerIndex;
		_rotationValue = Data.Rotation;
		_positionValue = Data.Position;
		_sizeValue = Data.Size;
		_isDrawStrokeActive = Data.DrawStroke;
		_isMirrorActive = Data.Mirror;
		Refresh();
		IsLayerPattern = layerIndex == 0;
		CanDeleteLayer = !IsLayerPattern;
		TotalAreaSize = 1528;
		EditableAreaSize = 512;
	}

	public void Refresh()
	{
		IconID = Data.MeshId;
		IconIDAsString = IconID.ToString();
		uint color = BannerManager.Instance.ReadOnlyColorPalette[Data.ColorId].Color;
		Color1 = Color.FromUint(color);
		uint color2 = BannerManager.Instance.ReadOnlyColorPalette[Data.ColorId2].Color;
		Color2 = Color.FromUint(color2);
	}

	public void ExecuteDelete()
	{
		_onDeletion?.Invoke(this);
	}

	public void ExecuteSelection()
	{
		_onSelection?.Invoke(this);
	}

	public void SetLayerIndex(int newIndex)
	{
		LayerIndex = newIndex;
	}

	public void ExecuteSelectColor1()
	{
		_onColorSelection?.Invoke(Data.ColorId, OnSelectColor1);
	}

	private void OnSelectColor1(BannerBuilderColorItemVM selectedColor)
	{
		Data.ColorId = selectedColor.ColorID;
		Color1 = Color.FromUint(selectedColor.BannerColor.Color);
		ExecuteUpdateBanner();
	}

	public void ExecuteSelectColor2()
	{
		_onColorSelection?.Invoke(Data.ColorId2, OnSelectColor2);
	}

	private void OnSelectColor2(BannerBuilderColorItemVM selectedColor)
	{
		Data.ColorId2 = selectedColor.ColorID;
		Color2 = Color.FromUint(selectedColor.BannerColor.Color);
		ExecuteUpdateBanner();
	}

	public void ExecuteSwapColors()
	{
		int colorId = Data.ColorId2;
		Data.ColorId2 = Data.ColorId;
		Data.ColorId = colorId;
		Color color = Color2;
		Color color2 = Color1;
		Color color3 = (Color1 = color);
		color3 = (Color2 = color2);
		Refresh();
		ExecuteUpdateBanner();
	}

	public void ExecuteCenterSigil()
	{
		PositionValue = new Vec2((float)TotalAreaSize / 2f, (float)TotalAreaSize / 2f);
		ExecuteUpdateBanner();
	}

	public void ExecuteResetSize()
	{
		float num = (IsLayerPattern ? TotalAreaSize : 483);
		SizeValue = new Vec2(num, num);
		ExecuteUpdateBanner();
	}

	public void ExecuteUpdateBanner()
	{
		_refresh?.Invoke();
	}

	public static void SetLayerActions(Action refresh, Action<BannerBuilderLayerVM> onSelection, Action<BannerBuilderLayerVM> onDeletion, Action<int, Action<BannerBuilderColorItemVM>> onColorSelection)
	{
		_onSelection = onSelection;
		_onDeletion = onDeletion;
		_onColorSelection = onColorSelection;
		_refresh = refresh;
	}

	public static void ResetLayerActions()
	{
		_onSelection = null;
		_onDeletion = null;
		_onColorSelection = null;
		_refresh = null;
	}
}
