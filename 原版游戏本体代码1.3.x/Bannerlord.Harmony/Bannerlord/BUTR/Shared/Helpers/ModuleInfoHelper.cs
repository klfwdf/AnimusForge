using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Xml;
using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.ModuleManager;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BUTR.Shared.Helpers
{
	// Token: 0x02000010 RID: 16
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class ModuleInfoHelper
	{
		// Token: 0x060000AC RID: 172 RVA: 0x00004281 File Offset: 0x00002481
		static ModuleInfoHelper()
		{
			ModuleInfoHelper._platformModuleExtensionField = AccessTools2.StaticFieldRefAccess<IPlatformModuleExtension>("TaleWorlds.ModuleManager.ModuleHelper:_platformModuleExtension", true);
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000042BC File Offset: 0x000024BC
		[return: Nullable(2)]
		public static ModuleInfoExtendedHelper LoadFromId(string id)
		{
			return ModuleInfoHelper.GetModules().FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == id);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000042EC File Offset: 0x000024EC
		public static IEnumerable<ModuleInfoExtendedHelper> GetLoadedModules()
		{
			return new ModuleInfoHelper.<GetLoadedModules>d__5(-2);
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000042F5 File Offset: 0x000024F5
		public static IEnumerable<ModuleInfoExtendedHelper> GetModules()
		{
			return ModuleInfoHelper._cachedModules.Value;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00004304 File Offset: 0x00002504
		[NullableContext(2)]
		public static string GetModulePath(Type type)
		{
			bool flag = type == null;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = string.IsNullOrWhiteSpace(type.Assembly.Location);
				if (flag2)
				{
					result = null;
				}
				else
				{
					string modulePath;
					bool flag3 = ModuleInfoHelper._cachedAssemblyLocationToModulePath.TryGetValue(type.Assembly.Location, out modulePath);
					if (flag3)
					{
						result = modulePath;
					}
					else
					{
						FileInfo assemblyFile = new FileInfo(Path.GetFullPath(type.Assembly.Location));
						DirectoryInfo directoryInfo = ModuleInfoHelper.<GetModulePath>g__GetMainDirectory|9_0(assemblyFile.Directory);
						modulePath = (result = ((directoryInfo != null) ? directoryInfo.FullName : null));
					}
				}
			}
			return result;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x00004390 File Offset: 0x00002590
		[return: Nullable(2)]
		public static string GetModulePath(ModuleInfoExtended module)
		{
			ModuleInfoExtendedHelper moduleInfoExtendedHelper = ModuleInfoHelper._cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == module.Id);
			return (moduleInfoExtendedHelper != null) ? moduleInfoExtendedHelper.Path : null;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x000043D4 File Offset: 0x000025D4
		[NullableContext(2)]
		public static ModuleInfoExtendedHelper GetModuleByType(Type type)
		{
			string modulePath = ModuleInfoHelper.GetModulePath(type);
			return ModuleInfoHelper._cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Path == modulePath);
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x00004414 File Offset: 0x00002614
		private static string GetFullPathWithEndingSlashes(string input)
		{
			DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(0, 2);
			defaultInterpolatedStringHandler.AppendFormatted(Path.GetFullPath(input).TrimEnd(new char[]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}));
			defaultInterpolatedStringHandler.AppendFormatted<char>(Path.DirectorySeparatorChar);
			return defaultInterpolatedStringHandler.ToStringAndClear();
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00004467 File Offset: 0x00002667
		public static IEnumerable<ModuleInfoExtendedHelper> GetPhysicalModules()
		{
			return new ModuleInfoHelper.<GetPhysicalModules>d__13(-2);
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00004470 File Offset: 0x00002670
		public static IEnumerable<ModuleInfoExtendedHelper> GetPlatformModules()
		{
			return new ModuleInfoHelper.<GetPlatformModules>d__14(-2);
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00004479 File Offset: 0x00002679
		public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo)
		{
			return ModuleInfoHelper.CheckIfSubModuleCanBeLoaded(subModuleInfo, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x000044AC File Offset: 0x000026AC
		public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
		{
			bool flag = subModuleInfo.Tags.Count <= 0;
			bool result;
			if (flag)
			{
				result = true;
			}
			else
			{
				foreach (KeyValuePair<string, IReadOnlyList<string>> tuple in subModuleInfo.Tags)
				{
					string text;
					IReadOnlyList<string> readOnlyList;
					tuple.Deconstruct(out text, out readOnlyList);
					string key = text;
					IReadOnlyList<string> values = readOnlyList;
					SubModuleTags tag;
					bool flag2 = !Enum.TryParse<SubModuleTags>(key, out tag);
					if (!flag2)
					{
						bool flag3 = values.Any((string value) => !ModuleInfoHelper.GetSubModuleTagValiditiy(tag, value, cPlatform, cRuntime, cServerType, playerHostedDedicatedServer));
						if (flag3)
						{
							return false;
						}
					}
				}
				result = true;
			}
			return result;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00004594 File Offset: 0x00002794
		public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value)
		{
			return ModuleInfoHelper.GetSubModuleTagValiditiy(tag, value, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x000045C8 File Offset: 0x000027C8
		public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
		{
			if (!true)
			{
			}
			bool result;
			switch (tag)
			{
			case SubModuleTags.RejectedPlatform:
			{
				Platform platform;
				result = !Enum.TryParse<Platform>(value, out platform) || cPlatform != platform;
				break;
			}
			case SubModuleTags.ExclusivePlatform:
			{
				Platform platform2;
				result = !Enum.TryParse<Platform>(value, out platform2) || cPlatform == platform2;
				break;
			}
			case SubModuleTags.DedicatedServerType:
			{
				string a = value.ToLower();
				if (!true)
				{
				}
				bool flag;
				if (!(a == "none"))
				{
					if (!(a == "both"))
					{
						if (!(a == "custom"))
						{
							flag = a == "matchmaker" && cServerType == DedicatedServerType.Matchmaker;
						}
						else
						{
							flag = cServerType == DedicatedServerType.Custom;
						}
					}
					else
					{
						flag = cServerType == DedicatedServerType.None;
					}
				}
				else
				{
					flag = cServerType == DedicatedServerType.None;
				}
				if (!true)
				{
				}
				result = flag;
				break;
			}
			case SubModuleTags.IsNoRenderModeElement:
				result = value.Equals("false");
				break;
			case SubModuleTags.DependantRuntimeLibrary:
			{
				Runtime runtime;
				result = !Enum.TryParse<Runtime>(value, out runtime) || cRuntime == runtime;
				break;
			}
			case SubModuleTags.PlayerHostedDedicatedServer:
				result = playerHostedDedicatedServer && value.Equals("true");
				break;
			default:
				result = true;
				break;
			}
			if (!true)
			{
			}
			return result;
		}

		// Token: 0x060000BA RID: 186 RVA: 0x000046EC File Offset: 0x000028EC
		public static bool ValidateLoadOrder(Type subModuleType, out string report)
		{
			return ModuleInfoHelper.ValidateLoadOrder(ModuleInfoHelper.GetModuleByType(subModuleType), out report);
		}

		// Token: 0x060000BB RID: 187 RVA: 0x0000470C File Offset: 0x0000290C
		public static bool ValidateLoadOrder([Nullable(2)] ModuleInfoExtended moduleInfo, out string report)
		{
			ModuleInfoHelper.<>c__DisplayClass20_0 CS$<>8__locals1;
			CS$<>8__locals1.moduleInfo = moduleInfo;
			bool flag = CS$<>8__locals1.moduleInfo == null;
			bool result;
			if (flag)
			{
				report = "CRITICAL ERROR";
				result = false;
			}
			else
			{
				List<ModuleInfoExtendedHelper> loadedModules = ModuleInfoHelper.GetLoadedModules().ToList<ModuleInfoExtendedHelper>();
				CS$<>8__locals1.moduleIndex = ModuleInfoHelper.<ValidateLoadOrder>g__IndexOf|20_0<ModuleInfoExtended>(loadedModules, CS$<>8__locals1.moduleInfo);
				CS$<>8__locals1.sb = new StringBuilder();
				using (IEnumerator<DependentModule> enumerator = CS$<>8__locals1.moduleInfo.DependentModules.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						DependentModule dependedModule = enumerator.Current;
						ModuleInfoExtendedHelper module = loadedModules.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == dependedModule.Id);
						int dependedModuleIndex = ((module != null) ? loadedModules.IndexOf(module) : (-1));
						ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleLoadBeforeThis|20_6(dependedModuleIndex, dependedModule.Id, false, ref CS$<>8__locals1);
					}
				}
				using (IEnumerator<DependentModuleMetadata> enumerator2 = CS$<>8__locals1.moduleInfo.DependentModuleMetadatas.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						DependentModuleMetadata dependedModule = enumerator2.Current;
						ModuleInfoExtendedHelper module2 = loadedModules.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == dependedModule.Id);
						int dependedModuleIndex2 = ((module2 != null) ? loadedModules.IndexOf(module2) : (-1));
						bool isIncompatible = dependedModule.IsIncompatible;
						if (isIncompatible)
						{
							bool flag2 = CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id);
							if (flag2)
							{
								ModuleInfoHelper.<ValidateLoadOrder>g__ReportMutuallyExclusiveDirectives|20_4(dependedModule.Id, ref CS$<>8__locals1);
							}
							else
							{
								ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleCompatibility|20_5(dependedModuleIndex2, dependedModule.Id, ref CS$<>8__locals1);
							}
						}
						else
						{
							bool flag3 = dependedModule.LoadType == LoadType.LoadBeforeThis;
							if (flag3)
							{
								bool flag4 = CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id);
								if (!flag4)
								{
									ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleLoadBeforeThis|20_6(dependedModuleIndex2, dependedModule.Id, dependedModule.IsOptional, ref CS$<>8__locals1);
								}
							}
							else
							{
								bool flag5 = dependedModule.LoadType == LoadType.LoadAfterThis;
								if (flag5)
								{
									bool flag6 = CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id) || CS$<>8__locals1.moduleInfo.DependentModuleMetadatas.Any((DependentModuleMetadata dm) => dm.Id == dependedModule.Id && dm.LoadType == LoadType.LoadBeforeThis);
									if (flag6)
									{
										ModuleInfoHelper.<ValidateLoadOrder>g__ReportMutuallyExclusiveDirectives|20_4(dependedModule.Id, ref CS$<>8__locals1);
									}
									else
									{
										ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleLoadAfterThis|20_7(dependedModuleIndex2, dependedModule.Id, dependedModule.IsOptional, ref CS$<>8__locals1);
									}
								}
							}
						}
					}
				}
				bool flag7 = CS$<>8__locals1.sb.Length > 0;
				if (flag7)
				{
					report = CS$<>8__locals1.sb.ToString();
					result = false;
				}
				else
				{
					report = string.Empty;
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00004A34 File Offset: 0x00002C34
		public static bool IsModuleAssembly(ModuleInfoExtendedHelper loadedModule, Assembly assembly)
		{
			bool flag = assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.CodeBase);
			return !flag && ModuleInfoHelper.IsInModule(loadedModule, assembly.CodeBase);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00004A70 File Offset: 0x00002C70
		public static bool IsInModule(ModuleInfoExtendedHelper loadedModule, string filePath)
		{
			bool flag = string.IsNullOrWhiteSpace(filePath);
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				Uri modulePath = new Uri(Path.GetFullPath(loadedModule.Path));
				string moduleDirectory = Path.GetFileName(loadedModule.Path);
				Uri assemblyPath = new Uri(filePath);
				Uri relativePath = modulePath.MakeRelativeUri(assemblyPath);
				result = relativePath.OriginalString.StartsWith(moduleDirectory);
			}
			return result;
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00004AD0 File Offset: 0x00002CD0
		private static bool TryReadXml(string path, [Nullable(2)] out XmlDocument xml)
		{
			bool result;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(File.ReadAllText(path));
				xml = xmlDocument;
				result = true;
			}
			catch (Exception)
			{
				xml = null;
				result = false;
			}
			return result;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x00004B14 File Offset: 0x00002D14
		[NullableContext(2)]
		[CompilerGenerated]
		internal static DirectoryInfo <GetModulePath>g__GetMainDirectory|9_0(DirectoryInfo directoryInfo)
		{
			while (((directoryInfo != null) ? directoryInfo.Parent : null) != null && directoryInfo.Exists)
			{
				bool flag = directoryInfo.GetFiles("SubModule.xml").Length == 1;
				if (flag)
				{
					return directoryInfo;
				}
				directoryInfo = directoryInfo.Parent;
			}
			return null;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x00004B68 File Offset: 0x00002D68
		[NullableContext(0)]
		[CompilerGenerated]
		internal static int <ValidateLoadOrder>g__IndexOf|20_0<T>([Nullable(new byte[] { 1, 0 })] IReadOnlyList<T> self, T elementToFind)
		{
			int i = 0;
			foreach (T element in self)
			{
				bool flag = object.Equals(element, elementToFind);
				if (flag)
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00004BD8 File Offset: 0x00002DD8
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportMissingModule|20_1(string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			bool flag = A_1.sb.Length != 0;
			if (flag)
			{
				A_1.sb.AppendLine();
			}
			StringBuilder sb = A_1.sb;
			TextObject textObject = new TextObject("{=FE6ya1gzZR}{REQUIRED_MODULE} module was not found!", null);
			string text;
			if (textObject == null)
			{
				text = null;
			}
			else
			{
				TextObject textObject2 = textObject.SetTextVariable("REQUIRED_MODULE", requiredModuleId);
				text = ((textObject2 != null) ? textObject2.ToString() : null);
			}
			sb.AppendLine(text ?? "ERROR");
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x00004C44 File Offset: 0x00002E44
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportIncompatibleModule|20_2(string deniedModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			bool flag = A_1.sb.Length != 0;
			if (flag)
			{
				A_1.sb.AppendLine();
			}
			StringBuilder sb = A_1.sb;
			TextObject textObject = new TextObject("{=EvI6KPAqTT}Incompatible module {DENIED_MODULE} was found!", null);
			string text;
			if (textObject == null)
			{
				text = null;
			}
			else
			{
				TextObject textObject2 = textObject.SetTextVariable("DENIED_MODULE", deniedModuleId);
				text = ((textObject2 != null) ? textObject2.ToString() : null);
			}
			sb.AppendLine(text ?? "ERROR");
		}

		// Token: 0x060000C3 RID: 195 RVA: 0x00004CB0 File Offset: 0x00002EB0
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3(string reason, string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_2)
		{
			bool flag = A_2.sb.Length != 0;
			if (flag)
			{
				A_2.sb.AppendLine();
			}
			StringBuilder sb = A_2.sb;
			TextObject textObject = new TextObject(reason, null);
			string text;
			if (textObject == null)
			{
				text = null;
			}
			else
			{
				TextObject textObject2 = textObject.SetTextVariable("MODULE", A_2.moduleInfo.Id);
				if (textObject2 == null)
				{
					text = null;
				}
				else
				{
					TextObject textObject3 = textObject2.SetTextVariable("REQUIRED_MODULE", requiredModuleId);
					if (textObject3 == null)
					{
						text = null;
					}
					else
					{
						TextObject textObject4 = textObject3.SetTextVariable("NL", Environment.NewLine);
						text = ((textObject4 != null) ? textObject4.ToString() : null);
					}
				}
			}
			sb.AppendLine(text ?? "ERROR");
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x00004D48 File Offset: 0x00002F48
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportMutuallyExclusiveDirectives|20_4(string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			bool flag = A_1.sb.Length != 0;
			if (flag)
			{
				A_1.sb.AppendLine();
			}
			StringBuilder sb = A_1.sb;
			TextObject textObject = new TextObject("{=FcR4BXnhx8}{MODULE} has mutually exclusive mod order directives specified for the {REQUIRED_MODULE}!", null);
			string text;
			if (textObject == null)
			{
				text = null;
			}
			else
			{
				TextObject textObject2 = textObject.SetTextVariable("MODULE", A_1.moduleInfo.Id);
				if (textObject2 == null)
				{
					text = null;
				}
				else
				{
					TextObject textObject3 = textObject2.SetTextVariable("REQUIRED_MODULE", requiredModuleId);
					text = ((textObject3 != null) ? textObject3.ToString() : null);
				}
			}
			sb.AppendLine(text ?? "ERROR");
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x00004DD0 File Offset: 0x00002FD0
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleCompatibility|20_5(int deniedModuleIndex, string deniedModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_2)
		{
			bool flag = deniedModuleIndex != -1;
			if (flag)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportIncompatibleModule|20_2(deniedModuleId, ref A_2);
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x00004DF4 File Offset: 0x00002FF4
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleLoadBeforeThis|20_6(int requiredModuleIndex, string requiredModuleId, bool isOptional = false, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_3)
		{
			bool flag = !isOptional && requiredModuleIndex == -1;
			if (flag)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportMissingModule|20_1(requiredModuleId, ref A_3);
			}
			else
			{
				bool flag2 = requiredModuleIndex > A_3.moduleIndex;
				if (flag2)
				{
					ModuleInfoHelper.<ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3("{=5G9zffrgMh}{MODULE} is loaded before the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded after it!", requiredModuleId, ref A_3);
				}
			}
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x00004E38 File Offset: 0x00003038
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleLoadAfterThis|20_7(int requiredModuleIndex, string requiredModuleId, bool isOptional, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_3)
		{
			bool flag = requiredModuleIndex == -1;
			if (flag)
			{
				bool flag2 = !isOptional;
				if (flag2)
				{
					ModuleInfoHelper.<ValidateLoadOrder>g__ReportMissingModule|20_1(requiredModuleId, ref A_3);
				}
			}
			else
			{
				bool flag3 = requiredModuleIndex < A_3.moduleIndex;
				if (flag3)
				{
					ModuleInfoHelper.<ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3("{=UZ8zfvudMs}{MODULE} is loaded after the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded before it!", requiredModuleId, ref A_3);
				}
			}
		}

		// Token: 0x04000028 RID: 40
		public const string ModulesFolder = "Modules";

		// Token: 0x04000029 RID: 41
		public const string SubModuleFile = "SubModule.xml";

		// Token: 0x0400002A RID: 42
		[Nullable(new byte[] { 2, 1 })]
		private static readonly AccessTools.FieldRef<IPlatformModuleExtension> _platformModuleExtensionField;

		// Token: 0x0400002B RID: 43
		private static Lazy<List<ModuleInfoExtendedHelper>> _cachedModules = new Lazy<List<ModuleInfoExtendedHelper>>(delegate()
		{
			List<ModuleInfoExtendedHelper> list = new List<ModuleInfoExtendedHelper>();
			HashSet<string> foundIds = new HashSet<string>();
			foreach (ModuleInfoExtendedHelper moduleInfo in ModuleInfoHelper.GetPhysicalModules().Concat(ModuleInfoHelper.GetPlatformModules()))
			{
				bool flag = !foundIds.Contains(moduleInfo.Id.ToLower());
				if (flag)
				{
					foundIds.Add(moduleInfo.Id.ToLower());
					list.Add(moduleInfo);
				}
			}
			return list;
		}, LazyThreadSafetyMode.ExecutionAndPublication);

		// Token: 0x0400002C RID: 44
		private static ConcurrentDictionary<string, string> _cachedAssemblyLocationToModulePath = new ConcurrentDictionary<string, string>();
	}
}
