using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ItemTypeVisualBrushWidget : BrushWidget
{
	private const string ItemTypeBrushNameBase = "Item.Type.Icon.";

	private bool _isInitialized;

	private string _itemTypeAsString;

	[Editor(false)]
	public string ItemTypeAsString
	{
		get
		{
			return _itemTypeAsString;
		}
		set
		{
			if (value != _itemTypeAsString)
			{
				_itemTypeAsString = value;
				OnPropertyChanged(value, "ItemTypeAsString");
				_isInitialized = false;
			}
		}
	}

	public ItemTypeVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isInitialized)
		{
			return;
		}
		string brushName = null;
		if (!string.IsNullOrEmpty(ItemTypeAsString))
		{
			switch (ItemTypeAsString)
			{
			case "None":
			case "Spear":
			case "Javelin":
			case "Bow":
			case "Crossbow":
			case "Sword":
			case "Axe":
			case "Mace":
			case "ThrowingAxe":
			case "ThrowingKnife":
			case "Ammo":
			case "Shield":
			case "Mount":
			case "Banner":
			case "PickUp":
			case "Stone":
				brushName = "Item.Type.Icon." + ItemTypeAsString;
				break;
			default:
				Debug.FailedAssert("Unidentified item type to show type for: " + ItemTypeAsString, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\ItemTypeVisualBrushWidget.cs", "OnLateUpdate", 66);
				break;
			}
		}
		else
		{
			brushName = "Item.Type.Icon.None";
		}
		if (!string.IsNullOrEmpty(brushName))
		{
			base.Brush = base.Context.Brushes.SingleOrDefault((Brush b) => b.Name == brushName);
		}
		_isInitialized = true;
	}
}
