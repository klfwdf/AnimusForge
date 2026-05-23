using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class CircularAutoScrollablePanelWidget : Widget
{
	public enum ScrollMovementType
	{
		ByPixels,
		ByRatio
	}

	private float _currentScrollValue;

	private bool _isScrolling;

	private bool _isIdle;

	private float _idleTimer;

	private int _direction = 1;

	private float _maxScroll;

	private bool _shouldResetImmediately = true;

	public Widget InnerPanel { get; set; }

	public Widget ClipRect { get; set; }

	public float ScrollRatioPerSecond { get; set; }

	public float ScrollPixelsPerSecond { get; set; }

	public float IdleTime { get; set; }

	public bool AutoScrollWhenSelected { get; set; }

	public bool AutoScroll { get; set; }

	public ScrollMovementType ScrollType { get; set; }

	public bool ShouldResetImmediately
	{
		get
		{
			return _shouldResetImmediately;
		}
		set
		{
			if (value != _shouldResetImmediately)
			{
				_shouldResetImmediately = value;
			}
		}
	}

	public CircularAutoScrollablePanelWidget(UIContext context)
		: base(context)
	{
		IdleTime = 0.8f;
		ScrollRatioPerSecond = 0.25f;
		ScrollPixelsPerSecond = 35f;
		ScrollType = ScrollMovementType.ByPixels;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		_isScrolling = _isScrolling || (base.CurrentState == "Selected" && AutoScrollWhenSelected) || AutoScroll;
		_maxScroll = 0f;
		Widget innerPanel = InnerPanel;
		if (innerPanel != null && innerPanel.Size.Y > 0f)
		{
			Widget clipRect = ClipRect;
			if (clipRect != null && clipRect.Size.Y > 0f && InnerPanel.Size.Y > ClipRect.Size.Y)
			{
				_maxScroll = InnerPanel.Size.Y - ClipRect.Size.Y;
			}
		}
		if (_isScrolling && !_isIdle)
		{
			ScrollToDirection(_direction, dt);
			if (_currentScrollValue.ApproximatelyEqualsTo(0f) || _currentScrollValue.ApproximatelyEqualsTo(_maxScroll))
			{
				_isIdle = true;
				_idleTimer = 0f;
				_direction *= -1;
			}
		}
		else if (_isScrolling && _isIdle)
		{
			ScrollToDirection(0, dt);
			_idleTimer += dt;
			if (_idleTimer > IdleTime)
			{
				_isIdle = false;
				_idleTimer = 0f;
			}
		}
		else if (_currentScrollValue > 0f)
		{
			ScrollToDirection(-1, dt);
		}
	}

	private void ScrollToDirection(int direction, float dt)
	{
		float num = 0f;
		if (ScrollType == ScrollMovementType.ByPixels)
		{
			num = ScrollPixelsPerSecond;
		}
		else if (ScrollType == ScrollMovementType.ByRatio)
		{
			num = ScrollRatioPerSecond * _maxScroll;
		}
		_currentScrollValue += num * (float)direction * dt;
		_currentScrollValue = MathF.Clamp(_currentScrollValue, 0f, _maxScroll);
		InnerPanel.ScaledPositionYOffset = 0f - _currentScrollValue;
	}

	protected override void OnMouseScroll()
	{
		base.OnMouseScroll();
		if (!AutoScroll)
		{
			_isScrolling = false;
			float num = ((ScrollPixelsPerSecond != 0f) ? (ScrollPixelsPerSecond * 0.2f) : 10f);
			float num2 = base.EventManager.DeltaMouseScroll * num;
			_currentScrollValue += num2;
			InnerPanel.ScaledPositionYOffset = 0f - _currentScrollValue;
		}
	}

	public void SetScrollMouse()
	{
		OnMouseScroll();
	}

	protected override void OnHoverBegin()
	{
		base.OnHoverBegin();
		if (!AutoScroll && !_isScrolling)
		{
			_isScrolling = true;
			_isIdle = false;
			_direction = 1;
			_idleTimer = 0f;
		}
	}

	public void SetHoverBegin()
	{
		OnHoverBegin();
	}

	protected override void OnHoverEnd()
	{
		base.OnHoverEnd();
		if (!AutoScroll && _isScrolling)
		{
			_isScrolling = false;
			_direction = -1;
			if (_isIdle && _currentScrollValue < float.Epsilon)
			{
				_currentScrollValue = 1f;
			}
			if (ShouldResetImmediately)
			{
				_currentScrollValue = 0f;
				InnerPanel.ScaledPositionYOffset = 0f;
			}
		}
	}

	public void SetHoverEnd()
	{
		OnHoverEnd();
	}
}
