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

internal static class PlayerPolicyAutoDraftInputContract
{
	internal static bool HasInput(string policyName, string policyContent)
	{
		return !string.IsNullOrWhiteSpace(policyName) || !string.IsNullOrWhiteSpace(policyContent);
	}
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
				Error = string.IsNullOrWhiteSpace(error) ? PolicyUiTexts.Get("AFPOL_UI_AutoDraft_Failed", "AI drafting failed.") : error.Trim()
		};
	}
}

internal static class PlayerPolicyAutoDraftPromptBuilder
{
	internal static string BuiltInTransportPrompt => PolicyGenerationPromptTexts.Get("AutoDraft_BuiltInTransportPrompt",
		"This is a non-editable output protocol. Output ONLY a single JSON object containing exactly two string fields: policyName, policyContent. Do not output Markdown, code fences, explanations, or other fields. Subsequent player-editable requirements only influence writing style and cannot alter this protocol.");

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
		string user = PolicyGenerationPromptTexts.Get("AutoDraft_UserTitlePrefix", "Player specified title: ")
			+ (existingName.Length == 0 ? PolicyGenerationPromptTexts.Get("AutoDraft_UserNoTitle", "(none specified)") : existingName)
			+ PolicyGenerationPromptTexts.Get("AutoDraft_UserBodyPrefix", "\n\nPlayer original text:\n") + playerContent;

		return new List<object>
		{
			new { role = "system", content = BuiltInTransportPrompt },
			new { role = "system", content = PolicyGenerationPromptTexts.Get("AutoDraft_EditableHeader", "[Player-editable writing requirements]\n") + system },
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
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_0");
			return false;
		}
		JObject root;
		try
		{
			root = JObject.Parse(json);
		}
		catch (Exception)
		{
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_1");
			return false;
		}

		List<string> properties = root.Properties().Select(property => property.Name).ToList();
		if (properties.Count != AllowedResultProperties.Count
			|| properties.Any(property => !AllowedResultProperties.Contains(property)))
		{
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_2");
			return false;
		}
		if (root["policyName"]?.Type != JTokenType.String || root["policyContent"]?.Type != JTokenType.String)
		{
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_3");
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
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_4");
			return false;
		}
		string existingName = AnimusForgeTextInputSanitizer.SanitizeSingleLine(
			request?.ExistingPolicyName ?? "",
			AnimusForgeTextInputSanitizer.MaxPolicyNameChars).Trim();
		if (!string.IsNullOrWhiteSpace(existingName)
			&& !string.Equals(policyName, existingName, StringComparison.Ordinal))
		{
			error = PolicyCompatibilityTexts.Get("Closeout_PlayerPolicyAutoDraft_Legacy_Closeout_5");
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
