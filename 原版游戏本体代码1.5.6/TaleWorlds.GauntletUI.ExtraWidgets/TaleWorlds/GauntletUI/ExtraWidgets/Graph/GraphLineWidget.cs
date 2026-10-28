using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph;

public class GraphLineWidget : Widget
{
	public Action<GraphLineWidget, GraphLinePointWidget> OnPointAdded;

	private Widget _pointContainerWidget;

	public string LineBrushStateName { get; set; }

	public Widget PointContainerWidget
	{
		get
		{
			return _pointContainerWidget;
		}
		set
		{
			if (value != _pointContainerWidget)
			{
				if (_pointContainerWidget != null)
				{
					_pointContainerWidget.EventFire -= OnPointContainerEventFire;
				}
				_pointContainerWidget = value;
				if (_pointContainerWidget != null)
				{
					_pointContainerWidget.EventFire += OnPointContainerEventFire;
				}
				OnPropertyChanged(value, "PointContainerWidget");
			}
		}
	}

	public GraphLineWidget(UIContext context)
		: base(context)
	{
	}

	private void OnPointContainerEventFire(Widget widget, string eventName, object[] eventArgs)
	{
		if (eventName == "ItemAdd" && eventArgs.Length != 0 && eventArgs[0] is GraphLinePointWidget arg)
		{
			OnPointAdded?.Invoke(this, arg);
		}
	}
}
