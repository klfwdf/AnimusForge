using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class RadioContainerWidget : Widget
{
	private int _selectedIndex;

	private Container _container;

	[Editor(false)]
	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (_selectedIndex != value)
			{
				_selectedIndex = value;
				OnPropertyChanged(value, "SelectedIndex");
				if (Container != null)
				{
					Container.IntValue = _selectedIndex;
				}
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
			if (_container != value)
			{
				ContainerUpdated(value);
				_container = value;
				OnPropertyChanged(value, "Container");
			}
		}
	}

	public RadioContainerWidget(UIContext context)
		: base(context)
	{
	}

	private void ContainerOnPropertyChanged(PropertyOwnerObject owner, string propertyName, int value)
	{
		if (propertyName == "IntValue")
		{
			SelectedIndex = Container.IntValue;
		}
	}

	private void ContainerOnEventFire(Widget owner, string eventName, object[] arguments)
	{
		if (eventName == "ItemAdd" || eventName == "ItemRemove")
		{
			Container.IntValue = SelectedIndex;
		}
	}

	private void ContainerUpdated(Container newContainer)
	{
		if (Container != null)
		{
			Container.intPropertyChanged -= ContainerOnPropertyChanged;
			Container.EventFire -= ContainerOnEventFire;
		}
		if (newContainer != null)
		{
			newContainer.intPropertyChanged += ContainerOnPropertyChanged;
			newContainer.EventFire += ContainerOnEventFire;
			newContainer.IntValue = SelectedIndex;
		}
	}
}
