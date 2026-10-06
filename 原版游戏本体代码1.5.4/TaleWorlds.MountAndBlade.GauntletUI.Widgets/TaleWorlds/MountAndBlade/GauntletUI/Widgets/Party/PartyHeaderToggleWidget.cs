using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyHeaderToggleWidget : ToggleButtonWidget
{
	private int _latestChildCount;

	private ListPanel _listPanel;

	private ButtonWidget _transferButtonWidget;

	private BrushWidget _collapseIndicator;

	private bool _isRelevant = true;

	private bool _blockInputsWhenDisabled;

	public bool AutoToggleTransferButtonState { get; set; } = true;

	[Editor(false)]
	public ListPanel ListPanel
	{
		get
		{
			return _listPanel;
		}
		set
		{
			if (_listPanel != value)
			{
				_listPanel = value;
				OnPropertyChanged(value, "ListPanel");
				ListPanelUpdated();
			}
		}
	}

	[Editor(false)]
	public ButtonWidget TransferButtonWidget
	{
		get
		{
			return _transferButtonWidget;
		}
		set
		{
			if (_transferButtonWidget != value)
			{
				_transferButtonWidget = value;
				OnPropertyChanged(value, "TransferButtonWidget");
				TransferButtonUpdated();
			}
		}
	}

	[Editor(false)]
	public BrushWidget CollapseIndicator
	{
		get
		{
			return _collapseIndicator;
		}
		set
		{
			if (_collapseIndicator != value)
			{
				_collapseIndicator = value;
				OnPropertyChanged(value, "CollapseIndicator");
				CollapseIndicatorUpdated();
			}
		}
	}

	[Editor(false)]
	public bool IsRelevant
	{
		get
		{
			return _isRelevant;
		}
		set
		{
			if (_isRelevant != value)
			{
				_isRelevant = value;
				if (!_isRelevant)
				{
					base.IsVisible = false;
				}
				UpdateSize();
				OnPropertyChanged(value, "IsRelevant");
			}
		}
	}

	[Editor(false)]
	public bool BlockInputsWhenDisabled
	{
		get
		{
			return _blockInputsWhenDisabled;
		}
		set
		{
			if (_blockInputsWhenDisabled != value)
			{
				_blockInputsWhenDisabled = value;
				OnPropertyChanged(value, "BlockInputsWhenDisabled");
			}
		}
	}

	public PartyHeaderToggleWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnClick(Widget widget)
	{
		if (!BlockInputsWhenDisabled || _listPanel == null || _listPanel.ChildCount > 0)
		{
			base.OnClick(widget);
			UpdateCollapseIndicator();
		}
	}

	private void OnListSizeChange(Widget widget)
	{
		UpdateSize();
	}

	private void OnListSizeChange(Widget parentWidget, Widget addedWidget)
	{
		UpdateSize();
	}

	public override void SetState(string stateName)
	{
		if (!BlockInputsWhenDisabled || _listPanel == null || _listPanel.ChildCount > 0)
		{
			base.SetState(stateName);
		}
	}

	private void UpdateSize()
	{
		if (TransferButtonWidget != null && AutoToggleTransferButtonState)
		{
			TransferButtonWidget.IsEnabled = _listPanel.ChildCount > 0;
		}
		if (IsRelevant)
		{
			base.IsVisible = true;
			if (_listPanel.ChildCount > 0)
			{
				_listPanel.IsVisible = true;
			}
			if (_listPanel.ChildCount > _latestChildCount && !base.WidgetToClose.IsVisible)
			{
				HandleClick();
			}
		}
		else
		{
			_listPanel.IsVisible = false;
		}
		_latestChildCount = _listPanel.ChildCount;
		UpdateCollapseIndicator();
	}

	private void ListPanelUpdated()
	{
		if (TransferButtonWidget != null)
		{
			TransferButtonWidget.IsEnabled = false;
		}
		_listPanel.ItemAfterRemoveEventHandlers.Add(OnListSizeChange);
		_listPanel.ItemAddEventHandlers.Add(OnListSizeChange);
		UpdateSize();
	}

	private void TransferButtonUpdated()
	{
		TransferButtonWidget.IsEnabled = false;
	}

	private void CollapseIndicatorUpdated()
	{
		CollapseIndicator.AddState("Collapsed");
		CollapseIndicator.AddState("Expanded");
		UpdateCollapseIndicator();
	}

	private void UpdateCollapseIndicator()
	{
		if (base.WidgetToClose != null && CollapseIndicator != null)
		{
			if (base.WidgetToClose.IsVisible)
			{
				CollapseIndicator.SetState("Expanded");
			}
			else
			{
				CollapseIndicator.SetState("Collapsed");
			}
		}
	}
}
