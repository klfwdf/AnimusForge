using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Scoreboard;

public class MultiplayerScoreboardAnimatedFillBarWidget : FillBarWidget
{
	public delegate void FullFillFinishedHandler(bool isPositive);

	private bool _isStarted;

	private bool _shouldStopLerping;

	private bool _isXPIncreasing;

	private bool _isFirstStepCalculated;

	private bool _isProgressCompleted;

	private float _ratioOfChangePerTick;

	private float _lerpRatioPerTick;

	private float _animationDelayTimer;

	private float _highlightedFillFromValue;

	private float _highlightedFillToValue;

	private float _normalFillFromValue;

	private float _normalFillToValue;

	private string _xpBarSoundEventName = "multiplayer/xpbar";

	private string _xpBarStopSoundEventName = "multiplayer/xpbar_stop";

	private bool _isStartRequested;

	private float _animationDelay;

	private float _animationFillSpeed;

	private int _timesOfFullFill;

	[Editor(false)]
	public bool IsStartRequested
	{
		get
		{
			return _isStartRequested;
		}
		set
		{
			if (value != _isStartRequested)
			{
				_isStartRequested = value;
				if (_isStartRequested)
				{
					Reset();
					StartAnimation();
				}
				OnPropertyChanged(value, "IsStartRequested");
			}
		}
	}

	[Editor(false)]
	public float AnimationDelay
	{
		get
		{
			return _animationDelay;
		}
		set
		{
			if (_animationDelay != value)
			{
				_animationDelay = value;
				OnPropertyChanged(value, "AnimationDelay");
			}
		}
	}

	[Editor(false)]
	public float AnimationFillSpeed
	{
		get
		{
			return _animationFillSpeed;
		}
		set
		{
			if (_animationFillSpeed != value)
			{
				_animationFillSpeed = value;
				OnPropertyChanged(value, "AnimationFillSpeed");
			}
		}
	}

	[Editor(false)]
	public int TimesOfFullFill
	{
		get
		{
			return _timesOfFullFill;
		}
		set
		{
			if (_timesOfFullFill != value)
			{
				_timesOfFullFill = value;
				OnPropertyChanged(value, "TimesOfFullFill");
			}
		}
	}

	public event FullFillFinishedHandler OnFullFillFinished;

	public MultiplayerScoreboardAnimatedFillBarWidget(UIContext context)
		: base(context)
	{
	}

	public void StartAnimation(float animationDelay = 0f)
	{
		if (base.FillWidget != null && base.ChangeWidget != null && !(TaleWorlds.Library.MathF.Abs(AnimationFillSpeed) <= float.Epsilon))
		{
			AnimationDelay = animationDelay;
			_ratioOfChangePerTick = AnimationFillSpeed;
			_isXPIncreasing = TimesOfFullFill > 0 || (TimesOfFullFill == 0 && base.CurrentAmountAsFloat > base.InitialAmountAsFloat);
			if (!_isStarted)
			{
				base.Context.TwoDimensionContext.CreateSoundEvent(_xpBarSoundEventName);
				base.Context.TwoDimensionContext.PlaySoundEvent(_xpBarSoundEventName);
			}
			_isStarted = true;
			CalculateTargetValues();
		}
	}

	public void Reset()
	{
		_normalFillFromValue = 0f;
		_normalFillToValue = 0f;
		_highlightedFillFromValue = 0f;
		_highlightedFillToValue = 0f;
		_ratioOfChangePerTick = AnimationFillSpeed;
		_animationDelayTimer = 0f;
		_lerpRatioPerTick = 0f;
		AnimationDelay = 0f;
		_isStarted = false;
		_shouldStopLerping = false;
		_isFirstStepCalculated = false;
		_isProgressCompleted = false;
		base.Context.TwoDimensionContext.StopAndRemoveSoundEvent(_xpBarSoundEventName);
	}

	protected override void OnUpdate(float dt)
	{
		if (!_isStarted || _isProgressCompleted)
		{
			return;
		}
		_animationDelayTimer += dt;
		if (_animationDelayTimer >= AnimationDelay)
		{
			_lerpRatioPerTick += dt * _ratioOfChangePerTick;
			_lerpRatioPerTick = Mathf.Clamp(_lerpRatioPerTick, 0f, 1f);
			if (_lerpRatioPerTick >= 1f && !_shouldStopLerping)
			{
				_lerpRatioPerTick = 0f;
				CalculateTargetValues();
			}
			else if (_lerpRatioPerTick >= 1f)
			{
				_isProgressCompleted = true;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		if (base.FillWidget != null)
		{
			ChangeFillAmountOfWidget(base.FillWidget, _normalFillFromValue, _normalFillToValue, _lerpRatioPerTick);
		}
		if (base.ChangeWidget != null)
		{
			ChangeFillAmountOfWidget(base.ChangeWidget, _highlightedFillFromValue, _highlightedFillToValue, _lerpRatioPerTick);
		}
		if (base.DividerWidget != null)
		{
			if (_isXPIncreasing && base.ChangeWidget != null)
			{
				base.DividerWidget.ScaledPositionXOffset = base.ChangeWidget.ScaledSuggestedWidth;
				base.DividerWidget.Color = Color.FromUint(uint.MaxValue);
			}
			else if (base.FillWidget != null)
			{
				base.DividerWidget.ScaledPositionXOffset = base.FillWidget.ScaledSuggestedWidth;
				base.DividerWidget.Color = Color.FromUint(4293185972u);
			}
			base.DividerWidget.ScaledPositionXOffset -= base.DividerWidget.Size.X;
		}
	}

	private void CalculateTargetValues()
	{
		SetRegularFromValues();
		bool isFirstStep = !_isFirstStepCalculated;
		if (!_isFirstStepCalculated)
		{
			float inputFromValue = Mathf.Clamp(Mathf.Clamp(base.InitialAmountAsFloat, 0f, base.MaxAmountAsFloat) / base.MaxAmountAsFloat, 0f, 1f);
			SetRegularFromValues(inputFromValue, useInputFromValue: true);
			_isFirstStepCalculated = true;
		}
		DecideNextStep(isFirstStep);
	}

	private void DecideNextStep(bool isFirstStep)
	{
		if (DoHaveFullFillStep())
		{
			FullFillStep();
		}
		else
		{
			LastFillStep();
		}
		if (!isFirstStep)
		{
			this.OnFullFillFinished?.Invoke(_isXPIncreasing);
		}
	}

	private void LastFillStep()
	{
		float num = Mathf.Clamp(Mathf.Clamp(base.CurrentAmountAsFloat, 0f, base.MaxAmountAsFloat) / base.MaxAmountAsFloat, 0f, 1f);
		if (_isXPIncreasing)
		{
			_highlightedFillToValue = num;
		}
		else
		{
			_normalFillToValue = num;
		}
		_shouldStopLerping = true;
		base.Context.TwoDimensionContext.StopAndRemoveSoundEvent(_xpBarSoundEventName);
		base.Context.TwoDimensionContext.PlaySound(_xpBarStopSoundEventName);
	}

	private void FullFillStep()
	{
		if (DoHaveFullFillStep())
		{
			if (_isXPIncreasing)
			{
				_highlightedFillToValue = 1f;
			}
			else
			{
				_normalFillToValue = 0f;
			}
			TimesOfFullFill -= Math.Sign(TimesOfFullFill);
		}
	}

	private void SetRegularFromValues(float inputFromValue = 0f, bool useInputFromValue = false)
	{
		_highlightedFillFromValue = (_normalFillFromValue = (useInputFromValue ? inputFromValue : ((float)((!_isXPIncreasing) ? 1 : 0))));
		StopMovementOfFillAmountAtFromValue(!_isXPIncreasing);
	}

	private bool DoHaveFullFillStep()
	{
		return Math.Abs(TimesOfFullFill) > 0;
	}

	private void StopMovementOfFillAmountAtFromValue(bool stopHighlightedFill)
	{
		if (stopHighlightedFill)
		{
			_highlightedFillToValue = _highlightedFillFromValue;
		}
		else
		{
			_normalFillToValue = _normalFillFromValue;
		}
	}

	private void ChangeFillAmountOfWidget(Widget fillWidget, float fromValue, float toValue, float stepSize)
	{
		float num = Mathf.Lerp(fromValue, toValue, stepSize);
		fillWidget.ScaledSuggestedWidth = num * fillWidget.ParentWidget.Size.X;
	}
}
