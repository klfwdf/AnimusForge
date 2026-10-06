using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class WidgetCreationData
{
	private Dictionary<string, object> _extensionData;

	public Widget Parent { get; private set; }

	public UIContext Context { get; private set; }

	public WidgetFactory WidgetFactory { get; private set; }

	public BrushFactory BrushFactory => Context.BrushFactory;

	public SpriteData SpriteData => Context.SpriteData;

	public PrefabExtensionContext PrefabExtensionContext => WidgetFactory.PrefabExtensionContext;

	public WidgetCreationData(UIContext context, WidgetFactory widgetFactory, Widget parent)
	{
		Context = context;
		WidgetFactory = widgetFactory;
		Parent = parent;
		_extensionData = new Dictionary<string, object>();
	}

	public WidgetCreationData(UIContext context, WidgetFactory widgetFactory)
	{
		Context = context;
		WidgetFactory = widgetFactory;
		Parent = null;
		_extensionData = new Dictionary<string, object>();
	}

	public WidgetCreationData(WidgetCreationData widgetCreationData, WidgetInstantiationResult parentResult)
	{
		Context = widgetCreationData.Context;
		WidgetFactory = widgetCreationData.WidgetFactory;
		Parent = parentResult.Widget;
		_extensionData = new Dictionary<string, object>();
		foreach (KeyValuePair<string, object> extensionDatum in widgetCreationData._extensionData)
		{
			_extensionData.Add(extensionDatum.Key, extensionDatum.Value);
		}
		foreach (WidgetInstantiationResultExtensionData extensionData in parentResult.ExtensionDatas)
		{
			if (extensionData.PassToChildWidgetCreation)
			{
				AddExtensionData(extensionData.Name, extensionData.Data);
			}
		}
	}

	public void AddExtensionData(string name, object data)
	{
		if (_extensionData.ContainsKey(name))
		{
			_extensionData[name] = data;
		}
		else
		{
			_extensionData.Add(name, data);
		}
	}

	public T GetExtensionData<T>(string name) where T : class
	{
		if (_extensionData.ContainsKey(name))
		{
			return _extensionData[name] as T;
		}
		return null;
	}

	public void AddExtensionData(object data)
	{
		AddExtensionData(data.GetType().Name, data);
	}

	public T GetExtensionData<T>() where T : class
	{
		return GetExtensionData<T>(typeof(T).Name);
	}
}
