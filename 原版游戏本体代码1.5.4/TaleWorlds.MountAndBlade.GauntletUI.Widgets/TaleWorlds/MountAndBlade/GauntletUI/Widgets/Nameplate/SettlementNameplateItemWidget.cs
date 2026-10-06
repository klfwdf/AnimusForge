using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Nameplate;

public class SettlementNameplateItemWidget : Widget
{
	private bool _hoverBegan;

	private Widget _inspectedIconWidget;

	private Widget _portIconWidget;

	private MapEventVisualBrushWidget _mapEventVisualWidget;

	private MaskedTextureWidget _settlementBannerWidget;

	private TextWidget _settlementNameTextWidget;

	private GridWidget _settlementPartiesGridWidget;

	private Widget _widgetToShow;

	private Widget _parleyIconWidget;

	public bool IsOverWidget { get; private set; }

	public int QuestType { get; set; }

	public int IssueType { get; set; }

	public Widget InspectedIconWidget
	{
		get
		{
			return _inspectedIconWidget;
		}
		set
		{
			if (_inspectedIconWidget != value)
			{
				_inspectedIconWidget = value;
				OnPropertyChanged(value, "InspectedIconWidget");
			}
		}
	}

	public Widget PortIconWidget
	{
		get
		{
			return _portIconWidget;
		}
		set
		{
			if (_portIconWidget != value)
			{
				_portIconWidget = value;
				OnPropertyChanged(value, "PortIconWidget");
			}
		}
	}

	public GridWidget SettlementPartiesGridWidget
	{
		get
		{
			return _settlementPartiesGridWidget;
		}
		set
		{
			if (_settlementPartiesGridWidget != value)
			{
				_settlementPartiesGridWidget = value;
				OnPropertyChanged(value, "SettlementPartiesGridWidget");
			}
		}
	}

	public MapEventVisualBrushWidget MapEventVisualWidget
	{
		get
		{
			return _mapEventVisualWidget;
		}
		set
		{
			if (_mapEventVisualWidget != value)
			{
				_mapEventVisualWidget = value;
				OnPropertyChanged(value, "MapEventVisualWidget");
			}
		}
	}

	[Editor(false)]
	public Widget WidgetToShow
	{
		get
		{
			return _widgetToShow;
		}
		set
		{
			if (_widgetToShow != value)
			{
				_widgetToShow = value;
				OnPropertyChanged(value, "WidgetToShow");
			}
		}
	}

	public MaskedTextureWidget SettlementBannerWidget
	{
		get
		{
			return _settlementBannerWidget;
		}
		set
		{
			if (_settlementBannerWidget != value)
			{
				_settlementBannerWidget = value;
				OnPropertyChanged(value, "SettlementBannerWidget");
			}
		}
	}

	public TextWidget SettlementNameTextWidget
	{
		get
		{
			return _settlementNameTextWidget;
		}
		set
		{
			if (_settlementNameTextWidget != value)
			{
				_settlementNameTextWidget = value;
				OnPropertyChanged(value, "SettlementNameTextWidget");
			}
		}
	}

	public Widget ParleyIconWidget
	{
		get
		{
			return _parleyIconWidget;
		}
		set
		{
			if (_parleyIconWidget != value)
			{
				_parleyIconWidget = value;
				OnPropertyChanged(value, "ParleyIconWidget");
			}
		}
	}

	public SettlementNameplateItemWidget(UIContext context)
		: base(context)
	{
	}

	public void ParallelUpdate(float dt)
	{
		Widget widgetToShow = _widgetToShow;
		Widget parentWidget = base.ParentWidget;
		if (widgetToShow == null)
		{
			Debug.FailedAssert("widgetToShow is null during ParallelUpdate!", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI.Widgets\\Nameplate\\SettlementNameplateItemWidget.cs", "ParallelUpdate", 24);
		}
		else if (parentWidget != null && parentWidget.IsEnabled)
		{
			IsOverWidget = IsMouseOverWidget();
			if (IsOverWidget && !_hoverBegan)
			{
				_hoverBegan = true;
				widgetToShow.IsVisible = true;
			}
			else if (!IsOverWidget && _hoverBegan)
			{
				_hoverBegan = false;
				widgetToShow.IsVisible = false;
			}
			if (!IsOverWidget && widgetToShow.IsVisible)
			{
				widgetToShow.IsVisible = false;
			}
		}
		else
		{
			widgetToShow.IsVisible = false;
		}
	}

	private bool IsMouseOverWidget()
	{
		if (!base.EventManager.GetIsHitThisFrame() || !base.EventManager.IsPointInsideUsableArea(base.EventManager.MousePosition))
		{
			return false;
		}
		return AreaRect.IsPointInside(base.EventManager.MousePosition);
	}
}
