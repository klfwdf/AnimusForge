using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Menu.TownManagement;

public class ShopVisualIconBrushWidget : BrushWidget
{
	private bool _initialized;

	private string _shopId;

	[Editor(false)]
	public string ShopId
	{
		get
		{
			return _shopId;
		}
		set
		{
			if (_shopId != value)
			{
				_shopId = value;
				OnPropertyChanged(value, "ShopId");
			}
		}
	}

	public ShopVisualIconBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			switch (ShopId)
			{
			case "mill":
				SetState("Mill");
				break;
			case "brewery":
				SetState("Brewery");
				break;
			case "velvet_weavery":
				SetState("VelvetWeavery");
				break;
			case "linen_weavery":
				SetState("LinenWeavery");
				break;
			case "wine_press":
				SetState("WinePress");
				break;
			case "pottery_shop":
				SetState("PotteryShop");
				break;
			case "olive_press":
				SetState("OlivePress");
				break;
			case "wool_weavery":
				SetState("WoolWeavery");
				break;
			case "tannery":
				SetState("Tannery");
				break;
			case "wood_WorkshopType":
				SetState("WoodWorkshop");
				break;
			case "smithy":
				SetState("Smithy");
				break;
			case "stable":
				SetState("Stable");
				break;
			case "silversmithy":
				SetState("SilverSmithy");
				break;
			case "empty":
				SetState("Default");
				break;
			default:
				SetState("Default");
				Debug.FailedAssert("No workshop visual with this type: " + ShopId, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Map\\Menu\\TownManagement\\ShopVisualIconBrushWidget.cs", "OnLateUpdate", 68);
				break;
			}
			_initialized = true;
		}
	}
}
