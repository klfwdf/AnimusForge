using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class AnimatedNumberTextWidget : TextWidget
{
	private int _currentNumber;

	private bool _isAnimationActive;

	private float _timePassed;

	private float _animationDelay;

	private float _animationDuration;

	private int _referenceNumber;

	private int _number;

	private bool _autoStart;

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
	public float AnimationDuration
	{
		get
		{
			return _animationDuration;
		}
		set
		{
			if (_animationDuration != value)
			{
				_animationDuration = value;
				OnPropertyChanged(value, "AnimationDuration");
			}
		}
	}

	[Editor(false)]
	public int ReferenceNumber
	{
		get
		{
			return _referenceNumber;
		}
		set
		{
			if (_referenceNumber != value)
			{
				_referenceNumber = value;
				OnPropertyChanged(value, "ReferenceNumber");
			}
		}
	}

	[Editor(false)]
	public int Number
	{
		get
		{
			return _number;
		}
		set
		{
			if (_number != value)
			{
				_number = value;
				OnPropertyChanged(value, "Number");
				NumberChanged();
			}
		}
	}

	[Editor(false)]
	public bool AutoStart
	{
		get
		{
			return _autoStart;
		}
		set
		{
			if (_autoStart != value)
			{
				_autoStart = value;
				OnPropertyChanged(value, "AutoStart");
			}
		}
	}

	public AnimatedNumberTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		if (!_isAnimationActive)
		{
			return;
		}
		_timePassed += dt;
		if (_timePassed >= AnimationDelay)
		{
			float num = _timePassed - AnimationDelay;
			_currentNumber = (int)(num / AnimationDuration * (float)ReferenceNumber);
			_currentNumber = MathF.Min(_currentNumber, Number);
			if (_currentNumber == Number)
			{
				_isAnimationActive = false;
			}
			base.IntText = _currentNumber;
		}
	}

	public void StartAnimation()
	{
		if (!(AnimationDuration <= 0f) && ReferenceNumber > 0)
		{
			_isAnimationActive = true;
			_currentNumber = 0;
			base.IntText = 0;
			_timePassed = 0f;
		}
	}

	public void Reset()
	{
		_isAnimationActive = false;
		_currentNumber = 0;
		base.IntText = 0;
	}

	private void NumberChanged()
	{
		if (AutoStart)
		{
			StartAnimation();
		}
	}
}
