using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.GameMenu;

public class GameMenuWidget : Widget
{
	private bool _firstFrame = true;

	private const string _extendedState = "Default";

	private const string _hiddenState = "Disabled";

	private bool _isOverlayExtended = true;

	private NavigationScopeTargeter _scopeTargeter;

	private TextWidget _titleTextWidget;

	private Widget _titleContainerWidget;

	private bool _isNight;

	private bool _isEncounterMenu;

	private Widget _overlay;

	private ButtonWidget _extendButtonWidget;

	private BrushWidget _extendButtonArrowWidget;

	private ListPanel _optionItemsList;

	private string _spriteName;

	private string _menuId;

	private Brush _overriddenSpriteMapBrush;

	public int EncounterModeMenuWidth { get; set; }

	public int EncounterModeMenuHeight { get; set; }

	public int EncounterModeMenuMarginTop { get; set; }

	public int NormalModeMenuWidth { get; set; }

	public int NormalModeMenuHeight { get; set; }

	public int NormalModeMenuMarginTop { get; set; }

	public bool IsOverlayExtended
	{
		get
		{
			return _isOverlayExtended;
		}
		private set
		{
			if (value != _isOverlayExtended)
			{
				_isOverlayExtended = value;
				UpdateOverlayState();
			}
		}
	}

	[Editor(false)]
	public NavigationScopeTargeter ScopeTargeter
	{
		get
		{
			return _scopeTargeter;
		}
		set
		{
			if (_scopeTargeter != value)
			{
				_scopeTargeter = value;
				OnPropertyChanged(value, "ScopeTargeter");
			}
		}
	}

	[Editor(false)]
	public TextWidget TitleTextWidget
	{
		get
		{
			return _titleTextWidget;
		}
		set
		{
			if (_titleTextWidget != value)
			{
				_titleTextWidget = value;
				OnPropertyChanged(value, "TitleTextWidget");
				if (value != null)
				{
					value.PropertyChanged += TitleTextWidget_PropertyChanged;
				}
			}
		}
	}

	[Editor(false)]
	public Widget TitleContainerWidget
	{
		get
		{
			return _titleContainerWidget;
		}
		set
		{
			if (_titleContainerWidget != value)
			{
				_titleContainerWidget = value;
				OnPropertyChanged(value, "TitleContainerWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsNight
	{
		get
		{
			return _isNight;
		}
		set
		{
			if (_isNight != value)
			{
				_isNight = value;
				OnPropertyChanged(value, "IsNight");
			}
		}
	}

	[Editor(false)]
	public bool IsEncounterMenu
	{
		get
		{
			return _isEncounterMenu;
		}
		set
		{
			if (_isEncounterMenu != value)
			{
				_isEncounterMenu = value;
				OnPropertyChanged(value, "IsEncounterMenu");
				RefreshSize();
			}
		}
	}

	[Editor(false)]
	public Widget Overlay
	{
		get
		{
			return _overlay;
		}
		set
		{
			if (value != _overlay)
			{
				_overlay = value;
				OnPropertyChanged(value, "Overlay");
			}
		}
	}

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
				if (_extendButtonWidget != null)
				{
					_extendButtonWidget.ClickEventHandlers.Add(OnExtendButtonClick);
				}
			}
		}
	}

	[Editor(false)]
	public BrushWidget ExtendButtonArrowWidget
	{
		get
		{
			return _extendButtonArrowWidget;
		}
		set
		{
			if (value != _extendButtonArrowWidget)
			{
				_extendButtonArrowWidget = value;
				OnPropertyChanged(value, "ExtendButtonArrowWidget");
			}
		}
	}

	[Editor(false)]
	public ListPanel OptionItemsList
	{
		get
		{
			return _optionItemsList;
		}
		set
		{
			if (value != _optionItemsList)
			{
				_optionItemsList = value;
				_optionItemsList.ItemAddEventHandlers.Add(OnOptionAdded);
				_optionItemsList.ItemRemoveEventHandlers.Add(OnOptionRemoved);
				OnPropertyChanged(value, "OptionItemsList");
			}
		}
	}

	[Editor(false)]
	public string SpriteName
	{
		get
		{
			return _spriteName;
		}
		set
		{
			if (value != _spriteName)
			{
				_spriteName = value;
				OnPropertyChanged(value, "SpriteName");
			}
		}
	}

	[Editor(false)]
	public string MenuId
	{
		get
		{
			return _menuId;
		}
		set
		{
			if (value != _menuId)
			{
				_menuId = value;
				OnPropertyChanged(value, "MenuId");
			}
		}
	}

	[Editor(false)]
	public Brush OverriddenSpriteMapBrush
	{
		get
		{
			return _overriddenSpriteMapBrush;
		}
		set
		{
			if (value != _overriddenSpriteMapBrush)
			{
				_overriddenSpriteMapBrush = value;
				OnPropertyChanged(value, "OverriddenSpriteMapBrush");
			}
		}
	}

	public GameMenuWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		if (!_firstFrame)
		{
			if (IsNight)
			{
				base.Color = Color.Lerp(base.Color, new Color(0.23921569f, 23f / 51f, 0.8f), dt);
			}
			else
			{
				base.Color = Color.Lerp(base.Color, Color.White, dt);
			}
		}
		else
		{
			if (IsNight)
			{
				base.Color = new Color(0.23921569f, 23f / 51f, 0.8f);
			}
			else
			{
				base.Color = Color.White;
			}
			_firstFrame = false;
			RefreshSize();
		}
		if (base.Sprite == null && OverriddenSpriteMapBrush != null && SpriteName != null)
		{
			base.Sprite = OverriddenSpriteMapBrush.GetLayer(SpriteName)?.Sprite;
		}
		base.OnLateUpdate(dt);
	}

	private void RefreshSize()
	{
		base.SuggestedWidth = (IsEncounterMenu ? EncounterModeMenuWidth : NormalModeMenuWidth);
		base.SuggestedHeight = (IsEncounterMenu ? EncounterModeMenuHeight : NormalModeMenuHeight);
		base.ScaledSuggestedWidth = base.SuggestedWidth * base._scaleToUse;
		base.ScaledSuggestedHeight = base.SuggestedHeight * base._scaleToUse;
		base.MarginTop = (IsEncounterMenu ? EncounterModeMenuMarginTop : NormalModeMenuMarginTop);
		ExtendButtonWidget.MarginTop = base.MarginTop;
	}

	private void OnExtendButtonClick(Widget button)
	{
		IsOverlayExtended = !IsOverlayExtended;
	}

	public void UpdateOverlayState()
	{
		ScopeTargeter.IsScopeEnabled = _isOverlayExtended;
		string state = (_isOverlayExtended ? "Default" : "Disabled");
		Overlay.SetState(state);
		foreach (Style style in ExtendButtonArrowWidget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].HorizontalFlip = !_isOverlayExtended;
			}
		}
	}

	private void TitleTextWidget_PropertyChanged(PropertyOwnerObject widget, string propertyName, object propertyValue)
	{
		if (propertyName == "Text")
		{
			TitleContainerWidget.IsVisible = !string.IsNullOrEmpty((string)propertyValue);
			IsOverlayExtended = true;
		}
	}

	private void OnOptionAdded(Widget parentWidget, Widget childWidget)
	{
		GameMenuItemWidget obj = childWidget as GameMenuItemWidget;
		obj.OnOptionStateChanged = (Action)Delegate.Combine(obj.OnOptionStateChanged, new Action(OnOptionStateChanged));
		IsOverlayExtended = true;
	}

	public void OnOptionStateChanged()
	{
		IsOverlayExtended = true;
	}

	private void OnOptionRemoved(Widget widget, Widget child)
	{
		IsOverlayExtended = true;
	}
}
