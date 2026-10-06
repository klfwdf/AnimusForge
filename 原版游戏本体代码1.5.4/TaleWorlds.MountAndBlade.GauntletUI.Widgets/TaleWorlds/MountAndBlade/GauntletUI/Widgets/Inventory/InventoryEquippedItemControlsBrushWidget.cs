using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryEquippedItemControlsBrushWidget : BrushWidget
{
	public delegate void ButtonClickEventHandler(Widget itemWidget);

	private float _lastTransitionStartTime;

	private bool _isScopeDirty;

	private bool _panelVisible;

	private InventoryItemButtonWidget _itemWidget;

	public NavigationForcedScopeCollectionTargeter ForcedScopeCollection { get; set; }

	public NavigationScopeTargeter NavigationScope { get; set; }

	[Editor(false)]
	public InventoryItemButtonWidget ItemWidget
	{
		get
		{
			return _itemWidget;
		}
		set
		{
			if (_itemWidget != value)
			{
				_itemWidget = value;
				OnPropertyChanged(value, "ItemWidget");
			}
		}
	}

	public event Action OnHidePanel;

	public InventoryEquippedItemControlsBrushWidget(UIContext context)
		: base(context)
	{
		AddState("LeftHidden");
		AddState("LeftVisible");
		AddState("RightHidden");
		AddState("RightVisible");
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isScopeDirty && base.EventManager.Time - _lastTransitionStartTime > base.VisualDefinition.TransitionDuration)
		{
			ForcedScopeCollection.IsCollectionDisabled = base.CurrentState == "RightHidden" || base.CurrentState == "LeftHidden";
			NavigationScope.IsScopeDisabled = ForcedScopeCollection.IsCollectionDisabled;
			_isScopeDirty = false;
		}
	}

	public void ShowPanel()
	{
		if (!_panelVisible)
		{
			if (ItemWidget.IsRightSide)
			{
				base.HorizontalAlignment = HorizontalAlignment.Right;
				base.Brush.HorizontalFlip = false;
				SetState("RightHidden");
				base.PositionXOffset = base.VisualDefinition.VisualStates["RightHidden"].PositionXOffset;
				SetState("RightVisible");
			}
			else
			{
				base.HorizontalAlignment = HorizontalAlignment.Left;
				base.Brush.HorizontalFlip = true;
				SetState("LeftHidden");
				base.PositionXOffset = base.VisualDefinition.VisualStates["LeftHidden"].PositionXOffset;
				SetState("LeftVisible");
			}
			base.IsVisible = true;
			_panelVisible = true;
			_isScopeDirty = true;
			_lastTransitionStartTime = base.Context.EventManager.Time;
		}
	}

	public void HidePanel()
	{
		if (_panelVisible)
		{
			if (ItemWidget.IsRightSide)
			{
				SetState("RightHidden");
			}
			else
			{
				SetState("LeftHidden");
			}
			this.OnHidePanel?.Invoke();
			_panelVisible = false;
			_isScopeDirty = true;
			_lastTransitionStartTime = base.Context.EventManager.Time;
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_panelVisible && ItemWidget.IsSelected)
		{
			ShowPanel();
		}
		else if (_panelVisible && !ItemWidget.IsSelected)
		{
			HidePanel();
		}
	}
}
