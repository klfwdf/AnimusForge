using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyManageTroopPopupWidget : Widget
{
	private bool _isPrimaryActionAvailable;

	private bool _isSecondaryActionAvailable;

	private bool _isTertiaryActionAvailable;

	public Widget PrimaryInputKeyVisualParent { get; set; }

	public Widget SecondaryInputKeyVisualParent { get; set; }

	public Widget TertiaryInputKeyVisualParent { get; set; }

	public bool IsPrimaryActionAvailable
	{
		get
		{
			return _isPrimaryActionAvailable;
		}
		set
		{
			if (value != _isPrimaryActionAvailable)
			{
				_isPrimaryActionAvailable = value;
				OnPropertyChanged(value, "IsPrimaryActionAvailable");
			}
		}
	}

	public bool IsSecondaryActionAvailable
	{
		get
		{
			return _isSecondaryActionAvailable;
		}
		set
		{
			if (value != _isSecondaryActionAvailable)
			{
				_isSecondaryActionAvailable = value;
				OnPropertyChanged(value, "IsSecondaryActionAvailable");
			}
		}
	}

	public bool IsTertiaryActionAvailable
	{
		get
		{
			return _isTertiaryActionAvailable;
		}
		set
		{
			if (value != _isTertiaryActionAvailable)
			{
				_isTertiaryActionAvailable = value;
				OnPropertyChanged(value, "IsTertiaryActionAvailable");
			}
		}
	}

	public PartyManageTroopPopupWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (!base.IsVisible)
		{
			return;
		}
		Widget hoveredWidget = base.EventManager.HoveredWidget;
		if (hoveredWidget == null || PrimaryInputKeyVisualParent == null || SecondaryInputKeyVisualParent == null || TertiaryInputKeyVisualParent == null)
		{
			return;
		}
		PartyTroopManagementItemButtonWidget firstParentTupleOfWidget = GetFirstParentTupleOfWidget(hoveredWidget);
		if (firstParentTupleOfWidget != null)
		{
			Widget actionButtonAtIndex = firstParentTupleOfWidget.GetActionButtonAtIndex(0);
			if (IsPrimaryActionAvailable && actionButtonAtIndex != null)
			{
				PrimaryInputKeyVisualParent.IsVisible = true;
				PrimaryInputKeyVisualParent.ScaledPositionXOffset = actionButtonAtIndex.GlobalPosition.X;
				PrimaryInputKeyVisualParent.ScaledPositionYOffset = actionButtonAtIndex.GlobalPosition.Y - 10f;
			}
			else
			{
				PrimaryInputKeyVisualParent.IsVisible = false;
			}
			Widget actionButtonAtIndex2 = firstParentTupleOfWidget.GetActionButtonAtIndex(1);
			if (IsSecondaryActionAvailable && actionButtonAtIndex2 != null)
			{
				SecondaryInputKeyVisualParent.IsVisible = true;
				SecondaryInputKeyVisualParent.ScaledPositionXOffset = actionButtonAtIndex2.GlobalPosition.X;
				SecondaryInputKeyVisualParent.ScaledPositionYOffset = actionButtonAtIndex2.GlobalPosition.Y - 10f;
			}
			else
			{
				SecondaryInputKeyVisualParent.IsVisible = false;
			}
			Widget actionButtonAtIndex3 = firstParentTupleOfWidget.GetActionButtonAtIndex(2);
			if (IsTertiaryActionAvailable && actionButtonAtIndex3 != null)
			{
				TertiaryInputKeyVisualParent.IsVisible = true;
				TertiaryInputKeyVisualParent.ScaledPositionXOffset = actionButtonAtIndex3.GlobalPosition.X;
				TertiaryInputKeyVisualParent.ScaledPositionYOffset = actionButtonAtIndex3.GlobalPosition.Y - 10f;
			}
			else
			{
				TertiaryInputKeyVisualParent.IsVisible = false;
			}
		}
		else
		{
			PrimaryInputKeyVisualParent.IsVisible = false;
			SecondaryInputKeyVisualParent.IsVisible = false;
			TertiaryInputKeyVisualParent.IsVisible = false;
		}
	}

	private PartyTroopManagementItemButtonWidget GetFirstParentTupleOfWidget(Widget widget)
	{
		for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
		{
			if (widget2 is PartyTroopManagementItemButtonWidget result)
			{
				return result;
			}
		}
		return null;
	}
}
