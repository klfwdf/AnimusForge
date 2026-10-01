using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized town-only GCCZ manual once per process.
/// </summary>
internal static class GcczTownManualResourceProvider
{
	private const string FileName = "GcczTownManual.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownManual.zh-CN.json";
	private static readonly Lazy<TownManualCatalog> CachedCatalog = new Lazy<TownManualCatalog>(LoadCatalog, true);

	internal static TownManualCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownManualCatalog LoadCatalog()
	{
		return GcczLocalizedResourceLoader.Load(
			FileName,
			EmbeddedResourceName,
			"GcczManual",
			Deserialize,
			TownManualCatalog.CreateEnglishFallback);
	}

	private static TownManualCatalog Deserialize(string json)
	{
		TownManualCatalog parsed = JsonConvert.DeserializeObject<TownManualCatalog>(json ?? string.Empty);
		return TownManualCatalog.Resolve(parsed);
	}
}
