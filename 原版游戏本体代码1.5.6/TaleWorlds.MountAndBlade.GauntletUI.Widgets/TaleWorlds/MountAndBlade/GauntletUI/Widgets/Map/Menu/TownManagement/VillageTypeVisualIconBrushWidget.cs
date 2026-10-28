using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.Menu.TownManagement;

public class VillageTypeVisualIconBrushWidget : BrushWidget
{
	private bool _initialized;

	private int _villageType;

	[Editor(false)]
	public int VillageType
	{
		get
		{
			return _villageType;
		}
		set
		{
			if (_villageType != value)
			{
				_villageType = value;
				OnPropertyChanged(value, "VillageType");
			}
		}
	}

	public VillageTypeVisualIconBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_initialized)
		{
			switch (VillageType)
			{
			case 1:
				SetState("EuropeHorseRanch");
				break;
			case 2:
				SetState("BattanianHorseRanch");
				break;
			case 3:
				SetState("SteppeHorseRanch");
				break;
			case 4:
				SetState("DesertHorseRanch");
				break;
			case 5:
				SetState("WheatFarm");
				break;
			case 6:
				SetState("Lumberjack");
				break;
			case 7:
				SetState("ClayMine");
				break;
			case 8:
				SetState("SaltMine");
				break;
			case 9:
				SetState("IronMine");
				break;
			case 10:
				SetState("Fisherman");
				break;
			case 11:
				SetState("CattleRange");
				break;
			case 12:
				SetState("SheepFarm");
				break;
			case 13:
				SetState("VineYard");
				break;
			case 14:
				SetState("FlaxPlant");
				break;
			case 15:
				SetState("DateFarm");
				break;
			case 16:
				SetState("OliveTrees");
				break;
			case 17:
				SetState("SilkPlant");
				break;
			case 18:
				SetState("SilverMine");
				break;
			case 0:
				SetState("Default");
				break;
			default:
				Debug.FailedAssert("No workshop visual with this type: " + VillageType, "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Map\\Menu\\TownManagement\\VillageTypeVisualIconBrushWidget.cs", "OnLateUpdate", 103);
				SetState("Default");
				break;
			}
			_initialized = true;
		}
	}
}
