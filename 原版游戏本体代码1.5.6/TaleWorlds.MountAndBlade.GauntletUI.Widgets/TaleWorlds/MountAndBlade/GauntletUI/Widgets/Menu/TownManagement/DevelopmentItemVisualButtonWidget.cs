using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.TownManagement;

public class DevelopmentItemVisualButtonWidget : ButtonWidget
{
	private const string _defaultBuildingSpriteName = "building_default";

	private bool _changedVisualToSmallVariant;

	private bool _useSmallVariant;

	private bool _isDaily;

	private string _spriteCode;

	private Widget _developmentFrontVisualWidget;

	[Editor(false)]
	public bool UseSmallVariant
	{
		get
		{
			return _useSmallVariant;
		}
		set
		{
			if (_useSmallVariant != value)
			{
				_useSmallVariant = value;
				OnPropertyChanged(value, "UseSmallVariant");
			}
		}
	}

	[Editor(false)]
	public bool IsDaily
	{
		get
		{
			return _isDaily;
		}
		set
		{
			if (_isDaily != value)
			{
				_isDaily = value;
				OnPropertyChanged(value, "IsDaily");
			}
		}
	}

	[Editor(false)]
	public string SpriteCode
	{
		get
		{
			return _spriteCode;
		}
		set
		{
			if (_spriteCode != value)
			{
				_spriteCode = value;
				OnPropertyChanged(value, "SpriteCode");
				_changedVisualToSmallVariant = false;
			}
		}
	}

	[Editor(false)]
	public Widget DevelopmentFrontVisualWidget
	{
		get
		{
			return _developmentFrontVisualWidget;
		}
		set
		{
			if (_developmentFrontVisualWidget != value)
			{
				_developmentFrontVisualWidget = value;
				OnPropertyChanged(value, "DevelopmentFrontVisualWidget");
			}
		}
	}

	public DevelopmentItemVisualButtonWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!_changedVisualToSmallVariant)
		{
			string text = DetermineSpriteImageFromSpriteCode(SpriteCode, UseSmallVariant);
			base.Sprite = base.Context.SpriteData.GetSprite((!string.IsNullOrEmpty(text)) ? text : "building_default");
			if (!IsDaily && DevelopmentFrontVisualWidget != null)
			{
				DevelopmentFrontVisualWidget.Sprite = base.Sprite;
			}
			_changedVisualToSmallVariant = true;
		}
	}

	private string DetermineSpriteImageFromSpriteCode(string spriteCode, bool useSmallVariant)
	{
		string text = "";
		switch (spriteCode)
		{
		case "building_shipyard":
			text = "building_shipyard";
			break;
		case "building_settlement_fortifications":
		case "building_castle_fortifications":
			text = "building_fortifications";
			break;
		case "building_settlement_barracks":
		case "building_castle_barracks":
			text = "building_barracks";
			break;
		case "building_settlement_training_fields":
		case "building_castle_training_fields":
			text = "building_training_fields";
			break;
		case "building_settlement_guard_house":
		case "building_castle_guard_house":
			text = "building_guard_house";
			break;
		case "building_settlement_siege_workshop":
		case "building_castle_siege_workshop":
			text = "building_siege_workshop";
			break;
		case "building_settlement_tax_office":
			text = "building_tax_office";
			break;
		case "building_castle_castallans_office":
			text = "building_wardens_office";
			break;
		case "building_settlement_marketplace":
			text = "building_marketplace";
			break;
		case "building_settlement_warehouse":
		case "building_castle_granary":
			text = "building_granary";
			break;
		case "building_castle_craftmans_quarters":
			text = "building_workshop";
			break;
		case "building_castle_farmlands":
			text = "building_gardens";
			break;
		case "building_settlement_mason":
		case "building_castle_mason":
			text = "building_masonry";
			break;
		case "building_settlement_waterworks":
			text = "building_waterworks";
			break;
		case "building_settlement_courthouse":
			text = "building_courthouse";
			break;
		case "building_settlement_roads_and_paths":
		case "building_castle_roads_and_paths":
			text = "building_settlement_roads_and_paths";
			break;
		case "building_settlement_daily_housing":
			text = "building_daily_build_house";
			break;
		case "building_settlement_daily_train_militia":
			text = "building_daily_train_militia";
			break;
		case "building_settlement_daily_festival_and_games":
			text = "building_daily_festivals_and_games";
			break;
		case "building_settlement_daily_irrigation":
		case "building_castle_daily_irrigation":
			text = "building_daily_irrigation";
			break;
		case "building_castle_daily_slacken_garrison":
			text = "building_daily_train_militia";
			break;
		case "building_castle_daily_raise_troops":
			text = "building_daily_train_militia";
			break;
		case "building_castle_daily_drills":
			text = "building_daily_train_militia";
			break;
		default:
			return "";
		}
		if (useSmallVariant)
		{
			text += "_t";
		}
		return text;
	}
}
