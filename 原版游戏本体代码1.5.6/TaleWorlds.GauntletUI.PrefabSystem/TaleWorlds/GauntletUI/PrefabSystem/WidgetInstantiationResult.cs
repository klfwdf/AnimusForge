using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetInstantiationResult
{
	private Dictionary<string, WidgetInstantiationResultExtensionData> _entensionData;

	public Widget Widget { get; private set; }

	public WidgetTemplate Template { get; private set; }

	public WidgetInstantiationResult CustomWidgetInstantiationData { get; private set; }

	public List<WidgetInstantiationResult> Children { get; private set; }

	internal IEnumerable<WidgetInstantiationResultExtensionData> ExtensionDatas => _entensionData.Values;

	public WidgetInstantiationResult(Widget widget, WidgetTemplate widgetTemplate, WidgetInstantiationResult customWidgetInstantiationData)
	{
		CustomWidgetInstantiationData = customWidgetInstantiationData;
		Widget = widget;
		Template = widgetTemplate;
		Children = new List<WidgetInstantiationResult>();
		_entensionData = new Dictionary<string, WidgetInstantiationResultExtensionData>();
	}

	public void AddExtensionData(string name, object data, bool passToChildWidgetCreation = false)
	{
		WidgetInstantiationResultExtensionData value = new WidgetInstantiationResultExtensionData
		{
			Name = name,
			Data = data,
			PassToChildWidgetCreation = passToChildWidgetCreation
		};
		_entensionData.Add(name, value);
	}

	public T GetExtensionData<T>(string name)
	{
		return (T)_entensionData[name].Data;
	}

	internal WidgetInstantiationResultExtensionData GetExtensionData(string name)
	{
		return _entensionData[name];
	}

	public void AddExtensionData(object data, bool passToChildWidgetCreation = false)
	{
		AddExtensionData(data.GetType().Name, data, passToChildWidgetCreation);
	}

	public T GetExtensionData<T>() where T : class
	{
		return GetExtensionData<T>(typeof(T).Name);
	}

	public WidgetInstantiationResult(Widget widget, WidgetTemplate widgetTemplate)
		: this(widget, widgetTemplate, null)
	{
	}

	public WidgetInstantiationResult GetLogicalOrDefaultChildrenLocation()
	{
		return GetLogicalOrDefaultChildrenLocation(this, isRoot: true);
	}

	private static WidgetInstantiationResult GetLogicalOrDefaultChildrenLocation(WidgetInstantiationResult data, bool isRoot)
	{
		if (isRoot)
		{
			foreach (WidgetInstantiationResult child in data.CustomWidgetInstantiationData.Children)
			{
				if (child.Template.LogicalChildrenLocation)
				{
					return child;
				}
			}
			{
				foreach (WidgetInstantiationResult child2 in data.CustomWidgetInstantiationData.Children)
				{
					WidgetInstantiationResult logicalOrDefaultChildrenLocation = GetLogicalOrDefaultChildrenLocation(child2, isRoot: false);
					if (logicalOrDefaultChildrenLocation != null)
					{
						return logicalOrDefaultChildrenLocation;
					}
				}
				return data;
			}
		}
		foreach (WidgetInstantiationResult child3 in data.Children)
		{
			if (child3.Template.LogicalChildrenLocation)
			{
				return child3;
			}
		}
		foreach (WidgetInstantiationResult child4 in data.Children)
		{
			WidgetInstantiationResult logicalOrDefaultChildrenLocation2 = GetLogicalOrDefaultChildrenLocation(child4, isRoot: false);
			if (logicalOrDefaultChildrenLocation2 != null)
			{
				return logicalOrDefaultChildrenLocation2;
			}
		}
		return null;
	}
}
