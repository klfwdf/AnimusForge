using System;
using System.IO;
using System.Reflection;
using System.Text;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized GCCZ town prompt text once per process.
/// The reusable composer and all prompt policy remain in the GCCZ core project.
/// </summary>
internal static class GcczTownPromptResourceProvider
{
	private const string FileName = "GcczTownPrompt.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownPrompt.zh-CN.json";
	private static readonly Lazy<TownPromptTextCatalog> CachedCatalog = new Lazy<TownPromptTextCatalog>(LoadCatalog, true);

	internal static TownPromptTextCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownPromptTextCatalog LoadCatalog()
	{
		try
		{
			string path = AnimusForgeModulePaths.GetModuleDataFilePath(FileName);
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
			{
				TownPromptTextCatalog diskCatalog = Deserialize(File.ReadAllText(path, Encoding.UTF8));
				Logger.Log("GcczPrompt", "Loaded town prompt resource from ModuleData. Version=" + diskCatalog.Version);
				return diskCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczPrompt", "ModuleData town prompt resource load failed: " + ex.Message);
		}

		try
		{
			Assembly assembly = typeof(GcczTownPromptResourceProvider).Assembly;
			using Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
			if (stream != null)
			{
				using var reader = new StreamReader(stream, Encoding.UTF8, true);
				TownPromptTextCatalog embeddedCatalog = Deserialize(reader.ReadToEnd());
				Logger.Log("GcczPrompt", "Loaded embedded town prompt resource. Version=" + embeddedCatalog.Version);
				return embeddedCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczPrompt", "Embedded town prompt resource load failed: " + ex.Message);
		}

		Logger.Log("GcczPrompt", "Using English fail-safe town prompt resource.");
		return TownPromptTextCatalog.CreateEnglishFallback();
	}

	private static TownPromptTextCatalog Deserialize(string json)
	{
		TownPromptTextCatalog parsed = JsonConvert.DeserializeObject<TownPromptTextCatalog>(json ?? string.Empty);
		return TownPromptTextCatalog.Resolve(parsed);
	}
}
