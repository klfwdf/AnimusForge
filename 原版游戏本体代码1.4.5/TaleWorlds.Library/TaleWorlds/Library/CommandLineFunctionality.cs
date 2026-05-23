using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library;

public static class CommandLineFunctionality
{
	private class CommandLineFunction
	{
		public Func<List<string>, string> CommandLineFunc;

		public List<CommandLineFunction> Children;

		public CommandLineFunction(Func<List<string>, string> commandlinefunc)
		{
			CommandLineFunc = commandlinefunc;
			Children = new List<CommandLineFunction>();
		}

		public string Call(List<string> objects)
		{
			return CommandLineFunc(objects);
		}
	}

	public class CommandLineArgumentFunction : Attribute
	{
		public string Name;

		public string GroupName;

		public CommandLineArgumentFunction(string name, string groupname)
		{
			Name = name;
			GroupName = groupname;
		}
	}

	private static Dictionary<string, CommandLineFunction> AllFunctions = new Dictionary<string, CommandLineFunction>();

	private static bool CheckAssemblyReferencesThis(Assembly assembly)
	{
		Assembly assembly2 = typeof(CommandLineFunctionality).Assembly;
		if (assembly2.GetName().Name == assembly.GetName().Name)
		{
			return true;
		}
		AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
		for (int i = 0; i < referencedAssemblies.Length; i++)
		{
			if (referencedAssemblies[i].Name == assembly2.GetName().Name)
			{
				return true;
			}
		}
		return false;
	}

	public static List<string> CollectCommandLineFunctions()
	{
		List<string> list = new List<string>();
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (!CheckAssemblyReferencesThis(assembly))
			{
				continue;
			}
			foreach (Type item in assembly.GetTypesSafe())
			{
				MethodInfo[] methods = item.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(typeof(CommandLineArgumentFunction), inherit: false);
					if (customAttributesSafe != null && customAttributesSafe.Length != 0 && customAttributesSafe[0] is CommandLineArgumentFunction commandLineArgumentFunction && !(methodInfo.ReturnType != typeof(string)))
					{
						string name = commandLineArgumentFunction.Name;
						string text = commandLineArgumentFunction.GroupName + "." + name;
						if (!AllFunctions.ContainsKey(text))
						{
							list.Add(text);
							CommandLineFunction value = new CommandLineFunction((Func<List<string>, string>)Delegate.CreateDelegate(typeof(Func<List<string>, string>), methodInfo));
							AllFunctions.Add(text, value);
						}
					}
				}
			}
		}
		return list;
	}

	public static bool HasFunctionForCommand(string command)
	{
		CommandLineFunction value;
		return AllFunctions.TryGetValue(command, out value);
	}

	public static string CallFunction(string concatName, string concatArguments, out bool found)
	{
		if (AllFunctions.TryGetValue(concatName, out var value))
		{
			List<string> objects = ((!(concatArguments != string.Empty)) ? new List<string>() : new List<string>(concatArguments.Split(new char[1] { ' ' })));
			found = true;
			return value.Call(objects);
		}
		found = false;
		return "Could not find the command " + concatName;
	}

	public static string CallFunction(string concatName, List<string> argList, out bool found)
	{
		if (AllFunctions.TryGetValue(concatName, out var value))
		{
			found = true;
			return value.Call(argList);
		}
		found = false;
		return "Could not find the command " + concatName;
	}
}
