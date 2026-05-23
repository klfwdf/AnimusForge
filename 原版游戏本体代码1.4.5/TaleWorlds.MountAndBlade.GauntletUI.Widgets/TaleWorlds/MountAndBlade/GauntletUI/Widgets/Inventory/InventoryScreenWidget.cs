using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryScreenWidget : Widget
{
	private readonly int TooltipHideFrameLength = 2;

	private Widget _latestMouseDownWidget;

	private InventoryItemButtonWidget _currentHoveredItemWidget;

	private InventoryItemButtonWidget _currentDraggedItemWidget;

	private InventoryItemButtonWidget _lastDisplayedTooltipItem;

	private int _tooltipHiddenFrameCount;

	private int _scrollToBannersInFrames = -1;

	private float _scrollToItemInSeconds = -1f;

	private InputKeyVisualWidget _previousCharacterInputKeyVisual;

	private InputKeyVisualWidget _nextCharacterInputKeyVisual;

	private Widget _previousCharacterInputVisualParent;

	private Widget _nextCharacterInputVisualParent;

	private InputKeyVisualWidget _transferInputKeyVisualWidget;

	private RichTextWidget _tradeLabel;

	private Widget _inventoryTooltip;

	private InventoryItemPreviewWidget _itemPreviewWidget;

	private int _transactionCount;

	private int _equipmentMode;

	private int _targetEquipmentIndex;

	private ScrollablePanel _otherInventoryListWidget;

	private ScrollablePanel _playerInventoryListWidget;

	private bool _focusLostThisFrame;

	private bool _isFocusedOnItemList;

	private bool _isBannerTutorialActive;

	private bool _scrollToItem;

	private string _bannerTypeName;

	private string _scrollItemId;

	[Editor(false)]
	public InputKeyVisualWidget TransferInputKeyVisualWidget
	{
		get
		{
			return _transferInputKeyVisualWidget;
		}
		set
		{
			if (_transferInputKeyVisualWidget != value)
			{
				_transferInputKeyVisualWidget = value;
				OnPropertyChanged(value, "TransferInputKeyVisualWidget");
			}
		}
	}

	public Widget PreviousCharacterInputVisualParent
	{
		get
		{
			return _previousCharacterInputVisualParent;
		}
		set
		{
			if (value == _previousCharacterInputVisualParent)
			{
				return;
			}
			_previousCharacterInputVisualParent = value;
			if (_previousCharacterInputVisualParent != null)
			{
				_previousCharacterInputKeyVisual = _previousCharacterInputVisualParent.Children.FirstOrDefault((Widget x) => x is InputKeyVisualWidget) as InputKeyVisualWidget;
			}
		}
	}

	public Widget NextCharacterInputVisualParent
	{
		get
		{
			return _nextCharacterInputVisualParent;
		}
		set
		{
			if (value == _nextCharacterInputVisualParent)
			{
				return;
			}
			_nextCharacterInputVisualParent = value;
			if (_nextCharacterInputVisualParent != null)
			{
				_nextCharacterInputKeyVisual = _nextCharacterInputVisualParent.Children.FirstOrDefault((Widget x) => x is InputKeyVisualWidget) as InputKeyVisualWidget;
			}
		}
	}

	[Editor(false)]
	public RichTextWidget TradeLabel
	{
		get
		{
			return _tradeLabel;
		}
		set
		{
			if (_tradeLabel != value)
			{
				if (_tradeLabel != null)
				{
					_tradeLabel.PropertyChanged -= TradeLabelOnPropertyChanged;
				}
				_tradeLabel = value;
				if (_tradeLabel != null)
				{
					_tradeLabel.PropertyChanged += TradeLabelOnPropertyChanged;
				}
				OnPropertyChanged(value, "TradeLabel");
			}
		}
	}

	[Editor(false)]
	public Widget InventoryTooltip
	{
		get
		{
			return _inventoryTooltip;
		}
		set
		{
			if (_inventoryTooltip != value)
			{
				_inventoryTooltip = value;
				OnPropertyChanged(value, "InventoryTooltip");
			}
		}
	}

	[Editor(false)]
	public InventoryItemPreviewWidget ItemPreviewWidget
	{
		get
		{
			return _itemPreviewWidget;
		}
		set
		{
			if (_itemPreviewWidget != value)
			{
				_itemPreviewWidget = value;
				OnPropertyChanged(value, "ItemPreviewWidget");
			}
		}
	}

	[Editor(false)]
	public int TransactionCount
	{
		get
		{
			return _transactionCount;
		}
		set
		{
			if (_transactionCount != value)
			{
				_transactionCount = value;
				OnPropertyChanged(value, "TransactionCount");
			}
		}
	}

	[Editor(false)]
	public int EquipmentMode
	{
		get
		{
			return _equipmentMode;
		}
		set
		{
			if (_equipmentMode != value)
			{
				_equipmentMode = value;
				OnPropertyChanged(value, "EquipmentMode");
			}
		}
	}

	[Editor(false)]
	public int TargetEquipmentIndex
	{
		get
		{
			return _targetEquipmentIndex;
		}
		set
		{
			if (_targetEquipmentIndex != value)
			{
				_targetEquipmentIndex = value;
				OnPropertyChanged(value, "TargetEquipmentIndex");
			}
		}
	}

	[Editor(false)]
	public ScrollablePanel OtherInventoryListWidget
	{
		get
		{
			return _otherInventoryListWidget;
		}
		set
		{
			if (value != _otherInventoryListWidget)
			{
				_otherInventoryListWidget = value;
				OnPropertyChanged(value, "OtherInventoryListWidget");
			}
		}
	}

	[Editor(false)]
	public ScrollablePanel PlayerInventoryListWidget
	{
		get
		{
			return _playerInventoryListWidget;
		}
		set
		{
			if (value != _playerInventoryListWidget)
			{
				_playerInventoryListWidget = value;
				OnPropertyChanged(value, "PlayerInventoryListWidget");
			}
		}
	}

	[Editor(false)]
	public bool IsFocusedOnItemList
	{
		get
		{
			return _isFocusedOnItemList;
		}
		set
		{
			if (value != _isFocusedOnItemList)
			{
				_isFocusedOnItemList = value;
				OnPropertyChanged(value, "IsFocusedOnItemList");
			}
		}
	}

	[Editor(false)]
	public bool IsBannerTutorialActive
	{
		get
		{
			return _isBannerTutorialActive;
		}
		set
		{
			if (value != _isBannerTutorialActive)
			{
				_isBannerTutorialActive = value;
				OnPropertyChanged(value, "IsBannerTutorialActive");
				if (value)
				{
					_scrollToBannersInFrames = 1;
				}
			}
		}
	}

	[Editor(false)]
	public string BannerTypeName
	{
		get
		{
			return _bannerTypeName;
		}
		set
		{
			if (value != _bannerTypeName)
			{
				_bannerTypeName = value;
				OnPropertyChanged(value, "BannerTypeName");
			}
		}
	}

	[Editor(false)]
	public bool ScrollToItem
	{
		get
		{
			return _scrollToItem;
		}
		set
		{
			if (value != _scrollToItem)
			{
				_scrollToItem = value;
				OnPropertyChanged(value, "ScrollToItem");
			}
		}
	}

	[Editor(false)]
	public string ScrollItemId
	{
		get
		{
			return _scrollItemId;
		}
		set
		{
			if (value != _scrollItemId)
			{
				_scrollItemId = value;
				OnPropertyChanged(value, "ScrollItemId");
			}
		}
	}

	public InventoryScreenWidget(UIContext context)
		: base(context)
	{
	}

	private T IsWidgetChildOfType<T>(Widget currentWidget) where T : Widget
	{
		while (currentWidget != null)
		{
			if (currentWidget is T)
			{
				return (T)currentWidget;
			}
			currentWidget = currentWidget.ParentWidget;
		}
		return null;
	}

	private bool IsWidgetChildOf(Widget parentWidget, Widget currentWidget)
	{
		while (currentWidget != null)
		{
			if (currentWidget == parentWidget)
			{
				return true;
			}
			currentWidget = currentWidget.ParentWidget;
		}
		return false;
	}

	private bool IsWidgetChildOfId(string parentId, Widget currentWidget)
	{
		while (currentWidget != null)
		{
			if (currentWidget.Id == parentId)
			{
				return true;
			}
			currentWidget = currentWidget.ParentWidget;
		}
		return false;
	}

	private InventoryListPanel GetCurrentHoveredListPanel()
	{
		for (int i = 0; i < base.EventManager.MouseOveredWidgets.Count; i++)
		{
			if (base.EventManager.MouseOveredWidgets[i] is InventoryListPanel result)
			{
				return result;
			}
		}
		return null;
	}

	private Widget GetFirstBannerItem()
	{
		return ((OtherInventoryListWidget.InnerPanel as ListPanel)?.GetChild(0) as ListPanel)?.FindChild((Widget x) => (x as InventoryItemTupleWidget).ItemType == BannerTypeName);
	}

	private Widget GetItemWithId(ScrollablePanel listWidget, string id)
	{
		return ((listWidget.InnerPanel as ListPanel)?.GetChild(0) as ListPanel)?.FindChild((Widget x) => (x as InventoryItemTupleWidget).ItemID == id);
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (base.EventManager.DraggedWidget == null)
		{
			TargetEquipmentIndex = -1;
			_currentDraggedItemWidget = null;
		}
		if (_latestMouseDownWidget != base.EventManager.LatestMouseDownWidget)
		{
			_latestMouseDownWidget = base.EventManager.LatestMouseDownWidget;
			bool flag = _latestMouseDownWidget != null && (_latestMouseDownWidget is InventoryItemButtonWidget || _latestMouseDownWidget is InventoryEquippedItemControlsBrushWidget || _latestMouseDownWidget.GetAllParents().Any((Widget x) => x is InventoryItemButtonWidget || x is InventoryEquippedItemControlsBrushWidget));
			bool flag2 = IsWidgetChildOf(InventoryTooltip, _latestMouseDownWidget);
			if (_latestMouseDownWidget == null || (!flag && !flag2 && !ItemPreviewWidget.IsVisible))
			{
				EventFired("OnEmptyClick");
			}
		}
		Widget hoveredWidget = base.EventManager.HoveredWidget;
		if (hoveredWidget != null)
		{
			InventoryItemButtonWidget inventoryItemButtonWidget = IsWidgetChildOfType<InventoryItemButtonWidget>(hoveredWidget);
			bool flag3 = IsWidgetChildOfId("InventoryTooltip", hoveredWidget);
			if (inventoryItemButtonWidget != null)
			{
				ItemWidgetHoverBegin(inventoryItemButtonWidget);
			}
			else if (flag3 && GauntletGamepadNavigationManager.Instance.IsCursorMovingForNavigation)
			{
				ItemWidgetHoverEnd(null);
			}
			else if (!flag3 && hoveredWidget.ParentWidget != null)
			{
				ItemWidgetHoverEnd(null);
			}
		}
		else
		{
			ItemWidgetHoverEnd(null);
		}
		UpdateControllerTransferKeyVisuals();
	}

	private void UpdateControllerTransferKeyVisuals()
	{
		InventoryListPanel currentHoveredListPanel = GetCurrentHoveredListPanel();
		IsFocusedOnItemList = currentHoveredListPanel != null;
		if (base.EventManager.IsControllerActive && IsFocusedOnItemList)
		{
			PreviousCharacterInputVisualParent.IsVisible = false;
			NextCharacterInputVisualParent.IsVisible = false;
			if (_currentHoveredItemWidget is InventoryItemTupleWidget { IsHovered: not false, IsTransferable: not false } inventoryItemTupleWidget)
			{
				TransferInputKeyVisualWidget.IsVisible = true;
				Vector2 vector;
				if (inventoryItemTupleWidget.IsRightSide)
				{
					TransferInputKeyVisualWidget.KeyID = _nextCharacterInputKeyVisual?.KeyID ?? "";
					vector = _currentHoveredItemWidget.GlobalPosition - new Vector2(0f, 20f * base._scaleToUse);
				}
				else
				{
					TransferInputKeyVisualWidget.KeyID = _previousCharacterInputKeyVisual?.KeyID ?? "";
					vector = _currentHoveredItemWidget.GlobalPosition - new Vector2(60f * base._scaleToUse - _currentHoveredItemWidget.Size.X, 20f * base._scaleToUse);
				}
				TransferInputKeyVisualWidget.ScaledPositionXOffset = vector.X;
				TransferInputKeyVisualWidget.ScaledPositionYOffset = vector.Y;
			}
			else
			{
				TransferInputKeyVisualWidget.IsVisible = false;
			}
		}
		else
		{
			PreviousCharacterInputVisualParent.IsVisible = true;
			NextCharacterInputVisualParent.IsVisible = true;
			TransferInputKeyVisualWidget.IsVisible = false;
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_scrollToBannersInFrames > -1)
		{
			if (_scrollToBannersInFrames == 0)
			{
				ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(0f, 0f, 0f, 0f, -1f, 0.2f, 0.35f);
				OtherInventoryListWidget.ScrollToChild(GetFirstBannerItem(), scrollParameters);
			}
			_scrollToBannersInFrames--;
		}
		if (ScrollToItem)
		{
			_scrollToItemInSeconds = 0.2f;
			ScrollToItem = false;
		}
		if (_scrollToItemInSeconds >= 0f)
		{
			_scrollToItemInSeconds -= dt;
			if (_scrollToItemInSeconds <= 0f)
			{
				ScrollablePanel.AutoScrollParameters scrollParameters2 = new ScrollablePanel.AutoScrollParameters(100f, 100f, 0f, 0f, -1f, -1f, 0.35f);
				OtherInventoryListWidget.ScrollToChild(GetItemWithId(OtherInventoryListWidget, ScrollItemId), scrollParameters2);
				PlayerInventoryListWidget.ScrollToChild(GetItemWithId(PlayerInventoryListWidget, ScrollItemId), scrollParameters2);
			}
		}
		if (_focusLostThisFrame)
		{
			EventFired("OnFocusLose");
			_focusLostThisFrame = false;
		}
		UpdateTooltipPosition();
	}

	private void UpdateTooltipPosition()
	{
		if (base.EventManager.DraggedWidget != null)
		{
			InventoryTooltip.IsHidden = true;
		}
		if (_currentHoveredItemWidget?.ParentWidget != null)
		{
			if (_tooltipHiddenFrameCount < TooltipHideFrameLength)
			{
				_tooltipHiddenFrameCount++;
				InventoryTooltip.PositionXOffset = 5000f;
				InventoryTooltip.PositionYOffset = 5000f;
				return;
			}
			if (_currentHoveredItemWidget.IsRightSide)
			{
				InventoryTooltip.ScaledPositionXOffset = _currentHoveredItemWidget.ParentWidget.GlobalPosition.X - InventoryTooltip.Size.X + 10f * base._scaleToUse;
			}
			else
			{
				InventoryTooltip.ScaledPositionXOffset = _currentHoveredItemWidget.ParentWidget.GlobalPosition.X + _currentHoveredItemWidget.ParentWidget.Size.X - 10f * base._scaleToUse;
			}
			float max = base.EventManager.PageSize.Y - InventoryTooltip.MeasuredSize.Y;
			InventoryTooltip.ScaledPositionYOffset = Mathf.Clamp(_currentHoveredItemWidget.GlobalPosition.Y, 0f, max);
			_lastDisplayedTooltipItem = _currentHoveredItemWidget;
		}
		else
		{
			_lastDisplayedTooltipItem = null;
		}
	}

	private void TradeLabelOnPropertyChanged(PropertyOwnerObject owner, string propertyName, object value)
	{
		if (propertyName == "Text")
		{
			TradeLabel.IsDisabled = string.IsNullOrEmpty(TradeLabel.Text);
		}
	}

	private void ItemWidgetHoverBegin(InventoryItemButtonWidget itemWidget)
	{
		if (_currentHoveredItemWidget != itemWidget)
		{
			_currentHoveredItemWidget = itemWidget;
			_tooltipHiddenFrameCount = 0;
			Widget widget = InventoryTooltip.FindChild("TargetItemTooltip");
			if (_currentHoveredItemWidget.IsRightSide)
			{
				widget.SetSiblingIndex(1);
			}
			else
			{
				widget.SetSiblingIndex(0);
			}
			InventoryTooltip.IsHidden = false;
			EventFired("ItemHoverBegin", itemWidget);
		}
	}

	private void ItemWidgetHoverEnd(InventoryItemButtonWidget itemWidget)
	{
		if (_currentHoveredItemWidget != null && itemWidget == null)
		{
			_currentHoveredItemWidget = null;
			InventoryTooltip.IsHidden = true;
			EventFired("ItemHoverEnd");
		}
	}

	public void ItemWidgetDragBegin(InventoryItemButtonWidget itemWidget)
	{
		EventFired("OnEmptyClick");
		_currentDraggedItemWidget = itemWidget;
		if (itemWidget is InventoryEquippedItemSlotWidget inventoryEquippedItemSlotWidget)
		{
			TargetEquipmentIndex = inventoryEquippedItemSlotWidget.TargetEquipmentIndex;
		}
		else
		{
			TargetEquipmentIndex = itemWidget.EquipmentIndex;
		}
	}

	public void ItemWidgetDrop(InventoryItemButtonWidget itemWidget)
	{
		if (_currentDraggedItemWidget == itemWidget)
		{
			_currentDraggedItemWidget = null;
			TargetEquipmentIndex = -1;
		}
	}
}
