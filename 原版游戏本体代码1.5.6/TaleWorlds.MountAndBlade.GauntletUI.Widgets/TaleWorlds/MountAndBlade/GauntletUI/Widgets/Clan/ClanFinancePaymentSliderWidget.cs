using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Clan;

public class ClanFinancePaymentSliderWidget : SliderWidget
{
	private Widget _initialFillWidget;

	private Widget _newIncreaseFillWidget;

	private Widget _newDecreaseFillWidget;

	private Widget _currentRatioIndicatorWidget;

	private int _currentSize;

	private int _targetSize;

	private int _sizeLimit;

	[Editor(false)]
	public Widget InitialFillWidget
	{
		get
		{
			return _initialFillWidget;
		}
		set
		{
			if (_initialFillWidget != value)
			{
				_initialFillWidget = value;
			}
		}
	}

	[Editor(false)]
	public Widget NewIncreaseFillWidget
	{
		get
		{
			return _newIncreaseFillWidget;
		}
		set
		{
			if (_newIncreaseFillWidget != value)
			{
				_newIncreaseFillWidget = value;
			}
		}
	}

	[Editor(false)]
	public Widget NewDecreaseFillWidget
	{
		get
		{
			return _newDecreaseFillWidget;
		}
		set
		{
			if (_newDecreaseFillWidget != value)
			{
				_newDecreaseFillWidget = value;
			}
		}
	}

	[Editor(false)]
	public Widget CurrentRatioIndicatorWidget
	{
		get
		{
			return _currentRatioIndicatorWidget;
		}
		set
		{
			if (_currentRatioIndicatorWidget != value)
			{
				_currentRatioIndicatorWidget = value;
			}
		}
	}

	[Editor(false)]
	public int CurrentSize
	{
		get
		{
			return _currentSize;
		}
		set
		{
			if (_currentSize != value)
			{
				_currentSize = value;
			}
		}
	}

	[Editor(false)]
	public int TargetSize
	{
		get
		{
			return _targetSize;
		}
		set
		{
			if (_targetSize != value)
			{
				_targetSize = value;
			}
		}
	}

	[Editor(false)]
	public int SizeLimit
	{
		get
		{
			return _sizeLimit;
		}
		set
		{
			if (_sizeLimit != value)
			{
				_sizeLimit = value;
			}
		}
	}

	public ClanFinancePaymentSliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		CurrentRatioIndicatorWidget.ScaledPositionXOffset = Mathf.Clamp(base.Size.X * ((float)CurrentSize / (float)SizeLimit) - CurrentRatioIndicatorWidget.Size.X / 2f, 0f, base.Size.X);
		InitialFillWidget.ScaledPositionXOffset = CurrentRatioIndicatorWidget.PositionXOffset * base._scaleToUse + CurrentRatioIndicatorWidget.Size.X / 2f;
		InitialFillWidget.ScaledSuggestedWidth = base.Size.X - CurrentRatioIndicatorWidget.PositionXOffset * base._scaleToUse - CurrentRatioIndicatorWidget.Size.X / 2f;
		if (base.Handle.PositionXOffset > CurrentRatioIndicatorWidget.PositionXOffset)
		{
			NewIncreaseFillWidget.ScaledPositionXOffset = CurrentRatioIndicatorWidget.PositionXOffset * base._scaleToUse + CurrentRatioIndicatorWidget.Size.X / 2f;
			NewIncreaseFillWidget.ScaledSuggestedWidth = Mathf.Clamp((base.Handle.PositionXOffset - CurrentRatioIndicatorWidget.PositionXOffset) * base._scaleToUse, 0f, base.Size.X);
			NewDecreaseFillWidget.ScaledSuggestedWidth = 0f;
		}
		else if (base.Handle.PositionXOffset < CurrentRatioIndicatorWidget.PositionXOffset)
		{
			NewDecreaseFillWidget.ScaledPositionXOffset = base.Handle.PositionXOffset * base._scaleToUse + base.Handle.Size.X / 2f;
			NewDecreaseFillWidget.ScaledSuggestedWidth = Mathf.Clamp(CurrentRatioIndicatorWidget.PositionXOffset * base._scaleToUse + CurrentRatioIndicatorWidget.Size.X / 2f - (base.Handle.PositionXOffset * base._scaleToUse + base.Handle.Size.X / 2f), 0f, base.Size.X);
			NewIncreaseFillWidget.ScaledSuggestedWidth = 0f;
		}
		else
		{
			NewIncreaseFillWidget.ScaledSuggestedWidth = 0f;
			NewDecreaseFillWidget.ScaledSuggestedWidth = 0f;
		}
		base.OnLateUpdate(dt);
	}
}
