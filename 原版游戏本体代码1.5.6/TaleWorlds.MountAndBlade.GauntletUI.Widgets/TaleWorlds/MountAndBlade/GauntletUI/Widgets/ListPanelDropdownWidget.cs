using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ListPanelDropdownWidget : DropdownWidget
{
	private Widget _listPanelContainer;

	[Editor(false)]
	public Widget ListPanelContainer
	{
		get
		{
			return _listPanelContainer;
		}
		set
		{
			if (_listPanelContainer != value)
			{
				_listPanelContainer = value;
				OnPropertyChanged(value, "ListPanelContainer");
			}
		}
	}

	public ListPanelDropdownWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OpenPanel()
	{
		base.OpenPanel();
		if (ListPanelContainer != null)
		{
			ListPanelContainer.IsVisible = true;
		}
		base.Button.IsSelected = true;
	}

	protected override void ClosePanel()
	{
		if (ListPanelContainer != null)
		{
			ListPanelContainer.IsVisible = false;
		}
		base.Button.IsSelected = false;
		base.ClosePanel();
	}
}
