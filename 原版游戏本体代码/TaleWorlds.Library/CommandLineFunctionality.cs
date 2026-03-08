using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library
{
	// Token: 0x02000023 RID: 35
	public static class CommandLineFunctionality
	{
		// Token: 0x060000C5 RID: 197 RVA: 0x00004714 File Offset: 0x00002914
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

		// Token: 0x060000C6 RID: 198 RVA: 0x00004784 File Offset: 0x00002984
		public static List<string> CollectCommandLineFunctions()
		{
			List<string> list = new List<string>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (CommandLineFunctionality.CheckAssemblyReferencesThis(assembly))
				{
					foreach (Type type in assembly.GetTypesSafe(null))
					{
						foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
						{
							object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(typeof(CommandLineFunctionality.CommandLineArgumentFunction), false);
							if (customAttributesSafe != null && customAttributesSafe.Length != 0)
							{
								CommandLineFunctionality.CommandLineArgumentFunction commandLineArgumentFunction = customAttributesSafe[0] as CommandLineFunctionality.CommandLineArgumentFunction;
								if (commandLineArgumentFunction != null && !(methodInfo.ReturnType != typeof(string)))
								{
									string name = commandLineArgumentFunction.Name;
									string text = commandLineArgumentFunction.GroupName + "." + name;
									if (!CommandLineFunctionality.AllFunctions.ContainsKey(text))
									{
										list.Add(text);
										CommandLineFunctionality.CommandLineFunction value = new CommandLineFunctionality.CommandLineFunction((Func<List<string>, string>)Delegate.CreateDelegate(typeof(Func<List<string>, string>), methodInfo));
										CommandLineFunctionality.AllFunctions.Add(text, value);
									}
								}
							}
						}
					}
				}
			}
			return list;
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x000048E0 File Offset: 0x00002AE0
		public static bool HasFunctionForCommand(string command)
		{
			CommandLineFunctionality.CommandLineFunction commandLineFunction;
			return CommandLineFunctionality.AllFunctions.TryGetValue(command, out commandLineFunction);
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x000048FC File Offset: 0x00002AFC
		public static string CallFunction(string concatName, string concatArguments, out bool found)
		{
			CommandLineFunctionality.CommandLineFunction commandLineFunction;
			if (CommandLineFunctionality.AllFunctions.TryGetValue(concatName, out commandLineFunction))
			{
				List<string> objects;
				if (concatArguments != string.Empty)
				{
					objects = new List<string>(concatArguments.Split(new char[] { ' ' }));
				}
				else
				{
					objects = new List<string>();
				}
				found = true;
				return commandLineFunction.Call(objects);
			}
			found = false;
			return "Could not find the command " + concatName;
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x00004960 File Offset: 0x00002B60
		public static string CallFunction(string concatName, List<string> argList, out bool found)
		{
			CommandLineFunctionality.CommandLineFunction commandLineFunction;
			if (CommandLineFunctionality.AllFunctions.TryGetValue(concatName, out commandLineFunction))
			{
				found = true;
				return commandLineFunction.Call(argList);
			}
			found = false;
			return "Could not find the command " + concatName;
		}

		// Token: 0x04000072 RID: 114
		private static Dictionary<string, CommandLineFunctionality.CommandLineFunction> AllFunctions = new Dictionary<string, CommandLineFunctionality.CommandLineFunction>();

		// Token: 0x020000C9 RID: 201
		private class CommandLineFunction
		{
			// Token: 0x0600074B RID: 1867 RVA: 0x00018470 File Offset: 0x00016670
			public CommandLineFunction(Func<List<string>, string> commandlinefunc)
			{
				this.CommandLineFunc = commandlinefunc;
				this.Children = new List<CommandLineFunctionality.CommandLineFunction>();
			}

			// Token: 0x0600074C RID: 1868 RVA: 0x0001848A File Offset: 0x0001668A
			public string Call(List<string> objects)
			{
				return this.CommandLineFunc(objects);
			}

			// Token: 0x04000254 RID: 596
			public Func<List<string>, string> CommandLineFunc;

			// Token: 0x04000255 RID: 597
			public List<CommandLineFunctionality.CommandLineFunction> Children;
		}

		// Token: 0x020000CA RID: 202
		public class CommandLineArgumentFunction : Attribute
		{
			// Token: 0x0600074D RID: 1869 RVA: 0x00018498 File Offset: 0x00016698
			public CommandLineArgumentFunction(string name, string groupname)
			{
				this.Name = name;
				this.GroupName = groupname;
			}

			// Token: 0x04000256 RID: 598
			public string Name;

			// Token: 0x04000257 RID: 599
			public string GroupName;
		}
	}
}
