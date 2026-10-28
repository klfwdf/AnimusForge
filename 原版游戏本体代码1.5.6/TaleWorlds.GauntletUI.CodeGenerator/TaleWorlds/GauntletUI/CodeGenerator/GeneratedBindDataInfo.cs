using System;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class GeneratedBindDataInfo
{
	public string Property { get; private set; }

	public string Path { get; private set; }

	public Type WidgetPropertyType { get; internal set; }

	public Type ViewModelPropertType { get; internal set; }

	public bool RequiresConversion { get; internal set; }

	internal GeneratedBindDataInfo(string property, string path)
	{
		Property = property;
		Path = path;
	}
}
