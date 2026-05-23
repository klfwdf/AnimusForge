using System;
using System.Collections.Generic;
using System.Reflection;

namespace TaleWorlds.Library
{
	// Token: 0x0200000D RID: 13
	public static class AssemblyLoader
	{
		// Token: 0x0600002D RID: 45 RVA: 0x000026DC File Offset: 0x000008DC
		static AssemblyLoader()
		{
			foreach (Assembly item in AppDomain.CurrentDomain.GetAssemblies())
			{
				AssemblyLoader._loadedAssemblies.Add(item);
			}
			AppDomain.CurrentDomain.AssemblyResolve += AssemblyLoader.OnAssemblyResolve;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002731 File Offset: 0x00000931
		public static void Initialize()
		{
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002734 File Offset: 0x00000934
		public static Assembly LoadFrom(string assemblyFile, bool show_error = true)
		{
			Assembly assembly = null;
			Debug.Print("Loading assembly: " + assemblyFile + "\n", 0, Debug.DebugColor.White, 17592186044416UL);
			try
			{
				if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.DotNetCore)
				{
					try
					{
						assembly = Assembly.LoadFrom(assemblyFile);
					}
					catch (Exception)
					{
						assembly = null;
					}
					if (assembly != null && !AssemblyLoader._loadedAssemblies.Contains(assembly))
					{
						AssemblyLoader._loadedAssemblies.Add(assembly);
						AssemblyName[] referencedAssemblies = assembly.GetReferencedAssemblies();
						for (int i = 0; i < referencedAssemblies.Length; i++)
						{
							string text = referencedAssemblies[i].Name + ".dll";
							if (!text.StartsWith("System") && !text.StartsWith("mscorlib") && !text.StartsWith("netstandard"))
							{
								AssemblyLoader.LoadFrom(text, true);
							}
						}
					}
				}
				else
				{
					assembly = Assembly.LoadFrom(assemblyFile);
				}
			}
			catch (Exception ex)
			{
				if (show_error)
				{
					string lpText = "Cannot load: " + assemblyFile;
					string lpCaption = "ERROR";
					Debug.ShowMessageBox(lpText, lpCaption, 4U);
				}
				Debug.Print("ERROR: " + assemblyFile + ": " + ex.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				if (ex.InnerException != null)
				{
					Debug.Print(string.Format("ERROR: {0}: {1}", assemblyFile, ex.InnerException), 0, Debug.DebugColor.White, 17592186044416UL);
				}
			}
			Debug.Print("Assembly load result: " + ((assembly == null) ? "NULL" : "SUCCESS"), 0, Debug.DebugColor.White, 17592186044416UL);
			return assembly;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000028C4 File Offset: 0x00000AC4
		private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.FullName == args.Name)
				{
					return assembly;
				}
			}
			if (ApplicationPlatform.CurrentRuntimeLibrary == Runtime.Mono && ApplicationPlatform.IsPlatformWindows())
			{
				return AssemblyLoader.LoadFrom(args.Name.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0] + ".dll", false);
			}
			return null;
		}

		// Token: 0x0400002D RID: 45
		private static List<Assembly> _loadedAssemblies = new List<Assembly>();
	}
}
