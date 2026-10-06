using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library;

public static class AssemblyLoader
{
	public enum AssemblyLoadResult
	{
		Success,
		LoadedWithErrors,
		CriticalError
	}

	private static List<Assembly> _loadedAssemblies;

	static AssemblyLoader()
	{
		_loadedAssemblies = new List<Assembly>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly item in assemblies)
		{
			_loadedAssemblies.Add(item);
		}
		AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
	}

	public static void Initialize()
	{
	}

	public static Assembly LoadFrom(string assemblyFile, bool showError = true)
	{
		AssemblyLoadResult result;
		return LoadFrom(assemblyFile, out result, showError);
	}

	public static Assembly LoadFrom(string assemblyFile, out AssemblyLoadResult result, bool showError = true)
	{
		Assembly assembly = null;
		Debug.Print("Loading assembly: " + assemblyFile + "\n");
		try
		{
			assembly = Assembly.LoadFrom(assemblyFile);
			result = AssemblyLoadResult.Success;
		}
		catch (Exception ex)
		{
			if (showError)
			{
				Debug.ShowMessageBox("Cannot load: " + assemblyFile, "ERROR", 4u);
			}
			Debug.Print("ERROR: " + assemblyFile + ": " + ex.Message);
			if (ex.InnerException != null)
			{
				Debug.Print($"ERROR: {assemblyFile}: {ex.InnerException}");
			}
			result = AssemblyLoadResult.CriticalError;
		}
		if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.DotNetCore && assembly != null && !_loadedAssemblies.Contains(assembly))
		{
			_loadedAssemblies.Add(assembly);
			AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
			for (int i = 0; i < referencedAssemblies.Length; i++)
			{
				string text = referencedAssemblies[i].Name + ".dll";
				if (!text.StartsWith("System") && !text.StartsWith("mscorlib") && !text.StartsWith("netstandard"))
				{
					LoadFrom(text, out result);
					if (result != AssemblyLoadResult.Success)
					{
						result = AssemblyLoadResult.LoadedWithErrors;
					}
				}
			}
		}
		Debug.Print("Assembly load result: " + ((assembly == null) ? "NULL" : "SUCCESS"));
		return assembly;
	}

	private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.FullName == args.Name)
			{
				return assembly;
			}
		}
		if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.Mono && ApplicationPlatform.IsPlatformWindows())
		{
			return LoadFrom(args.Name.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0] + ".dll", showError: false);
		}
		return null;
	}
}
