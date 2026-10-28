using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class AssemblyLoader : IAssemblyLoader
{
	public IEnumerable<Assembly> Load(string searchPattern)
	{
		string directory = Path.GetDirectoryName(new Uri(GetAssemblyCodeBasePath()).LocalPath);
		string[] searchPatterns = searchPattern.Split(new char[1] { '|' });
		foreach (string file in searchPatterns.SelectMany((string sp) => Directory.GetFiles(directory, sp)).Where(CanLoad))
		{
			yield return LoadAssembly(file);
		}
	}

	protected virtual bool CanLoad(string fileName)
	{
		return true;
	}

	protected virtual Assembly LoadAssembly(string filename)
	{
		return Assembly.LoadFrom(filename);
	}

	protected virtual string GetAssemblyCodeBasePath()
	{
		return typeof(ServiceContainer).Assembly.Location;
	}
}
