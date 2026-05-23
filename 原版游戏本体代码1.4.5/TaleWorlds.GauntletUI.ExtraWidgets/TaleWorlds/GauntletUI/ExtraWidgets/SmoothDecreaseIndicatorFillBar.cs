using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class SmoothDecreaseIndicatorFillBar : BrushWidget
{
	private float _localDt;

	private float _smoothedCurrentAmount = -1f;

	private float _maxAmount;

	private float _currentAmount;

	private bool _isVertical;

	[Editor(false)]
	public float MaxAmount
	{
		get
		{
			return (int)_maxAmount;
		}
		set
		{
			if (_maxAmount != value)
			{
				_maxAmount = value;
				OnPropertyChanged(value, "MaxAmount");
			}
		}
	}

	[Editor(false)]
	public float CurrentAmount
	{
		get
		{
			return (int)_currentAmount;
		}
		set
		{
			if (_currentAmount != value)
			{
				_currentAmount = value;
				OnPropertyChanged(value, "CurrentAmount");
				if (_smoothedCurrentAmount == -1f)
				{
					_smoothedCurrentAmount = CurrentAmount;
				}
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

	public SmoothDecreaseIndicatorFillBar(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (base.IsVisible)
		{
			StyleLayer layer = base.Brush.DefaultStyle.GetLayer("DefaultFill");
			StyleLayer layer2 = base.Brush.DefaultStyle.GetLayer("ChangeFill");
			layer.WidthPolicy = BrushLayerSizePolicy.Overriden;
			layer2.WidthPolicy = BrushLayerSizePolicy.Overriden;
			layer.HeightPolicy = BrushLayerSizePolicy.Overriden;
			layer2.HeightPolicy = BrushLayerSizePolicy.Overriden;
			float num = Mathf.Clamp(CurrentAmount / MaxAmount, 0f, 1f);
			float num2 = Mathf.Clamp(Mathf.Clamp(_smoothedCurrentAmount - CurrentAmount, 0f, MaxAmount - CurrentAmount) / MaxAmount, 0f, 1f);
			if (_smoothedCurrentAmount > CurrentAmount)
			{
				_smoothedCurrentAmount = Mathf.Lerp(_smoothedCurrentAmount * 0.99f, CurrentAmount, _localDt);
			}
			else
			{
				_smoothedCurrentAmount = CurrentAmount;
			}
			if (IsVertical)
			{
				layer.OverridenHeight = base.Size.Y * num * base._inverseScaleToUse;
				layer.YOffset = base.Size.Y - layer.OverridenHeight;
				layer2.OverridenHeight = base.Size.Y * num2 * base._inverseScaleToUse;
				layer2.YOffset = base.Size.Y - (layer.OverridenHeight + layer2.OverridenHeight);
				layer.OverridenWidth = base.Size.X * base._inverseScaleToUse;
				layer2.OverridenWidth = base.Size.X * base._inverseScaleToUse;
			}
			else
			{
				layer.OverridenWidth = base.Size.X * num * base._inverseScaleToUse;
				layer2.XOffset = layer.OverridenWidth;
				layer2.OverridenWidth = base.Size.X * num2 * base._inverseScaleToUse;
				layer.OverridenHeight = base.Size.Y * base._inverseScaleToUse;
				layer2.OverridenHeight = base.ScaledSuggestedHeight * base._inverseScaleToUse;
			}
			base.OnRender(twoDimensionContext, drawContext);
		}
		else
		{
			_smoothedCurrentAmount = CurrentAmount;
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		_localDt = dt * 3f;
	}
}
