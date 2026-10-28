using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public abstract class InventoryItemButtonWidget : ButtonWidget
{
	private bool _isRightSide;

	private string _itemType;

	private int _equipmentIndex;

	private InventoryScreenWidget _screenWidget;

	[Editor(false)]
	public bool IsRightSide
	{
		get
		{
			return _isRightSide;
		}
		set
		{
			if (_isRightSide != value)
			{
				_isRightSide = value;
				OnPropertyChanged(value, "IsRightSide");
			}
		}
	}

	[Editor(false)]
	public string ItemType
	{
		get
		{
			return _itemType;
		}
		set
		{
			if (_itemType != value)
			{
				_itemType = value;
				OnPropertyChanged(value, "ItemType");
				ItemTypeUpdated();
			}
		}
	}

	[Editor(false)]
	public int EquipmentIndex
	{
		get
		{
			return _equipmentIndex;
		}
		set
		{
			if (_equipmentIndex != value)
			{
				_equipmentIndex = value;
				OnPropertyChanged(value, "EquipmentIndex");
			}
		}
	}

	public InventoryScreenWidget ScreenWidget
	{
		get
		{
			if (_screenWidget == null)
			{
				AssignScreenWidget();
			}
			return _screenWidget;
		}
	}

	protected InventoryItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnDragBegin()
	{
		ScreenWidget?.ItemWidgetDragBegin(this);
		base.OnDragBegin();
	}

	protected override bool OnDrop()
	{
		ScreenWidget?.ItemWidgetDrop(this);
		return base.OnDrop();
	}

	private void AssignScreenWidget()
	{
		Widget widget = this;
		while (widget != base.EventManager.Root && _screenWidget == null)
		{
			if (widget is InventoryScreenWidget)
			{
				_screenWidget = (InventoryScreenWidget)widget;
			}
			else
			{
				widget = widget.ParentWidget;
			}
		}
	}

	private void ItemTypeUpdated()
	{
		AudioProperty audioProperty = base.Brush.SoundProperties.GetEventAudioProperty("DragEnd");
		if (audioProperty == null)
		{
			audioProperty = new AudioProperty();
			base.Brush.SoundProperties.AddEventSound("DragEnd", audioProperty);
		}
		audioProperty.AudioName = GetSound(ItemType);
	}

	private string GetSound(string typeID)
	{
		switch (typeID)
		{
		case "Horse":
			return "inventory/horse";
		case "OneHandedWeapon":
			return "inventory/onehanded";
		case "TwoHandedWeapon":
			return "inventory/twohanded";
		case "Polearm":
			return "inventory/polearm";
		case "Arrows":
		case "Bolts":
		case "SlingStones":
			return "inventory/quiver";
		case "Shield":
			return "inventory/shield";
		case "Bow":
			return "inventory/bow";
		case "Crossbow":
			return "inventory/crossbow";
		case "Sling":
			return "inventory/bow";
		case "Thrown":
			return "inventory/throwing";
		case "Goods":
			return "inventory/sack";
		case "HeadArmor":
			return "inventory/helmet";
		case "BodyArmor":
		case "ChestArmor":
		case "Cape":
			return "inventory/leather";
		case "LegArmor":
		case "HandArmor":
			return "inventory/leather_lite";
		case "Banner":
			return "inventory/perk";
		case "Animal":
			return "inventory/animal";
		case "Book":
			return "inventory/book";
		case "HorseHarness":
			return "inventory/horsearmor";
		case "Pistol":
		case "Musket":
		case "Bullets":
			return "inventory/leather";
		default:
			return "inventory/leather";
		}
	}
}
