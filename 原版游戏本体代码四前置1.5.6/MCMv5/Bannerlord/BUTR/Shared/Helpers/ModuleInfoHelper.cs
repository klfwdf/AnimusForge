using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;
using Bannerlord.BUTR.Shared.Extensions;
using Bannerlord.ModuleManager;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.BUTR.Shared.Helpers;

[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal static class ModuleInfoHelper
{
	public const string ModulesFolder = "Modules";

	public const string SubModuleFile = "SubModule.xml";

	private static readonly AccessTools.FieldRef<IPlatformModuleExtension>? _platformModuleExtensionField;

	private static Lazy<List<ModuleInfoExtendedHelper>> _cachedModules;

	private static ConcurrentDictionary<string, string> _cachedAssemblyLocationToModulePath;

	static ModuleInfoHelper()
	{
		_cachedModules = new Lazy<List<ModuleInfoExtendedHelper>>(delegate
		{
			List<ModuleInfoExtendedHelper> list = new List<ModuleInfoExtendedHelper>();
			HashSet<string> hashSet = new HashSet<string>();
			foreach (ModuleInfoExtendedHelper item in GetPhysicalModules().Concat(GetPlatformModules()))
			{
				if (!hashSet.Contains(item.Id.ToLower()))
				{
					hashSet.Add(item.Id.ToLower());
					list.Add(item);
				}
			}
			return list;
		}, LazyThreadSafetyMode.ExecutionAndPublication);
		_cachedAssemblyLocationToModulePath = new ConcurrentDictionary<string, string>();
		_platformModuleExtensionField = AccessTools2.StaticFieldRefAccess<IPlatformModuleExtension>("TaleWorlds.ModuleManager.ModuleHelper:_platformModuleExtension");
	}

	public static ModuleInfoExtendedHelper? LoadFromId(string id)
	{
		return GetModules().FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == id);
	}

	public static IEnumerable<ModuleInfoExtendedHelper> GetLoadedModules()
	{
		string[] moduleNames = Utilities.GetModulesNames();
		if (moduleNames.Length == 0)
		{
			yield break;
		}
		Dictionary<string, ModuleInfoExtendedHelper> allModulesAvailable = GetModules().ToDictionary((ModuleInfoExtendedHelper x) => x.Id, (ModuleInfoExtendedHelper x) => x);
		string[] array = moduleNames;
		foreach (string modulesId in array)
		{
			if (allModulesAvailable.TryGetValue(modulesId, out var moduleInfo))
			{
				yield return moduleInfo;
			}
			moduleInfo = null;
		}
	}

	public static IEnumerable<ModuleInfoExtendedHelper> GetModules()
	{
		return _cachedModules.Value;
	}

	public static string? GetModulePath(Type? type)
	{
		if ((object)type == null)
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(type.Assembly.Location))
		{
			return null;
		}
		if (_cachedAssemblyLocationToModulePath.TryGetValue(type.Assembly.Location, out string value))
		{
			return value;
		}
		FileInfo fileInfo = new FileInfo(System.IO.Path.GetFullPath(type.Assembly.Location));
		return value = GetMainDirectory(fileInfo.Directory)?.FullName;
		static DirectoryInfo? GetMainDirectory(DirectoryInfo? directoryInfo)
		{
			while (directoryInfo?.Parent != null && directoryInfo.Exists)
			{
				if (directoryInfo.GetFiles("SubModule.xml").Length == 1)
				{
					return directoryInfo;
				}
				directoryInfo = directoryInfo.Parent;
			}
			return null;
		}
	}

	public static string? GetModulePath(ModuleInfoExtended module)
	{
		return _cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Id == module.Id)?.Path;
	}

	public static ModuleInfoExtendedHelper? GetModuleByType(Type? type)
	{
		string modulePath = GetModulePath(type);
		return _cachedModules.Value.FirstOrDefault((ModuleInfoExtendedHelper x) => x.Path == modulePath);
	}

	private static string GetFullPathWithEndingSlashes(string input)
	{
		return $"{System.IO.Path.GetFullPath(input).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)}{System.IO.Path.DirectorySeparatorChar}";
	}

	public static IEnumerable<ModuleInfoExtendedHelper> GetPhysicalModules()
	{
		if (string.IsNullOrEmpty(BasePath.Name))
		{
			yield break;
		}
		string[] directories = Directory.GetDirectories(System.IO.Path.Combine(BasePath.Name, "Modules"));
		ModuleInfoExtended moduleInfo = default(ModuleInfoExtended);
		foreach (string modulePath in directories)
		{
			int num;
			if (TryReadXml(System.IO.Path.Combine(modulePath, "SubModule.xml"), out XmlDocument xml))
			{
				moduleInfo = ModuleInfoExtended.FromXml(xml);
				num = (((object)moduleInfo != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				yield return new ModuleInfoExtendedHelper(moduleInfo, isExternal: false, System.IO.Path.GetFullPath(modulePath));
			}
			xml = null;
			moduleInfo = null;
		}
	}

	public static IEnumerable<ModuleInfoExtendedHelper> GetPlatformModules()
	{
		if (_platformModuleExtensionField == null)
		{
			yield break;
		}
		IPlatformModuleExtension platformModuleExtension = _platformModuleExtensionField();
		if (platformModuleExtension == null)
		{
			yield break;
		}
		string[] modulePaths = platformModuleExtension.GetModulePaths();
		if (modulePaths == null)
		{
			yield break;
		}
		string[] array = modulePaths;
		ModuleInfoExtended moduleInfo = default(ModuleInfoExtended);
		foreach (string modulePath in array)
		{
			int num;
			if (TryReadXml(System.IO.Path.Combine(modulePath, "SubModule.xml"), out XmlDocument xml))
			{
				moduleInfo = ModuleInfoExtended.FromXml(xml);
				num = (((object)moduleInfo != null) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			if (num != 0)
			{
				yield return new ModuleInfoExtendedHelper(moduleInfo, isExternal: true, System.IO.Path.GetFullPath(modulePath));
			}
			xml = null;
			moduleInfo = null;
		}
	}

	public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo)
	{
		return CheckIfSubModuleCanBeLoaded(subModuleInfo, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
	}

	public static bool CheckIfSubModuleCanBeLoaded(SubModuleInfoExtended subModuleInfo, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
	{
		if (subModuleInfo.Tags.Count <= 0)
		{
			return true;
		}
		foreach (var (value, source) in subModuleInfo.Tags)
		{
			if (!Enum.TryParse<SubModuleTags>(value, out var tag) || !source.Any((string value2) => !GetSubModuleTagValiditiy(tag, value2, cPlatform, cRuntime, cServerType, playerHostedDedicatedServer)))
			{
				continue;
			}
			return false;
		}
		return true;
	}

	public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value)
	{
		return GetSubModuleTagValiditiy(tag, value, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
	}

	public static bool GetSubModuleTagValiditiy(SubModuleTags tag, string value, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
	{
		if (1 == 0)
		{
		}
		bool result;
		switch (tag)
		{
		case SubModuleTags.RejectedPlatform:
		{
			result = !Enum.TryParse<Platform>(value, out var result2) || cPlatform != result2;
			break;
		}
		case SubModuleTags.ExclusivePlatform:
		{
			result = !Enum.TryParse<Platform>(value, out var result3) || cPlatform == result3;
			break;
		}
		case SubModuleTags.DependantRuntimeLibrary:
		{
			result = !Enum.TryParse<Runtime>(value, out var result4) || cRuntime == result4;
			break;
		}
		case SubModuleTags.IsNoRenderModeElement:
			result = value.Equals("false");
			break;
		case SubModuleTags.DedicatedServerType:
		{
			string text = value.ToLower();
			if (1 == 0)
			{
			}
			bool flag = text switch
			{
				"none" => cServerType == DedicatedServerType.None, 
				"both" => cServerType == DedicatedServerType.None, 
				"custom" => cServerType == DedicatedServerType.Custom, 
				"matchmaker" => cServerType == DedicatedServerType.Matchmaker, 
				_ => false, 
			};
			if (1 == 0)
			{
			}
			result = flag;
			break;
		}
		case SubModuleTags.PlayerHostedDedicatedServer:
			result = playerHostedDedicatedServer && value.Equals("true");
			break;
		default:
			result = true;
			break;
		}
		if (1 == 0)
		{
		}
		return result;
	}

	public static bool ValidateLoadOrder(Type subModuleType, out string report)
	{
		return ValidateLoadOrder(GetModuleByType(subModuleType), out report);
	}

	public static bool ValidateLoadOrder(ModuleInfoExtended? moduleInfo, out string report)
	{
		if ((object)moduleInfo == null)
		{
			report = "CRITICAL ERROR";
			return false;
		}
		List<ModuleInfoExtendedHelper> list = GetLoadedModules().ToList();
		int moduleIndex = IndexOf<ModuleInfoExtended>(list, moduleInfo);
		StringBuilder sb = new StringBuilder();
		foreach (DependentModule dependedModule in moduleInfo.DependentModules)
		{
			ModuleInfoExtendedHelper moduleInfoExtendedHelper = list.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == dependedModule.Id);
			int requiredModuleIndex = (((object)moduleInfoExtendedHelper != null) ? list.IndexOf(moduleInfoExtendedHelper) : (-1));
			ValidateDependedModuleLoadBeforeThis(requiredModuleIndex, dependedModule.Id);
		}
		foreach (DependentModuleMetadata dependedModule2 in moduleInfo.DependentModuleMetadatas)
		{
			ModuleInfoExtendedHelper moduleInfoExtendedHelper2 = list.SingleOrDefault((ModuleInfoExtendedHelper x) => x.Id == dependedModule2.Id);
			int num = (((object)moduleInfoExtendedHelper2 != null) ? list.IndexOf(moduleInfoExtendedHelper2) : (-1));
			if (dependedModule2.IsIncompatible)
			{
				if (moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule2.Id))
				{
					ReportMutuallyExclusiveDirectives(dependedModule2.Id);
				}
				else
				{
					ValidateDependedModuleCompatibility(num, dependedModule2.Id);
				}
			}
			else if (dependedModule2.LoadType == LoadType.LoadBeforeThis)
			{
				if (!moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule2.Id))
				{
					ValidateDependedModuleLoadBeforeThis(num, dependedModule2.Id, dependedModule2.IsOptional);
				}
			}
			else if (dependedModule2.LoadType == LoadType.LoadAfterThis)
			{
				if (moduleInfo.DependentModules.Any((DependentModule dm) => dm.Id == dependedModule2.Id) || moduleInfo.DependentModuleMetadatas.Any((DependentModuleMetadata dm) => dm.Id == dependedModule2.Id && dm.LoadType == LoadType.LoadBeforeThis))
				{
					ReportMutuallyExclusiveDirectives(dependedModule2.Id);
				}
				else
				{
					ValidateDependedModuleLoadAfterThis(num, dependedModule2.Id, dependedModule2.IsOptional);
				}
			}
		}
		if (sb.Length > 0)
		{
			report = sb.ToString();
			return false;
		}
		report = string.Empty;
		return true;
		static int IndexOf<T>(IReadOnlyList<T> self, T elementToFind)
		{
			int num2 = 0;
			foreach (T item in self)
			{
				if (object.Equals(item, elementToFind))
				{
					return num2;
				}
				num2++;
			}
			return -1;
		}
		void ReportIncompatibleModule(string deniedModuleId)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(new TextObject("{=EvI6KPAqTT}Incompatible module {DENIED_MODULE} was found!")?.SetTextVariable("DENIED_MODULE", deniedModuleId)?.ToString() ?? "ERROR");
		}
		void ReportLoadingOrderIssue(string reason, string requiredModuleId)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(new TextObject(reason)?.SetTextVariable("MODULE", moduleInfo.Id)?.SetTextVariable("REQUIRED_MODULE", requiredModuleId)?.SetTextVariable("NL", Environment.NewLine)?.ToString() ?? "ERROR");
		}
		void ReportMissingModule(string requiredModuleId)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(new TextObject("{=FE6ya1gzZR}{REQUIRED_MODULE} module was not found!")?.SetTextVariable("REQUIRED_MODULE", requiredModuleId)?.ToString() ?? "ERROR");
		}
		void ReportMutuallyExclusiveDirectives(string requiredModuleId)
		{
			if (sb.Length != 0)
			{
				sb.AppendLine();
			}
			sb.AppendLine(new TextObject("{=FcR4BXnhx8}{MODULE} has mutually exclusive mod order directives specified for the {REQUIRED_MODULE}!")?.SetTextVariable("MODULE", moduleInfo.Id)?.SetTextVariable("REQUIRED_MODULE", requiredModuleId)?.ToString() ?? "ERROR");
		}
		void ValidateDependedModuleCompatibility(int deniedModuleIndex, string deniedModuleId)
		{
			if (deniedModuleIndex != -1)
			{
				ReportIncompatibleModule(deniedModuleId);
			}
		}
		void ValidateDependedModuleLoadAfterThis(int num2, string requiredModuleId, bool isOptional)
		{
			if (num2 == -1)
			{
				if (!isOptional)
				{
					ReportMissingModule(requiredModuleId);
				}
			}
			else if (num2 < moduleIndex)
			{
				ReportLoadingOrderIssue("{=UZ8zfvudMs}{MODULE} is loaded after the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded before it!", requiredModuleId);
			}
		}
		void ValidateDependedModuleLoadBeforeThis(int num2, string requiredModuleId, bool isOptional = false)
		{
			if (!isOptional && num2 == -1)
			{
				ReportMissingModule(requiredModuleId);
			}
			else if (num2 > moduleIndex)
			{
				ReportLoadingOrderIssue("{=5G9zffrgMh}{MODULE} is loaded before the {REQUIRED_MODULE}!{NL}Make sure {MODULE} is loaded after it!", requiredModuleId);
			}
		}
	}

	public static bool IsModuleAssembly(ModuleInfoExtendedHelper loadedModule, Assembly assembly)
	{
		if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.CodeBase))
		{
			return false;
		}
		return IsInModule(loadedModule, assembly.CodeBase);
	}

	public static bool IsInModule(ModuleInfoExtendedHelper loadedModule, string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return false;
		}
		Uri uri = new Uri(System.IO.Path.GetFullPath(loadedModule.Path));
		string fileName = System.IO.Path.GetFileName(loadedModule.Path);
		Uri uri2 = new Uri(filePath);
		Uri uri3 = uri.MakeRelativeUri(uri2);
		return uri3.OriginalString.StartsWith(fileName);
	}

	private static bool TryReadXml(string path, out XmlDocument? xml)
	{
		try
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(File.ReadAllText(path));
			xml = xmlDocument;
			return true;
		}
		catch (Exception)
		{
			xml = null;
			return false;
		}
	}
}
