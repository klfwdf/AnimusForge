using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Credits;

public class CreditsWidget : Widget
{
	private float _currentOffset = 1080f;

	private float _targetOffset = 1080f;

	private float _doNotScrollTimer;

	private Widget _rootItemWidget;

	private float _scrollPixelsPerSecond = 75f;

	private float _manualScrollWaitTimer = 1f;

	[Editor(false)]
	public Widget RootItemWidget
	{
		get
		{
			return _rootItemWidget;
		}
		set
		{
			if (_rootItemWidget != value)
			{
				_rootItemWidget = value;
				OnPropertyChanged(value, "RootItemWidget");
			}
		}
	}

	[Editor(false)]
	public float ScrollPixelsPerSecond
	{
		get
		{
			return _scrollPixelsPerSecond;
		}
		set
		{
			if (_scrollPixelsPerSecond != value)
			{
				_scrollPixelsPerSecond = value;
				OnPropertyChanged(value, "ScrollPixelsPerSecond");
			}
		}
	}

	[Editor(false)]
	public float ManualScrollWaitTimer
	{
		get
		{
			return _manualScrollWaitTimer;
		}
		set
		{
			if (_manualScrollWaitTimer != value)
			{
				_manualScrollWaitTimer = value;
				OnPropertyChanged(value, "ManualScrollWaitTimer");
			}
		}
	}

	public CreditsWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (RootItemWidget != null)
		{
			RootItemWidget.PositionYOffset = _currentOffset;
			if (_doNotScrollTimer > 0f)
			{
				_doNotScrollTimer -= dt;
			}
			else
			{
				_targetOffset -= dt * ScrollPixelsPerSecond;
			}
			_currentOffset = MathF.Lerp(_currentOffset, _targetOffset, MathF.Min(1f, dt * 10f));
			if (_currentOffset < (0f - RootItemWidget.Size.Y) * base._inverseScaleToUse)
			{
				_currentOffset = 1080f;
				_targetOffset = 1080f;
			}
		}
	}

	protected override bool OnPreviewMouseScroll()
	{
		return true;
	}

	protected override bool OnPreviewRightStickMovement()
	{
		return true;
	}

	protected override void OnMouseScroll()
	{
		base.OnMouseScroll();
		OnScroll(base.EventManager.DeltaMouseScroll * 0.5f);
	}

	protected override void OnRightStickMovement()
	{
		base.OnRightStickMovement();
		OnScroll(base.EventManager.RightStickVerticalScrollAmount);
	}

	private void OnScroll(float scrollAmount)
	{
		if (_targetOffset <= 0f || scrollAmount <= 0f)
		{
			_targetOffset += scrollAmount;
			_targetOffset = MathF.Min(_targetOffset, 0f);
		}
		_doNotScrollTimer = ManualScrollWaitTimer;
	}
}
