using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapInfoBarWidget : Widget
{
	public delegate void MapBarExtendStateChangeEvent(bool newState);

	private ButtonWidget _extendButtonWidget;

	private bool _isInfoBarExtended;

	[Editor(false)]
	public ButtonWidget ExtendButtonWidget
	{
		get
		{
			return _extendButtonWidget;
		}
		set
		{
			if (_extendButtonWidget != value)
			{
				_extendButtonWidget = value;
				OnPropertyChanged(value, "ExtendButtonWidget");
				if (!_extendButtonWidget.ClickEventHandlers.Contains(OnExtendButtonClick))
				{
					_extendButtonWidget.ClickEventHandlers.Add(OnExtendButtonClick);
				}
			}
		}
	}

	[Editor(false)]
	public bool IsInfoBarExtended
	{
		get
		{
			return _isInfoBarExtended;
		}
		set
		{
			if (_isInfoBarExtended != value)
			{
				_isInfoBarExtended = value;
				OnPropertyChanged(value, "IsInfoBarExtended");
				this.OnMapInfoBarExtendStateChange?.Invoke(IsInfoBarExtended);
			}
		}
	}

	public event MapBarExtendStateChangeEvent OnMapInfoBarExtendStateChange;

	public MapInfoBarWidget(UIContext context)
		: base(context)
	{
		AddState("Disabled");
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		RefreshBarExtendState();
	}

	private void OnExtendButtonClick(Widget widget)
	{
		IsInfoBarExtended = !IsInfoBarExtended;
		RefreshBarExtendState();
	}

	private void RefreshBarExtendState()
	{
		if (IsInfoBarExtended && base.CurrentState != "Extended")
		{
			SetState("Extended");
			RefreshVerticalVisual();
		}
		else if (!IsInfoBarExtended && base.CurrentState != "Default")
		{
			SetState("Default");
			RefreshVerticalVisual();
		}
	}

	private void RefreshVerticalVisual()
	{
		foreach (Style style in ExtendButtonWidget.Brush.Styles)
		{
			for (int i = 0; i < style.LayerCount; i++)
			{
				style.GetLayer(i).VerticalFlip = IsInfoBarExtended;
			}
		}
	}
}
