using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Library;

namespace TaleWorlds.Engine
{
	// Token: 0x02000012 RID: 18
	public static class CrashInformationCollector
	{
		// Token: 0x06000095 RID: 149 RVA: 0x00003610 File Offset: 0x00001810
		[EngineCallback(null, false)]
		public static string CollectInformation()
		{
			List<CrashInformationCollector.CrashInformation> list = new List<CrashInformationCollector.CrashInformation>();
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					Type[] types = assembly.GetTypes();
					for (int j = 0; j < types.Length; j++)
					{
						foreach (MethodInfo methodInfo in types[j].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
						{
							object[] customAttributesSafe = methodInfo.GetCustomAttributesSafe(typeof(CrashInformationCollector.CrashInformationProvider), false);
							if (customAttributesSafe != null && customAttributesSafe.Length != 0 && customAttributesSafe[0] is CrashInformationCollector.CrashInformationProvider)
							{
								CrashInformationCollector.CrashInformation crashInformation = methodInfo.Invoke(null, new object[0]) as CrashInformationCollector.CrashInformation;
								if (crashInformation != null)
								{
									list.Add(crashInformation);
								}
							}
						}
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					foreach (Exception ex2 in ex.LoaderExceptions)
					{
						MBDebug.Print("Unable to load types from assembly: " + ex2.Message, 0, Debug.DebugColor.White, 17592186044416UL);
					}
				}
				catch (Exception ex3)
				{
					MBDebug.Print("Exception while collecting crash information : " + ex3.Message, 0, Debug.DebugColor.White, 17592186044416UL);
				}
			}
			string text = "";
			foreach (CrashInformationCollector.CrashInformation crashInformation2 in list)
			{
				foreach (ValueTuple<string, string> valueTuple in crashInformation2.Lines)
				{
					text = string.Concat(new string[] { text, "[", crashInformation2.Id, "][", valueTuple.Item1, "][", valueTuple.Item2, "]\n" });
				}
			}
			return text;
		}

		// Token: 0x020000B1 RID: 177
		public class CrashInformation
		{
			// Token: 0x06000FA4 RID: 4004 RVA: 0x00013886 File Offset: 0x00011A86
			public CrashInformation(string id, MBReadOnlyList<ValueTuple<string, string>> lines)
			{
				this.Id = id;
				this.Lines = lines;
			}

			// Token: 0x04000357 RID: 855
			public readonly string Id;

			// Token: 0x04000358 RID: 856
			public readonly MBReadOnlyList<ValueTuple<string, string>> Lines;
		}

		// Token: 0x020000B2 RID: 178
		[AttributeUsage(AttributeTargets.Method)]
		public class CrashInformationProvider : Attribute
		{
		}
	}
}
