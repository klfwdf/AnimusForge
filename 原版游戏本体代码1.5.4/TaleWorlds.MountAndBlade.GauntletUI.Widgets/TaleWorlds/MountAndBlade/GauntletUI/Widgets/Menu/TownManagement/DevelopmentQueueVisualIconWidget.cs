using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class DevelopmentQueueVisualIconWidget : Widget
{
	public enum AnimState
	{
		Idle,
		Start,
		Starting,
		Playing
	}

	private AnimState _animState;

	private float _tickCount;

	private int _queueIndex = -1;

	private Widget _queueIconWidget;

	private BrushWidget _inProgressIconWidget;

	[Editor(false)]
	public int QueueIndex
	{
		get
		{
			return _queueIndex;
		}
		set
		{
			if (_queueIndex != value)
			{
				_queueIndex = value;
				OnPropertyChanged(value, "QueueIndex");
				UpdateVisual(value);
			}
		}
	}

	[Editor(false)]
	public Widget QueueIconWidget
	{
		get
		{
			return _queueIconWidget;
		}
		set
		{
			if (_queueIconWidget != value)
			{
				_queueIconWidget = value;
				OnPropertyChanged(value, "QueueIconWidget");
				UpdateVisual(QueueIndex);
			}
		}
	}

	[Editor(false)]
	public BrushWidget InProgressIconWidget
	{
		get
		{
			return _inProgressIconWidget;
		}
		set
		{
			if (_inProgressIconWidget != value)
			{
				_inProgressIconWidget = value;
				OnPropertyChanged(value, "InProgressIconWidget");
				UpdateVisual(QueueIndex);
			}
		}
	}

	public DevelopmentQueueVisualIconWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_animState == AnimState.Start)
		{
			_tickCount += 1f;
			if (_tickCount > 20f)
			{
				_animState = AnimState.Starting;
			}
		}
		else if (_animState == AnimState.Starting)
		{
			InProgressIconWidget?.BrushRenderer.RestartAnimation();
			_animState = AnimState.Playing;
		}
	}

	private void UpdateVisual(int index)
	{
		if (InProgressIconWidget != null && QueueIconWidget != null)
		{
			base.IsVisible = index >= 0;
			InProgressIconWidget.IsVisible = index == 0;
			_animState = (InProgressIconWidget.IsVisible ? AnimState.Start : AnimState.Idle);
			_tickCount = 0f;
			QueueIconWidget.IsVisible = index > 0;
		}
	}
}
