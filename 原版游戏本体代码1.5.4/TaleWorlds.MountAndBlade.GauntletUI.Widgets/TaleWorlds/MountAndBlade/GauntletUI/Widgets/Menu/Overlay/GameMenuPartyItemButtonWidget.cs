using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;

public class GameMenuPartyItemButtonWidget : ButtonWidget
{
	private bool _initialized;

	private int _relation;

	private string _location = "";

	private string _description = "";

	private string _profession = "";

	private string _power = "";

	private string _name = "";

	private Widget _contextMenu;

	private ImageIdentifierWidget _currentCharacterImageWidget;

	private OverlayPopupWidget _popupWidget;

	private bool _parentKnowsPopup = true;

	private bool _isMergedWithArmy = true;

	private bool _isPartyItem;

	public Brush PartyBackgroundBrush { get; set; }

	public Brush CharacterBackgroundBrush { get; set; }

	public ImageWidget BackgroundImageWidget { get; set; }

	[Editor(false)]
	public int Relation
	{
		get
		{
			return _relation;
		}
		set
		{
			if (_relation != value)
			{
				_relation = value;
				OnPropertyChanged(value, "Relation");
			}
		}
	}

	[Editor(false)]
	public string Location
	{
		get
		{
			return _location;
		}
		set
		{
			if (_location != value)
			{
				_location = value;
				OnPropertyChanged(value, "Location");
			}
		}
	}

	[Editor(false)]
	public string Power
	{
		get
		{
			return _power;
		}
		set
		{
			if (_power != value)
			{
				_power = value;
				OnPropertyChanged(value, "Power");
			}
		}
	}

	[Editor(false)]
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (_description != value)
			{
				_description = value;
				OnPropertyChanged(value, "Description");
			}
		}
	}

	[Editor(false)]
	public string Profession
	{
		get
		{
			return _profession;
		}
		set
		{
			if (_profession != value)
			{
				_profession = value;
				OnPropertyChanged(value, "Profession");
			}
		}
	}

	[Editor(false)]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (_name != value)
			{
				_name = value;
				OnPropertyChanged(value, "Name");
			}
		}
	}

	[Editor(false)]
	public bool IsMergedWithArmy
	{
		get
		{
			return _isMergedWithArmy;
		}
		set
		{
			if (_isMergedWithArmy != value)
			{
				_isMergedWithArmy = value;
			}
		}
	}

	[Editor(false)]
	public bool IsPartyItem
	{
		get
		{
			return _isPartyItem;
		}
		set
		{
			if (_isPartyItem != value)
			{
				_isPartyItem = value;
			}
		}
	}

	[Editor(false)]
	public Widget ContextMenu
	{
		get
		{
			return _contextMenu;
		}
		set
		{
			if (_contextMenu != value)
			{
				_contextMenu = value;
				OnPropertyChanged(value, "ContextMenu");
			}
		}
	}

	[Editor(false)]
	public ImageIdentifierWidget CurrentCharacterImageWidget
	{
		get
		{
			return _currentCharacterImageWidget;
		}
		set
		{
			if (_currentCharacterImageWidget != value)
			{
				_currentCharacterImageWidget = value;
				OnPropertyChanged(value, "CurrentCharacterImageWidget");
			}
		}
	}

	public GameMenuPartyItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private string GetRelationBackgroundName(int relation)
	{
		return "";
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (_popupWidget == null)
		{
			Widget widget = this;
			while (widget != base.EventManager.Root && _popupWidget == null && _parentKnowsPopup)
			{
				if (widget is OverlayBaseWidget)
				{
					OverlayBaseWidget overlayBaseWidget = (OverlayBaseWidget)widget;
					if (overlayBaseWidget.PopupWidget == null)
					{
						_parentKnowsPopup = false;
						break;
					}
					_popupWidget = overlayBaseWidget.PopupWidget;
				}
				else
				{
					widget = widget.ParentWidget;
				}
			}
		}
		if (CurrentCharacterImageWidget != null)
		{
			CurrentCharacterImageWidget.Brush.SaturationFactor = ((!IsMergedWithArmy) ? (-100) : 0);
			CurrentCharacterImageWidget.Brush.ValueFactor = ((!IsMergedWithArmy) ? (-20) : 0);
		}
		if (!_initialized)
		{
			BackgroundImageWidget.Brush = (IsPartyItem ? PartyBackgroundBrush : CharacterBackgroundBrush);
			_initialized = true;
		}
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (_popupWidget != null)
		{
			_popupWidget.SetCurrentCharacter(this);
		}
	}
}
