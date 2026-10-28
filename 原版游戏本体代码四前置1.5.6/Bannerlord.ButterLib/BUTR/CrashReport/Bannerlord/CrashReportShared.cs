using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BUTR.CrashReport.Extensions;
using BUTR.CrashReport.Models;

namespace BUTR.CrashReport.Bannerlord;

internal static class CrashReportShared
{
	private static readonly string[] OSFileSystemTypeReferences = new string[7]
	{
		typeof(File).FullName,
		typeof(FileStream).FullName,
		typeof(FileInfo).FullName,
		typeof(Directory).FullName,
		typeof(DirectoryInfo).FullName,
		typeof(DriveInfo).FullName,
		typeof(FileSystemWatcher).FullName
	};

	private static readonly string[] GameFileSystemTypeReferences = new string[3] { "TaleWorlds.Library.*File*", "TaleWorlds.Library.*Directory*", "TaleWorlds.SaveSystem.*File*" };

	private static readonly string[] ShellTypeReferences = new string[1] { typeof(Process).FullName };

	private static readonly string[] SaveSystemTypeReferences = new string[2] { "TaleWorlds.Library.*Save*", "TaleWorlds.Core.MBSaveLoad" };

	private static readonly string[] SaveSystemAssemblyReferences = new string[1] { "TaleWorlds.SaveSystem" };

	private static readonly string[] GameEntitiesTypeReferences = new string[1] { "TaleWorlds.Core.EntitySystem*" };

	private static readonly string[] GameEntitiesAssemblyReferences = new string[1] { "TaleWorlds.ObjectSystem" };

	private static readonly string[] InputSystemAssemblyReferences = new string[1] { "TaleWorlds.InputSystem" };

	private static readonly string[] LocalizationSystemAssemblyReferences = new string[1] { "TaleWorlds.Localization" };

	private static readonly string[] UITypeReferences = new string[2] { "TaleWorlds.Library.IViewModel", "TaleWorlds.Library.ViewModel" };

	private static readonly string[] UIAssemblyReferences = new string[1] { "*GauntletUI*" };

	private static readonly string[] HttpTypeReferences = new string[2] { "TaleWorlds.Library.*Http*", "System.Net*Http.*" };

	private static readonly string[] AchievementSystemTypeReferences = new string[1] { "TaleWorlds.*Achievement*" };

	private static readonly string[] CampaignSystemTypeReferences = new string[1] { "TaleWorlds.*CampaignSystem*" };

	private static readonly string[] SkillSystemTypeReferences = new string[3] { "TaleWorlds.Core.CharacterSkills", "TaleWorlds.Core.DefaultSkills", "TaleWorlds.Core.SkillObject" };

	private static readonly string[] ItemSystemTypeReferences = new string[1] { "TaleWorlds.Core.ItemObject" };

	private static readonly string[] CultureSystemTypeReferences = new string[1] { "TaleWorlds.*Culture*" };

	public static IEnumerable<CapabilityModuleOrPluginModel> GetModuleCapabilities(ICollection<AssemblyModel> assemblies, ModuleModel module)
	{
		if (module.ContainsTypeReferences(assemblies, OSFileSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("OS File System");
		}
		if (module.ContainsTypeReferences(assemblies, GameFileSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Game File System");
		}
		if (module.ContainsTypeReferences(assemblies, ShellTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Shell");
		}
		if (module.ContainsTypeReferences(assemblies, SaveSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Save System");
		}
		if (module.ContainsAssemblyReferences(assemblies, SaveSystemAssemblyReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Save System");
		}
		if (module.ContainsTypeReferences(assemblies, GameEntitiesTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Game Entities");
		}
		if (module.ContainsAssemblyReferences(assemblies, GameEntitiesAssemblyReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Game Entities");
		}
		if (module.ContainsAssemblyReferences(assemblies, InputSystemAssemblyReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Input System");
		}
		if (module.ContainsAssemblyReferences(assemblies, LocalizationSystemAssemblyReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Localization");
		}
		if (module.ContainsTypeReferences(assemblies, UITypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("User Interface");
		}
		if (module.ContainsAssemblyReferences(assemblies, UIAssemblyReferences))
		{
			yield return new CapabilityModuleOrPluginModel("User Interface");
		}
		if (module.ContainsTypeReferences(assemblies, HttpTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Http");
		}
		if (module.ContainsTypeReferences(assemblies, AchievementSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Achievements");
		}
		if (module.ContainsTypeReferences(assemblies, CampaignSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Campaign");
		}
		if (module.ContainsTypeReferences(assemblies, SkillSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Skills");
		}
		if (module.ContainsTypeReferences(assemblies, ItemSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Items");
		}
		if (module.ContainsTypeReferences(assemblies, CultureSystemTypeReferences))
		{
			yield return new CapabilityModuleOrPluginModel("Cultures");
		}
	}

	public static string GetBUTRLoaderVersion(CrashReportModel crashReport)
	{
		AssemblyModel assemblyModel = crashReport.Assemblies.FirstOrDefault((AssemblyModel x) => x.Id.Name == "Bannerlord.BUTRLoader");
		if ((object)assemblyModel != null)
		{
			return assemblyModel.Id.Version ?? string.Empty;
		}
		return string.Empty;
	}

	public static string GetBLSEVersion(CrashReportModel crashReport)
	{
		AssemblyModel assemblyModel = crashReport.Assemblies.FirstOrDefault((AssemblyModel x) => x.Id.Name == "Bannerlord.BLSE");
		if ((object)assemblyModel != null)
		{
			return assemblyModel.Id.Version ?? string.Empty;
		}
		return string.Empty;
	}

	public static string GetLauncherType(CrashReportModel crashReport)
	{
		string text = crashReport.AdditionalMetadata.FirstOrDefault((MetadataModel x) => x.Key == "METADATA:Parent_Process_Name")?.Value;
		switch (text)
		{
		case "Vortex":
			return "vortex";
		case "BannerLordLauncher":
			return "bannerlordlauncher";
		case "steam":
			return "steam";
		case "GalaxyClient":
			return "gog";
		case "EpicGamesLauncher":
			return "epicgames";
		case "devenv":
			return "debuggervisualstudio";
		case "JetBrains.Debugger.Worker64c":
			return "debuggerjetbrains";
		case "explorer":
			return "explorer";
		case "NovusLauncher":
			return "novus";
		case "ModOrganizer":
			return "modorganizer";
		default:
			return "unknown launcher - " + text;
		case null:
			if (!string.IsNullOrEmpty(GetBUTRLoaderVersion(crashReport)))
			{
				return "butrloader";
			}
			if (!string.IsNullOrEmpty(GetBLSEVersion(crashReport)))
			{
				return "blse";
			}
			return "vanilla";
		}
	}

	public static string GetLauncherVersion(CrashReportModel crashReport)
	{
		string text = crashReport.AdditionalMetadata.FirstOrDefault((MetadataModel x) => x.Key == "METADATA:Parent_Process_File_Version")?.Value;
		if (text != null)
		{
			return text;
		}
		string bUTRLoaderVersion = GetBUTRLoaderVersion(crashReport);
		if (bUTRLoaderVersion != null && !string.IsNullOrEmpty(bUTRLoaderVersion))
		{
			return bUTRLoaderVersion;
		}
		string bLSEVersion = GetBLSEVersion(crashReport);
		if (bLSEVersion != null && !string.IsNullOrEmpty(bLSEVersion))
		{
			return bLSEVersion;
		}
		return "0";
	}
}
