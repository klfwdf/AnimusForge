using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads localized feedback for the scene-local hidden-resident flow once per process.
/// </summary>
internal static class GcczTownHiddenResidentResourceProvider
{
	private const string BaseName = "GcczTownHiddenResidents";
	private static readonly Lazy<TownHiddenResidentTextCatalog> ChineseCatalog = new Lazy<TownHiddenResidentTextCatalog>(() => LoadCatalog("zh-CN"), true);
	private static readonly Lazy<TownHiddenResidentTextCatalog> EnglishCatalog = new Lazy<TownHiddenResidentTextCatalog>(() => LoadCatalog("en"), true);

	internal static TownHiddenResidentTextCatalog GetCatalog()
	{
		return GcczLocalizedResourceLoader.UseChineseInterface ? ChineseCatalog.Value : EnglishCatalog.Value;
	}

	internal static TownHiddenResidentTextCatalog GetFactCatalog() => ChineseCatalog.Value;

	private static TownHiddenResidentTextCatalog LoadCatalog(string language)
	{
		return GcczLocalizedResourceLoader.Load(
			BaseName + "." + language + ".json",
			"AnimusForge.Defaults." + BaseName + "." + language + ".json",
			"GcczHiddenResidents",
			Deserialize,
			TownHiddenResidentTextCatalog.CreateEnglishFallback);
	}

	private static TownHiddenResidentTextCatalog Deserialize(string json)
	{
		TownHiddenResidentTextCatalog parsed = JsonConvert.DeserializeObject<TownHiddenResidentTextCatalog>(json ?? string.Empty);
		return TownHiddenResidentTextCatalog.Resolve(parsed);
	}
}
