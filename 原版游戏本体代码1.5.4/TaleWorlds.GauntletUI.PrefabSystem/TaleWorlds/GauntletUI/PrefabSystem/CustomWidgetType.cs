using System;
using System.IO;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class CustomWidgetType
{
	private DateTime _lastWriteTime = DateTime.MinValue;

	private string _resourcesPath;

	public WidgetTemplate WidgetTemplate => WidgetPrefab.RootTemplate;

	public WidgetPrefab WidgetPrefab { get; private set; }

	public WidgetFactory WidgetFactory { get; private set; }

	public string Name { get; private set; }

	public string FullPath => _resourcesPath + Name + ".xml";

	public CustomWidgetType(WidgetFactory widgetFactory, string resourcesPath, string name)
	{
		_resourcesPath = resourcesPath;
		Name = name;
		WidgetFactory = widgetFactory;
		Load();
		_lastWriteTime = File.GetLastWriteTime(FullPath);
	}

	public bool CheckForUpdate()
	{
		DateTime lastWriteTime = File.GetLastWriteTime(FullPath);
		if (_lastWriteTime != lastWriteTime)
		{
			try
			{
				Load();
				_lastWriteTime = lastWriteTime;
				return true;
			}
			catch
			{
			}
		}
		return false;
	}

	private void Load()
	{
		WidgetPrefab = WidgetPrefab.LoadFrom(WidgetFactory.PrefabExtensionContext, WidgetFactory.WidgetAttributeContext, FullPath);
	}
}
