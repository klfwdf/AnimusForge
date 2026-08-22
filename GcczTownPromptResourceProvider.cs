using System;
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
		return GcczLocalizedResourceLoader.Load(
			FileName,
			EmbeddedResourceName,
			"GcczPrompt",
			Deserialize,
			TownPromptTextCatalog.CreateEnglishFallback);
	}

	private static TownPromptTextCatalog Deserialize(string json)
	{
		TownPromptTextCatalog parsed = JsonConvert.DeserializeObject<TownPromptTextCatalog>(json ?? string.Empty);
		return TownPromptTextCatalog.Resolve(parsed);
	}
}
