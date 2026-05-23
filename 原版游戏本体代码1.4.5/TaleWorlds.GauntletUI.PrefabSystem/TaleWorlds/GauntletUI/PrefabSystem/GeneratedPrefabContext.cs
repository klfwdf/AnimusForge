using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.PrefabSystem;

public class GeneratedPrefabContext
{
	private Assembly[] _assemblies;

	private List<IGeneratedUIPrefabCreator> _prefabCreators;

	private Dictionary<string, Dictionary<string, CreateGeneratedWidget>> _generatedPrefabs;

	public GeneratedPrefabContext()
	{
		_generatedPrefabs = new Dictionary<string, Dictionary<string, CreateGeneratedWidget>>();
		_prefabCreators = new List<IGeneratedUIPrefabCreator>();
	}

	public void CollectPrefabs()
	{
		_generatedPrefabs.Clear();
		_assemblies = GetPrefabAssemblies();
		FindGeneratedPrefabCreators();
		foreach (IGeneratedUIPrefabCreator prefabCreator in _prefabCreators)
		{
			prefabCreator.CollectGeneratedPrefabDefinitions(this);
		}
	}

	public void AddGeneratedPrefab(string prefabName, string variantName, CreateGeneratedWidget creator)
	{
		if (!_generatedPrefabs.ContainsKey(prefabName))
		{
			_generatedPrefabs.Add(prefabName, new Dictionary<string, CreateGeneratedWidget>());
		}
		if (!_generatedPrefabs[prefabName].ContainsKey(variantName))
		{
			_generatedPrefabs[prefabName].Add(variantName, creator);
		}
		else
		{
			_generatedPrefabs[prefabName][variantName] = creator;
		}
	}

	private static Assembly[] GetPrefabAssemblies()
	{
		List<Assembly> list = new List<Assembly>();
		Assembly assembly = typeof(WidgetPrefab).Assembly;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		list.Add(assembly);
		Assembly[] array = assemblies;
		foreach (Assembly assembly2 in array)
		{
			if (!(assembly2 != assembly))
			{
				continue;
			}
			AssemblyName[] referencedAssemblies = assembly2.GetReferencedAssemblies();
			for (int j = 0; j < referencedAssemblies.Length; j++)
			{
				if (referencedAssemblies[j].ToString() == assembly.GetName().ToString())
				{
					list.Add(assembly2);
					break;
				}
			}
		}
		return list.ToArray();
	}

	private void FindGeneratedPrefabCreators()
	{
		_prefabCreators.Clear();
		Assembly[] assemblies = _assemblies;
		for (int i = 0; i < assemblies.Length; i++)
		{
			List<Type> typesSafe = assemblies[i].GetTypesSafe();
			for (int j = 0; j < typesSafe.Count; j++)
			{
				Type type = typesSafe[j];
				if (typeof(IGeneratedUIPrefabCreator).IsAssignableFrom(type) && typeof(IGeneratedUIPrefabCreator) != type)
				{
					IGeneratedUIPrefabCreator item = (IGeneratedUIPrefabCreator)Activator.CreateInstance(type);
					_prefabCreators.Add(item);
				}
			}
		}
	}

	public GeneratedPrefabInstantiationResult InstantiatePrefab(UIContext conext, string prefabName, string variantName, Dictionary<string, object> data)
	{
		if (_generatedPrefabs.TryGetValue(prefabName, out var value) && value.TryGetValue(variantName, out var value2))
		{
			return value2(conext, data);
		}
		return null;
	}
}
