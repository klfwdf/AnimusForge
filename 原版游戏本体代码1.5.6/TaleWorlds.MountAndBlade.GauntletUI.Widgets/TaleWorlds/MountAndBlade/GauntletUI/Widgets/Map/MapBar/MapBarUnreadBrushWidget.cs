using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapBarUnreadBrushWidget : BrushWidget
{
	public enum AnimState
	{
		Idle,
		Start,
		FirstFrame,
		Playing
	}

	private AnimState _animState;

	private TextWidget _unreadTextWidget;

	public bool IsBannerNotification { get; set; }

	[Editor(false)]
	public TextWidget UnreadTextWidget
	{
		get
		{
			return _unreadTextWidget;
		}
		set
		{
			if (_unreadTextWidget != value)
			{
				_unreadTextWidget = value;
				OnPropertyChanged(value, "UnreadTextWidget");
				if (value != null)
				{
					value.boolPropertyChanged += UnreadTextWidgetOnPropertyChanged;
				}
			}
		}
	}

	public MapBarUnreadBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (base.IsVisible && _animState == AnimState.Idle)
		{
			_animState = AnimState.Start;
		}
		if (_animState == AnimState.Start)
		{
			_animState = AnimState.FirstFrame;
		}
		else if (_animState == AnimState.FirstFrame)
		{
			if (base.BrushRenderer.Brush == null)
			{
				_animState = AnimState.Start;
			}
			else
			{
				_animState = AnimState.Playing;
				base.BrushRenderer.RestartAnimation();
			}
		}
		if (IsBannerNotification && base.IsVisible && _animState == AnimState.Idle)
		{
			_animState = AnimState.Start;
		}
	}

	private void UnreadTextWidgetOnPropertyChanged(PropertyOwnerObject widget, string propertyName, bool propertyValue)
	{
		if (propertyName == "IsVisible")
		{
			base.IsVisible = propertyValue;
			_animState = (base.IsVisible ? AnimState.Start : AnimState.Idle);
		}
	}
}
