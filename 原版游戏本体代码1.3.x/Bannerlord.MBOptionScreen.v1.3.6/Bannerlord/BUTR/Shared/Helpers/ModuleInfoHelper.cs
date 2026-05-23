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
	// Token: 0x02000052 RID: 82
	[NullableContext(1)]
	[Nullable(0)]
	[ExcludeFromCodeCoverage]
	[DebuggerNonUserCode]
	internal static class ModuleInfoHelper
	{
		// Token: 0x060002B5 RID: 693 RVA: 0x00009789 File Offset: 0x00007989
		static ModuleInfoHelper()
		{
			ModuleInfoHelper._platformModuleExtensionField = AccessTools2.StaticFieldRefAccess<IPlatformModuleExtension>("TaleWorlds.ModuleManager.ModuleHelper:_platformModuleExtension", true);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x000097C0 File Offset: 0x000079C0
		[return: Nullable(2)]
		public static ModuleInfoExtendedHelper LoadFromId(string id)
		{
			return ModuleInfoHelper.GetModules().FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == id);
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x000097F0 File Offset: 0x000079F0
		public static IEnumerable<ModuleInfoExtendedHelper> GetLoadedModules()
		{
			return new ModuleInfoHelper.<GetLoadedModules>d__5(-2);
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x000097F9 File Offset: 0x000079F9
		public static IEnumerable<ModuleInfoExtendedHelper> GetModules()
		{
			return ModuleInfoHelper._cachedModules.Value;
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x00009808 File Offset: 0x00007A08
		[NullableContext(2)]
		public static string GetModulePath(Type type)
		{
			if (type == null)
			{
				return null;
			}
			if (string.IsNullOrWhiteSpace(type.Assembly.Location))
			{
				return null;
			}
			string modulePath;
			if (ModuleInfoHelper._cachedAssemblyLocationToModulePath.TryGetValue(type.Assembly.Location, out modulePath))
			{
				return modulePath;
			}
			FileInfo assemblyFile = new FileInfo(Path.GetFullPath(type.Assembly.Location));
			DirectoryInfo directoryInfo = ModuleInfoHelper.<GetModulePath>g__GetMainDirectory|9_0(assemblyFile.Directory);
			return modulePath = ((directoryInfo != null) ? directoryInfo.FullName : null);
		}

		// Token: 0x060002BA RID: 698 RVA: 0x00009878 File Offset: 0x00007A78
		[return: Nullable(2)]
		public static string GetModulePath(ModuleInfoExtended module)
		{
			ModuleInfoExtendedHelper moduleInfoExtendedHelper = ModuleInfoHelper._cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == module.Id);
			if (moduleInfoExtendedHelper == null)
			{
				return null;
			}
			return moduleInfoExtendedHelper.Path;
		}

		// Token: 0x060002BB RID: 699 RVA: 0x000098B8 File Offset: 0x00007AB8
		[NullableContext(2)]
		public static ModuleInfoExtendedHelper GetModuleByType(Type type)
		{
			string modulePath = ModuleInfoHelper.GetModulePath(type);
			return ModuleInfoHelper._cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Path == modulePath);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x000098F2 File Offset: 0x00007AF2
		private static string GetFullPathWithEndingSlashes(string input)
		{
			return string.Format("{0}{1}", Path.GetFullPath(input).TrimEnd(new char[]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}), Path.DirectorySeparatorChar);
		}

		// Token: 0x060002BD RID: 701 RVA: 0x00009929 File Offset: 0x00007B29
		public static IEnumerable<ModuleInfoExtendedHelper> GetPhysicalModules()
		{
			return new ModuleInfoHelper.<GetPhysicalModules>d__13(-2);
		}

		// Token: 0x060002BE RID: 702 RVA: 0x00009932 File Offset: 0x00007B32
		public static IEnumerable<ModuleInfoExtendedHelper> GetPlatformModules()
		{
			return new ModuleInfoHelper.<GetPlatformModules>d__14(-2);
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000993B File Offset: 0x00007B3B
		public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo)
		{
			return ModuleInfoHelper.CheckIfSubModuleCanBeLoaded(subModuleInfo, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, Module.CurrentModule.StartupInfo.DedicatedServerType, Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000996C File Offset: 0x00007B6C
		public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
		{
			if (subModuleInfo.Tags.Count <= 0)
			{
				return true;
			}
			foreach (KeyValuePair<string, IReadOnlyList<string>> tuple in subModuleInfo.Tags)
			{
				string text;
				IReadOnlyList<string> readOnlyList;
				tuple.Deconstruct(out text, out readOnlyList);
				string key = text;
				IReadOnlyList<string> values = readOnlyList;
				SubModuleTags tag;
				if (Enum.TryParse<SubModuleTags>(key, out tag) && values.Any((string value) => !ModuleInfoHelper.GetSubModuleTagValiditiy(tag, value, cPlatform, cRuntime, cServerType, playerHostedDedicatedServer)))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x00009A30 File Offset: 0x00007C30
		public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value)
		{
			return ModuleInfoHelper.GetSubModuleTagValiditiy(tag, value, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, Module.CurrentModule.StartupInfo.DedicatedServerType, Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x00009A64 File Offset: 0x00007C64
		public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
		{
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
			return result;
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x00009B73 File Offset: 0x00007D73
		public static bool ValidateLoadOrder(Type subModuleType, out string report)
		{
			return ModuleInfoHelper.ValidateLoadOrder(ModuleInfoHelper.GetModuleByType(subModuleType), out report);
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x00009B84 File Offset: 0x00007D84
		public static bool ValidateLoadOrder([Nullable(2)] ModuleInfoExtended moduleInfo, out string report)
		{
			ModuleInfoHelper.<>c__DisplayClass20_0 CS$<>8__locals1;
			CS$<>8__locals1.moduleInfo = moduleInfo;
			if (CS$<>8__locals1.moduleInfo == null)
			{
				report = "CRITICAL ERROR";
				return false;
			}
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
					if (dependedModule.IsIncompatible)
					{
						if (CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id))
						{
							ModuleInfoHelper.<ValidateLoadOrder>g__ReportMutuallyExclusiveDirectives|20_4(dependedModule.Id, ref CS$<>8__locals1);
						}
						else
						{
							ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleCompatibility|20_5(dependedModuleIndex2, dependedModule.Id, ref CS$<>8__locals1);
						}
					}
					else if (dependedModule.LoadType == LoadType.LoadBeforeThis)
					{
						if (!CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id))
						{
							ModuleInfoHelper.<ValidateLoadOrder>g__ValidateDependedModuleLoadBeforeThis|20_6(dependedModuleIndex2, dependedModule.Id, dependedModule.IsOptional, ref CS$<>8__locals1);
						}
					}
					else if (dependedModule.LoadType == LoadType.LoadAfterThis)
					{
						if (CS$<>8__locals1.moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule.Id) || CS$<>8__locals1.moduleInfo.DependentModuleMetadatas.Any((DependentModuleMetadata dm) => dm.Id == dependedModule.Id && dm.LoadType == LoadType.LoadBeforeThis))
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
			if (CS$<>8__locals1.sb.Length > 0)
			{
				report = CS$<>8__locals1.sb.ToString();
				return false;
			}
			report = string.Empty;
			return true;
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x00009E48 File Offset: 0x00008048
		public static bool IsModuleAssembly(ModuleInfoExtendedHelper loadedModule, Assembly assembly)
		{
			return !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.CodeBase) && ModuleInfoHelper.IsInModule(loadedModule, assembly.CodeBase);
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x00009E70 File Offset: 0x00008070
		public static bool IsInModule(ModuleInfoExtendedHelper loadedModule, string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return false;
			}
			Uri modulePath = new Uri(Path.GetFullPath(loadedModule.Path));
			string moduleDirectory = Path.GetFileName(loadedModule.Path);
			Uri assemblyPath = new Uri(filePath);
			Uri relativePath = modulePath.MakeRelativeUri(assemblyPath);
			return relativePath.OriginalString.StartsWith(moduleDirectory);
		}

		// Token: 0x060002C7 RID: 711 RVA: 0x00009EC0 File Offset: 0x000080C0
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

		// Token: 0x060002C8 RID: 712 RVA: 0x00009F00 File Offset: 0x00008100
		[NullableContext(2)]
		[CompilerGenerated]
		internal static DirectoryInfo <GetModulePath>g__GetMainDirectory|9_0(DirectoryInfo directoryInfo)
		{
			while (((directoryInfo != null) ? directoryInfo.Parent : null) != null && directoryInfo.Exists)
			{
				if (directoryInfo.GetFiles("SubModule.xml").Length == 1)
				{
					return directoryInfo;
				}
				directoryInfo = directoryInfo.Parent;
			}
			return null;
		}

		// Token: 0x060002C9 RID: 713 RVA: 0x00009F38 File Offset: 0x00008138
		[NullableContext(0)]
		[CompilerGenerated]
		internal static int <ValidateLoadOrder>g__IndexOf|20_0<T>([Nullable(new byte[] { 1, 0 })] IReadOnlyList<T> self, T elementToFind)
		{
			int i = 0;
			foreach (T element in self)
			{
				if (object.Equals(element, elementToFind))
				{
					return i;
				}
				i++;
			}
			return -1;
		}

		// Token: 0x060002CA RID: 714 RVA: 0x00009F9C File Offset: 0x0000819C
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportMissingModule|20_1(string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			if (A_1.sb.Length != 0)
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

		// Token: 0x060002CB RID: 715 RVA: 0x0000A000 File Offset: 0x00008200
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportIncompatibleModule|20_2(string deniedModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			if (A_1.sb.Length != 0)
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

		// Token: 0x060002CC RID: 716 RVA: 0x0000A064 File Offset: 0x00008264
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3(string reason, string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_2)
		{
			if (A_2.sb.Length != 0)
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

		// Token: 0x060002CD RID: 717 RVA: 0x0000A0F8 File Offset: 0x000082F8
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ReportMutuallyExclusiveDirectives|20_4(string requiredModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_1)
		{
			if (A_1.sb.Length != 0)
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

		// Token: 0x060002CE RID: 718 RVA: 0x0000A178 File Offset: 0x00008378
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleCompatibility|20_5(int deniedModuleIndex, string deniedModuleId, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_2)
		{
			if (deniedModuleIndex != -1)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportIncompatibleModule|20_2(deniedModuleId, ref A_2);
			}
		}

		// Token: 0x060002CF RID: 719 RVA: 0x0000A185 File Offset: 0x00008385
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleLoadBeforeThis|20_6(int requiredModuleIndex, string requiredModuleId, bool isOptional = false, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_3)
		{
			if (!isOptional && requiredModuleIndex == -1)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportMissingModule|20_1(requiredModuleId, ref A_3);
				return;
			}
			if (requiredModuleIndex > A_3.moduleIndex)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3("{=5G9zffrgMh}{MODULE} is loaded before the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded after it!", requiredModuleId, ref A_3);
			}
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x0000A1AB File Offset: 0x000083AB
		[CompilerGenerated]
		internal static void <ValidateLoadOrder>g__ValidateDependedModuleLoadAfterThis|20_7(int requiredModuleIndex, string requiredModuleId, bool isOptional, ref ModuleInfoHelper.<>c__DisplayClass20_0 A_3)
		{
			if (requiredModuleIndex == -1)
			{
				if (!isOptional)
				{
					ModuleInfoHelper.<ValidateLoadOrder>g__ReportMissingModule|20_1(requiredModuleId, ref A_3);
					return;
				}
			}
			else if (requiredModuleIndex < A_3.moduleIndex)
			{
				ModuleInfoHelper.<ValidateLoadOrder>g__ReportLoadingOrderIssue|20_3("{=UZ8zfvudMs}{MODULE} is loaded after the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded before it!", requiredModuleId, ref A_3);
			}
		}

		// Token: 0x040000F8 RID: 248
		public const string ModulesFolder = "Modules";

		// Token: 0x040000F9 RID: 249
		public const string SubModuleFile = "SubModule.xml";

		// Token: 0x040000FA RID: 250
		[Nullable(new byte[] { 2, 1 })]
		private static readonly AccessTools.FieldRef<IPlatformModuleExtension> _platformModuleExtensionField;

		// Token: 0x040000FB RID: 251
		private static Lazy<List<ModuleInfoExtendedHelper>> _cachedModules = new Lazy<List<ModuleInfoExtendedHelper>>(delegate()
		{
			List<ModuleInfoExtendedHelper> list = new List<ModuleInfoExtendedHelper>();
			HashSet<string> foundIds = new HashSet<string>();
			foreach (ModuleInfoExtendedHelper moduleInfo in ModuleInfoHelper.GetPhysicalModules().Concat(ModuleInfoHelper.GetPlatformModules()))
			{
				if (!foundIds.Contains(moduleInfo.Id.ToLower()))
				{
					foundIds.Add(moduleInfo.Id.ToLower());
					list.Add(moduleInfo);
				}
			}
			return list;
		}, LazyThreadSafetyMode.ExecutionAndPublication);

		// Token: 0x040000FC RID: 252
		private static ConcurrentDictionary<string, string> _cachedAssemblyLocationToModulePath = new ConcurrentDictionary<string, string>();
	}
}
