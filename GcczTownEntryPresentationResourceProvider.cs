using System;
using System.IO;
using System.Reflection;
using System.Text;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized staged town entry presentation once per process.
/// </summary>
internal static class GcczTownEntryPresentationResourceProvider
{
	private const string FileName = "GcczTownEntryPresentation.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownEntryPresentation.zh-CN.json";
	private static readonly Lazy<TownEntryPresentationTextCatalog> CachedCatalog = new Lazy<TownEntryPresentationTextCatalog>(LoadCatalog, true);

	internal static TownEntryPresentationTextCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownEntryPresentationTextCatalog LoadCatalog()
	{
		try
		{
			string path = AnimusForgeModulePaths.GetModuleDataFilePath(FileName);
			if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
			{
				TownEntryPresentationTextCatalog diskCatalog = Deserialize(File.ReadAllText(path, Encoding.UTF8));
				Logger.Log("GcczEntry", "Loaded town entry presentation from ModuleData. Version=" + diskCatalog.Version);
				return diskCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczEntry", "ModuleData town entry presentation load failed: " + ex.Message);
		}

		try
		{
			Assembly assembly = typeof(GcczTownEntryPresentationResourceProvider).Assembly;
			using Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName);
			if (stream != null)
			{
				using var reader = new StreamReader(stream, Encoding.UTF8, true);
				TownEntryPresentationTextCatalog embeddedCatalog = Deserialize(reader.ReadToEnd());
				Logger.Log("GcczEntry", "Loaded embedded town entry presentation. Version=" + embeddedCatalog.Version);
				return embeddedCatalog;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("GcczEntry", "Embedded town entry presentation load failed: " + ex.Message);
		}

		Logger.Log("GcczEntry", "Using English fail-safe town entry presentation.");
		return TownEntryPresentationTextCatalog.CreateEnglishFallback();
	}

	private static TownEntryPresentationTextCatalog Deserialize(string json)
	{
		TownEntryPresentationTextCatalog parsed = JsonConvert.DeserializeObject<TownEntryPresentationTextCatalog>(json ?? string.Empty);
		return TownEntryPresentationTextCatalog.Resolve(parsed);
	}
}
