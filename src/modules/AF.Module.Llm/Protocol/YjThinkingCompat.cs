using System;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

/// <summary>
/// YJ 中转站的 Gemini 思考参数兼容层。
/// 只匹配 YJ 域名下的 Gemini 模型，不改变其他 OpenAI/Claude/GLM 站点的请求。
/// </summary>
internal static class YjThinkingCompat
{
	private const string YjHost = "yjapi.manqiaotechnology.com";
	private const string Gemini37FlashHighModelPrefix = "gemini-3.7-flash-high";

	public static bool IsYjGeminiEndpoint(string apiUrl, string modelName)
	{
		if (!IsGeminiModel(modelName))
		{
			return false;
		}
		try
		{
			if (Uri.TryCreate((apiUrl ?? string.Empty).Trim(), UriKind.Absolute, out Uri uri))
			{
				return string.Equals(uri.Host, YjHost, StringComparison.OrdinalIgnoreCase);
			}
		}
		catch
		{
		}
		return false;
	}

	public static bool IsGeminiModel(string modelName)
	{
		string text = (modelName ?? string.Empty).Trim();
		return text.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsGemini37FlashHigh(string modelName)
	{
		return (modelName ?? string.Empty).Trim().StartsWith(Gemini37FlashHighModelPrefix, StringComparison.OrdinalIgnoreCase);
	}

	public static bool TryApply(JObject payload, string apiUrl, string modelName, bool thinkingEnabled, string effort, out string thinkingMode)
	{
		thinkingMode = "plain";
		if (payload == null || !IsYjGeminiEndpoint(apiUrl, modelName))
		{
			return false;
		}

		string selectedEffort = NormalizeEffort(effort);
		if (IsGemini37FlashHigh(modelName)
			&& (!thinkingEnabled
				|| string.Equals(selectedEffort, DuelSettings.ReasoningEffortNone, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(selectedEffort, DuelSettings.ReasoningEffortMinimal, StringComparison.OrdinalIgnoreCase)))
		{
			// 3.7-flash-high 会拒绝 none/minimal；关闭思考时也不发送伪造的 disabled 字段，直接走纯请求。
			payload.Remove("thinking");
			payload.Remove("output_config");
			payload.Remove("reasoning_effort");
			thinkingMode = "yj_plain_unsupported_" + selectedEffort;
			return true;
		}
		if (!thinkingEnabled || string.Equals(selectedEffort, DuelSettings.ReasoningEffortNone, StringComparison.OrdinalIgnoreCase))
		{
			selectedEffort = DuelSettings.ReasoningEffortNone;
		}

		// YJ 对 thinking.type=disabled 兼容性不一致；只发送它实际支持的字段。
		payload.Remove("thinking");
		payload.Remove("output_config");
		payload["reasoning_effort"] = selectedEffort;
		thinkingMode = "yj_reasoning_effort_" + selectedEffort;
		return true;
	}

	private static string NormalizeEffort(string effort)
	{
		string text = (effort ?? string.Empty).Trim().ToLowerInvariant();
		switch (text)
		{
		case DuelSettings.ReasoningEffortNone:
		case DuelSettings.ReasoningEffortMinimal:
		case DuelSettings.ReasoningEffortLow:
		case DuelSettings.ReasoningEffortMedium:
		case DuelSettings.ReasoningEffortHigh:
		case DuelSettings.ReasoningEffortXHigh:
		case DuelSettings.ReasoningEffortMax:
			return text;
		default:
			return DuelSettings.ReasoningEffortLow;
		}
	}
}
