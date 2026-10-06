using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

public static class CrashInformationCollector
{
	public class CrashInformation
	{
		public readonly string Id;

		public readonly MBReadOnlyList<(string, string)> Lines;

		public CrashInformation(string id, MBReadOnlyList<(string, string)> lines)
		{
			Id = id;
			Lines = lines;
		}
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class CrashInformationProvider : Attribute
	{
	}

	[EngineCallback(null, false)]
	public static string CollectInformation()
	{
		List<CrashInformation> list = new List<CrashInformation>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			try
			{
				Type[] types = assembly.GetTypes();
				for (int j = 0; j < types.Length; j++)
				{
					MethodInfo[] methods = types[j].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (MethodInfo methodInfo in methods)
					{
						object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(typeof(CrashInformationProvider), inherit: false);
						if (customAttributesSafe != null && customAttributesSafe.Length != 0 && customAttributesSafe[0] is CrashInformationProvider && methodInfo.Invoke(null, new object[0]) is CrashInformation item)
						{
							list.Add(item);
						}
					}
				}
			}
			catch (ReflectionTypeLoadException ex)
			{
				Exception[] loaderExceptions = ex.LoaderExceptions;
				foreach (Exception ex2 in loaderExceptions)
				{
					MBDebug.Print("Unable to load types from assembly: " + ex2.Message);
				}
			}
			catch (Exception ex3)
			{
				MBDebug.Print("Exception while collecting crash information : " + ex3.Message);
			}
		}
		string text = "";
		foreach (CrashInformation item2 in list)
		{
			foreach (var line in item2.Lines)
			{
				text = text + "[" + item2.Id + "][" + line.Item1 + "][" + line.Item2 + "]\n";
			}
		}
		return text;
	}
}
