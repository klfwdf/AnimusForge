using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class ArmyOverlayWidget : OverlayBaseWidget
{
	private bool _isOverlayExtended = true;

	private int _armyItemCount;

	private bool _initialized;

	private Widget _overlay;

	private bool _isInfoBarExtended;

	private ButtonWidget _extendButton;

	private GridWidget _armyListGridWidget;

	private ContainerPageControlWidget _pageControlWidget;

	[Editor(false)]
	public Widget Overlay
	{
		get
		{
			return _overlay;
		}
		set
		{
			if (_overlay != value)
			{
				_overlay = value;
				OnPropertyChanged(value, "Overlay");
			}
		}
	}

	[Editor(false)]
	public GridWidget ArmyListGridWidget
	{
		get
		{
			return _armyListGridWidget;
		}
		set
		{
			if (_armyListGridWidget != value)
			{
				_armyListGridWidget = value;
				OnPropertyChanged(value, "ArmyListGridWidget");
			}
		}
	}

	[Editor(false)]
	public ButtonWidget ExtendButton
	{
		get
		{
			return _extendButton;
		}
		set
		{
			if (_extendButton != value)
			{
				_extendButton = value;
				OnPropertyChanged(value, "ExtendButton");
				if (_extendButton != null)
				{
					_extendButton.ClickEventHandlers.Add(OnExtendButtonClick);
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
			}
		}
	}

	[Editor(false)]
	public ContainerPageControlWidget PageControlWidget
	{
		get
		{
			return _pageControlWidget;
		}
		set
		{
			if (value != _pageControlWidget)
			{
				if (_pageControlWidget != null)
				{
					_pageControlWidget.OnPageCountChanged -= OnArmyListPageCountChanged;
				}
				_pageControlWidget = value;
				_pageControlWidget.OnPageCountChanged += OnArmyListPageCountChanged;
				OnPropertyChanged(value, "PageControlWidget");
			}
		}
	}

	public ArmyOverlayWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		int num = _armyListGridWidget.Children.Count((Widget c) => c.IsVisible);
		if (num != _armyItemCount)
		{
			Overlay.SetState("Reset");
			_armyItemCount = num;
		}
		RefreshOverlayExtendState(!_initialized);
		UpdateExtendButtonVisual();
		_initialized = true;
	}

	private void RefreshOverlayExtendState(bool forceSetPosition)
	{
		string text = (_isInfoBarExtended ? "MapExtended" : "MapNormal");
		string text2 = (_isOverlayExtended ? "OverlayExtended" : "OverlayNormal");
		if (!(text + text2 != Overlay.CurrentState))
		{
			return;
		}
		if (!_isOverlayExtended)
		{
			if (forceSetPosition)
			{
				Overlay.VisualDefinition.VisualStates.TryGetValue(text + text2, out var value);
				Overlay.PositionYOffset = value.PositionYOffset;
			}
		}
		else
		{
			float y = ArmyListGridWidget.Size.Y;
			float positionYOffset;
			if (_isInfoBarExtended)
			{
				Overlay.VisualDefinition.VisualStates.TryGetValue("MapExtendedOverlayNormal", out var value2);
				positionYOffset = value2.PositionYOffset - y * base._inverseScaleToUse;
			}
			else
			{
				Overlay.VisualDefinition.VisualStates.TryGetValue("MapNormalOverlayNormal", out var value3);
				positionYOffset = value3.PositionYOffset - y * base._inverseScaleToUse;
			}
			if (forceSetPosition)
			{
				Overlay.PositionYOffset = positionYOffset;
			}
			Overlay.VisualDefinition.VisualStates.TryGetValue(text + text2, out var value4);
			value4.PositionYOffset = positionYOffset;
		}
		Overlay.SetState(text + text2);
	}

	private void UpdateExtendButtonVisual()
	{
		foreach (Style style in ExtendButton.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].VerticalFlip = _isOverlayExtended;
			}
		}
	}

	private void OnExtendButtonClick(Widget button)
	{
		_isOverlayExtended = !_isOverlayExtended;
		UpdateExtendButtonVisual();
		RefreshOverlayExtendState(forceSetPosition: false);
	}

	private void OnArmyListPageCountChanged()
	{
		if (PageControlWidget.PageCount == 1)
		{
			Overlay.PositionXOffset = 40f;
			ExtendButton.PositionXOffset = -40f;
		}
		else
		{
			Overlay.PositionXOffset = 0f;
			ExtendButton.PositionXOffset = 0f;
		}
	}
}
