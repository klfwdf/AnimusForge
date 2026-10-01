using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads localized feedback for the scene-local hidden-resident flow once per process.
/// </summary>
internal static class GcczTownHiddenResidentResourceProvider
{
	private const string FileName = "GcczTownHiddenResidents.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownHiddenResidents.zh-CN.json";
	private static readonly Lazy<TownHiddenResidentTextCatalog> CachedCatalog = new Lazy<TownHiddenResidentTextCatalog>(LoadCatalog, true);

	internal static TownHiddenResidentTextCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownHiddenResidentTextCatalog LoadCatalog()
	{
		return GcczLocalizedResourceLoader.Load(
			FileName,
			EmbeddedResourceName,
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
