using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DropdownButtonWidget : ButtonWidget
{
	private Widget _displayedList;

	private bool _isDisplayingList;

	public Widget DisplayedList
	{
		get
		{
			return _displayedList;
		}
		set
		{
			if (value != _displayedList)
			{
				if (_displayedList != null && _displayedList.GetFirstInChildrenAndThisRecursive((Widget x) => x is ListPanel) is ListPanel listPanel)
				{
					listPanel.SelectEventHandlers.Remove(OnListItemSelected);
				}
				_displayedList = value;
				_displayedList.IsVisible = false;
				_isDisplayingList = false;
				if (_displayedList.GetFirstInChildrenAndThisRecursive((Widget x) => x is ListPanel) is ListPanel listPanel2)
				{
					listPanel2.SelectEventHandlers.Add(OnListItemSelected);
				}
			}
		}
	}

	public DropdownButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void OnListItemSelected(Widget list)
	{
		HideList();
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_isDisplayingList)
		{
			DisplayedList.ScaledPositionXOffset = Mathf.Clamp(base.GlobalPosition.X, 0f, base.EventManager.Root.Size.X * base._inverseScaleToUse - DisplayedList.Size.X);
			DisplayedList.ScaledPositionYOffset = Mathf.Clamp(base.GlobalPosition.Y + base.Size.Y, 0f, base.EventManager.Root.Size.Y * base._inverseScaleToUse - DisplayedList.Size.Y);
			if (base.EventManager.LatestMouseUpWidget == null)
			{
				HideList();
			}
			else if (base.EventManager.LatestMouseUpWidget != this && !DisplayedList.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget))
			{
				HideList();
			}
		}
	}

	private void DisplayList()
	{
		DisplayedList.ParentWidget = base.EventManager.Root;
		DisplayedList.IsVisible = true;
		DisplayedList.HorizontalAlignment = HorizontalAlignment.Left;
		DisplayedList.VerticalAlignment = VerticalAlignment.Top;
		_isDisplayingList = true;
		base.DoNotUseCustomScaleAndChildren = false;
	}

	private void HideList()
	{
		DisplayedList.ParentWidget = this;
		DisplayedList.IsVisible = false;
		DisplayedList.PositionXOffset = 0f;
		DisplayedList.PositionYOffset = 0f;
		_isDisplayingList = false;
		base.DoNotUseCustomScaleAndChildren = true;
	}

	protected override void HandleClick()
	{
		base.HandleClick();
		if (DisplayedList != null)
		{
			if (!_isDisplayingList)
			{
				DisplayList();
			}
			else
			{
				HideList();
			}
		}
	}
}
