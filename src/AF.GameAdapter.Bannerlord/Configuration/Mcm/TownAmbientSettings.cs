using System;
using System.Collections.Generic;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace AnimusForge;

/// <summary>
/// Player-facing controls for the data-only settlement life layer.  Keeping these
/// settings in MCM lets low-end users turn the feature off without changing
/// the dialogue file, while high-end users can raise both chatter and crowd
/// density in one place.
/// </summary>
public partial class DuelSettings : AttributeGlobalSettings<DuelSettings>
{
	[SettingPropertyBool("{=AF_MCM_TownAmbient_Enable}Enable settlement ambient chatter and extra residents", Order = 0, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Enable_Hint}Covers towns, villages, ports, and lord halls. Disabling stops ambient bubbles and extra residents; it takes full effect after re-entering the scene.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life", GroupOrder = -50)]
	public bool EnableTownAmbientDialogue { get; set; } = true;

	[SettingPropertyInteger("{=AF_MCM_TownAmbient_Density}Settlement-life performance tier", 0, 3, "0", Order = 1, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Density_Hint}0=off, 1=low, 2=standard, 3=high density. The tier adjusts chatter rates and extra residents across towns, villages, ports, and lord halls.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life", GroupOrder = -50)]
	public int TownAmbientDialogueDensity { get; set; } = 2;

	[SettingPropertyFloatingInteger("{=AF_MCM_TownAmbient_Population}Extra resident target multiplier", 1.0f, 3.0f, "0.0", Order = 2, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Population_Hint}Uses the native resident count as the baseline. The standard target is about 2.0; high density can target up to 3x, limited by scene spawn points and safety caps.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life", GroupOrder = -50)]
	public float TownAmbientPopulationMultiplier { get; set; } = 2.2f;

	[SettingPropertyBool("{=AF_MCM_TownAmbient_Echo}Enable ambient multi-person replies", Order = 3, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Echo_Hint}Occasionally lets another nearby resident continue the previous line using local preset text without AI. Disabling reduces bubbles and scanning work.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life", GroupOrder = -50)]
	public bool EnableTownAmbientEventEcho { get; set; } = true;

	[SettingPropertyBool("{=AF_MCM_TownAmbient_Ai}Enable ambient AI interface (explicit opt-in)", Order = 4, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Ai_Hint}Off by default. Ambient replies use AI only after you enter a separate endpoint and opt in; otherwise they use local preset text and consume 0 tokens.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public bool EnableTownAmbientAi { get; set; } = false;

	[SettingPropertyText("{=AF_MCM_TownAmbient_ApiUrl}Ambient AI API address (Base URL supported)", -1, true, "", Order = 0, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_ApiUrl_Hint}For example, https://api.openai.com/v1 or a full /v1/chat/completions URL. Only the address you enter is contacted; AnimusForge servers are not used.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public string TownAmbientAiApiUrl { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_TownAmbient_ApiKey}Ambient AI API key", -1, true, "", Order = 1, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_ApiKey_Hint}Use your own API key. It is sent only to the local endpoint you configure; logs and usage notices never show the full key.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public string TownAmbientAiApiKey { get; set; } = "";

	[SettingPropertyText("{=AF_MCM_TownAmbient_Model}Ambient AI model name", -1, true, "", Order = 2, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Model_Hint}For example, gpt-4o-mini or deepseek-chat. Enter a model supported by the configured endpoint.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public string TownAmbientAiModelName
	{
		get => ReadModelNameForPreset(ref _townAmbientAiModelName, _townAmbientAiModelDropdown);
		set => SetModelNameForPreset(value, ref _townAmbientAiModelName, ref _townAmbientAiModelDropdown, ref _townAmbientAiModelOptions);
	}

	[SettingPropertyButton("{=AF_MCM_TownAmbient_Fetch}Fetch ambient AI model list", -1, true, "", Content = "{=AF_MCM_TownAmbient_FetchContent}Fetch", Order = 3, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Fetch_Hint}Requests /models using the ambient AI address and key. Fetching normally produces no chat tokens.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public Action FetchTownAmbientAiModelList { get; set; }

	[SettingPropertyDropdown("{=AF_MCM_TownAmbient_Dropdown}Ambient AI model name (dropdown)", Order = 4, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Dropdown_Hint}Fetch the model list first, then choose an entry. Selecting *manual entry* uses the text field above.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public Dropdown<string> TownAmbientAiModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selected = GetTownAmbientAiSelectedModelOption();
			if (_townAmbientAiModelDropdown == null || _townAmbientAiModelDropdown.Count == 0)
				_townAmbientAiModelDropdown = BuildDropdownFromOptions(_townAmbientAiModelOptions, selected, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out var _);
			return _townAmbientAiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			string selected = _townAmbientAiModelName;
			_townAmbientAiModelDropdown = BuildDropdownFromIncoming(ResolveLoadedModelDropdown(value, _townAmbientAiModelDropdown, _townAmbientAiModelName), _townAmbientAiModelOptions, selected, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out var normalized);
			if (!string.IsNullOrWhiteSpace(normalized) && !IsManualModelOption(normalized))
			{
				TownAmbientAiModelName = normalized;
			}
		}
	}

	[SettingPropertyButton("{=AF_MCM_TownAmbient_Test}Test ambient AI interface", -1, true, "", Content = "{=AF_MCM_TownAmbient_TestContent}Test", Order = 5, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Test_Hint}Sends one short test request. The test consumes a small number of tokens and shows an explicit in-game notice.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public Action TestTownAmbientAiConnection { get; set; }

	[SettingPropertyInteger("{=AF_MCM_TownAmbient_MaxTokens}Ambient AI maximum output tokens", 32, 512, "0", Order = 6, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_MaxTokens_Hint}Ambient replies need only short sentences; 64-128 is recommended. Higher values increase potential per-request usage.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public int TownAmbientAiMaxOutputTokens { get; set; } = 128;

	[SettingPropertyInteger("{=AF_MCM_TownAmbient_Chance}Ambient AI trigger chance (%)", 0, 100, "0", Order = 7, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Chance_Hint}Only affects an existing multi-person reply event. It never makes AI scan or start events. 5-10 is recommended.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public int TownAmbientAiChancePercent { get; set; } = 8;

	[SettingPropertyInteger("{=AF_MCM_TownAmbient_Rate}Ambient AI requests per minute", 0, 10, "0", Order = 8, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Rate_Hint}0 forbids ambient AI requests; 0-1 is recommended for low-end systems and 1-2 for mid-range systems. Local text is used after the cap is reached.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public int TownAmbientAiRequestsPerMinute { get; set; } = 2;

	[SettingPropertyInteger("{=AF_MCM_TownAmbient_Budget}Ambient AI session token budget", 0, 100000, "0", Order = 9, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Budget_Hint}Estimated input plus output limit for ambient AI during this session. Local text is used after the limit; 0 forbids calls.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public int TownAmbientAiSessionTokenBudget { get; set; } = 5000;

	[SettingPropertyBool("{=AF_MCM_TownAmbient_Usage}Show ambient AI usage notices", Order = 10, RequireRestart = false,
		HintText = "{=AF_MCM_TownAmbient_Usage_Hint}After each ambient AI reply, show estimated input/output tokens and the session total. Keeping this enabled is recommended.")]
	[SettingPropertyGroup("{=AF_MCM_TownAmbient_Group}3. Scene shout / Settlement life/{=AF_MCM_TownAmbient_AiGroup}Ambient AI interface", GroupOrder = -49)]
	public bool TownAmbientAiShowUsageNotifications { get; set; } = true;

	public static bool IsTownAmbientDialogueEnabled()
	{
		return GetSettings()?.EnableTownAmbientDialogue ?? true;
	}

	public static int GetTownAmbientDialogueDensity()
	{
		DuelSettings settings = GetSettings();
		if (settings == null || !settings.EnableTownAmbientDialogue)
		{
			return 0;
		}
		return Math.Max(0, Math.Min(3, settings.TownAmbientDialogueDensity));
	}

	public static float GetTownAmbientPopulationMultiplier()
	{
		DuelSettings settings = GetSettings();
		if (settings == null || !settings.EnableTownAmbientDialogue)
		{
			return 1f;
		}
		float value = settings.TownAmbientPopulationMultiplier;
		if (float.IsNaN(value) || float.IsInfinity(value))
		{
			value = 2.2f;
		}
		return Math.Max(1f, Math.Min(3f, value));
	}

	public static bool IsTownAmbientEventEchoEnabled()
	{
		DuelSettings settings = GetSettings();
		return settings != null && settings.EnableTownAmbientDialogue && settings.EnableTownAmbientEventEcho;
	}

	public static bool IsTownAmbientAiEnabled()
	{
		DuelSettings settings = GetSettings();
		return settings != null && settings.EnableTownAmbientDialogue && settings.EnableTownAmbientEventEcho && settings.EnableTownAmbientAi;
	}

	public static string GetTownAmbientAiApiUrl()
	{
		return (GetSettings()?.TownAmbientAiApiUrl ?? "").Trim();
	}

	public static string GetTownAmbientAiApiKey()
	{
		return (GetSettings()?.TownAmbientAiApiKey ?? "").Trim();
	}

	public static string GetTownAmbientAiModelName()
	{
		DuelSettings settings = GetSettings();
		return settings?.GetEffectiveTownAmbientAiModelName() ?? "";
	}

	public static int GetTownAmbientAiMaxOutputTokens()
	{
		return Math.Max(32, Math.Min(512, GetSettings()?.TownAmbientAiMaxOutputTokens ?? 128));
	}

	public static int GetTownAmbientAiChancePercent()
	{
		return Math.Max(0, Math.Min(100, GetSettings()?.TownAmbientAiChancePercent ?? 8));
	}

	public static int GetTownAmbientAiRequestsPerMinute()
	{
		return Math.Max(0, Math.Min(10, GetSettings()?.TownAmbientAiRequestsPerMinute ?? 2));
	}

	public static int GetTownAmbientAiSessionTokenBudget()
	{
		return Math.Max(0, Math.Min(100000, GetSettings()?.TownAmbientAiSessionTokenBudget ?? 5000));
	}

	public static bool ShouldShowTownAmbientAiUsage()
	{
		return GetSettings()?.TownAmbientAiShowUsageNotifications ?? true;
	}

	public string GetTownAmbientAiSelectedModelOption()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveSelectedModelOption(_townAmbientAiModelOptions, _townAmbientAiModelDropdown, TownAmbientAiModelName, "", preserveBlankSelection: false);
	}

	public string GetEffectiveTownAmbientAiModelName()
	{
		EnsureModelDropdownCacheHydrated();
		return ResolveEffectiveModelName(_townAmbientAiModelOptions, _townAmbientAiModelDropdown, TownAmbientAiModelName, "", preserveBlankSelection: false);
	}

	private void ApplyTownAmbientAiModelList(List<string> models)
	{
		EnsureModelDropdownCacheHydrated();
		List<string> list = models ?? new List<string>();
		string selected = ResolveSelectedOptionAfterFetch(list, GetTownAmbientAiSelectedModelOption(), TownAmbientAiModelName, "", preserveBlankSelection: false);
		_townAmbientAiModelDropdown = BuildDropdownFromOptions(list, selected, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out var _);
		PersistModelDropdownCacheSnapshot();
	}
}
