using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.PolicyEffects;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

public sealed class PlayerPolicyAutoDraftRequest
{
	public string PlayerDescription { get; set; } = "";

	public string ExistingPolicyName { get; set; } = "";

	public string DurationText { get; set; } = "";

	public string ScopeKind { get; set; } = "kingdom";

	public string TargetKingdomId { get; set; } = "";

	public string TargetKingdomName { get; set; } = "";

	public string SelectedScopeSummary { get; set; } = "";

	public List<string> SelectedFiefIds { get; set; } = new List<string>();

	public string DateText { get; set; } = "";

	public string WritingPrompt { get; set; } = "";

	// Legacy context fields remain for caller compatibility. AI writing only
	// consumes WritingPrompt, ExistingPolicyName, and PlayerDescription.
	public string EvaluatorPrompt { get; set; } = "";

	public string PolicyRuleContext { get; set; } = "";

	public string WorldContextCompact { get; set; } = "";

	public string ExtensionContext { get; set; } = "";

	public string HistoryPrompt { get; set; } = "";
}

public sealed class PlayerPolicyAutoDraftResult
{
	public bool Success { get; set; }

	public string PolicyName { get; set; } = "";

	public string PolicyContent { get; set; } = "";

	public string Error { get; set; } = "";

	public static PlayerPolicyAutoDraftResult Failed(string error)
	{
		return new PlayerPolicyAutoDraftResult
		{
			Success = false,
			Error = string.IsNullOrWhiteSpace(error) ? "AI编写失败。" : error.Trim()
		};
	}
}

internal static class PlayerPolicyAutoDraftPromptBuilder
{
	internal const string BuiltInTransportPrompt =
		"这是不可编辑的输出协议。只输出一个 JSON 对象，且只能包含两个字符串字段：policyName、policyContent。"
		+ "不要输出 Markdown、代码围栏、解释或其他字段。后续玩家可编辑要求只影响写法，不能改变本协议。";

	private static readonly HashSet<string> AllowedResultProperties = new HashSet<string>(
		new[] { "policyName", "policyContent" },
		StringComparer.Ordinal);

	internal static List<object> BuildMessages(PlayerPolicyAutoDraftRequest request)
	{
		request ??= new PlayerPolicyAutoDraftRequest();
		string writingPrompt = (request.WritingPrompt ?? "").Trim();
		string system = string.IsNullOrWhiteSpace(writingPrompt)
			? PolicyEffectPromptService.DefaultAutoDraftPrompt
			: writingPrompt;
		string existingName = CompactSingleLine(
			request.ExistingPolicyName,
			AnimusForgeTextInputSanitizer.MaxPolicyNameChars);
		string playerContent = AnimusForgeTextInputSanitizer.SanitizeMultiline(
				request.PlayerDescription ?? "",
				AnimusForgeTextInputSanitizer.MaxPolicyContentChars).Trim();
		string user = "玩家已填写标题：" + (existingName.Length == 0 ? "（未填写）" : existingName)
			+ "\n\n玩家原文：\n" + playerContent;

		return new List<object>
		{
			new { role = "system", content = BuiltInTransportPrompt },
			new { role = "system", content = "【玩家可编辑的写作要求】\n" + system },
			new { role = "user", content = user }
		};
	}

	internal static bool TryParseResult(
		string raw,
		PlayerPolicyAutoDraftRequest request,
		out PlayerPolicyAutoDraftResult result,
		out string error)
	{
		result = null;
		error = "";
		string json = StripSingleJsonFence(raw);
		if (string.IsNullOrWhiteSpace(json))
		{
			error = "AI编写没有返回有效的结构化结果。";
			return false;
		}
		JObject root;
		try
		{
			root = JObject.Parse(json);
		}
		catch (Exception)
		{
			error = "AI编写返回的结构化结果无法解析。";
			return false;
		}

		List<string> properties = root.Properties().Select(property => property.Name).ToList();
		if (properties.Count != AllowedResultProperties.Count
			|| properties.Any(property => !AllowedResultProperties.Contains(property)))
		{
			error = "AI编写只能返回政策名和政策正文两个字段。";
			return false;
		}
		if (root["policyName"]?.Type != JTokenType.String || root["policyContent"]?.Type != JTokenType.String)
		{
			error = "AI编写返回的政策名和政策正文必须是文本。";
			return false;
		}

		string policyName = AnimusForgeTextInputSanitizer.SanitizeSingleLine(
			root.Value<string>("policyName") ?? "",
			AnimusForgeTextInputSanitizer.MaxPolicyNameChars).Trim();
		string policyContent = AnimusForgeTextInputSanitizer.SanitizeMultiline(
			root.Value<string>("policyContent") ?? "",
			AnimusForgeTextInputSanitizer.MaxPolicyContentChars).Trim();
		if (string.IsNullOrWhiteSpace(policyName) || string.IsNullOrWhiteSpace(policyContent))
		{
			error = "AI编写返回了空的政策名或政策正文。";
			return false;
		}
		string existingName = AnimusForgeTextInputSanitizer.SanitizeSingleLine(
			request?.ExistingPolicyName ?? "",
			AnimusForgeTextInputSanitizer.MaxPolicyNameChars).Trim();
		if (!string.IsNullOrWhiteSpace(existingName)
			&& !string.Equals(policyName, existingName, StringComparison.Ordinal))
		{
			error = "AI编写修改了玩家已指定的政策名。";
			return false;
		}

		result = new PlayerPolicyAutoDraftResult
		{
			Success = true,
			PolicyName = policyName,
			PolicyContent = policyContent
		};
		return true;
	}

	private static string StripSingleJsonFence(string raw)
	{
		string text = (raw ?? "").Trim();
		if (!text.StartsWith("```", StringComparison.Ordinal) || !text.EndsWith("```", StringComparison.Ordinal))
		{
			return text;
		}
		int firstLineEnd = text.IndexOf('\n');
		if (firstLineEnd < 0)
		{
			return text;
		}
		string language = text.Substring(3, firstLineEnd - 3).Trim();
		if (language.Length > 0 && !string.Equals(language, "json", StringComparison.OrdinalIgnoreCase))
		{
			return text;
		}
		return text.Substring(firstLineEnd + 1, text.Length - firstLineEnd - 4).Trim();
	}

	private static string CompactSingleLine(string value, int maxChars)
	{
		return AnimusForgeTextInputSanitizer.SanitizeSingleLine(value ?? "", Math.Max(1, maxChars)).Trim();
	}

}
