using System;

namespace AnimusForge.Refactor.Adapters;

internal enum ConfiguredChatRoute { Main = 0, Auxiliary = 1, EventAndRebellion = 2 }

// Captured once by each application attempt. Credentials remain local to the
// gateway closure; this adapter does not create a serializable secret snapshot.
internal static class LlmRequestConfigurationCaptureAdapter
{
    private const int UniversalApiDefaultMaxTokens = DuelSettings.DefaultGeneralApiMaxTokens;
    private const int EventAndRebellionApiDefaultMaxTokens = DuelSettings.DefaultEventAndRebellionApiMaxTokens;
	internal static bool TryGetPrimaryUniversalApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName)
	{
		effectiveApiUrl = "";
		apiKey = "";
		modelName = "";
		if (settings == null)
		{
			return false;
		}
		effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(settings.ApiUrl ?? "");
		apiKey = (settings.ApiKey ?? "").Trim();
		modelName = settings.GetEffectiveMainModelName();
		return !string.IsNullOrWhiteSpace(effectiveApiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
	}

	internal static bool TryGetEventAndRebellionDedicatedApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName, out bool hasAnyField)
	{
		effectiveApiUrl = "";
		apiKey = "";
		modelName = "";
		hasAnyField = false;
		if (settings == null)
		{
			return false;
		}
		string text = (settings.EventAndRebellionApiUrl ?? "").Trim();
		string text2 = (settings.EventAndRebellionApiKey ?? "").Trim();
		string text3 = settings.GetEffectiveEventAndRebellionModelName();
		string text4 = settings.GetEventAndRebellionSelectedModelOption();
		hasAnyField = !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(text2) || !string.IsNullOrWhiteSpace((settings.EventAndRebellionModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(text4);
		if (!hasAnyField)
		{
			return false;
		}
		effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(text);
		apiKey = text2;
		modelName = text3;
		return !string.IsNullOrWhiteSpace(effectiveApiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
	}

	internal static bool TryGetAuxiliaryDedicatedApiConfig(DuelSettings settings, out string effectiveApiUrl, out string apiKey, out string modelName, out bool hasAnyField)
	{
		effectiveApiUrl = "";
		apiKey = "";
		modelName = "";
		hasAnyField = false;
		if (settings == null)
		{
			return false;
		}
		string text = (settings.AuxiliaryApiUrl ?? "").Trim();
		string text2 = (settings.AuxiliaryApiKey ?? "").Trim();
		string text3 = settings.GetEffectiveAuxiliaryModelName();
		string text4 = settings.GetAuxiliarySelectedModelOption();
		hasAnyField = !string.IsNullOrWhiteSpace(text) || !string.IsNullOrWhiteSpace(text2) || !string.IsNullOrWhiteSpace((settings.AuxiliaryModelName ?? "").Trim()) || !string.IsNullOrWhiteSpace(text4);
		if (!hasAnyField)
		{
			return false;
		}
		effectiveApiUrl = DuelSettings.GetEffectiveApiUrl(text);
		apiKey = text2;
		modelName = text3;
		return !string.IsNullOrWhiteSpace(effectiveApiUrl) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(modelName);
	}

	internal static bool TryResolveUniversalApiConfig(DuelSettings settings, ConfiguredChatRoute route, out string effectiveApiUrl, out string apiKey, out string modelName, out string resolvedRoute, out string errorMessage)
	{
		effectiveApiUrl = "";
		apiKey = "";
		modelName = "";
		resolvedRoute = "main";
		errorMessage = "请检查 MCM 设置。";
		bool hasAuxiliaryDedicatedFields = false;
		bool hasEventAndRebellionDedicatedFields = false;
		if (settings == null)
		{
			return false;
		}
		if (route == ConfiguredChatRoute.Auxiliary)
		{
			if (TryGetAuxiliaryDedicatedApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName, out hasAuxiliaryDedicatedFields))
			{
				resolvedRoute = "auxiliary_dedicated";
				errorMessage = "";
				return true;
			}
		}
		if (route == ConfiguredChatRoute.EventAndRebellion)
		{
			if (TryGetEventAndRebellionDedicatedApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName, out hasEventAndRebellionDedicatedFields))
			{
				resolvedRoute = "event_rebellion_dedicated";
				errorMessage = "";
				return true;
			}
		}
		if (TryGetPrimaryUniversalApiConfig(settings, out effectiveApiUrl, out apiKey, out modelName))
		{
			resolvedRoute = ((route == ConfiguredChatRoute.Auxiliary) ? (hasAuxiliaryDedicatedFields ? "auxiliary_partial_fallback_main" : "auxiliary_fallback_main") : ((route == ConfiguredChatRoute.EventAndRebellion) ? (hasEventAndRebellionDedicatedFields ? "event_rebellion_partial_fallback_main" : "event_rebellion_fallback_main") : "main"));
			errorMessage = "";
			return true;
		}
		return false;
	}

	internal static void ResolveUniversalThinkingSettings(DuelSettings settings, string resolvedRoute, out bool thinkingEnabled, out string effort)
	{
		thinkingEnabled = true;
		effort = DuelSettings.ReasoningEffortHigh;
		if (settings == null)
		{
			return;
		}
		string route = (resolvedRoute ?? "").Trim();
		if (route.StartsWith("event_rebellion", StringComparison.OrdinalIgnoreCase))
		{
			thinkingEnabled = settings.EventAndRebellionApiThinkingEnabled;
			effort = settings.GetEventAndRebellionApiReasoningEffort();
			return;
		}
		if (route.StartsWith("auxiliary_dedicated", StringComparison.OrdinalIgnoreCase))
		{
			thinkingEnabled = settings.AuxiliaryApiThinkingEnabled;
			effort = settings.GetAuxiliaryApiReasoningEffort();
			return;
		}
		thinkingEnabled = settings.MainApiThinkingEnabled;
		effort = settings.GetMainApiReasoningEffort();
	}

	internal static float ResolveUniversalApiTemperature(DuelSettings settings, string resolvedRoute)
	{
		if (settings == null)
		{
			return 0.8f;
		}
		string route = (resolvedRoute ?? "").Trim();
		if (route.StartsWith("event_rebellion_dedicated", StringComparison.OrdinalIgnoreCase))
		{
			return settings.GetEventAndRebellionApiTemperature();
		}
		if (route.StartsWith("auxiliary_dedicated", StringComparison.OrdinalIgnoreCase))
		{
			return settings.GetAuxiliaryApiTemperature();
		}
		return settings.GetMainApiTemperature();
	}

	internal static int ResolveUniversalMaxTokens(DuelSettings settings, string resolvedRoute)
	{
		string route = (resolvedRoute ?? "").Trim();
		if (route.StartsWith("event_rebellion", StringComparison.OrdinalIgnoreCase))
		{
			return settings?.GetEventAndRebellionApiMaxTokens() ?? EventAndRebellionApiDefaultMaxTokens;
		}
		if (route.StartsWith("auxiliary_dedicated", StringComparison.OrdinalIgnoreCase))
		{
			return settings?.GetAuxiliaryApiMaxTokens() ?? UniversalApiDefaultMaxTokens;
		}
		return settings?.GetMainApiMaxTokens() ?? UniversalApiDefaultMaxTokens;
	}

	internal static int GetEventAndRebellionApiMaxTokens()
	{
		try
		{
			return ResolveUniversalMaxTokens(DuelSettings.GetSettings(), "event_rebellion");
		}
		catch
		{
			return EventAndRebellionApiDefaultMaxTokens;
		}
	}

    internal static AnimusForge.ConversationSpeechTextOptions CaptureSceneSpeechTextOptions()
    {
        DuelSettings settings;
        try { settings = DuelSettings.GetSettings(); }
        catch { settings = null; }
        return new AnimusForge.ConversationSpeechTextOptions(
            settings?.UseDetailedSceneSpeechPrompt == true,
            settings?.PreserveSceneAsteriskActions == true);
    }
	internal static int GetMemoryCompressionDenominatorFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return TaleWorlds.Library.MBMath.ClampInt(settings.MemoryCompressionDenominator, 3, 10);
			}
		}
		catch
		{
		}
		return 5;
	}
	internal static int GetMemoryOverviewStartBlockCountFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return TaleWorlds.Library.MBMath.ClampInt(settings.MemoryOverviewStartBlockCount, 3, 10);
			}
		}
		catch
		{
		}
		return 5;
	}
	internal static int GetMemoryOverviewTargetCharsFromSettings()
	{
		try
		{
			DuelSettings settings = DuelSettings.GetSettings();
			if (settings != null)
			{
				return TaleWorlds.Library.MBMath.ClampInt(settings.MemoryOverviewTargetChars, 100, 1000);
			}
		}
		catch
		{
		}
		return 200;
	}
}
