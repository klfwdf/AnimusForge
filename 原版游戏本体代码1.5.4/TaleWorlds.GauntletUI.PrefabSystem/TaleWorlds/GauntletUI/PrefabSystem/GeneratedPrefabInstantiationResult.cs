using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class GeneratedPrefabInstantiationResult
{
	private Dictionary<string, object> _data;

	public Widget Root { get; private set; }

	public GeneratedPrefabInstantiationResult(Widget root)
	{
		Root = root;
		_data = new Dictionary<string, object>();
	}

	public void AddData(string tag, object data)
	{
		_data.Add(tag, data);
	}

	public object GetExtensionData(string tag)
	{
		_data.TryGetValue(tag, out var value);
		return value;
	}
}
