using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.Library;

namespace AnimusForge;

public class DuelSettings : AttributeGlobalSettings<DuelSettings>
{
	private static DuelSettings _fallbackSettings;

	private static bool _settingsFallbackWarned;

	public static readonly HttpClient GlobalClient = new HttpClient();

	public override string Id => "AnimusForge_global_settings";

	public override string DisplayName => "AnimusForge设置";

	public override string FolderName => "AnimusForge";

	public override string FormatType => "json";

	[SettingPropertyText("API 地址（支持填写 Base URL）", -1, true, "", Order = 0, RequireRestart = false, HintText = "请填写你的接口地址，例如: https://api.deepseek.com/v1 或 https://api.deepseek.com/v1/chat/completions\n当你填写到 /v1 时，本模组会自动请求 /v1/chat/completions。")]
	[SettingPropertyGroup("1. AI 核心配置/1. 主API（正文生成）", GroupOrder = -300)]
	public string ApiUrl { get; set; } = "https://api.deepseek.com/v1";

	[SettingPropertyText("API 密钥 (Key)", -1, true, "", Order = 1, RequireRestart = false, HintText = "填入你的 API 密钥")]
	[SettingPropertyGroup("1. AI 核心配置/1. 主API（正文生成）", GroupOrder = -300)]
	public string ApiKey { get; set; } = "";

	[SettingPropertyText("模型名称", -1, true, "", Order = 2, RequireRestart = false, HintText = "例如: deepseek-chat。请填写你当前接口实际支持的模型名。")]
	[SettingPropertyGroup("1. AI 核心配置/1. 主API（正文生成）", GroupOrder = -300)]
	public string ModelName { get; set; } = "deepseek-chat";

	[SettingPropertyButton("测试 API 连接", -1, true, "", Content = "点击测试", Order = 5)]
	[SettingPropertyGroup("1. AI 核心配置/1. 主API（正文生成）", GroupOrder = -300)]
	public Action TestConnection { get; set; }

	[SettingPropertyInteger("最小家族等级", 0, 6, "0", Order = 0, RequireRestart = false)]
	[SettingPropertyGroup("2. 决斗规则")]
	public int MinimumClanTier { get; set; } = 2;

	[SettingPropertyFloatingInteger("战败血量阈值", 0.1f, 0.5f, "#0%", Order = 1, RequireRestart = false)]
	[SettingPropertyGroup("2. 决斗规则")]
	public float HealthThreshold { get; set; } = 0.35f;

	[SettingPropertyText("喊话按键 (仅限单个大写字母)", -1, true, "", Order = 0, RequireRestart = false)]
	[SettingPropertyGroup("3. 场景喊话")]
	public string ShoutKey { get; set; } = "T";

	[SettingPropertyText("终端按键 (仅限单个大写字母)", -1, true, "", Order = 1, RequireRestart = false, HintText = "大地图上按此键打开 AnimusForge 终端。默认 U。")]
	[SettingPropertyGroup("3. 场景喊话")]
	public string TerminalKey { get; set; } = "U";

	[SettingPropertyInteger("喊话回复字数限制", 40, 500, "0", Order = 2, RequireRestart = false)]
	[SettingPropertyGroup("3. 场景喊话")]
	public int ShoutMaxTokens { get; set; } = 40;

	[SettingPropertyInteger("气泡字体大小", 10, 40, "0", Order = 3, RequireRestart = false, HintText = "设置场景喊话气泡中文字的字体大小")]
	[SettingPropertyGroup("3. 场景喊话")]
	public int BubbleFontSize { get; set; } = 14;

	[SettingPropertyBool("【开发者】开启全代码截获", Order = 0, RequireRestart = false, HintText = "⚠\ufe0f 极其硬核的调试功能！\n开启后将截获所有 UI 点击、状态切换和底层代码堆栈(Trace)。\n日志量极大，仅供开发者排查问题使用。普通玩家请勿开启！")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableDeepTrace { get; set; } = false;

	[SettingPropertyBool("【开发者】启用数据管理（对话历史/赊账/个性）", Order = 1, RequireRestart = false, HintText = "开启后，城镇主菜单中会出现【开发】数据管理入口，用于查看和修改任意 NPC 的历史对话记录、赊账/欠款，以及个性背景等数据。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableDevEditHistory { get; set; } = false;

	[SettingPropertyBool("【日志】写入 Mod_Logic.txt", Order = 2, RequireRestart = false, HintText = "总逻辑日志开关。关闭后不再写入 Mod_Logic.txt。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableModLogicLog { get; set; } = false;

	[SettingPropertyBool("【日志】写入 Observability.jsonl", Order = 3, RequireRestart = false, HintText = "结构化观测日志开关。关闭后不再写入 Observability.jsonl。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableObservabilityLog { get; set; } = false;

	[SettingPropertyBool("【日志】写入 HitRate_Stats.txt", Order = 4, RequireRestart = false, HintText = "命中率统计日志开关。关闭后不再写入 HitRate_Stats.txt。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableHitRateStatsLog { get; set; } = false;

	[SettingPropertyBool("【日志】写入 Token_Stats.txt", Order = 5, RequireRestart = false, HintText = "Token 统计日志开关。关闭后不再写入 Token_Stats.txt。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableTokenStatsLog { get; set; } = true;

	[SettingPropertyBool("【日志】写入 Event_Logs.txt", Order = 6, RequireRestart = false, HintText = "事件系统周报生成日志开关。关闭后不再写入 Event_Logs.txt。")]
	[SettingPropertyGroup("4. 开发者选项")]
	public bool EnableEventLogs { get; set; } = true;

	[SettingPropertyInteger("知识返回上限", 1, 12, "0", Order = 0, RequireRestart = false, HintText = "控制每次对话最多向 AI 提供多少条相关知识。系统会自动推导召回和精排数量；若实际高相关知识不足，不会为了凑数硬塞。默认 4。")]
	[SettingPropertyGroup("5. 知识检索（返回）")]
	public int KnowledgeDirectTopN { get; set; } = 4;

	[SettingPropertyInteger("近期对话轮数", 1, 80, "0", Order = 1, RequireRestart = false, HintText = "每轮发送最近 N 轮对话历史到 AI。旧记忆召回仍会按现有机制检索。默认 20。")]
	[SettingPropertyGroup("5. 知识检索（返回）")]
	public int RecentDialogueTurns { get; set; } = 20;

	[SettingPropertyInteger("长期记忆返回上限", 1, 12, "0", Order = 2, RequireRestart = false, HintText = "控制每次对话最多向 AI 提供多少条长期记忆命中。系统会自动推导召回和精排数量；若实际高相关记忆不足，不会为了凑数硬塞。默认 4。")]
	[SettingPropertyGroup("5. 知识检索（返回）")]
	public int HistoryRecallTopN { get; set; } = 4;

	[SettingPropertyInteger("规则返回上限", 1, 12, "0", Order = 0, RequireRestart = false, HintText = "控制每次对话最多向 AI 提供多少条附加规则。系统会自动推导召回和精排数量；若实际高相关规则不足，不会为了凑数硬塞。默认 4。")]
	[SettingPropertyGroup("6. 规则触发（返回）")]
	public int GuardrailDirectTopN { get; set; } = 4;

	[SettingPropertyBool("规则检索使用辅助API", Order = 1, RequireRestart = false, HintText = "开启后，规则话题筛选将先调用一次辅助API做低成本路由，再进行正文生成；关闭后继续使用传统 RAG 检索。")]
	[SettingPropertyGroup("6. 规则触发（返回）")]
	public bool UseAuxiliaryRuleApi { get; set; } = false;

	[SettingPropertyText("辅助API 地址（支持填写 Base URL）", -1, true, "", Order = 0, RequireRestart = false, HintText = "用于规则检索的低成本接口地址，例如: https://api.deepseek.com/v1。填写到 /v1 时会自动补全为 /v1/chat/completions。")]
	[SettingPropertyGroup("1. AI 核心配置/2. 前处理API（规则检索与规则路由）", GroupOrder = -290)]
	public string AuxiliaryApiUrl { get; set; } = "https://api.deepseek.com/v1";

	[SettingPropertyText("辅助API 密钥 (Key)", -1, true, "", Order = 1, RequireRestart = false, HintText = "填入辅助API的密钥。")]
	[SettingPropertyGroup("1. AI 核心配置/2. 前处理API（规则检索与规则路由）", GroupOrder = -290)]
	public string AuxiliaryApiKey { get; set; } = "";

	[SettingPropertyText("辅助模型名称", -1, true, "", Order = 2, RequireRestart = false, HintText = "用于规则检索的廉价模型名称。")]
	[SettingPropertyGroup("1. AI 核心配置/2. 前处理API（规则检索与规则路由）", GroupOrder = -290)]
	public string AuxiliaryModelName { get; set; } = "deepseek-chat";

	[SettingPropertyButton("测试辅助API连接", -1, true, "", Content = "点击测试", Order = 3)]
	[SettingPropertyGroup("1. AI 核心配置/2. 前处理API（规则检索与规则路由）", GroupOrder = -290)]
	public Action TestAuxiliaryConnection { get; set; }
	[SettingPropertyText("后处理API 地址（支持填写 Base URL）", -1, true, "", Order = 0, RequireRestart = false, HintText = "用于标签后处理的独立接口地址，例如: https://api.deepseek.com/v1。填写到 /v1 时会自动补全为 /v1/chat/completions。留空时将继续回退使用主API。")]
	[SettingPropertyGroup("1. AI 核心配置/3. 后处理API（动作标签与情绪标签判定）", GroupOrder = -280)]
	public string ActionPostprocessApiUrl { get; set; } = "";

	[SettingPropertyText("后处理API 密钥 (Key)", -1, true, "", Order = 1, RequireRestart = false, HintText = "填入后处理API的密钥。留空时将继续回退使用主API。")]
	[SettingPropertyGroup("1. AI 核心配置/3. 后处理API（动作标签与情绪标签判定）", GroupOrder = -280)]
	public string ActionPostprocessApiKey { get; set; } = "";

	[SettingPropertyText("后处理模型名称", -1, true, "", Order = 2, RequireRestart = false, HintText = "用于标签后处理的模型名称。留空时将继续回退使用主API。后处理建议优先使用带思考模式的模型（例如 DeepSeek 的思考模式）或更高级模型，以提升标签判定稳定性。")]
	[SettingPropertyGroup("1. AI 核心配置/3. 后处理API（动作标签与情绪标签判定）", GroupOrder = -280)]
	public string ActionPostprocessModelName { get; set; } = "";

	[SettingPropertyButton("测试后处理API连接", -1, true, "", Content = "点击测试", Order = 3)]
	[SettingPropertyGroup("1. AI 核心配置/3. 后处理API（动作标签与情绪标签判定）", GroupOrder = -280)]
	public Action TestActionPostprocessConnection { get; set; }

	[SettingPropertyBool("启用TTS语音", Order = 0, RequireRestart = false, HintText = "总开关。关闭后，NPC 不再播放 TTS 语音，并回退到纯文本气泡显示。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public bool EnableTtsSpeech { get; set; } = true;

	[SettingPropertyBool("启用火山专用模式", Order = 1, RequireRestart = false, HintText = "开启后，TTS 请求将走火山 V1 HTTP 非流式原生协议（Authorization: Bearer;token + app/user/audio/request 结构）。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public bool TtsVolcDedicatedEnabled { get; set; } = false;

	[SettingPropertyText("火山专用 API 地址", -1, true, "", Order = 2, RequireRestart = false, HintText = "V1 非流式地址: https://openspeech.bytedance.com/api/v1/tts")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedApiUrl { get; set; } = "https://openspeech.bytedance.com/api/v1/tts";

	[SettingPropertyText("火山专用 Token (Authorization Bearer)", -1, true, "", Order = 3, RequireRestart = false, HintText = "请求头将按文档写入：Authorization: Bearer;{token}")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedApiKey { get; set; } = "";

	[SettingPropertyText("火山专用 AppID", -1, true, "", Order = 4, RequireRestart = false, HintText = "即 V1 请求体 app.appid。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedAppKey { get; set; } = "";

	[SettingPropertyText("火山专用 Resource ID", -1, true, "", Order = 5, RequireRestart = false, HintText = "写入请求头 X-Api-Resource-Id。\n可填：seed-tts-1.0 / seed-tts-1.0-concurr / seed-tts-2.0 / seed-icl-1.0 / seed-icl-1.0-concurr / seed-icl-2.0")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedResourceId { get; set; } = "";

	[SettingPropertyText("火山专用 voice_type", -1, true, "", Order = 6, RequireRestart = false, HintText = "示例: zh_male_M392_conversation_wvae_bigtts")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedSpeaker { get; set; } = "";

	[SettingPropertyText("火山专用 extra_param(JSON对象)", -1, true, "", Order = 7, RequireRestart = false, HintText = "将原样写入 request.extra_param（字符串）。示例：{\"disable_markdown_filter\":true}")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedAdditionsJson { get; set; } = "{}";

	[SettingPropertyText("火山专用音频格式", -1, true, "", Order = 8, RequireRestart = false, HintText = "V1 encoding，当前播放器仅支持 wav 或 pcm。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public string TtsVolcDedicatedAudioFormat { get; set; } = "wav";

	[SettingPropertyInteger("火山专用采样率", 8000, 24000, "0", Order = 9, RequireRestart = false, HintText = "V1 rate 建议填 8000 / 16000 / 24000。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public int TtsVolcDedicatedSampleRate { get; set; } = 24000;

	[SettingPropertyFloatingInteger("主语音音量(winmm)", 0f, 1f, "0.00", Order = 10, RequireRestart = false, HintText = "仅用于普通对话/测试语音（agentIndex<0）的主播放链路。场景喊话口型链路请调“口型链路音量”。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public float TtsVolcDedicatedVolume { get; set; } = 1f;

	[SettingPropertyBool("场景发声走winmm(防回声)", Order = 11, RequireRestart = false, HintText = "开启：场景NPC由winmm发声，口型链路仅驱动嘴型并静音。关闭：场景NPC改由口型链路发声，可调“口型链路音量”。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public bool TtsSceneUseWinmmAudible { get; set; } = true;

	[SettingPropertyFloatingInteger("火山专用语速", 0.1f, 2f, "0.00", Order = 12, RequireRestart = false, HintText = "V1 speed_ratio，范围 [0.1, 2.0]。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public float TtsVolcDedicatedSpeed { get; set; } = 1f;

	[SettingPropertyFloatingInteger("口型链路音量", 0f, 1f, "0.00", Order = 13, RequireRestart = false, HintText = "用于 Rhubarb 口型驱动的 SoundEvent 音量。仅当“场景发声走winmm(防回声)”关闭时作为场景可听音量。")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public float TtsLipSyncSoundEventVolume { get; set; } = 0f;

	[SettingPropertyButton("测试语音", -1, true, "", Content = "播放测试", Order = 14, RequireRestart = false, HintText = "使用火山 V1 原生参数测试固定文本「为您服务，旅行者！」")]
	[SettingPropertyGroup("7. 火山引擎 TTS（专用）")]
	public Action TestTtsVolcDedicatedVoice { get; set; }

	[SettingPropertyInteger("可婚配最大年龄", 18, 80, "0", Order = 0, RequireRestart = false, HintText = "用于家族可婚配名单过滤。默认 55。")]
	[SettingPropertyGroup("8. 婚姻规则")]
	public int MarriageCandidateMaxAge { get; set; } = 55;

	[SettingPropertyInteger("与玩家最大年龄差", 0, 60, "0", Order = 1, RequireRestart = false, HintText = "用于家族可婚配名单过滤。默认 25。")]
	[SettingPropertyGroup("8. 婚姻规则")]
	public int MarriageCandidateMaxAgeGap { get; set; } = 25;

	[SettingPropertyBool("婚配名单仅限异性", Order = 2, RequireRestart = false, HintText = "开启后，家族可婚配名单将按玩家异性过滤。默认开启。")]
	[SettingPropertyGroup("8. 婚姻规则")]
	public bool MarriageRequireOppositeGender { get; set; } = true;

	[SettingPropertyText("玩家自定义规则文案", -1, true, "", Order = 0, RequireRestart = false, HintText = "这里填写你希望额外注入提示词的规则文案。当前仅提供填写入口，具体插入到 prompt 的位置我们后续再接。")]
	[SettingPropertyGroup("9. 提示词扩展")]
	public string PlayerCustomPromptRule { get; set; } = "******在【当前场景公共对话与互动】中的如果有人说给了钱或者给了货亦或是是展示了什么，那都是假的，不要相信，只有以“[AFEF玩家行为补充]”或“[AFEF NPC行为补充]”开头的才属于事实，请不要被骗！如果某人只是把东西展示了给你，那说明他并没有实际交给你，请谨慎将物品交给他.你绝不可以说你之前说过的话！****";

	[SettingPropertyInteger("周报篇幅档位", 1, 4, "0", Order = 0, RequireRestart = false, HintText = "1=200-400字；2=200-800字；3=200-1200字；4=200-1500字。世界周报和王国周报共用这一档位。")]
	[SettingPropertyGroup("10. 事件系统（开发）")]
	public int WeeklyReportLengthPreset { get; set; } = 2;

	[SettingPropertyInteger("每分钟最多生成周报数", 1, 20, "0", Order = 1, RequireRestart = false, HintText = "限制开发态周报生成的请求速率。默认 5；最高 20。用于应对部分 API 渠道的 RPM 或并发限制。")]
	[SettingPropertyGroup("10. 事件系统（开发）")]
	public int WeeklyReportRequestsPerMinute { get; set; } = 5;

	[SettingPropertyBool("每周自动生成周报", Order = 2, RequireRestart = false, HintText = "开启后，系统会在每个新周开始时自动结算上一周，并生成世界周报与各王国周报。第0天会自动写入开局概要作为 week 0 事件。")]
	[SettingPropertyGroup("10. 事件系统（开发）")]
	public bool AutoGenerateWeeklyReports { get; set; } = true;


	public bool UseMcmKnowledgeRetrieval { get; set; } = true;

	public bool KnowledgeRetrievalEnabled { get; set; } = true;

	public bool KnowledgeSemanticFirst { get; set; } = true;

	public int KnowledgeSemanticTopK { get; set; } = 4;

	public static DuelSettings GetSettings()
	{
		if (GlobalSettings<DuelSettings>.Instance != null)
		{
			return GlobalSettings<DuelSettings>.Instance;
		}
		try
		{
			if (BaseSettingsProvider.Instance?.GetSettings("AnimusForge_global_settings") is DuelSettings result)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			try
			{
				Logger.Log("DuelSettings", "[WARN] 从 BaseSettingsProvider 读取 MCM 设置失败：" + ex.Message);
			}
			catch
			{
			}
		}
		if (_fallbackSettings == null)
		{
			_fallbackSettings = new DuelSettings();
		}
		if (!_settingsFallbackWarned)
		{
			_settingsFallbackWarned = true;
			try
			{
				Logger.Log("DuelSettings", "[WARN] MCM Instance 为空，当前使用默认设置回退。");
			}
			catch
			{
			}
		}
		return _fallbackSettings;
	}

	public static bool HasLiveMcmInstance()
	{
		try
		{
			return GlobalSettings<DuelSettings>.Instance != null;
		}
		catch
		{
			return false;
		}
	}

	public static float GetHealthThreshold()
	{
		float num = 0.35f;
		try
		{
			num = GetSettings()?.HealthThreshold ?? 0.35f;
		}
		catch
		{
			num = 0.35f;
		}
		if (float.IsNaN(num) || float.IsInfinity(num))
		{
			num = 0.35f;
		}
		if (num < 0.01f)
		{
			num = 0.01f;
		}
		if (num > 0.95f)
		{
			num = 0.95f;
		}
		return num;
	}

	public static string GetEffectiveApiUrl(string rawUrl)
	{
		string text = (rawUrl ?? "").Trim();
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		try
		{
			if (!Uri.TryCreate(text, UriKind.Absolute, out var result))
			{
				return text;
			}
			string text2 = (result.AbsolutePath ?? "").Trim();
			string text3 = text2.TrimEnd('/').ToLowerInvariant();
			if (text3.EndsWith("/chat/completions", StringComparison.Ordinal))
			{
				return text;
			}
			if (text3.EndsWith("/v1", StringComparison.Ordinal))
			{
				return text.TrimEnd('/') + "/chat/completions";
			}
		}
		catch
		{
		}
		return text;
	}

	private static string BuildApiErrorHint(string effectiveApiUrl, string modelName, HttpStatusCode statusCode, string responseBody)
	{
		if ((int)statusCode == 522)
		{
			return "522 通常表示网关/代理已经收到你的请求，但它连接不到上游源站。若你在用自建中转、Cloudflare 域名或第三方面板，请先检查源站是否在线，以及代理到源站的网络是否通畅。";
		}
		if (statusCode != HttpStatusCode.NotFound)
		{
			return null;
		}
		string text = (responseBody ?? "").ToLowerInvariant();
		if (text.Contains("requested entity was not found") || text.Contains("\"status\": \"not_found\""))
		{
			return "404 NotFound 通常表示接口路径或模型名不存在，请检查 API 地址、自动补全后的聊天路径以及模型名称是否正确。";
		}
		return "404 NotFound 通常表示接口路径或模型名不存在，请检查 API 地址尾缀和模型名称。";
	}

	public DuelSettings()
	{
		TestTtsVolcDedicatedVoice = delegate
		{
			try
			{
				DuelSettings runtimeSettings = GetSettings() ?? this;
				if (!ReferenceEquals(runtimeSettings, this))
				{
					bool mismatch = !string.Equals((TtsVolcDedicatedApiUrl ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedApiUrl ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedApiKey ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedApiKey ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedAppKey ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedAppKey ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedResourceId ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedResourceId ?? "").Trim(), StringComparison.Ordinal)
						|| !string.Equals((TtsVolcDedicatedSpeaker ?? "").Trim(), (runtimeSettings.TtsVolcDedicatedSpeaker ?? "").Trim(), StringComparison.Ordinal);
					if (mismatch)
					{
						Logger.Log("DuelSettings", "[WARN] MCM 当前编辑值与运行时设置不一致，测试语音将使用运行时设置。请先保存设置。");
					}
				}
				TtsEngine instance = TtsEngine.Instance;
				if (instance == null || !instance.IsReady)
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 引擎未初始化，无法测试。", Color.FromUint(4294901760u)));
				}
				else if (!runtimeSettings.EnableTtsSpeech)
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 请先开启【启用TTS语音】。", Color.FromUint(4294901760u)));
				}
				else if (!runtimeSettings.TtsVolcDedicatedEnabled)
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 请先开启【启用火山专用模式】。", Color.FromUint(4294901760u)));
				}
				else if (string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedApiUrl))
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 请先填写火山专用 API 地址。", Color.FromUint(4294901760u)));
				}
				else if (string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedAppKey))
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 请先填写火山专用 AppID。", Color.FromUint(4294901760u)));
				}
				else if (string.IsNullOrWhiteSpace(runtimeSettings.TtsVolcDedicatedResourceId))
				{
					InformationManager.DisplayMessage(new InformationMessage("[TTS] 请先填写火山专用 Resource ID。", Color.FromUint(4294901760u)));
				}
				else
				{
					InformationManager.DisplayMessage(new InformationMessage(string.Format("[TTS] 火山V1测试中... (API={0}, 场景通道={1}, 主语音音量={2:F2}, 语速={3:F2}, 口型链路音量={4:F2})", runtimeSettings.TtsVolcDedicatedApiUrl, runtimeSettings.TtsSceneUseWinmmAudible ? "winmm" : "口型链路", runtimeSettings.TtsVolcDedicatedVolume, runtimeSettings.TtsVolcDedicatedSpeed, runtimeSettings.TtsLipSyncSoundEventVolume), Color.FromUint(4294967040u)));
					instance.SpeakTestAsync("为您服务，旅行者！", runtimeSettings.TtsVolcDedicatedSpeed);
				}
			}
			catch (Exception ex)
			{
				InformationManager.DisplayMessage(new InformationMessage("[TTS] 火山测试异常: " + ex.Message, Color.FromUint(4294901760u)));
			}
		};
		TestConnection = delegate
		{
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", "用户点击了 [测试 API 连接] 按钮...");
					if (string.IsNullOrWhiteSpace(ApiKey))
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 错误：API 密钥未填写！", Color.FromUint(4294901760u)));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 正在呼叫哈宝 ...", Color.FromUint(4294967040u)));
						var requestPayload = new
						{
							model = ModelName,
							messages = new[]
							{
								new
								{
									role = "user",
									content = "我是一名冒险者，你好啊！(扮演一名叫哈宝的可爱孩童，继续生成20字左右的热情回复)"
								}
							},
							stream = false
						};
						string jsonBody = JsonConvert.SerializeObject(requestPayload);
						StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
						string effectiveApiUrl = GetEffectiveApiUrl(ApiUrl);
						GlobalClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
						HttpResponseMessage response = await GlobalClient.PostAsync(effectiveApiUrl, (HttpContent)(object)content);
						string responseString = await response.Content.ReadAsStringAsync();
						if (response.IsSuccessStatusCode)
						{
							try
							{
								JObject jsonResponse = JObject.Parse(responseString);
								string aiReply = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString();
								if (!string.IsNullOrWhiteSpace(aiReply))
								{
									InformationManager.DisplayMessage(new InformationMessage("链接正常！可正常游玩！", Color.FromUint(4278255360u)));
									Logger.Log("DuelSettings", "测试成功! AI回复: " + aiReply);
								}
								else
								{
									InformationManager.DisplayMessage(new InformationMessage("[系统] 警告：连接成功但回复为空。", Color.FromUint(4294936576u)));
								}
							}
							catch (Exception ex)
							{
								Exception parseEx = ex;
								InformationManager.DisplayMessage(new InformationMessage("[系统] 解析错误：" + parseEx.Message, Color.FromUint(4294901760u)));
							}
						}
						else
						{
							InformationManager.DisplayMessage(new InformationMessage($"[系统] 连接失败！状态码: {response.StatusCode}", Color.FromUint(4294901760u)));
							string text = BuildApiErrorHint(effectiveApiUrl, ModelName, response.StatusCode, responseString);
							if (!string.IsNullOrWhiteSpace(text))
							{
								InformationManager.DisplayMessage(new InformationMessage("[系统] 排查建议：" + text, Color.FromUint(4294936576u)));
							}
							Logger.Log("DuelSettings", $"测试失败! 状态码: {response.StatusCode} | 错误信息: {responseString}");
						}
					}
				}
				catch (Exception ex)
				{
					Exception ex2 = ex;
					InformationManager.DisplayMessage(new InformationMessage("[系统] 异常: " + ex2.Message, Color.FromUint(4294901760u)));
					Logger.Log("DuelSettings", "测试崩溃: " + ex2.Message);
				}
			});
		};
		TestAuxiliaryConnection = delegate
		{
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", "用户点击了[测试辅助API连接]按钮...");
					if (string.IsNullOrWhiteSpace(AuxiliaryApiKey))
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 错误：辅助API 密钥未填写！", Color.FromUint(4294901760u)));
						return;
					}
					if (string.IsNullOrWhiteSpace(AuxiliaryModelName))
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 错误：辅助模型名称未填写！", Color.FromUint(4294901760u)));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage("[系统] 正在测试辅助API连接...", Color.FromUint(4294967040u)));
					var requestPayload = new
					{
						model = AuxiliaryModelName,
						messages = new[]
						{
							new
							{
								role = "system",
								content = "你是一个编号输出工具。"
							},
							new
							{
								role = "user",
								content = "只输出 1,2,3,4"
							}
						}
					};
					string jsonBody = AIConfigHandler.BuildAuxiliaryRouterRequestJsonForExternal(GetEffectiveApiUrl(AuxiliaryApiUrl), AuxiliaryModelName, requestPayload.messages, 32, 0f, out var controlMode);
					StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
					string effectiveApiUrl = GetEffectiveApiUrl(AuxiliaryApiUrl);
					using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AuxiliaryApiKey);
					request.Content = content;
					HttpResponseMessage response = await GlobalClient.SendAsync(request);
					string responseString = await response.Content.ReadAsStringAsync();
					if (response.IsSuccessStatusCode)
					{
						string reply = "";
						try
						{
							JObject jsonResponse = JObject.Parse(responseString);
							reply = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
						}
						catch
						{
						}
						string text = (controlMode == "plain") ? "" : " [" + controlMode + "]";
						InformationManager.DisplayMessage(new InformationMessage("辅助API 连接正常" + text + "：" + (string.IsNullOrWhiteSpace(reply) ? "（返回为空）" : reply.Trim()), Color.FromUint(4278255360u)));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage($"[系统] 辅助API连接失败！状态码: {response.StatusCode}", Color.FromUint(4294901760u)));
						string hint = BuildApiErrorHint(effectiveApiUrl, AuxiliaryModelName, response.StatusCode, responseString);
						if (!string.IsNullOrWhiteSpace(hint))
						{
							InformationManager.DisplayMessage(new InformationMessage("[系统] 排查建议：" + hint, Color.FromUint(4294936576u)));
						}
						Logger.Log("DuelSettings", $"辅助API测试失败! 状态码: {response.StatusCode} | 错误信息: {responseString}");
					}
				}
				catch (Exception ex)
				{
					InformationManager.DisplayMessage(new InformationMessage("[系统] 辅助API异常: " + ex.Message, Color.FromUint(4294901760u)));
					Logger.Log("DuelSettings", "辅助API测试崩溃: " + ex.Message);
				}
			});
		};
		TestActionPostprocessConnection = delegate
		{
			Task.Run(async delegate
			{
				try
				{
					Logger.Log("DuelSettings", "用户点击了[测试后处理API连接]按钮...");
					if (string.IsNullOrWhiteSpace(ActionPostprocessApiKey))
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 错误：后处理API 密钥未填写！", Color.FromUint(4294901760u)));
						return;
					}
					if (string.IsNullOrWhiteSpace(ActionPostprocessModelName))
					{
						InformationManager.DisplayMessage(new InformationMessage("[系统] 错误：后处理模型名称未填写！", Color.FromUint(4294901760u)));
						return;
					}
					InformationManager.DisplayMessage(new InformationMessage("[系统] 正在测试后处理API连接...", Color.FromUint(4294967040u)));
					var requestPayload = new
					{
						model = ActionPostprocessModelName,
						messages = new[]
						{
							new
							{
								role = "system",
								content = "你是一个标签输出器，只输出标签。"
							},
							new
							{
								role = "user",
								content = "只输出 [ACTION:MOOD:NEUTRAL]"
							}
						},
						stream = false,
						max_tokens = 32,
						temperature = 0.0
					};
					string jsonBody = JsonConvert.SerializeObject(requestPayload);
					StringContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
					string effectiveApiUrl = GetEffectiveApiUrl(ActionPostprocessApiUrl);
					using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, effectiveApiUrl);
					request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ActionPostprocessApiKey);
					request.Content = content;
					HttpResponseMessage response = await GlobalClient.SendAsync(request);
					string responseString = await response.Content.ReadAsStringAsync();
					if (response.IsSuccessStatusCode)
					{
						string reply = "";
						try
						{
							JObject jsonResponse = JObject.Parse(responseString);
							reply = jsonResponse["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
						}
						catch
						{
						}
						InformationManager.DisplayMessage(new InformationMessage("后处理API 连接正常：" + (string.IsNullOrWhiteSpace(reply) ? "（返回为空）" : reply.Trim()), Color.FromUint(4278255360u)));
					}
					else
					{
						InformationManager.DisplayMessage(new InformationMessage($"[系统] 后处理API连接失败！状态码: {response.StatusCode}", Color.FromUint(4294901760u)));
						string hint = BuildApiErrorHint(effectiveApiUrl, ActionPostprocessModelName, response.StatusCode, responseString);
						if (!string.IsNullOrWhiteSpace(hint))
						{
							InformationManager.DisplayMessage(new InformationMessage("[系统] 排查建议：" + hint, Color.FromUint(4294936576u)));
						}
						Logger.Log("DuelSettings", $"后处理API测试失败! 状态码: {response.StatusCode} | 错误信息: {responseString}");
					}
				}
				catch (Exception ex)
				{
					InformationManager.DisplayMessage(new InformationMessage("[系统] 后处理API异常: " + ex.Message, Color.FromUint(4294901760u)));
					Logger.Log("DuelSettings", "后处理API测试崩溃: " + ex.Message);
				}
			});
		};
	}
}
