using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace TaleWorlds.SaveSystem.Definition;

public class SaveCodeGenerationContext
{
	private Dictionary<Assembly, SaveCodeGenerationContextAssembly> _assemblies;

	private DefinitionContext _definitionContext;

	public SaveCodeGenerationContext(DefinitionContext definitionContext)
	{
		_definitionContext = definitionContext;
		_assemblies = new Dictionary<Assembly, SaveCodeGenerationContextAssembly>();
	}

	public void AddAssembly(Assembly assembly, string defaultNamespace, string location, string fileName)
	{
		SaveCodeGenerationContextAssembly value = new SaveCodeGenerationContextAssembly(_definitionContext, assembly, defaultNamespace, location, fileName);
		_assemblies.Add(assembly, value);
	}

	internal SaveCodeGenerationContextAssembly FindAssemblyInformation(Assembly assembly)
	{
		_assemblies.TryGetValue(assembly, out var value);
		return value;
	}

	internal void FillFiles()
	{
		List<Tuple<string, string>> list = new List<Tuple<string, string>>();
		foreach (SaveCodeGenerationContextAssembly value in _assemblies.Values)
		{
			value.Generate();
			string item = value.GenerateText();
			list.Add(new Tuple<string, string>(value.Location + value.FileName, item));
		}
		foreach (Tuple<string, string> item2 in list)
		{
			File.WriteAllText(item2.Item1, item2.Item2, Encoding.UTF8);
		}
	}
}
