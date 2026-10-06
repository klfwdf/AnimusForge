using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class FillBarVerticalWidget : Widget
{
	private bool _isCurrentValueSet;

	private Widget _fillWidget;

	private Widget _changeWidget;

	private Widget _containerWidget;

	private Widget _dividerWidget;

	private float _maxAmount;

	private float _currentAmount;

	private float _initialAmount;

	private bool _isDirectionUpward;

	[Editor(false)]
	public bool IsDirectionUpward
	{
		get
		{
			return _isDirectionUpward;
		}
		set
		{
			if (_isDirectionUpward != value)
			{
				_isDirectionUpward = value;
				OnPropertyChanged(value, "IsDirectionUpward");
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
				_isCurrentValueSet = true;
			}
		}
	}

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
				_isCurrentValueSet = true;
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

	public Widget FillWidget
	{
		get
		{
			return _fillWidget;
		}
		set
		{
			if (_fillWidget != value)
			{
				_fillWidget = value;
				OnPropertyChanged(value, "FillWidget");
			}
		}
	}

	public Widget ChangeWidget
	{
		get
		{
			return _changeWidget;
		}
		set
		{
			if (_changeWidget != value)
			{
				_changeWidget = value;
				OnPropertyChanged(value, "ChangeWidget");
			}
		}
	}

	public Widget DividerWidget
	{
		get
		{
			return _dividerWidget;
		}
		set
		{
			if (_dividerWidget != value)
			{
				_dividerWidget = value;
				OnPropertyChanged(value, "DividerWidget");
			}
		}
	}

	public Widget ContainerWidget
	{
		get
		{
			return _containerWidget;
		}
		set
		{
			if (_containerWidget != value)
			{
				_containerWidget = value;
				OnPropertyChanged(value, "ContainerWidget");
			}
		}
	}

	public FillBarVerticalWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (FillWidget != null)
		{
			float y = FillWidget.ParentWidget.Size.Y;
			float num = 0f;
			if (_maxAmount != 0f)
			{
				num = Mathf.Clamp(Mathf.Clamp(_initialAmount, 0f, _maxAmount) / _maxAmount, 0f, 1f);
			}
			float num2 = (_isCurrentValueSet ? Mathf.Clamp(_currentAmount - _initialAmount, 0f - _maxAmount, _maxAmount) : 0f);
			float num3 = 0f;
			if (_maxAmount != 0f)
			{
				num3 = (_isCurrentValueSet ? Mathf.Clamp(num2 / _maxAmount, -1f, 1f) : 0f);
			}
			if (IsDirectionUpward)
			{
				FillWidget.VerticalAlignment = VerticalAlignment.Bottom;
				FillWidget.ScaledSuggestedHeight = num * (y - FillWidget.ScaledMarginTop - FillWidget.ScaledMarginBottom);
				if (ChangeWidget != null)
				{
					ChangeWidget.VerticalAlignment = VerticalAlignment.Bottom;
					ChangeWidget.ScaledSuggestedHeight = num3 * (y - ChangeWidget.ScaledMarginTop - ChangeWidget.ScaledMarginBottom);
					if (num3 >= 0f)
					{
						ChangeWidget.ScaledPositionYOffset = 0f - FillWidget.ScaledSuggestedHeight;
						ChangeWidget.Color = new Color(1f, 1f, 1f);
					}
					else
					{
						ChangeWidget.ScaledPositionYOffset = 0f - FillWidget.ScaledSuggestedHeight + ChangeWidget.ScaledSuggestedHeight;
						ChangeWidget.Color = new Color(1f, 0f, 0f);
					}
				}
			}
			else
			{
				FillWidget.VerticalAlignment = VerticalAlignment.Top;
				FillWidget.ScaledSuggestedHeight = num * (y - FillWidget.ScaledMarginTop - FillWidget.ScaledMarginBottom);
				if (ChangeWidget != null)
				{
					ChangeWidget.VerticalAlignment = VerticalAlignment.Top;
					ChangeWidget.ScaledSuggestedHeight = num3 * (y - ChangeWidget.ScaledMarginTop - ChangeWidget.ScaledMarginBottom);
					if (num3 >= 0f)
					{
						ChangeWidget.ScaledPositionYOffset = FillWidget.ScaledSuggestedHeight;
						ChangeWidget.Color = new Color(1f, 1f, 1f);
					}
					else
					{
						ChangeWidget.ScaledPositionYOffset = FillWidget.ScaledSuggestedHeight - ChangeWidget.ScaledSuggestedHeight;
						ChangeWidget.Color = new Color(1f, 0f, 0f);
					}
				}
			}
			if (ChangeWidget != null && DividerWidget != null)
			{
				DividerWidget.IsVisible = ChangeWidget != null && num3 != 0f;
			}
		}
		base.OnRender(twoDimensionContext, drawContext);
	}
}
