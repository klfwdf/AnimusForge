using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Notification;

public class MapNotificationContainerWidget : Widget
{
	private List<Widget> _newChildren;

	private TextWidget _moreTextWidget;

	private BrushWidget _moreTextWidgetContainer;

	private int _maxAmountOfNotificationsToShow = 5;

	[Editor(false)]
	public BrushWidget MoreTextWidgetContainer
	{
		get
		{
			return _moreTextWidgetContainer;
		}
		set
		{
			if (_moreTextWidgetContainer != value)
			{
				_moreTextWidgetContainer = value;
				OnPropertyChanged(value, "MoreTextWidgetContainer");
			}
		}
	}

	[Editor(false)]
	public TextWidget MoreTextWidget
	{
		get
		{
			return _moreTextWidget;
		}
		set
		{
			if (_moreTextWidget != value)
			{
				_moreTextWidget = value;
				OnPropertyChanged(value, "MoreTextWidget");
			}
		}
	}

	[Editor(false)]
	public int MaxAmountOfNotificationsToShow
	{
		get
		{
			return _maxAmountOfNotificationsToShow;
		}
		set
		{
			if (_maxAmountOfNotificationsToShow != value)
			{
				_maxAmountOfNotificationsToShow = value;
				OnPropertyChanged(value, "MaxAmountOfNotificationsToShow");
			}
		}
	}

	public MapNotificationContainerWidget(UIContext context)
		: base(context)
	{
		_newChildren = new List<Widget>();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_newChildren.Count > 0)
		{
			foreach (Widget newChild in _newChildren)
			{
				newChild.PositionYOffset = DetermineChildTargetYOffset(newChild, GetChildIndex(newChild));
			}
			DetermineChildrenVisibility();
			DetermineMoreTextStatus();
			DetermineNavigationIndicies();
			_newChildren.Clear();
		}
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			if (i < MaxAmountOfNotificationsToShow)
			{
				float end = DetermineChildTargetYOffset(child, i);
				child.PositionYOffset = LocalLerp(child.PositionYOffset, end, dt * 18f);
			}
		}
	}

	private void DetermineNavigationIndicies()
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			MapNotificationItemWidget mapNotificationItemWidget = GetChild(i) as MapNotificationItemWidget;
			if (i < MaxAmountOfNotificationsToShow)
			{
				mapNotificationItemWidget.NotificationRingWidget.GamepadNavigationIndex = base.ChildCount - 1 - i;
			}
			else
			{
				mapNotificationItemWidget.NotificationRingWidget.GamepadNavigationIndex = -1;
			}
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		_newChildren.Add(child);
	}

	protected override void OnAfterChildRemoved(Widget child, int previousIndexOfChild)
	{
		base.OnAfterChildRemoved(child, previousIndexOfChild);
		if (_newChildren.Contains(child))
		{
			_newChildren.Remove(child);
		}
		DetermineChildrenVisibility();
		DetermineMoreTextStatus();
		DetermineNavigationIndicies();
	}

	private void DetermineChildrenVisibility()
	{
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			bool isVisible = child.IsVisible;
			child.IsVisible = i < MaxAmountOfNotificationsToShow;
			if (!isVisible)
			{
				child.PositionYOffset = DetermineChildTargetYOffset(child, i);
			}
		}
	}

	private void DetermineMoreTextStatus()
	{
		MoreTextWidgetContainer.IsVisible = base.ChildCount > MaxAmountOfNotificationsToShow;
		if (MoreTextWidgetContainer.IsVisible)
		{
			MoreTextWidget.Text = "+" + (base.ChildCount - MaxAmountOfNotificationsToShow);
			MoreTextWidgetContainer.BrushRenderer.RestartAnimation();
			MoreTextWidget.BrushRenderer.RestartAnimation();
		}
	}

	private float DetermineChildTargetYOffset(Widget child, int childIndex)
	{
		if (childIndex < MaxAmountOfNotificationsToShow)
		{
			return (0f - child.Size.Y) * (float)childIndex * base._inverseScaleToUse;
		}
		return (0f - child.Size.Y) * (float)MaxAmountOfNotificationsToShow * base._inverseScaleToUse;
	}

	private float LocalLerp(float start, float end, float delta)
	{
		if (MathF.Abs(start - end) > float.Epsilon)
		{
			return (end - start) * delta + start;
		}
		return end;
	}
}
