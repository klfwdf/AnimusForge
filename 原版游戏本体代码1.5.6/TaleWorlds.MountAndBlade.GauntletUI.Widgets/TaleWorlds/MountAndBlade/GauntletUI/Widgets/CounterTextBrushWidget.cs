using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class CounterTextBrushWidget : BrushWidget
{
	private readonly Text _text;

	private float _currentValue;

	private float _initialValue;

	private float _startTime;

	private bool _initValueSet;

	private float _targetValue;

	private float _minValue = float.MinValue;

	private float _maxValue = float.MaxValue;

	private bool _showSign;

	public bool _clamped;

	private bool _showFloatingPoint;

	public float CounterTime { get; set; } = 0.5f;

	[Editor(false)]
	public int IntTarget
	{
		get
		{
			return (int)Math.Round(_targetValue);
		}
		set
		{
			if (_targetValue != (float)value)
			{
				SetTargetValue(value);
			}
		}
	}

	[Editor(false)]
	public float FloatTarget
	{
		get
		{
			return _targetValue;
		}
		set
		{
			if (_targetValue != value)
			{
				SetTargetValue(value);
			}
		}
	}

	[Editor(false)]
	public float MinValue
	{
		get
		{
			return _minValue;
		}
		set
		{
			if (value != _minValue)
			{
				_minValue = value;
				OnPropertyChanged(value, "MinValue");
			}
		}
	}

	[Editor(false)]
	public float MaxValue
	{
		get
		{
			return _maxValue;
		}
		set
		{
			if (value != _maxValue)
			{
				_maxValue = value;
				OnPropertyChanged(value, "MaxValue");
			}
		}
	}

	[Editor(false)]
	public bool ShowSign
	{
		get
		{
			return _showSign;
		}
		set
		{
			if (_showSign != value)
			{
				_showSign = value;
			}
		}
	}

	[Editor(false)]
	public bool Clamped
	{
		get
		{
			return _clamped;
		}
		set
		{
			if (_clamped != value)
			{
				_clamped = value;
			}
		}
	}

	[Editor(false)]
	public bool ShowFloatingPoint
	{
		get
		{
			return _showFloatingPoint;
		}
		set
		{
			if (value != _showFloatingPoint)
			{
				_showFloatingPoint = value;
			}
		}
	}

	public CounterTextBrushWidget(UIContext context)
		: base(context)
	{
		FontFactory fontFactory = context.FontFactory;
		_text = new Text((int)base.Size.X, (int)base.Size.Y, fontFactory.DefaultFont, fontFactory.GetUsableFontForCharacter);
		base.LayoutImp = new TextLayout(_text);
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		if (TaleWorlds.Library.MathF.Abs(_targetValue - _currentValue) > float.Epsilon && TaleWorlds.Library.MathF.Abs(base.Context.EventManager.Time - _startTime) < CounterTime)
		{
			_currentValue = Mathf.Lerp(_currentValue, _targetValue, (base.Context.EventManager.Time - _startTime) / CounterTime);
			if (Clamped)
			{
				_currentValue = Mathf.Clamp(_currentValue, MinValue, MaxValue);
			}
			ForceSetValue(_currentValue);
		}
		else
		{
			ForceSetValue(_targetValue);
		}
		RefreshTextParameters();
		TextMaterial materialOriginal = base.BrushRenderer.CreateTextMaterial(drawContext);
		drawContext.Draw(_text, materialOriginal, in base.ParentWidget.AreaRect, in AreaRect);
	}

	private void SetText(string value)
	{
		SetMeasureAndLayoutDirty();
		_text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		if (ShowSign && _currentValue > 0f)
		{
			_text.Value = "+" + value;
		}
		else
		{
			_text.Value = value;
		}
		RefreshTextParameters();
	}

	public void SetInitialValue(float value)
	{
		_initialValue = value;
		_currentValue = value;
		_initValueSet = true;
	}

	private void SetTargetValue(float targetValue)
	{
		if (!_initValueSet)
		{
			_currentValue = targetValue;
			_initValueSet = true;
		}
		_initialValue = _currentValue;
		_startTime = base.Context.EventManager.Time;
		RefreshTextAnimation(targetValue - _targetValue);
		_targetValue = targetValue;
	}

	private void RefreshTextParameters()
	{
		float fontSize = (float)base.ReadOnlyBrush.FontSize * base._scaleToUse;
		_text.HorizontalAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
		_text.VerticalAlignment = base.ReadOnlyBrush.TextVerticalAlignment;
		_text.FontSize = fontSize;
		_text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		if (base.ReadOnlyBrush.Font != null)
		{
			_text.Font = base.ReadOnlyBrush.Font;
			return;
		}
		FontFactory fontFactory = base.Context.FontFactory;
		_text.Font = fontFactory.DefaultFont;
	}

	private void RefreshTextAnimation(float valueDifference)
	{
		if (valueDifference > 0f)
		{
			if (base.CurrentState == "Positive")
			{
				base.BrushRenderer.RestartAnimation();
			}
			else
			{
				SetState("Positive");
			}
		}
		else if (valueDifference < 0f)
		{
			if (base.CurrentState == "Negative")
			{
				base.BrushRenderer.RestartAnimation();
			}
			else
			{
				SetState("Negative");
			}
		}
		else
		{
			Debug.FailedAssert("Value change in party label cannot be 0", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\CounterTextBrushWidget.cs", "RefreshTextAnimation", 141);
		}
	}

	public void ForceSetValue(float value)
	{
		SetText(ShowFloatingPoint ? value.ToString("F2") : TaleWorlds.Library.MathF.Floor(value).ToString());
	}
}
