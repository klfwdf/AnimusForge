using System.Collections.Generic;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library.CodeGeneration;

namespace TaleWorlds.GauntletUI.CodeGenerator;

public class PrefabDependencyContext
{
	private List<PrefabDependency> _prefabDependencies;

	private int _dependencyIndex;

	public string RootClassName { get; private set; }

	public PrefabDependencyContext(string rootClassName)
	{
		RootClassName = rootClassName;
		_prefabDependencies = new List<PrefabDependency>();
	}

	public string GenerateDependencyName()
	{
		_dependencyIndex++;
		return RootClassName + "_Dependency_" + _dependencyIndex;
	}

	public void AddDependentWidgetTemplateGenerateContext(WidgetTemplateGenerateContext widgetTemplateGenerateContext)
	{
		if (widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.DependendPrefab)
		{
			PrefabDependency item = new PrefabDependency(widgetTemplateGenerateContext.PrefabName, widgetTemplateGenerateContext.VariantName, isRoot: false, widgetTemplateGenerateContext);
			_prefabDependencies.Add(item);
		}
		if (widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.InheritedDependendPrefab)
		{
			PrefabDependency item2 = new PrefabDependency(widgetTemplateGenerateContext.PrefabName, widgetTemplateGenerateContext.VariantName, isRoot: true, widgetTemplateGenerateContext);
			_prefabDependencies.Add(item2);
		}
		else if (widgetTemplateGenerateContext.ContextType == WidgetTemplateGenerateContextType.CustomWidgetTemplate)
		{
			PrefabDependency item3 = new PrefabDependency(widgetTemplateGenerateContext.ClassName, widgetTemplateGenerateContext.VariantName, isRoot: false, widgetTemplateGenerateContext);
			_prefabDependencies.Add(item3);
		}
	}

	public PrefabDependency GetDependendPrefab(string type, Dictionary<string, WidgetAttributeTemplate> givenParameters, Dictionary<string, object> data, bool isRoot)
	{
		foreach (PrefabDependency prefabDependency in _prefabDependencies)
		{
			if (prefabDependency.Type == type && prefabDependency.IsRoot == isRoot)
			{
				Dictionary<string, WidgetAttributeTemplate> givenParameters2 = prefabDependency.WidgetTemplateGenerateContext.VariableCollection.GivenParameters;
				Dictionary<string, object> data2 = prefabDependency.WidgetTemplateGenerateContext.Data;
				if (CompareGivenParameters(givenParameters, givenParameters2) && CompareData(data, data2))
				{
					return prefabDependency;
				}
			}
		}
		return null;
	}

	private static bool CompareGivenParameters(Dictionary<string, WidgetAttributeTemplate> a, Dictionary<string, WidgetAttributeTemplate> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, WidgetAttributeTemplate> item in a)
		{
			WidgetAttributeTemplate value = item.Value;
			if (!b.ContainsKey(item.Key))
			{
				return false;
			}
			WidgetAttributeTemplate widgetAttributeTemplate = b[item.Key];
			if (value.Value != widgetAttributeTemplate.Value || value.KeyType.GetType() != widgetAttributeTemplate.KeyType.GetType() || value.ValueType.GetType() != widgetAttributeTemplate.ValueType.GetType())
			{
				return false;
			}
		}
		return true;
	}

	private static bool CompareData(Dictionary<string, object> a, Dictionary<string, object> b)
	{
		if (a.Count != b.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, object> item in a)
		{
			object value = item.Value;
			if (!b.ContainsKey(item.Key))
			{
				return false;
			}
			object obj = b[item.Key];
			if (value != obj)
			{
				return false;
			}
		}
		return true;
	}

	public void GenerateInto(NamespaceCode namespaceCode)
	{
		for (int i = 0; i < _prefabDependencies.Count; i++)
		{
			_prefabDependencies[i].WidgetTemplateGenerateContext.GenerateInto(namespaceCode);
		}
	}

	public bool ContainsDependency(string type, Dictionary<string, WidgetAttributeTemplate> givenParameters, Dictionary<string, object> data, bool isRoot)
	{
		return GetDependendPrefab(type, givenParameters, data, isRoot) != null;
	}
}
