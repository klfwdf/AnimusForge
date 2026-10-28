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

	private static Lazy<List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper>> _cachedModules;

	private static ConcurrentDictionary<string, string> _cachedAssemblyLocationToModulePath;

	static ModuleInfoHelper()
	{
		_cachedModules = new Lazy<List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper>>(delegate
		{
			List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> list = new List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper>();
			HashSet<string> hashSet = new HashSet<string>();
			foreach (Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper item in GetPhysicalModules().Concat(GetPlatformModules()))
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
		_platformModuleExtensionField = HarmonyLib.BUTR.Extensions.AccessTools2.StaticFieldRefAccess<IPlatformModuleExtension>("TaleWorlds.ModuleManager.ModuleHelper:_platformModuleExtension");
	}

	public static Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper? LoadFromId(string id)
	{
		return GetModules().FirstOrDefault((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Id == id);
	}

	public static IEnumerable<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> GetLoadedModules()
	{
		string[] modulesNames = Utilities.GetModulesNames();
		if (modulesNames.Length == 0)
		{
			yield break;
		}
		Dictionary<string, Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> allModulesAvailable = GetModules().ToDictionary((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Id, (Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x);
		string[] array = modulesNames;
		foreach (string key in array)
		{
			if (allModulesAvailable.TryGetValue(key, out var value))
			{
				yield return value;
			}
		}
	}

	public static IEnumerable<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> GetModules()
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

	public static string? GetModulePath(Bannerlord.ModuleManager.ModuleInfoExtended module)
	{
		return _cachedModules.Value.FirstOrDefault((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Id == module.Id)?.Path;
	}

	public static Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper? GetModuleByType(Type? type)
	{
		string modulePath = GetModulePath(type);
		return _cachedModules.Value.FirstOrDefault((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Path == modulePath);
	}

	private static string GetFullPathWithEndingSlashes(string input)
	{
		return $"{System.IO.Path.GetFullPath(input).TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)}{System.IO.Path.DirectorySeparatorChar}";
	}

	public static IEnumerable<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> GetPhysicalModules()
	{
		if (string.IsNullOrEmpty(BasePath.Name))
		{
			yield break;
		}
		string[] directories = Directory.GetDirectories(System.IO.Path.Combine(BasePath.Name, "Modules"));
		foreach (string text in directories)
		{
			if (TryReadXml(System.IO.Path.Combine(text, "SubModule.xml"), out XmlDocument xml))
			{
				Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = Bannerlord.ModuleManager.ModuleInfoExtended.FromXml(xml);
				if ((object)moduleInfoExtended != null)
				{
					yield return new Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper(moduleInfoExtended, isExternal: false, System.IO.Path.GetFullPath(text));
				}
			}
		}
	}

	public static IEnumerable<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> GetPlatformModules()
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
		foreach (string text in array)
		{
			if (TryReadXml(System.IO.Path.Combine(text, "SubModule.xml"), out XmlDocument xml))
			{
				Bannerlord.ModuleManager.ModuleInfoExtended moduleInfoExtended = Bannerlord.ModuleManager.ModuleInfoExtended.FromXml(xml);
				if ((object)moduleInfoExtended != null)
				{
					yield return new Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper(moduleInfoExtended, isExternal: true, System.IO.Path.GetFullPath(text));
				}
			}
		}
	}

	public static bool CheckIfSubModuleCanBeLoaded(Bannerlord.ModuleManager.SubModuleInfoExtended subModuleInfo)
	{
		return CheckIfSubModuleCanBeLoaded(subModuleInfo, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
	}

	public static bool CheckIfSubModuleCanBeLoaded(Bannerlord.ModuleManager.SubModuleInfoExtended subModuleInfo, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
	{
		if (subModuleInfo.Tags.Count <= 0)
		{
			return true;
		}
		foreach (var (value, source) in subModuleInfo.Tags)
		{
			if (Enum.TryParse<Bannerlord.ModuleManager.SubModuleTags>(value, out var tag) && source.Any((string value2) => !GetSubModuleTagValiditiy(tag, value2, cPlatform, cRuntime, cServerType, playerHostedDedicatedServer)))
			{
				return false;
			}
		}
		return true;
	}

	public static bool GetSubModuleTagValiditiy(Bannerlord.ModuleManager.SubModuleTags tag, string value)
	{
		return GetSubModuleTagValiditiy(tag, value, ApplicationPlatform.CurrentPlatform, ApplicationPlatform.CurrentRuntimeLibrary, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.DedicatedServerType, TaleWorlds.MountAndBlade.Module.CurrentModule.StartupInfo.PlayerHostedDedicatedServer);
	}

	public static bool GetSubModuleTagValiditiy(Bannerlord.ModuleManager.SubModuleTags tag, string value, Platform cPlatform, Runtime cRuntime, DedicatedServerType cServerType, bool playerHostedDedicatedServer)
	{
		Platform result;
		Platform result2;
		Runtime result3;
		return tag switch
		{
			Bannerlord.ModuleManager.SubModuleTags.RejectedPlatform => !Enum.TryParse<Platform>(value, out result) || cPlatform != result, 
			Bannerlord.ModuleManager.SubModuleTags.ExclusivePlatform => !Enum.TryParse<Platform>(value, out result2) || cPlatform == result2, 
			Bannerlord.ModuleManager.SubModuleTags.DependantRuntimeLibrary => !Enum.TryParse<Runtime>(value, out result3) || cRuntime == result3, 
			Bannerlord.ModuleManager.SubModuleTags.IsNoRenderModeElement => value.Equals("false"), 
			Bannerlord.ModuleManager.SubModuleTags.DedicatedServerType => value.ToLower() switch
			{
				"none" => cServerType == DedicatedServerType.None, 
				"both" => cServerType == DedicatedServerType.None, 
				"custom" => cServerType == DedicatedServerType.Custom, 
				"matchmaker" => cServerType == DedicatedServerType.Matchmaker, 
				_ => false, 
			}, 
			Bannerlord.ModuleManager.SubModuleTags.PlayerHostedDedicatedServer => playerHostedDedicatedServer && value.Equals("true"), 
			_ => true, 
		};
	}

	public static bool ValidateLoadOrder(Type subModuleType, out string report)
	{
		return ValidateLoadOrder(GetModuleByType(subModuleType), out report);
	}

	public static bool ValidateLoadOrder(Bannerlord.ModuleManager.ModuleInfoExtended? moduleInfo, out string report)
	{
		if ((object)moduleInfo == null)
		{
			report = "CRITICAL ERROR";
			return false;
		}
		List<Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper> list = GetLoadedModules().ToList();
		int moduleIndex = IndexOf<Bannerlord.ModuleManager.ModuleInfoExtended>(list, moduleInfo);
		StringBuilder sb = new StringBuilder();
		foreach (Bannerlord.ModuleManager.DependentModule dependedModule in moduleInfo.DependentModules)
		{
			Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper moduleInfoExtendedHelper = list.SingleOrDefault((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Id == dependedModule.Id);
			int requiredModuleIndex = (((object)moduleInfoExtendedHelper != null) ? list.IndexOf(moduleInfoExtendedHelper) : (-1));
			ValidateDependedModuleLoadBeforeThis(requiredModuleIndex, dependedModule.Id);
		}
		foreach (Bannerlord.ModuleManager.DependentModuleMetadata dependedModule2 in moduleInfo.DependentModuleMetadatas)
		{
			Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper moduleInfoExtendedHelper2 = list.SingleOrDefault((Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper x) => x.Id == dependedModule2.Id);
			int num = (((object)moduleInfoExtendedHelper2 != null) ? list.IndexOf(moduleInfoExtendedHelper2) : (-1));
			if (dependedModule2.IsIncompatible)
			{
				if (moduleInfo.DependentModules.Any((Bannerlord.ModuleManager.DependentModule dm) => dm.Id == dependedModule2.Id))
				{
					ReportMutuallyExclusiveDirectives(dependedModule2.Id);
				}
				else
				{
					ValidateDependedModuleCompatibility(num, dependedModule2.Id);
				}
			}
			else if (dependedModule2.LoadType == Bannerlord.ModuleManager.LoadType.LoadBeforeThis)
			{
				if (!moduleInfo.DependentModules.Any((Bannerlord.ModuleManager.DependentModule dm) => dm.Id == dependedModule2.Id))
				{
					ValidateDependedModuleLoadBeforeThis(num, dependedModule2.Id, dependedModule2.IsOptional);
				}
			}
			else if (dependedModule2.LoadType == Bannerlord.ModuleManager.LoadType.LoadAfterThis)
			{
				if (moduleInfo.DependentModules.Any((Bannerlord.ModuleManager.DependentModule dm) => dm.Id == dependedModule2.Id) || moduleInfo.DependentModuleMetadatas.Any((Bannerlord.ModuleManager.DependentModuleMetadata dm) => dm.Id == dependedModule2.Id && dm.LoadType == Bannerlord.ModuleManager.LoadType.LoadBeforeThis))
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

	public static bool IsModuleAssembly(Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper loadedModule, Assembly assembly)
	{
		if (assembly.IsDynamic || string.IsNullOrWhiteSpace(assembly.CodeBase))
		{
			return false;
		}
		return IsInModule(loadedModule, assembly.CodeBase);
	}

	public static bool IsInModule(Bannerlord.BUTR.Shared.Helpers.ModuleInfoExtendedHelper loadedModule, string filePath)
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
