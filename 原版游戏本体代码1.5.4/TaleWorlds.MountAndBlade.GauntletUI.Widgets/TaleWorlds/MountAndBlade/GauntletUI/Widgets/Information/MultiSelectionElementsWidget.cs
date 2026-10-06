using System.Collections.Generic;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Information;

public class MultiSelectionElementsWidget : Widget
{
	private bool _updateRequired;

	private List<ButtonWidget> _elementsList = new List<ButtonWidget>();

	private ButtonWidget _doneButtonWidget;

	private ListPanel _elementContainer;

	[Editor(false)]
	public ButtonWidget DoneButtonWidget
	{
		get
		{
			return _doneButtonWidget;
		}
		set
		{
			if (_doneButtonWidget != value)
			{
				_doneButtonWidget = value;
				OnPropertyChanged(value, "DoneButtonWidget");
			}
		}
	}

	public MultiSelectionElementsWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_updateRequired)
		{
			UpdateElementsList();
			_updateRequired = false;
		}
	}

	protected override void OnChildAdded(Widget child)
	{
		base.OnChildAdded(child);
		if (child is ListPanel)
		{
			_elementContainer = child as ListPanel;
			_elementContainer.ItemAddEventHandlers.Add(OnElementAdded);
		}
	}

	private void OnElementAdded(Widget parentWidget, Widget addedWidget)
	{
		_updateRequired = true;
	}

	private void UpdateElementsList()
	{
		_elementsList.Clear();
		for (int i = 0; i < _elementContainer.ChildCount; i++)
		{
			ButtonWidget item = _elementContainer.GetChild(i).GetChild(0) as ButtonWidget;
			_elementsList.Add(item);
		}
	}
}
