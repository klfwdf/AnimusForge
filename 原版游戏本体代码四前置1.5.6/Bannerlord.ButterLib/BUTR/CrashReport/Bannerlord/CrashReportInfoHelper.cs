#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using BUTR.CrashReport.Interfaces;
using BUTR.CrashReport.Models;
using Bannerlord.BUTR.Shared.Helpers;
using Bannerlord.ModuleManager;
using HarmonyLib;
using HarmonyLib.BUTR.Extensions;

namespace BUTR.CrashReport.Bannerlord;

internal class CrashReportInfoHelper : IAssemblyUtilities, ICrashReportMetadataProvider, ILoaderPluginProvider, IModelConverter, IModuleProvider, IPathAnonymizer
{
	private static readonly AccessTools.FieldRef<HashSet<string>>? GetFeatures = HarmonyLib.BUTR.Extensions.AccessTools2.StaticFieldRefAccess<HashSet<string>>("Bannerlord.BLSE.FeatureIds:Features");

	private static List<ILoaderPluginInfo> GetPlugins()
	{
		List<string> list = (from x in (from x in (from x in Harmony.GetAllPatchedMethods().Select(Harmony.GetPatchInfo).SelectMany((Patches x) => x.Prefixes.Concat(x.Postfixes).Concat(x.Finalizers).Concat(x.Transpilers))
					select x.PatchMethod into x
					select x.DeclaringType?.Namespace).OfType<string>()
				where x.StartsWith("Bannerlord.BLSE.Features.")
				select x.Substring("Bannerlord.BLSE.Features.".Length)).Select(delegate(string x)
			{
				int num = x.IndexOf('.');
				return x.Substring(0, (num != -1) ? num : x.Length);
			}).Distinct()
			select "BLSE." + x).ToList();
		if (list.Count > 0)
		{
			list.Add("BLSE.AssemblyResolver");
		}
		return list.Select((string x) => new LoaderPluginInfo
		{
			Id = x,
			Version = null,
			UpdateInfo = null
		}).OfType<ILoaderPluginInfo>().ToList() ?? new List<ILoaderPluginInfo>();
	}

	public virtual CrashReportMetadataModel GetCrashReportMetadataModel(CrashReportInfo crashReport)
	{
		string bUTRLoaderVersion = GetBUTRLoaderVersion(crashReport);
		string bLSEVersion = GetBLSEVersion(crashReport);
		string launcherExVersion = GetLauncherExVersion(crashReport);
		List<MetadataModel> list = new List<MetadataModel>();
		if (!string.IsNullOrEmpty(launcherExVersion))
		{
			list.Add(new MetadataModel
			{
				Key = "LauncherExVersion",
				Value = launcherExVersion
			});
		}
		return new CrashReportMetadataModel
		{
			GameName = "Bannerlord",
			GameVersion = ApplicationVersionHelper.GameVersionStr(),
			LoaderPluginProviderName = ((!string.IsNullOrEmpty(bUTRLoaderVersion)) ? "BUTRLoader" : ((!string.IsNullOrEmpty(bLSEVersion)) ? "BLSE" : null)),
			LoaderPluginProviderVersion = ((!string.IsNullOrEmpty(bUTRLoaderVersion)) ? bUTRLoaderVersion : ((!string.IsNullOrEmpty(bLSEVersion)) ? bLSEVersion : null)),
			LauncherType = GetLauncherType(crashReport),
			LauncherVersion = GetLauncherVersion(crashReport),
			Runtime = null,
			OperatingSystemType = GetOperatingSystemType(),
			OperatingSystemVersion = GetOSVersion(),
			AdditionalMetadata = list
		};
	}

	private OperatingSystemType GetOperatingSystemType()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			if (OSUtilities.IsWine())
			{
				return OperatingSystemType.WindowsOnWine;
			}
			return OperatingSystemType.Windows;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return OperatingSystemType.Linux;
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return OperatingSystemType.MacOS;
		}
		return OperatingSystemType.Unknown;
	}

	private string? GetOSVersion()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return OSUtilities.GetOSVersionWindows();
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			return OSUtilities.GetOSVersionLinux();
		}
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return OSUtilities.GetOSXVersion();
		}
		return null;
	}

	public virtual IEnumerable<Assembly> Assemblies()
	{
		return HarmonyLib.BUTR.Extensions.AccessTools2.AllAssemblies();
	}

	public virtual IModuleInfo? GetAssemblyModule(CrashReportInfo crashReport, Assembly assembly)
	{
		try
		{
			ModuleInfoExtendedHelper module = ((!assembly.IsDynamic) ? ModuleInfoHelper.GetModuleByType(HarmonyLib.BUTR.Extensions.AccessTools2.GetTypesFromAssembly(assembly).FirstOrDefault()) : null);
			return ((object)module != null) ? crashReport.LoadedModules.FirstOrDefault((IModuleInfo x) => x.Id == module.Id) : null;
		}
		catch (Exception ex)
		{
			Trace.TraceError(ex.ToString());
			return null;
		}
	}

	public virtual ILoaderPluginInfo? GetAssemblyPlugin(CrashReportInfo crashReport, Assembly assembly)
	{
		return null;
	}

	public virtual AssemblyModelType GetAssemblyType(AssemblyModelType type, CrashReportInfo crashReport, Assembly assembly)
	{
		if (IsTWCore(assembly))
		{
			type |= AssemblyModelType.GameCore;
		}
		ModuleInfoExtendedHelper moduleInfoExtendedHelper = ((!assembly.IsDynamic) ? ModuleInfoHelper.GetModuleByType(HarmonyLib.BUTR.Extensions.AccessTools2.GetTypesFromAssembly(assembly).FirstOrDefault()) : null);
		if ((object)moduleInfoExtendedHelper != null && !moduleInfoExtendedHelper.IsOfficial)
		{
			type |= AssemblyModelType.Module;
		}
		if ((object)moduleInfoExtendedHelper != null && moduleInfoExtendedHelper.IsOfficial)
		{
			type |= AssemblyModelType.GameModule;
		}
		return type;
		static bool IsTWCore(Assembly assembly2)
		{
			if (assembly2.IsDynamic || string.IsNullOrEmpty(assembly2.Location))
			{
				return false;
			}
			string directoryName = Path.GetDirectoryName(Path.GetDirectoryName(assembly2.Location));
			if (!string.Equals(Path.GetFileName(directoryName), "bin", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (string.Equals(Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(directoryName))), "Modules", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return true;
		}
	}

	public virtual ICollection<IModuleInfo> GetLoadedModules()
	{
		return (from x in ModuleInfoHelper.GetLoadedModules()
			select new ModuleInfo(x)).ToArray();
	}

	public virtual IModuleInfo? GetModuleByType(Type? type)
	{
		ModuleInfoExtendedHelper moduleByType = ModuleInfoHelper.GetModuleByType(type);
		if ((object)moduleByType == null)
		{
			return null;
		}
		return new ModuleInfo(moduleByType);
	}

	public virtual ICollection<ILoaderPluginInfo> GetLoadedLoaderPlugins()
	{
		return GetPlugins();
	}

	public virtual ILoaderPluginInfo? GetLoaderPluginByType(Type? type)
	{
		if ((object)type == null)
		{
			return null;
		}
		if (type.Namespace == null || !type.Namespace.StartsWith("Bannerlord.BLSE.Features."))
		{
			return null;
		}
		string text = type.Namespace.Substring("Bannerlord.BLSE.Features.".Length);
		string text2 = text;
		int num = text.IndexOf('.');
		text = text2.Substring(0, (num != -1) ? num : text.Length);
		return new LoaderPluginInfo
		{
			Id = "BLSE." + text,
			Version = null,
			UpdateInfo = null
		};
	}

	public virtual List<ModuleModel> ToModuleModels(ICollection<IModuleInfo> loadedModules, ICollection<AssemblyModel> assemblies)
	{
		List<ModuleModel> list = new List<ModuleModel>(loadedModules.Count);
		foreach (ModuleInfoExtendedHelper item in from x in loadedModules.OfType<ModuleInfo>()
			select x.InternalModuleInfo)
		{
			bool isManagedByVortex = File.Exists(Path.Combine(item.Path, "__folder_managed_by_vortex"));
			list.Add(Convert(item, isManagedByVortex, assemblies));
		}
		return list;
	}

	public virtual List<LoaderPluginModel> ToLoaderPluginModels(ICollection<ILoaderPluginInfo> loadedLoaderPlugins, ICollection<AssemblyModel> assemblies)
	{
		return loadedLoaderPlugins.OfType<LoaderPluginInfo>().Select(delegate(LoaderPluginInfo x)
		{
			LoaderPluginModel loaderPluginModel = new LoaderPluginModel
			{
				Id = x.Id,
				Name = x.Id,
				Version = x.Version
			};
			object updateInfo;
			if (x.UpdateInfo != null)
			{
				string[] array = x.UpdateInfo.Split(':');
				if (array != null && array.Length == 2)
				{
					updateInfo = new UpdateInfoModuleOrLoaderPlugin
					{
						Provider = array[0],
						Value = array[1]
					};
					goto IL_006d;
				}
			}
			updateInfo = null;
			goto IL_006d;
			IL_006d:
			loaderPluginModel.UpdateInfo = (UpdateInfoModuleOrLoaderPlugin?)updateInfo;
			loaderPluginModel.Dependencies = Array.Empty<DependencyMetadataModel>();
			loaderPluginModel.Capabilities = Array.Empty<CapabilityModuleOrPluginModel>();
			loaderPluginModel.AdditionalMetadata = Array.Empty<MetadataModel>();
			return loaderPluginModel;
		}).ToList();
	}

	public virtual bool TryHandlePath(string path, out string anonymizedPath)
	{
		anonymizedPath = string.Empty;
		int num = path.IndexOf("Mount & Blade II Bannerlord", StringComparison.OrdinalIgnoreCase);
		if (num != -1)
		{
			anonymizedPath = path.Substring(num);
			return true;
		}
		int num2 = path.IndexOf("Mount & Blade II- Bannerlord", StringComparison.OrdinalIgnoreCase);
		if (num2 != -1)
		{
			anonymizedPath = path.Substring(num2);
			return true;
		}
		return false;
	}

	protected static ModuleModel Convert(ModuleInfoExtendedHelper module, bool isManagedByVortex, ICollection<AssemblyModel> assemblies)
	{
		UpdateInfoModuleOrLoaderPlugin[] source = module.UpdateInfo.Split(';').Select(delegate(string x)
		{
			string[] array = x.Split(':');
			return (array == null || array.Length != 2) ? null : new UpdateInfoModuleOrLoaderPlugin
			{
				Provider = array[0],
				Value = array[1]
			};
		}).OfType<UpdateInfoModuleOrLoaderPlugin>()
			.ToArray();
		List<CapabilityModuleOrPluginModel> list = new List<CapabilityModuleOrPluginModel>();
		ModuleModel moduleModel = new ModuleModel();
		moduleModel.Id = module.Id;
		moduleModel.Name = module.Name;
		moduleModel.Version = module.Version.ToString();
		moduleModel.IsExternal = module.IsExternal;
		moduleModel.IsOfficial = module.IsOfficial;
		moduleModel.IsSingleplayer = module.IsSingleplayerModule;
		moduleModel.IsMultiplayer = module.IsMultiplayerModule;
		moduleModel.Url = ((!string.IsNullOrEmpty(module.Url)) ? module.Url : null);
		moduleModel.UpdateInfo = source.FirstOrDefault();
		moduleModel.DependencyMetadatas = (from x in module.DependenciesAllDistinct()
			select new DependencyMetadataModel
			{
				ModuleOrPluginId = x.Id,
				Type = (DependencyMetadataModelType)(x.IsIncompatible ? ((LoadType)3) : x.LoadType),
				IsOptional = x.IsOptional,
				Version = ((!x.Version.Equals(ApplicationVersion.Empty)) ? x.Version.ToString() : null),
				VersionRange = ((!x.VersionRange.Equals(ApplicationVersionRange.Empty)) ? x.VersionRange.ToString() : null),
				AdditionalMetadata = Array.Empty<MetadataModel>()
			}).ToArray();
		moduleModel.SubModules = (from x in module.SubModules.Where(ModuleInfoHelper.CheckIfSubModuleCanBeLoaded)
			select new ModuleSubModuleModel
			{
				Name = x.Name,
				AssemblyId = new AssemblyIdModel
				{
					Name = x.DLLName,
					Version = null,
					PublicKeyToken = null
				},
				Entrypoint = x.SubModuleClassType,
				AdditionalMetadata = x.Assemblies.Select((string y) => new MetadataModel
				{
					Key = "METADATA:Assembly",
					Value = y
				}).Concat(x.Tags.SelectMany<KeyValuePair<string, IReadOnlyList<string>>, MetadataModel>((KeyValuePair<string, IReadOnlyList<string>> y) => y.Value.Select((string z) => new MetadataModel
				{
					Key = y.Key,
					Value = z
				}))).ToArray()
			}).ToArray();
		moduleModel.Capabilities = list;
		moduleModel.AdditionalMetadata = new MetadataModel[2]
		{
			new MetadataModel
			{
				Key = "AdditionalUpdateInfos",
				Value = string.Join(";", from x in source.Skip(1)
					select x.Provider + ":" + x.Value)
			},
			new MetadataModel
			{
				Key = "METADATA:MANAGED_BY_VORTEX",
				Value = isManagedByVortex.ToString()
			}
		};
		ModuleModel moduleModel2 = moduleModel;
		list.AddRange(CrashReportShared.GetModuleCapabilities(assemblies, moduleModel2).DistinctBy((CapabilityModuleOrPluginModel x) => x.Name));
		return moduleModel2;
	}

	protected static string GetBUTRLoaderVersion(CrashReportInfo crashReport)
	{
		AssemblyName key = crashReport.AvailableAssemblies.FirstOrDefault<KeyValuePair<AssemblyName, Assembly>>((KeyValuePair<AssemblyName, Assembly> x) => x.Key.Name == "Bannerlord.BUTRLoader").Key;
		if (key != null)
		{
			return key.Version?.ToString() ?? string.Empty;
		}
		return string.Empty;
	}

	protected static string GetBLSEVersion(CrashReportInfo crashReport)
	{
		return (crashReport.AvailableAssemblies.FirstOrDefault<KeyValuePair<AssemblyName, Assembly>>((KeyValuePair<AssemblyName, Assembly> x) => x.Key.Name == "Bannerlord.BLSE.Shared").Value?.GetCustomAttributes<AssemblyMetadataAttribute>())?.FirstOrDefault((AssemblyMetadataAttribute x) => x.Key == "BLSEVersion")?.Value ?? string.Empty;
	}

	protected static string GetLauncherExVersion(CrashReportInfo crashReport)
	{
		return (crashReport.AvailableAssemblies.FirstOrDefault<KeyValuePair<AssemblyName, Assembly>>((KeyValuePair<AssemblyName, Assembly> x) => x.Key.Name == "Bannerlord.LauncherEx").Value?.GetCustomAttributes<AssemblyMetadataAttribute>())?.FirstOrDefault((AssemblyMetadataAttribute x) => x.Key == "LauncherExVersion")?.Value ?? string.Empty;
	}

	protected static string GetLauncherType(CrashReportInfo crashReport)
	{
		if (crashReport.AdditionalMetadata.TryGetValue("Parent_Process_Name", out string value))
		{
			return value switch
			{
				"Vortex" => "vortex", 
				"BannerLordLauncher" => "bannerlordlauncher", 
				"steam" => "steam", 
				"GalaxyClient" => "gog", 
				"EpicGamesLauncher" => "epicgames", 
				"devenv" => "debuggervisualstudio", 
				"JetBrains.Debugger.Worker64c" => "debuggerjetbrains", 
				"explorer" => "explorer", 
				"NovusLauncher" => "novus", 
				"ModOrganizer" => "modorganizer", 
				_ => "unknown launcher - " + value, 
			};
		}
		if (!string.IsNullOrEmpty(GetBUTRLoaderVersion(crashReport)))
		{
			return "butrloader";
		}
		return "vanilla";
	}

	protected static string GetLauncherVersion(CrashReportInfo crashReport)
	{
		if (crashReport.AdditionalMetadata.TryGetValue("Parent_Process_File_Version", out string value))
		{
			return value;
		}
		string bUTRLoaderVersion = GetBUTRLoaderVersion(crashReport);
		if (bUTRLoaderVersion != null && !string.IsNullOrEmpty(bUTRLoaderVersion))
		{
			return bUTRLoaderVersion;
		}
		return "0";
	}
}
