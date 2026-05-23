using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class FillBar : BrushWidget
{
	private float _localDt;

	private float _maxAmount;

	private float _currentAmount;

	private float _initialAmount;

	private bool _isVertical;

	private bool _isSmoothFillEnabled;

	[Editor(false)]
	public int MaxAmount
	{
		get
		{
			return (int)_maxAmount;
		}
		set
		{
			if (_maxAmount != (float)value)
			{
				_maxAmount = value;
				OnPropertyChanged(value, "MaxAmount");
			}
		}
	}

	[Editor(false)]
	public int CurrentAmount
	{
		get
		{
			return (int)_currentAmount;
		}
		set
		{
			if (_currentAmount != (float)value)
			{
				_currentAmount = value;
				OnPropertyChanged(value, "CurrentAmount");
			}
		}
	}

	[Editor(false)]
	public int InitialAmount
	{
		get
		{
			return (int)_initialAmount;
		}
		set
		{
			if (_initialAmount != (float)value)
			{
				_initialAmount = value;
				OnPropertyChanged(value, "InitialAmount");
			}
		}
	}

	[Editor(false)]
	public float MaxAmountAsFloat
	{
		get
		{
			return _maxAmount;
		}
		set
		{
			if (_maxAmount != value)
			{
				_maxAmount = value;
				OnPropertyChanged(value, "MaxAmountAsFloat");
			}
		}
	}

	[Editor(false)]
	public float CurrentAmountAsFloat
	{
		get
		{
			return _currentAmount;
		}
		set
		{
			if (_currentAmount != value)
			{
				_currentAmount = value;
				OnPropertyChanged(value, "CurrentAmountAsFloat");
			}
		}
	}

	[Editor(false)]
	public float InitialAmountAsFloat
	{
		get
		{
			return _initialAmount;
		}
		set
		{
			if (_initialAmount != value)
			{
				_initialAmount = value;
				OnPropertyChanged(value, "InitialAmountAsFloat");
			}
		}
	}

	[Editor(false)]
	public bool IsVertical
	{
		get
		{
			return _isVertical;
		}
		set
		{
			if (_isVertical != value)
			{
				_isVertical = value;
				OnPropertyChanged(value, "IsVertical");
			}
		}
	}

	[Editor(false)]
	public bool IsSmoothFillEnabled
	{
		get
		{
			return _isSmoothFillEnabled;
		}
		set
		{
			if (_isSmoothFillEnabled != value)
			{
				_isSmoothFillEnabled = value;
				OnPropertyChanged(value, "IsSmoothFillEnabled");
			}
		}
	}

	public FillBar(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (!base.IsVisible)
		{
			return;
		}
		StyleLayer layer = base.Brush.DefaultStyle.GetLayer("DefaultFill");
		StyleLayer layer2 = base.Brush.DefaultStyle.GetLayer("ChangeFill");
		layer.WidthPolicy = BrushLayerSizePolicy.Overriden;
		layer2.WidthPolicy = BrushLayerSizePolicy.Overriden;
		layer.HeightPolicy = BrushLayerSizePolicy.Overriden;
		layer2.HeightPolicy = BrushLayerSizePolicy.Overriden;
		float num = Mathf.Clamp(_initialAmount / (float)MaxAmount, 0f, 1f);
		float num2 = Mathf.Clamp(Mathf.Clamp(CurrentAmount - InitialAmount, 0f, MaxAmount - InitialAmount) / (float)MaxAmount, 0f, 1f);
		if (IsVertical)
		{
			if (!IsSmoothFillEnabled)
			{
				_localDt = 1f;
			}
			float end = base.Size.Y * num * base._inverseScaleToUse;
			layer.OverridenHeight = Mathf.Lerp(layer.OverridenHeight, end, _localDt);
			end = base.Size.Y - layer.OverridenHeight;
			layer.YOffset = Mathf.Lerp(layer.YOffset, end, _localDt);
			end = base.Size.Y * num2 * base._inverseScaleToUse;
			layer2.OverridenHeight = Mathf.Lerp(layer2.OverridenHeight, end, _localDt);
			end = base.Size.Y - (layer.OverridenHeight + layer2.OverridenHeight);
			layer2.YOffset = Mathf.Lerp(layer2.YOffset, end, _localDt);
			layer.OverridenWidth = base.Size.X * base._inverseScaleToUse;
			layer2.OverridenWidth = base.Size.X * base._inverseScaleToUse;
		}
		else
		{
			if (!IsSmoothFillEnabled)
			{
				_localDt = 1f;
			}
			float end2 = base.Size.X * num * base._inverseScaleToUse;
			layer.OverridenWidth = Mathf.Lerp(layer.OverridenWidth, end2, _localDt);
			end2 = layer.OverridenWidth;
			layer2.XOffset = Mathf.Lerp(layer2.XOffset, end2, _localDt);
			end2 = base.Size.X * num2 * base._inverseScaleToUse;
			layer2.OverridenWidth = Mathf.Lerp(layer2.OverridenWidth, end2, _localDt);
			layer.OverridenHeight = base.Size.Y * base._inverseScaleToUse;
			layer2.OverridenHeight = base.ScaledSuggestedHeight * base._inverseScaleToUse;
		}
		base.OnRender(twoDimensionContext, drawContext);
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		_localDt = dt * 10f;
	}
}
