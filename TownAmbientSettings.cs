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
	[SettingPropertyBool("启用定居点环境闲谈与额外居民", Order = 0, RequireRestart = false,
		HintText = "覆盖城镇、村庄、港口和领主大厅。关闭后不再生成环境气泡，也不再补充额外居民；重新进入场景后完全生效。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活", GroupOrder = -50)]
	public bool EnableTownAmbientDialogue { get; set; } = true;

	[SettingPropertyInteger("定居点生活性能档位", 0, 3, "0", Order = 1, RequireRestart = false,
		HintText = "0=关闭，1=低负担，2=标准，3=高密度。档位会同时调整城镇、村庄、港口和领主大厅的台词冒出率与额外居民数量。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活", GroupOrder = -50)]
	public int TownAmbientDialogueDensity { get; set; } = 2;

	[SettingPropertyFloatingInteger("额外居民目标倍率", 1.0f, 3.0f, "0.0", Order = 2, RequireRestart = false,
		HintText = "以原版当前定居点居民数为基准，标准值 2.0 左右；高档位最多按 3 倍目标补充。受场景可用出生点和安全上限限制。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活", GroupOrder = -50)]
	public float TownAmbientPopulationMultiplier { get; set; } = 2.2f;

	[SettingPropertyBool("启用环境多人接话", Order = 3, RequireRestart = false,
		HintText = "偶尔让附近另一名居民接上上一句台词；使用本地预设文本，不调用 AI。关闭可进一步降低气泡与扫描负担。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活", GroupOrder = -50)]
	public bool EnableTownAmbientEventEcho { get; set; } = true;

	[SettingPropertyBool("启用环境 AI 接口（明确授权）", Order = 4, RequireRestart = false,
		HintText = "默认关闭。只有你填写独立接口并主动开启后，环境多人接话才可能调用 AI；未开启时始终使用本地预设文本，消耗 0 Token。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public bool EnableTownAmbientAi { get; set; } = false;

	[SettingPropertyText("环境 AI API 地址（支持 Base URL）", -1, true, "", Order = 0, RequireRestart = false,
		HintText = "例如 https://api.openai.com/v1 或完整的 /v1/chat/completions。只会连接你填写的地址，不经过 AnimusForge 服务器。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public string TownAmbientAiApiUrl { get; set; } = "";

	[SettingPropertyText("环境 AI API 密钥（Key）", -1, true, "", Order = 1, RequireRestart = false,
		HintText = "使用你自己的 API Key。Key 只用于本地直连接口，日志和用量提示不会显示完整密钥。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public string TownAmbientAiApiKey { get; set; } = "";

	[SettingPropertyText("环境 AI 模型名称", -1, true, "", Order = 2, RequireRestart = false,
		HintText = "例如 gpt-4o-mini、deepseek-chat。请填写该接口实际支持的模型名。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public string TownAmbientAiModelName { get; set; } = "gpt-4o-mini";

	[SettingPropertyButton("拉取环境 AI 模型列表", -1, true, "", Content = "点击拉取", Order = 3, RequireRestart = false,
		HintText = "使用你填写的环境 AI 地址和 Key 请求 /models；拉取本身通常不产生聊天 Token。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public Action FetchTownAmbientAiModelList { get; set; }

	[SettingPropertyDropdown("环境 AI 模型名称（下拉）", Order = 4, RequireRestart = false,
		HintText = "先拉取模型列表，再从下拉选择；选择“*手动填写*”时使用上方文本框。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public Dropdown<string> TownAmbientAiModelDropdown
	{
		get
		{
			EnsureModelDropdownCacheHydrated();
			string selected = GetTownAmbientAiSelectedModelOption();
			_townAmbientAiModelDropdown = BuildDropdownFromOptions(_townAmbientAiModelOptions, selected, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out var _);
			return _townAmbientAiModelDropdown;
		}
		set
		{
			EnsureModelDropdownCacheHydrated();
			string selected = GetTownAmbientAiSelectedModelOption();
			_townAmbientAiModelDropdown = BuildDropdownFromIncoming(value, _townAmbientAiModelOptions, selected, "", preserveBlankSelection: false, out _townAmbientAiModelOptions, out var normalized);
			if (!string.IsNullOrWhiteSpace(normalized) && !IsManualModelOption(normalized))
			{
				TownAmbientAiModelName = normalized;
			}
			PersistModelDropdownCacheSnapshot();
		}
	}

	[SettingPropertyButton("测试环境 AI 接口", -1, true, "", Content = "点击测试", Order = 5, RequireRestart = false,
		HintText = "只发送一条很短的测试请求；测试本身会消耗少量 Token，并会在游戏内明确提示。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public Action TestTownAmbientAiConnection { get; set; }

	[SettingPropertyInteger("环境 AI 每次最大输出 Tokens", 32, 512, "0", Order = 6, RequireRestart = false,
		HintText = "环境接话只需要很短的句子，建议 64～128。数值越高，单次潜在消耗越高。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public int TownAmbientAiMaxOutputTokens { get; set; } = 128;

	[SettingPropertyInteger("环境 AI 触发概率（%）", 0, 100, "0", Order = 7, RequireRestart = false,
		HintText = "只影响已经产生多人接话事件的场合，不会让 AI 主动扫描或主动发起事件。建议 5～10。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public int TownAmbientAiChancePercent { get; set; } = 8;

	[SettingPropertyInteger("环境 AI 每分钟最多请求", 0, 10, "0", Order = 8, RequireRestart = false,
		HintText = "0 表示禁止环境 AI 请求；建议低配 0～1，中配 1～2。超过上限时自动回退本地台词。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public int TownAmbientAiRequestsPerMinute { get; set; } = 2;

	[SettingPropertyInteger("环境 AI 本次启动预算 Tokens", 0, 100000, "0", Order = 9, RequireRestart = false,
		HintText = "本次启动期间环境 AI 的输入+输出估算上限。达到上限后自动回退本地台词。0 表示不允许调用。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
	public int TownAmbientAiSessionTokenBudget { get; set; } = 5000;

	[SettingPropertyBool("显示环境 AI 用量提示", Order = 10, RequireRestart = false,
		HintText = "每次环境 AI 回复完成后显示本次估算输入/输出 Token 和本次启动累计用量；建议保持开启。")]
	[SettingPropertyGroup("3. 场景喊话/定居点生活/环境 AI 接口", GroupOrder = -49)]
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
