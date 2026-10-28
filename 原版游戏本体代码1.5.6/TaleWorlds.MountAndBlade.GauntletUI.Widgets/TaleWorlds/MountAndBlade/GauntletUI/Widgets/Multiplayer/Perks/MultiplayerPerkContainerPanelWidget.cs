using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Perks;

public class MultiplayerPerkContainerPanelWidget : Widget
{
	private MultiplayerPerkItemToggleWidget _currentSelectedItem;

	private MultiplayerPerkPopupWidget _popupWidgetFirst;

	private MultiplayerPerkPopupWidget _popupWidgetSecond;

	private MultiplayerPerkPopupWidget _popupWidgetThird;

	private MultiplayerClassLoadoutTroopSubclassButtonWidget _troopTupleBodyWidget;

	public MultiplayerPerkPopupWidget PopupWidgetFirst
	{
		get
		{
			return _popupWidgetFirst;
		}
		set
		{
			if (value != _popupWidgetFirst)
			{
				_popupWidgetFirst = value;
				OnPropertyChanged(value, "PopupWidgetFirst");
			}
		}
	}

	public MultiplayerPerkPopupWidget PopupWidgetSecond
	{
		get
		{
			return _popupWidgetSecond;
		}
		set
		{
			if (value != _popupWidgetSecond)
			{
				_popupWidgetSecond = value;
				OnPropertyChanged(value, "PopupWidgetSecond");
			}
		}
	}

	public MultiplayerPerkPopupWidget PopupWidgetThird
	{
		get
		{
			return _popupWidgetThird;
		}
		set
		{
			if (value != _popupWidgetThird)
			{
				_popupWidgetThird = value;
				OnPropertyChanged(value, "PopupWidgetThird");
			}
		}
	}

	public MultiplayerClassLoadoutTroopSubclassButtonWidget TroopTupleBodyWidget
	{
		get
		{
			return _troopTupleBodyWidget;
		}
		set
		{
			if (value != _troopTupleBodyWidget)
			{
				_troopTupleBodyWidget = value;
				OnPropertyChanged(value, "TroopTupleBodyWidget");
			}
		}
	}

	public MultiplayerPerkContainerPanelWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		Widget latestMouseUpWidget = base.EventManager.LatestMouseUpWidget;
		if (TroopTupleBodyWidget != null)
		{
			MultiplayerClassLoadoutTroopSubclassButtonWidget troopTupleBodyWidget = TroopTupleBodyWidget;
			if (troopTupleBodyWidget == null || !troopTupleBodyWidget.IsSelected)
			{
				goto IL_005e;
			}
		}
		if (!CheckIsMyChildRecursive(latestMouseUpWidget) && (PopupWidgetFirst.IsVisible || PopupWidgetSecond.IsVisible || PopupWidgetThird.IsVisible))
		{
			ClosePanel();
		}
		goto IL_005e;
		IL_005e:
		MultiplayerClassLoadoutTroopSubclassButtonWidget troopTupleBodyWidget2 = TroopTupleBodyWidget;
		if ((troopTupleBodyWidget2 == null || !troopTupleBodyWidget2.IsSelected) && _currentSelectedItem != null)
		{
			_currentSelectedItem.IsSelected = false;
			_currentSelectedItem = null;
		}
	}

	public void PerkSelected(MultiplayerPerkItemToggleWidget selectedItem)
	{
		if (selectedItem == _currentSelectedItem || selectedItem == null)
		{
			ClosePanel();
		}
		else if (selectedItem != null && selectedItem.ParentWidget != null)
		{
			if (_currentSelectedItem != null)
			{
				_currentSelectedItem.IsSelected = false;
			}
			int childIndex = selectedItem.ParentWidget.GetChildIndex(selectedItem);
			PopupWidgetFirst.IsVisible = childIndex == 0;
			PopupWidgetFirst.IsEnabled = childIndex == 0;
			PopupWidgetSecond.IsVisible = childIndex == 1;
			PopupWidgetSecond.IsEnabled = childIndex == 1;
			PopupWidgetThird.IsVisible = childIndex == 2;
			PopupWidgetThird.IsEnabled = childIndex == 2;
			PopupWidgetFirst.SetPopupPerksContainer(this);
			PopupWidgetSecond.SetPopupPerksContainer(this);
			PopupWidgetThird.SetPopupPerksContainer(this);
			_currentSelectedItem = selectedItem;
			_currentSelectedItem.IsSelected = true;
		}
	}

	private void ClosePanel()
	{
		if (_currentSelectedItem != null)
		{
			_currentSelectedItem.IsSelected = false;
		}
		_currentSelectedItem = null;
		PopupWidgetFirst.IsVisible = false;
		PopupWidgetSecond.IsVisible = false;
		PopupWidgetThird.IsVisible = false;
	}
}
