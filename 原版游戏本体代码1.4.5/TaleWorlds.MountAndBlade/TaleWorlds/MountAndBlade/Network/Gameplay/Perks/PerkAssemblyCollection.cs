using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Network.Gameplay.Perks;

internal static class PerkAssemblyCollection
{
	public static List<Type> GetPerkAssemblyTypes()
	{
		List<Type> list = new List<Type>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		List<Assembly> list2 = new List<Assembly>();
		Assembly[] array = assemblies;
		foreach (Assembly assembly in array)
		{
			try
			{
				if (CheckAssemblyForPerks(assembly))
				{
					list2.Add(assembly);
				}
			}
			catch
			{
			}
		}
		foreach (Assembly item in list2)
		{
			try
			{
				List<Type> typesSafe = item.GetTypesSafe();
				list.AddRange(typesSafe);
			}
			catch
			{
			}
		}
		return list;
	}

	private static bool CheckAssemblyForPerks(Assembly assembly)
	{
		Assembly assembly2 = Assembly.GetAssembly(typeof(MPPerkObject));
		if (assembly == assembly2)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		for (int i = 0; i < referencedAssemblies.Length; i++)
		{
			if (referencedAssemblies[i].FullName == assembly2.FullName)
			{
				return true;
			}
		}
		return false;
	}
}
