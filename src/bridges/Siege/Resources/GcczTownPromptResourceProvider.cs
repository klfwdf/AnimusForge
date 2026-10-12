using System;

using System.IO;

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

	private static readonly Lazy<TownPromptTextCatalog> CachedCatalog = new Lazy<TownPromptTextCatalog>(LoadCatalog, true);

	private static readonly Lazy<TownPromptTextCatalog> ChinesePresentation = new Lazy<TownPromptTextCatalog>(() => LoadPresentation("zh-CN"), true);

	private static readonly Lazy<TownPromptTextCatalog> EnglishPresentation = new Lazy<TownPromptTextCatalog>(() => LoadPresentation("en"), true);



	internal static TownPromptTextCatalog GetCatalog()

	{

		return CachedCatalog.Value;

	}



	private static TownPromptTextCatalog LoadCatalog()

	{

		string filePath = AnimusForgeModulePaths.GetModuleDataFilePath("GcczTownPrompt.json");

		if (File.Exists(filePath))

		{

			return Deserialize(File.ReadAllText(filePath, Encoding.UTF8));

		}

		throw new FileNotFoundException("The selected GCCZ prompt package is missing.", filePath);

	}



	internal static TownPromptTextCatalog GetPresentationCatalog()

		=> GcczLocalizedResourceLoader.UseChineseInterface ? ChinesePresentation.Value : EnglishPresentation.Value;



	internal static TownPromptTextCatalog GetFactCatalog() => ChinesePresentation.Value;

	// The package catalog is cached; UI language never controls prompt projection.
	internal static bool UseEnglishPrompt => EnglishPrompt.Value;
	private static readonly Lazy<bool> EnglishPrompt = new Lazy<bool>(() =>
	{
		string language = GetCatalog().Language;
		if (language == "en" || language == "en-US") return true;
		if (language == "zh-CN") return false;
		throw new InvalidDataException("Unsupported GCCZ prompt package language: " + language);
	}, true);



	private static TownPromptTextCatalog LoadPresentation(string language)

	{

		return GcczLocalizedResourceLoader.Load(

			"GcczTownPrompt." + language + ".json",

			"AnimusForge.Defaults.GcczTownPrompt." + language + ".json",

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

