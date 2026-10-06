using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class SelectorWidget : Widget
{
	private int _currentSelectedIndex;

	private Action<Widget> _listSelectionHandler;

	private Action<Widget, Widget> _listItemRemovedHandler;

	private Action<Widget, Widget> _listItemAddedHandler;

	private Container _container;

	[Editor(false)]
	public int ListPanelValue
	{
		get
		{
			if (Container != null)
			{
				return Container.IntValue;
			}
			return -1;
		}
		set
		{
			if (Container != null && Container.IntValue != value)
			{
				Container.IntValue = value;
			}
		}
	}

	[Editor(false)]
	public int CurrentSelectedIndex
	{
		get
		{
			return _currentSelectedIndex;
		}
		set
		{
			if (_currentSelectedIndex != value && value >= 0)
			{
				_currentSelectedIndex = value;
				RefreshSelectedItem();
			}
		}
	}

	[Editor(false)]
	public Container Container
	{
		get
		{
			return _container;
		}
		set
		{
			if (_container != null)
			{
				_container.SelectEventHandlers.Remove(_listSelectionHandler);
				_container.ItemAddEventHandlers.Remove(_listItemAddedHandler);
				_container.ItemRemoveEventHandlers.Remove(_listItemRemovedHandler);
			}
			_container = value;
			if (_container != null)
			{
				_container.SelectEventHandlers.Add(_listSelectionHandler);
				_container.ItemAddEventHandlers.Add(_listItemAddedHandler);
				_container.ItemRemoveEventHandlers.Add(_listItemRemovedHandler);
			}
			RefreshSelectedItem();
		}
	}

	public SelectorWidget(UIContext context)
		: base(context)
	{
		_listSelectionHandler = OnSelectionChanged;
		_listItemRemovedHandler = OnListChanged;
		_listItemAddedHandler = OnListChanged;
	}

	public void OnListChanged(Widget widget)
	{
		RefreshSelectedItem();
	}

	public void OnListChanged(Widget parentWidget, Widget addedWidget)
	{
		RefreshSelectedItem();
	}

	public void OnSelectionChanged(Widget widget)
	{
		CurrentSelectedIndex = ListPanelValue;
		RefreshSelectedItem();
		OnPropertyChanged(CurrentSelectedIndex, "CurrentSelectedIndex");
	}

	private void RefreshSelectedItem()
	{
		ListPanelValue = CurrentSelectedIndex;
	}
}
