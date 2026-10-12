using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized town-only GCCZ manual once per process.
/// </summary>
internal static class GcczTownManualResourceProvider
{
	private const string BaseName = "GcczTownManual";
	private static readonly Lazy<TownManualCatalog> ChineseCatalog = new Lazy<TownManualCatalog>(() => LoadCatalog("zh-CN"), true);
	private static readonly Lazy<TownManualCatalog> EnglishCatalog = new Lazy<TownManualCatalog>(() => LoadCatalog("en"), true);

	internal static TownManualCatalog GetCatalog()
	{
		return GcczLocalizedResourceLoader.UseChineseInterface ? ChineseCatalog.Value : EnglishCatalog.Value;
	}

	internal static TownManualCatalog GetFactCatalog() => ChineseCatalog.Value;

	private static TownManualCatalog LoadCatalog(string language)
	{
		return GcczLocalizedResourceLoader.Load(
			BaseName + "." + language + ".json",
			"AnimusForge.Defaults." + BaseName + "." + language + ".json",
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
