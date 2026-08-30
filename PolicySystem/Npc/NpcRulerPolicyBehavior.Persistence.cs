using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

public sealed partial class NpcRulerPolicyBehavior
{
	private List<NpcRulerPolicyRecord> GetRecentPolicyRecordsInternal(string kingdomId, int maxCount)
	{
		string filter = (kingdomId ?? "").Trim();
		int limit = Math.Max(1, Math.Min(200, maxCount <= 0 ? 20 : maxCount));
		return _policyRecords.Values
			.Select(DeserializeRecord)
			.Where(x => x != null
				&& IsPublishedPolicyAgendaStatus(x.AgendaStatus)
				&& (string.IsNullOrWhiteSpace(filter) || string.Equals((x.KingdomId ?? "").Trim(), filter, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.Take(limit)
			.ToList();
	}

	private static bool IsPublishedPolicyAgendaStatus(string status)
	{
		return string.IsNullOrWhiteSpace(status)
			|| string.Equals(status, AgendaStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusAbolished, StringComparison.OrdinalIgnoreCase);
	}

	private static List<NpcRulerPolicyRecord> ParsePolicyRecords(string raw)
	{
		return ParsePolicyRecords(raw, "", "", 0, "policy");
	}

	private static List<NpcRulerPolicyRecord> ParsePolicyRecords(string raw, string batchId, string route, int attempts, string parseSource)
	{
		string json = "";
		try
		{
			json = ExtractJson(raw, out bool ignoredTrailingText);
			if (ignoredTrailingText)
			{
				string message = "policy-json-tail-ignored batchId=" + (batchId ?? "")
					+ " route=" + (route ?? "")
					+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
					+ " source=" + FirstNonEmpty(parseSource, "policy");
				Log(message);
				PolicyTraceLog("policy-json-tail-ignored", message, "extractedChars=" + (json?.Length ?? 0).ToString(CultureInfo.InvariantCulture)
					+ " rawChars=" + (raw?.Length ?? 0).ToString(CultureInfo.InvariantCulture));
			}
			if (string.IsNullOrWhiteSpace(json))
			{
				List<NpcRulerPolicyRecord> recoveredWithoutRoot = RecoverPolicyRecordsFromFragments(raw, out int rootlessCandidates, out int rootlessRepaired);
				if (recoveredWithoutRoot.Count > 0)
				{
					LogPolicyFragmentRecovery(batchId, route, attempts, parseSource, recoveredWithoutRoot.Count, rootlessCandidates, rootlessRepaired, "no-complete-root");
					return recoveredWithoutRoot;
				}
				NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":no-json", raw, json);
				return new List<NpcRulerPolicyRecord>();
			}
			if (!TryDeserializePolicyRecords(json, out List<NpcRulerPolicyRecord> records, out Exception parseException))
			{
				string repaired = RepairNpcPolicyJson(json);
				Exception repairedException = null;
				if (!string.Equals(repaired, json, StringComparison.Ordinal) && TryDeserializePolicyRecords(repaired, out records, out repairedException))
				{
					LogPolicyJsonRepair(batchId, route, attempts, parseSource, parseException, json, repaired);
					json = repaired;
				}
				else
				{
					List<NpcRulerPolicyRecord> recovered = RecoverPolicyRecordsFromFragments(raw, out int fragmentCandidates, out int repairedFragments);
					if (recovered.Count > 0)
					{
						LogPolicyFragmentRecovery(batchId, route, attempts, parseSource, recovered.Count, fragmentCandidates, repairedFragments, "wrapper-parse-failed");
						return recovered;
					}
					throw repairedException ?? parseException ?? new JsonException("policy json parse failed");
				}
			}
			if (records.Count == 0)
			{
				NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":no-policy-records", raw, json);
			}
			return records;
		}
		catch (Exception ex)
		{
			NpcPolicyStructuredParseLogger.LogFailure("NpcRulerPolicy", "policy", batchId, route, attempts, FirstNonEmpty(parseSource, "policy") + ":" + ex.GetType().Name + ":" + ex.Message, raw, json);
			return new List<NpcRulerPolicyRecord>();
		}
	}

	private static void RecordUnifiedPolicyWeeklyMaterial(NpcRulerPolicyRecord policy)
	{
		if (policy == null || string.IsNullOrWhiteSpace(policy.PolicyId))
		{
			return;
		}
		UnifiedPolicyWeeklyAttribution attribution = ResolveUnifiedPolicyWeeklyAttribution(policy);
		List<NpcRulerPolicyEffectDto> effects = (policy.Effects ?? new List<NpcRulerPolicyEffectDto>()).Where(IsActivePolicyEffect).ToList();
		Dictionary<string, List<NpcRulerPolicyEffectDto>> recipients = new Dictionary<string, List<NpcRulerPolicyEffectDto>>(StringComparer.OrdinalIgnoreCase);
		string issuerId = (attribution.IssuerKingdomId ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(issuerId))
		{
			recipients[issuerId] = new List<NpcRulerPolicyEffectDto>();
		}
		string policyTargetId = (attribution.TargetKingdomId ?? string.Empty).Trim();
		if (!string.IsNullOrWhiteSpace(policyTargetId) && !recipients.ContainsKey(policyTargetId))
		{
			recipients[policyTargetId] = new List<NpcRulerPolicyEffectDto>();
		}
		foreach (NpcRulerPolicyEffectDto effect in effects)
		{
			string targetId = FirstNonEmpty(effect.TargetKingdomId, policyTargetId, issuerId).Trim();
			if (string.IsNullOrWhiteSpace(targetId))
			{
				continue;
			}
			if (!recipients.TryGetValue(targetId, out List<NpcRulerPolicyEffectDto> targetEffects))
			{
				targetEffects = new List<NpcRulerPolicyEffectDto>();
				recipients[targetId] = targetEffects;
			}
			targetEffects.Add(effect);
		}
		foreach (KeyValuePair<string, List<NpcRulerPolicyEffectDto>> recipient in recipients)
		{
			string targetId = recipient.Key;
			bool isIssuer = !string.IsNullOrWhiteSpace(issuerId) && string.Equals(targetId, issuerId, StringComparison.OrdinalIgnoreCase);
			List<NpcRulerPolicyEffectDto> relevantEffects = isIssuer ? effects : recipient.Value;
			NpcRulerPolicyEffectDto targetEffect = recipient.Value.FirstOrDefault();
			string targetName = Limit(isIssuer
				? FirstNonEmpty(attribution.IssuerKingdomName, issuerId)
				: string.Equals(targetId, policyTargetId, StringComparison.OrdinalIgnoreCase)
					? FirstNonEmpty(attribution.TargetKingdomName, targetEffect?.TargetKingdomName, targetId)
					: FirstNonEmpty(targetEffect?.TargetKingdomName, targetId), 50);
			string policyName = Limit(FirstNonEmpty(policy.PolicyName, "未命名政策"), 70);
			string effectSummary = Limit(BuildEffectSummary(relevantEffects), 120);
			if (string.IsNullOrWhiteSpace(effectSummary))
			{
				effectSummary = "无持续数值变化";
			}
			string snapshot = BuildUnifiedPolicyWeeklySnapshot(policy, attribution, targetName, effectSummary);
			string materialLabel = attribution.IsPlayerVassalPolicy ? "玩家附庸政策" : "统治者政策";
			int activeExecutableEffects = CountActiveExecutablePolicyEffectInstances(relevantEffects);
			MyBehavior.RecordPolicySystemWeeklyMaterialForExternal(attribution.MaterialKind, materialLabel + " - " + targetName + " / " + policyName, snapshot,
				"unified_policy:" + policy.PolicyId + ":weekly:" + NormalizeKeyPart(targetId), targetId, recipients.Count > 1 || !isIssuer,
				policy.RulerHeroId ?? "", attribution.IssuerKingdomId ?? "", Math.Max(0, policy.Day), policy.GameDate ?? "");
			string logMessage = "policyId=" + policy.PolicyId
				+ " issuer=" + issuerId
				+ " target=" + targetId
				+ " route=" + (isIssuer ? "issuer" : "affected")
				+ " effects=" + activeExecutableEffects.ToString(CultureInfo.InvariantCulture)
				+ " effectShells=" + relevantEffects.Count.ToString(CultureInfo.InvariantCulture)
				+ " chars=" + snapshot.Length.ToString(CultureInfo.InvariantCulture);
			PolicySystemLog.Lifecycle("Weekly", "material-recorded", "event", new PolicyLogContext
			{
				PolicyId = policy.PolicyId,
				IssuerKingdomId = issuerId,
				IssuerKingdomName = attribution.IssuerKingdomName,
				TargetKingdomId = targetId,
				TargetKingdomName = targetName,
				TargetName = targetName,
				Route = isIssuer ? "issuer" : "affected",
				MessageChars = logMessage.Length,
				MessageHash = PolicySystemLog.HashSensitive(logMessage),
				Counts = new Dictionary<string, int>(StringComparer.Ordinal)
				{
					["effects"] = activeExecutableEffects,
					["effectShells"] = relevantEffects.Count,
					["chars"] = snapshot.Length
				}
			});
		}
	}

	private static int CountActiveExecutablePolicyEffectInstances(IEnumerable<NpcRulerPolicyEffectDto> effects)
	{
		float currentDay = Math.Max(0, GetCurrentCampaignDay());
		int count = 0;
		foreach (NpcRulerPolicyEffectDto effect in effects ?? Enumerable.Empty<NpcRulerPolicyEffectDto>())
		{
			foreach (PolicyEffectInstanceSaveData instance in effect?.ModuleEffects ?? Enumerable.Empty<PolicyEffectInstanceSaveData>())
			{
				if (instance != null
					&& instance.LifecycleState == PolicyEffectLifecycleState.Active
					&& (instance.EndDay <= 0f || instance.EndDay > currentDay)
					&& PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
					&& PolicyEffectModuleCatalog.IsAllowedForScope(module, PolicyEffectScopes.Kingdom))
				{
					count++;
				}
			}
		}
		return count;
	}

	private static UnifiedPolicyWeeklyAttribution ResolveUnifiedPolicyWeeklyAttribution(NpcRulerPolicyRecord policy)
	{
		bool isPlayerVassalPolicy = policy?.IsPlayerPolicy == true
			&& string.Equals(policy.PolicyKind ?? string.Empty, "vassal", StringComparison.OrdinalIgnoreCase);
		string targetKingdomId = (policy?.KingdomId ?? string.Empty).Trim();
		string targetKingdomName = FirstNonEmpty(policy?.KingdomName, targetKingdomId, "目标附庸国");
		Kingdom playerKingdom = TryResolvePlayerKingdomForWeeklyAttribution();
		string issuerKingdomId = isPlayerVassalPolicy
			? FirstNonEmpty(policy?.IssuerKingdomId, playerKingdom?.StringId)
			: targetKingdomId;
		string issuerKingdomName = isPlayerVassalPolicy
			? FirstNonEmpty(policy?.IssuerKingdomName, playerKingdom?.Name?.ToString(), issuerKingdomId)
			: FirstNonEmpty(policy?.KingdomName, targetKingdomName, issuerKingdomId);
		string publisherLabel = isPlayerVassalPolicy
			? string.IsNullOrWhiteSpace(issuerKingdomName) ? "宗主玩家" : "宗主玩家（" + issuerKingdomName + "）"
			: FirstNonEmpty(issuerKingdomName, "未知发布国");
		return new UnifiedPolicyWeeklyAttribution
		{
			IsPlayerVassalPolicy = isPlayerVassalPolicy,
			MaterialKind = isPlayerVassalPolicy ? "player_vassal_policy" : "ruler_policy",
			IssuerKingdomId = issuerKingdomId,
			IssuerKingdomName = issuerKingdomName,
			TargetKingdomId = targetKingdomId,
			TargetKingdomName = targetKingdomName,
			PublisherLabel = publisherLabel
		};
	}

	private static Kingdom TryResolvePlayerKingdomForWeeklyAttribution()
	{
		try
		{
			return Clan.PlayerClan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}

	private static string BuildUnifiedPolicyWeeklySnapshot(
		NpcRulerPolicyRecord policy,
		UnifiedPolicyWeeklyAttribution attribution,
		string targetName,
		string effectSummary)
	{
		string policyName = Limit(FirstNonEmpty(policy?.PolicyName, "未命名政策"), 70);
		string prefix = attribution?.IsPlayerVassalPolicy == true
			? "玩家附庸政策。发布方：" + FirstNonEmpty(attribution.PublisherLabel, "宗主玩家")
				+ "。生效对象：" + Limit(FirstNonEmpty(attribution.TargetKingdomName, attribution.TargetKingdomId, "目标附庸国"), 50)
				+ "。发布者：" + Limit(FirstNonEmpty(policy?.RulerName, "玩家"), 50)
				+ "。归属说明：由宗主玩家发布，不是附庸统治者自行颁布"
			: "统治者政策。发布国：" + Limit(FirstNonEmpty(attribution?.IssuerKingdomName, policy?.KingdomName, policy?.KingdomId), 50)
				+ "。发布者：" + Limit(policy?.RulerName, 50);
		return Limit(prefix + "。政策：《" + policyName + "》"
			+ "。政策摘要：" + Limit(policy?.PolicyDigest, 140) + "。衍生事件：" + Limit(policy?.FeedbackDigest, 70)
			+ "。周报归属国：" + Limit(targetName, 50) + "。相关每日影响：" + Limit(effectSummary, 120) + "。", 420);
	}

	private static bool TryDeserializePolicyRecords(string json, out List<NpcRulerPolicyRecord> records, out Exception exception)
	{
		records = new List<NpcRulerPolicyRecord>();
		exception = null;
		try
		{
			string compatibleJson = NormalizeGeneratedPolicyEventFieldNames(json);
			if (!TryDeserializeWirePolicyRecords(compatibleJson, out records))
			{
				throw new JsonException("NPC policy response 不符合 canonical module wire contract");
			}
			return true;
		}
		catch (Exception ex)
		{
			exception = ex;
			records = new List<NpcRulerPolicyRecord>();
			return false;
		}
	}

	private static bool TryDeserializeWirePolicyRecords(string json, out List<NpcRulerPolicyRecord> records)
	{
		records = new List<NpcRulerPolicyRecord>();
		JToken root = JToken.Parse((json ?? "").Trim());
		if (root.Type != JTokenType.Object || root["policies"] is not JArray policies || policies.Count == 0
			|| policies.Any(token => token?.Type != JTokenType.Object))
		{
			return false;
		}
		foreach (JToken candidate in policies)
		{
			NpcRulerPolicyWireRecord wire = candidate.ToObject<NpcRulerPolicyWireRecord>();
			if (wire == null
				|| wire.EffectSchemaVersion != PolicyEffectDataVersions.WireSchemaVersion
				|| wire.DurationDays <= 0
				|| wire.Effects == null)
			{
				return false;
			}
			records.Add(wire.ToPersistedRecord());
		}
		return true;
	}

	private static NpcRulerPolicyRecord DeserializeGeneratedPolicyFragment(string json)
	{
		if (TryDeserializeWirePolicyRecords("{\"policies\":[" + (json ?? "") + "]}", out List<NpcRulerPolicyRecord> wireRecords))
		{
			return wireRecords.FirstOrDefault();
		}
		return null;
	}

	private static string NormalizeGeneratedPolicyEventFieldNames(string json)
	{
		string compatible = json ?? "";
		compatible = Regex.Replace(compatible, @"""derivedEventPremise""(?=\s*:)", "\"eventPremise\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventTitle""(?=\s*:)", "\"feedbackTitle\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventContent""(?=\s*:)", "\"publicFeedback\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		compatible = Regex.Replace(compatible, @"""derivedEventDigest""(?=\s*:)", "\"feedbackDigest\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		return compatible;
	}

	private static string RepairNpcPolicyJson(string json)
	{
		string repaired = Regex.Replace(json ?? "", @"'(?=\s*,\s*\r?\n\s*""[A-Za-z_])", "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>[’”])(?=\s*,\s*\r?\n\s*""[A-Za-z_])", match => match.Groups["closing"].Value + "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>['’”])(?<newline>\r?\n)\s*}\s*\r?\n\s*]\s*,\s*\r?\n(?<next>\s*""effects""\s*:)", match => match.Groups["closing"].Value + "\"," + match.Groups["newline"].Value + match.Groups["next"].Value, RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"'(?=\s*\r?\n\s*[}\]])", "\"", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<closing>[’”])(?=\s*\r?\n\s*[}\]])", match => match.Groups["closing"].Value + "\"", RegexOptions.CultureInvariant);
		repaired = NormalizeJsonStructuralPunctuation(repaired);
		repaired = Regex.Replace(repaired, @"(?<prefix>[{,]\s*)(?<name>[A-Za-z_][A-Za-z0-9_]*)""?\s*:", match => match.Groups["prefix"].Value + "\"" + match.Groups["name"].Value + "\":", RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>""(?:\\.|[^""\\])*"")\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.Singleline | RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|true|false|null)\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @"(?<value>[\]}])\s*(?<next>""[A-Za-z_][A-Za-z0-9_]*""\s*:)", match => match.Groups["value"].Value + "," + match.Groups["next"].Value, RegexOptions.CultureInvariant);
		repaired = Regex.Replace(repaired, @",\s*(?<close>[\]}])", match => match.Groups["close"].Value, RegexOptions.CultureInvariant);
		return repaired;
	}

	private static List<NpcRulerPolicyRecord> RecoverPolicyRecordsFromFragments(string raw, out int candidateCount, out int repairedCount)
	{
		candidateCount = 0;
		repairedCount = 0;
		List<NpcRulerPolicyRecord> records = new List<NpcRulerPolicyRecord>();
		foreach (string fragment in ExtractCompletePolicyObjectFragments(raw))
		{
			candidateCount++;
			string compatibleFragment = NormalizeGeneratedPolicyEventFieldNames(fragment);
			try
			{
				NpcRulerPolicyRecord record = DeserializeGeneratedPolicyFragment(compatibleFragment);
				if (record != null)
				{
					records.Add(record);
					continue;
				}
			}
			catch
			{
			}
			string repaired = RepairNpcPolicyJson(compatibleFragment);
			if (string.Equals(repaired, compatibleFragment, StringComparison.Ordinal))
			{
				continue;
			}
			try
			{
				NpcRulerPolicyRecord record = DeserializeGeneratedPolicyFragment(repaired);
				if (record != null)
				{
					records.Add(record);
					repairedCount++;
				}
			}
			catch
			{
			}
		}
		return records;
	}

	private static List<string> ExtractCompletePolicyObjectFragments(string text)
	{
		List<string> result = new List<string>();
		text = StripJsonCodeFence(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return result;
		}
		int policiesIndex = text.IndexOf("\"policies\"", StringComparison.OrdinalIgnoreCase);
		int arrayStart = policiesIndex >= 0 ? text.IndexOf('[', policiesIndex + 10) : text.IndexOf('[');
		if (arrayStart < 0)
		{
			return result;
		}
		List<char> expectedClosers = new List<char> { ']' };
		bool inString = false;
		bool escaped = false;
		int fragmentStart = -1;
		for (int i = arrayStart + 1; i < text.Length && expectedClosers.Count > 0; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == '{' || ch == '[')
			{
				if (ch == '{' && expectedClosers.Count == 1)
				{
					fragmentStart = i;
				}
				expectedClosers.Add(ch == '{' ? '}' : ']');
				continue;
			}
			if (ch != '}' && ch != ']')
			{
				continue;
			}
			if (expectedClosers.Count == 0 || expectedClosers[expectedClosers.Count - 1] != ch)
			{
				break;
			}
			expectedClosers.RemoveAt(expectedClosers.Count - 1);
			if (ch == '}' && fragmentStart >= 0 && expectedClosers.Count == 1)
			{
				result.Add(text.Substring(fragmentStart, i - fragmentStart + 1));
				fragmentStart = -1;
			}
		}
		return result;
	}

	private static void LogPolicyFragmentRecovery(string batchId, string route, int attempts, string parseSource, int recoveredCount, int candidateCount, int repairedCount, string reason)
	{
		string message = "policy-fragment-recovery"
			+ " batchId=" + (batchId ?? "")
			+ " route=" + (route ?? "")
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " source=" + FirstNonEmpty(parseSource, "policy")
			+ " reason=" + (reason ?? "")
			+ " candidates=" + Math.Max(0, candidateCount).ToString(CultureInfo.InvariantCulture)
			+ " recovered=" + Math.Max(0, recoveredCount).ToString(CultureInfo.InvariantCulture)
			+ " repaired=" + Math.Max(0, repairedCount).ToString(CultureInfo.InvariantCulture);
		Log(message);
		PolicyTraceLog("policy-fragment-recovery", message, "local recovery completed without another LLM request");
	}

	private static string NormalizeJsonStructuralPunctuation(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text ?? "";
		}
		StringBuilder sb = new StringBuilder(text.Length);
		bool inString = false;
		bool escaped = false;
		for (int i = 0; i < text.Length; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					sb.Append(ch);
					escaped = false;
				}
				else if (ch == '\\')
				{
					sb.Append(ch);
					escaped = true;
				}
				else if (ch == '"')
				{
					sb.Append(ch);
					inString = false;
				}
				else if (ch == '\r')
				{
					sb.Append("\\n");
					if (i + 1 < text.Length && text[i + 1] == '\n')
					{
						i++;
					}
				}
				else if (ch == '\n')
				{
					sb.Append("\\n");
				}
				else if (ch == '\t')
				{
					sb.Append("\\t");
				}
				else if (ch == '\b')
				{
					sb.Append("\\b");
				}
				else if (ch == '\f')
				{
					sb.Append("\\f");
				}
				else if (ch < ' ')
				{
					sb.Append("\\u");
					sb.Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
				}
				else
				{
					sb.Append(ch);
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				sb.Append(ch);
				continue;
			}
			if (ch == '，')
			{
				sb.Append(',');
				continue;
			}
			if (ch == '：')
			{
				sb.Append(':');
				continue;
			}
			sb.Append(ch);
		}
		return sb.ToString();
	}

	private static void LogPolicyJsonRepair(string batchId, string route, int attempts, string parseSource, Exception firstException, string originalJson, string repairedJson)
	{
		string message = "policy-parse-repaired"
			+ " batchId=" + (batchId ?? "")
			+ " route=" + (route ?? "")
			+ " attempts=" + Math.Max(0, attempts).ToString(CultureInfo.InvariantCulture)
			+ " source=" + FirstNonEmpty(parseSource, "policy")
			+ " firstError=" + (firstException == null ? "" : firstException.GetType().Name + ":" + firstException.Message);
		Log(message);
		PolicyTraceLog("policy-parse-repaired", message, "original_sample:\n" + Limit(originalJson, 1200) + "\n\nrepaired_sample:\n" + Limit(repairedJson, 1200));
	}

	private static string ExtractJson(string text)
	{
		return ExtractJson(text, out _);
	}

	private static string ExtractJson(string text, out bool ignoredTrailingText)
	{
		ignoredTrailingText = false;
		text = StripJsonCodeFence(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int objectStart = text.IndexOf('{');
		int arrayStart = text.IndexOf('[');
		int start;
		if (objectStart < 0)
		{
			start = arrayStart;
		}
		else if (arrayStart < 0)
		{
			start = objectStart;
		}
		else
		{
			start = Math.Min(objectStart, arrayStart);
		}
		if (start < 0)
		{
			return "";
		}
		List<char> expectedClosers = new List<char>();
		bool inString = false;
		bool escaped = false;
		for (int i = start; i < text.Length; i++)
		{
			char ch = text[i];
			if (inString)
			{
				if (escaped)
				{
					escaped = false;
				}
				else if (ch == '\\')
				{
					escaped = true;
				}
				else if (ch == '"')
				{
					inString = false;
				}
				continue;
			}
			if (ch == '"')
			{
				inString = true;
				continue;
			}
			if (ch == '{' || ch == '[')
			{
				expectedClosers.Add(ch == '{' ? '}' : ']');
				continue;
			}
			if (ch != '}' && ch != ']')
			{
				continue;
			}
			if (expectedClosers.Count == 0 || expectedClosers[expectedClosers.Count - 1] != ch)
			{
				return "";
			}
			expectedClosers.RemoveAt(expectedClosers.Count - 1);
			if (expectedClosers.Count == 0)
			{
				string trailing = text.Substring(i + 1).Trim();
				ignoredTrailingText = !string.IsNullOrWhiteSpace(trailing);
				return text.Substring(start, i - start + 1);
			}
		}
		return "";
	}

	private static string StripJsonCodeFence(string text)
	{
		text = (text ?? "").Trim();
		if (text.StartsWith("```", StringComparison.Ordinal))
		{
			text = Regex.Replace(text, "^```(?:json)?", "", RegexOptions.IgnoreCase).Trim();
			text = Regex.Replace(text, "```$", "", RegexOptions.IgnoreCase).Trim();
		}
		return text;
	}

	private sealed class NpcPolicyLoadNormalizationSummary
	{
		public int RecordsVisited;
		public int RecordsLoaded;
		public int InstancesVisited;
		public int KnownInstancesNormalized;
		public int UnknownInstancesPreserved;
		public int LegacyInstancesImported;
		public int RuntimeStatesMigrated;
		public int InstancesRejected;
		public int DuplicateInstancesRejected;
		public int ReceiptsRejected;
		public int ReceiptsDeduplicated;
		public readonly List<string> Warnings = new List<string>();

		public void AddWarning(string warning)
		{
			string value = Limit(warning ?? string.Empty, 220);
			if (value.Length > 0 && Warnings.Count < 12)
			{
				Warnings.Add(value);
			}
		}

		public string BuildMessage()
		{
			return "records=" + RecordsLoaded.ToString(CultureInfo.InvariantCulture)
				+ "/" + RecordsVisited.ToString(CultureInfo.InvariantCulture)
				+ " instances=" + InstancesVisited.ToString(CultureInfo.InvariantCulture)
				+ " known=" + KnownInstancesNormalized.ToString(CultureInfo.InvariantCulture)
				+ " unknownInert=" + UnknownInstancesPreserved.ToString(CultureInfo.InvariantCulture)
				+ " legacy=" + LegacyInstancesImported.ToString(CultureInfo.InvariantCulture)
				+ " runtimeMigrated=" + RuntimeStatesMigrated.ToString(CultureInfo.InvariantCulture)
				+ " rejected=" + InstancesRejected.ToString(CultureInfo.InvariantCulture)
				+ " duplicate=" + DuplicateInstancesRejected.ToString(CultureInfo.InvariantCulture)
				+ " receiptRejected=" + ReceiptsRejected.ToString(CultureInfo.InvariantCulture)
				+ " receiptDeduplicated=" + ReceiptsDeduplicated.ToString(CultureInfo.InvariantCulture);
		}
	}

	private static NpcRulerPolicyRecord DeserializeRecord(string raw)
	{
		return DeserializeRecordCore(raw, null);
	}

	private static NpcRulerPolicyRecord DeserializeRecordForLoad(string raw, NpcPolicyLoadNormalizationSummary summary)
	{
		return DeserializeRecordCore(raw, summary);
	}

	private static NpcRulerPolicyRecord DeserializeRecordCore(string raw, NpcPolicyLoadNormalizationSummary summary)
	{
		if (summary != null)
		{
			summary.RecordsVisited++;
		}
		try
		{
			JObject rawObject = JObject.Parse(raw ?? string.Empty);
			NpcRulerPolicyRecord record = rawObject.ToObject<NpcRulerPolicyRecord>();
			if (record == null || string.IsNullOrWhiteSpace(record.PolicyId))
			{
				summary?.AddWarning("record rejected: missing policyId");
				return null;
			}
			NpcRulerPolicyRecord normalized = NormalizePersistedNpcPolicyRecordCore(record, rawObject, summary);
			if (normalized != null && summary != null)
			{
				summary.RecordsLoaded++;
			}
			return normalized;
		}
		catch (Exception ex)
		{
			summary?.AddWarning("record rejected: " + ex.GetType().Name + ": " + ex.Message);
			return null;
		}
	}

	private static void LogNpcPolicyLoadNormalizationSummary(NpcPolicyLoadNormalizationSummary summary)
	{
		if (summary == null)
		{
			return;
		}
		string message = summary.BuildMessage();
		bool hasRejectedItems = summary.RecordsLoaded != summary.RecordsVisited || summary.InstancesRejected > 0
			|| summary.DuplicateInstancesRejected > 0 || summary.ReceiptsRejected > 0;
		PolicySystemLog.Lifecycle("Npc", "load-normalized", hasRejectedItems ? "warnings" : "success", new PolicyLogContext
		{
			Counts = new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["recordsVisited"] = summary.RecordsVisited,
				["recordsLoaded"] = summary.RecordsLoaded,
				["instancesVisited"] = summary.InstancesVisited,
				["knownInstances"] = summary.KnownInstancesNormalized,
				["unknownInstances"] = summary.UnknownInstancesPreserved,
				["legacyInstances"] = summary.LegacyInstancesImported,
				["instancesRejected"] = summary.InstancesRejected,
				["duplicateInstancesRejected"] = summary.DuplicateInstancesRejected,
				["receiptsRejected"] = summary.ReceiptsRejected,
				["warnings"] = summary.Warnings.Count
			}
		});
		if (hasRejectedItems)
		{
			PolicySystemLog.Failure("Npc", "save-normalization-rejected", message, string.Join("\n", summary.Warnings));
			return;
		}
		PolicySystemLog.WriteModuleLifecycle("Npc", "npc-policy-load", migration: message);
	}

	private static NpcRulerPolicyRecord NormalizePersistedNpcPolicyRecord(NpcRulerPolicyRecord record)
	{
		return NormalizePersistedNpcPolicyRecordCore(record, null, null);
	}

	private static NpcRulerPolicyRecord NormalizePersistedNpcPolicyRecord(NpcRulerPolicyRecord record, JObject rawObject)
	{
		return NormalizePersistedNpcPolicyRecordCore(record, rawObject, null);
	}

	private static NpcRulerPolicyRecord NormalizePersistedNpcPolicyRecordCore(
		NpcRulerPolicyRecord record,
		JObject rawObject,
		NpcPolicyLoadNormalizationSummary summary)
	{
		if (record == null)
		{
			return null;
		}
		if (rawObject != null && PolicyEffectSaveCodec.IsLegacyStoppedNpcPolicyShape(rawObject))
		{
			record.Version = 6;
			record.AgendaStatus = AgendaStatusAbolished;
			record.Effects ??= new List<NpcRulerPolicyEffectDto>();
			foreach (NpcRulerPolicyEffectDto effect in record.Effects.Where(value => value != null))
			{
				effect.ModuleEffects = new List<PolicyEffectInstanceSaveData>();
				effect.EffectId = string.Empty;
				effect.IsEnded = true;
				effect.RemainingDays = 0;
			}
			record.ExecutionReceipts = new List<PolicyEffectExecutionReceipt>();
			summary?.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
				+ " legacy fixed-field effects stopped; published text retained for re-review");
			return record;
		}
		int sourceVersion = record.Version;
		if (sourceVersion < 6
			&& record.IsPlayerPolicy
			&& string.Equals(record.AgendaStatus, AgendaStatusActive, StringComparison.OrdinalIgnoreCase))
		{
			// v5 player registration published all presentation artifacts synchronously.
			// Treat those rows as complete so loading an old save cannot replay announcements.
			record.ApprovalAnnouncementPublished = true;
			record.ApprovalPolicyEventPublished = true;
			record.ApprovalPublicFeedbackPublished = true;
			record.ApprovalWeeklyMaterialRecorded = true;
		}
		record.Version = Math.Max(6, record.Version);
		record.Effects ??= new List<NpcRulerPolicyEffectDto>();
		record.ExecutionReceipts ??= new List<PolicyEffectExecutionReceipt>();
		record.DurationDays = record.DurationDays > 0
			? record.DurationDays
			: record.Effects.Where(effect => effect != null).Select(effect => effect.DurationDays).DefaultIfEmpty(0).Max();
		JArray rawEffects = rawObject?.GetValue("effects", StringComparison.OrdinalIgnoreCase) as JArray;
		for (int index = 0; index < record.Effects.Count; index++)
		{
			NpcRulerPolicyEffectDto effect = record.Effects[index];
			if (effect == null)
			{
				continue;
			}
			effect.ModuleEffects ??= new List<PolicyEffectInstanceSaveData>();
			JObject rawEffect = index < (rawEffects?.Count ?? 0) ? rawEffects[index] as JObject : null;
			if (effect.ModuleEffects.Count == 0 && rawEffect != null)
			{
				if (!LegacyPolicyEffectFieldAdapter.TryReadLegacyFields(rawEffect, out IReadOnlyDictionary<string, float> legacyValues, out string legacyError))
				{
					summary?.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
						+ " effect=" + index.ToString(CultureInfo.InvariantCulture) + " legacy rejected: " + Limit(legacyError, 160));
					legacyValues = new Dictionary<string, float>();
				}
				PolicyEffectLifecycleState importedState = effect.IsEnded || effect.RemainingDays <= 0
					? PolicyEffectLifecycleState.Completed
					: (!string.IsNullOrWhiteSpace(effect.EffectId) ? PolicyEffectLifecycleState.Active : PolicyEffectLifecycleState.Prepared);
				effect.ModuleEffects = BuildLegacyNpcModuleEffectsFromRaw(
					record,
					effect,
					legacyValues,
					effect.DurationDays > 0 ? effect.DurationDays : record.DurationDays,
					importedState,
					summary);
				if (summary != null)
				{
					summary.LegacyInstancesImported += effect.ModuleEffects.Count;
				}
			}
			List<PolicyEffectInstanceSaveData> normalizedInstances = new List<PolicyEffectInstanceSaveData>();
			foreach (PolicyEffectInstanceSaveData instance in effect.ModuleEffects)
			{
				if (summary != null)
				{
					summary.InstancesVisited++;
				}
				int originalStateSchemaVersion = instance?.StateSchemaVersion ?? 0;
				if (!PolicyEffectSaveCodec.TryNormalizeInstance(instance, out PolicyEffectNormalizedInstance normalized, out string normalizeError))
				{
					if (summary != null)
					{
						summary.InstancesRejected++;
						summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " instance=" + (instance?.InstanceId ?? string.Empty)
							+ " rejected: " + Limit(normalizeError, 160));
					}
					continue;
				}
				PolicyEffectInstanceSaveData saveData;
				bool unknownInert = false;
				bool runtimeStateMigrated = false;
				if (normalized.IsInert)
				{
					if (!string.Equals(normalized.InertReason, "unknownModule", StringComparison.Ordinal))
					{
						if (summary != null)
						{
							summary.InstancesRejected++;
							summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " instance=" + (instance?.InstanceId ?? string.Empty)
								+ " rejected: " + Limit(normalized.InertReason, 160));
						}
						continue;
					}
					saveData = instance;
					unknownInert = true;
				}
				else
				{
					string sourceScope = (normalized.SaveData?.SourceScope ?? string.Empty).Trim();
					if (normalized.Module == null
						|| sourceScope.Length == 0
						|| !PolicyEffectModuleCatalog.IsAllowedForScope(normalized.Module, sourceScope)
						|| (!record.IsPlayerPolicy && !string.Equals(sourceScope, PolicyEffectScopes.Kingdom, StringComparison.OrdinalIgnoreCase))
						|| !string.Equals((normalized.SaveData?.PolicyId ?? string.Empty).Trim(), (record.PolicyId ?? string.Empty).Trim(), StringComparison.Ordinal))
					{
						if (summary != null)
						{
							summary.InstancesRejected++;
							summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " instance=" + (instance?.InstanceId ?? string.Empty)
								+ " rejected: module scope or policy identity mismatch");
						}
						continue;
					}
					saveData = normalized.SaveData;
					runtimeStateMigrated = originalStateSchemaVersion != saveData.StateSchemaVersion;
				}
				string instanceId = (saveData?.InstanceId ?? string.Empty).Trim();
				if (!TryGetNpcPolicyPayloadUtf8Size(saveData?.Payload, out int payloadBytes)
					|| payloadBytes > PolicyEffectSaveCodec.MaxPayloadBytes)
				{
					if (summary != null)
					{
						summary.InstancesRejected++;
						summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " instance=" + instanceId
							+ " rejected: raw shell payload limit");
					}
					continue;
				}
				normalizedInstances.Add(saveData);
				if (summary != null)
				{
					if (unknownInert)
					{
						summary.UnknownInstancesPreserved++;
					}
					else
					{
						summary.KnownInstancesNormalized++;
						if (runtimeStateMigrated)
						{
							summary.RuntimeStatesMigrated++;
						}
					}
				}
			}
			effect.ModuleEffects = normalizedInstances;
			SynchronizeNpcPolicyEffectShell(effect);
		}
		record.Effects = record.Effects.Where(HasValidEffectShape).ToList();
		Dictionary<string, int> durationByInstanceId = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (NpcRulerPolicyEffectDto effect in record.Effects)
		{
			foreach (PolicyEffectInstanceSaveData instance in effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			{
				string instanceId = (instance?.InstanceId ?? string.Empty).Trim();
				if (durationByInstanceId.TryGetValue(instanceId, out int existingDuration)
					&& existingDuration != effect.DurationDays)
				{
					if (summary != null)
					{
						summary.DuplicateInstancesRejected++;
						summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
							+ " instance=" + instanceId + " rejected: conflicting shell duration");
					}
					return null;
				}
				durationByInstanceId[instanceId] = effect.DurationDays;
			}
		}
		List<PolicyEffectInstanceSaveData> retainedShellInstances = record.Effects
			.SelectMany(effect => effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
			.Where(instance => instance != null)
			.ToList();
		if (!TryCoalesceNpcPolicyEffectShellInstances(
			retainedShellInstances,
			out List<PolicyEffectInstanceSaveData> canonicalInstances,
			out string coalesceError))
		{
			if (summary != null)
			{
				summary.DuplicateInstancesRejected++;
				summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " rejected: " + Limit(coalesceError, 160));
			}
			return null;
		}
		if (canonicalInstances.Count > PolicyEffectSaveCodec.MaxInstancesPerPolicy)
		{
			if (summary != null)
			{
				summary.InstancesRejected += canonicalInstances.Count - PolicyEffectSaveCodec.MaxInstancesPerPolicy;
				summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty) + " rejected: logical instance limit");
			}
			return null;
		}
		int totalPayloadBytes = 0;
		foreach (PolicyEffectInstanceSaveData canonicalInstance in canonicalInstances)
		{
			if (!TryGetNpcPolicyPayloadUtf8Size(canonicalInstance?.Payload, out int payloadBytes)
				|| payloadBytes > PolicyEffectSaveCodec.MaxPayloadBytes
				|| totalPayloadBytes > PolicyEffectSaveCodec.MaxTotalPayloadBytes - payloadBytes)
			{
				if (summary != null)
				{
					summary.InstancesRejected++;
					summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
						+ " instance=" + (canonicalInstance?.InstanceId ?? string.Empty)
						+ " rejected: logical payload aggregate limit");
				}
				return null;
			}
			totalPayloadBytes += payloadBytes;
		}
		Dictionary<string, PolicyEffectInstanceSaveData> validInstances = canonicalInstances
			.ToDictionary(instance => instance.InstanceId.Trim(), StringComparer.Ordinal);
		List<PolicyEffectExecutionReceipt> receiptCandidates = record.ExecutionReceipts
			.Concat(record.Effects.SelectMany(effect => effect.ModuleEffects ?? new List<PolicyEffectInstanceSaveData>())
				.Select(instance => instance?.ExecutionReceipt))
			.Where(receipt => receipt != null)
			.ToList();
		Dictionary<string, string> receiptOwnerById = new Dictionary<string, string>(StringComparer.Ordinal);
		Dictionary<string, PolicyEffectExecutionReceipt> receiptByInstanceId
			= new Dictionary<string, PolicyEffectExecutionReceipt>(StringComparer.Ordinal);
		foreach (PolicyEffectExecutionReceipt receipt in receiptCandidates)
		{
			string receiptId = (receipt?.ReceiptId ?? string.Empty).Trim();
			string instanceId = (receipt?.InstanceId ?? string.Empty).Trim();
			if (receipt == null || receiptId.Length == 0 || instanceId.Length == 0
				|| !validInstances.TryGetValue(instanceId, out PolicyEffectInstanceSaveData instance)
				|| !string.Equals((receipt.PolicyId ?? string.Empty).Trim(), (instance.PolicyId ?? string.Empty).Trim(), StringComparison.Ordinal)
				|| !string.Equals((receipt.ModuleId ?? string.Empty).Trim(), (instance.ModuleId ?? string.Empty).Trim(), StringComparison.Ordinal)
				|| receipt.TargetSet == null
				|| ContainsNpcPolicyTypeMetadata(receipt.RequestedPayload)
				|| ContainsNpcPolicyTypeMetadata(receipt.AppliedPayload))
			{
				if (summary != null)
				{
					summary.ReceiptsRejected++;
				}
				continue;
			}
			if (receiptOwnerById.TryGetValue(receiptId, out string existingOwner)
				&& !string.Equals(existingOwner, instanceId, StringComparison.Ordinal))
			{
				if (summary != null)
				{
					summary.ReceiptsRejected++;
					summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
						+ " rejected: receiptId is shared by multiple logical instances");
				}
				return null;
			}
			receiptOwnerById[receiptId] = instanceId;
			if (receiptByInstanceId.TryGetValue(instanceId, out PolicyEffectExecutionReceipt existingReceipt))
			{
				if (!AreCompatibleNpcPolicyEffectReceipts(existingReceipt, receipt))
				{
					if (summary != null)
					{
						summary.ReceiptsRejected++;
						summary.AddWarning("policy=" + (record.PolicyId ?? string.Empty)
							+ " instance=" + instanceId + " rejected: conflicting receipt mirrors");
					}
					return null;
				}
				if (summary != null)
				{
					summary.ReceiptsDeduplicated++;
				}
				continue;
			}
			PolicyEffectExecutionReceipt canonicalReceipt = CloneNpcPolicyEffectReceipt(receipt);
			canonicalReceipt.ReceiptId = receiptId;
			canonicalReceipt.InstanceId = instanceId;
			canonicalReceipt.TargetSet = CloneNpcPolicyEffectTargetSet(instance.TargetSet);
			receiptByInstanceId.Add(instanceId, canonicalReceipt);
		}
		record.ExecutionReceipts = canonicalInstances
			.Select(instance => receiptByInstanceId.TryGetValue(instance.InstanceId.Trim(), out PolicyEffectExecutionReceipt receipt)
				? receipt
				: null)
			.Where(receipt => receipt != null)
			.ToList();
		foreach (PolicyEffectInstanceSaveData shellInstance in retainedShellInstances)
		{
			string instanceId = (shellInstance?.InstanceId ?? string.Empty).Trim();
			shellInstance.ExecutionReceipt = receiptByInstanceId.TryGetValue(instanceId, out PolicyEffectExecutionReceipt receipt)
				? CloneNpcPolicyEffectReceipt(receipt)
				: null;
		}
		return record;
	}

	private static bool TryGetNpcPolicyPayloadUtf8Size(JToken payload, out int payloadBytes)
	{
		payloadBytes = 0;
		try
		{
			if (payload == null || payload.Type == JTokenType.Null)
			{
				return false;
			}
			payloadBytes = Encoding.UTF8.GetByteCount(payload.ToString(Formatting.None));
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool TryCoalesceNpcPolicyEffectShellInstances(
		IEnumerable<PolicyEffectInstanceSaveData> shellInstances,
		out List<PolicyEffectInstanceSaveData> canonicalInstances,
		out string failureReason)
	{
		return PolicyEffectBundleContract.TryCoalesceShellInstances(
			shellInstances,
			out canonicalInstances,
			out failureReason);
	}

	private static bool AreCompatibleNpcPolicyEffectShellInstances(
		PolicyEffectInstanceSaveData left,
		PolicyEffectInstanceSaveData right)
	{
		return PolicyEffectBundleContract.AreCompatibleShellInstances(left, right);
	}

	private static bool AreCompatibleNpcPolicyEffectReceipts(
		PolicyEffectExecutionReceipt left,
		PolicyEffectExecutionReceipt right)
	{
		return PolicyEffectBundleContract.AreCompatibleReceipts(left, right);
	}

	private static PolicyEffectCanonicalTargetSet MergeNpcPolicyEffectTargetSets(
		PolicyEffectCanonicalTargetSet left,
		PolicyEffectCanonicalTargetSet right)
	{
		return PolicyEffectBundleContract.MergeTargetSets(left, right);
	}

	private static PolicyEffectCanonicalTargetSet CloneNpcPolicyEffectTargetSet(PolicyEffectCanonicalTargetSet source)
	{
		return PolicyEffectBundleContract.NormalizeTargetSet(source);
	}

	private static PolicyEffectExecutionReceipt CloneNpcPolicyEffectReceipt(PolicyEffectExecutionReceipt source)
	{
		return PolicyEffectBundleContract.CloneReceipt(source);
	}

	private static PolicyEffectInstanceSaveData CloneNpcPolicyEffectInstance(
		PolicyEffectInstanceSaveData source,
		PolicyEffectCanonicalTargetSet targetSet)
	{
		return new PolicyEffectInstanceSaveData
		{
			MechanismContractVersion = source?.MechanismContractVersion ?? 0,
			MechanismContractHash = source?.MechanismContractHash ?? string.Empty,
			ExpectedMechanismLegIds = new List<string>(source?.ExpectedMechanismLegIds ?? new List<string>()),
			EffectPlanVersion = source?.EffectPlanVersion > 0 ? source.EffectPlanVersion : PolicyEffectPlanVersions.CurrentVersion,
			MechanismId = !string.IsNullOrWhiteSpace(source?.MechanismId)
				? source.MechanismId
				: PolicyEffectPlanDefaults.BuildIndependentMechanismId(
					string.IsNullOrWhiteSpace(source?.PolicyId) ? source?.InstanceId : source.PolicyId),
			MechanismKind = source?.MechanismKind ?? PolicyEffectMechanismKind.Independent,
			MechanismRole = source?.MechanismRole ?? PolicyEffectMechanismRole.Subject,
			SourceOmitted = source?.SourceOmitted == true,
			DestinationOmitted = source?.DestinationOmitted == true,
			InstanceId = source?.InstanceId ?? string.Empty,
			PolicyId = source?.PolicyId ?? string.Empty,
			ActorHeroId = source?.ActorHeroId ?? string.Empty,
			ModuleId = source?.ModuleId ?? string.Empty,
			SourceModuleId = source?.SourceModuleId ?? string.Empty,
			PayloadSchemaVersion = source?.PayloadSchemaVersion ?? 0,
			Payload = source?.Payload?.DeepClone(),
			TargetSet = CloneNpcPolicyEffectTargetSet(targetSet),
			LifecycleState = source?.LifecycleState ?? PolicyEffectLifecycleState.Prepared,
			StateSchemaVersion = source?.StateSchemaVersion ?? 0,
			RuntimeState = source?.RuntimeState?.DeepClone(),
			ExecutionReceipt = CloneNpcPolicyEffectReceipt(source?.ExecutionReceipt),
			StartDay = source?.StartDay ?? 0f,
			EndDay = source?.EndDay ?? 0f,
			SourceScope = source?.SourceScope ?? string.Empty,
			Reason = source?.Reason ?? string.Empty
		};
	}

	private static bool ContainsNpcPolicyTypeMetadata(JToken token)
	{
		if (token is JObject obj)
		{
			return obj.Properties().Any(property => string.Equals(property.Name, "$type", StringComparison.OrdinalIgnoreCase)
				|| ContainsNpcPolicyTypeMetadata(property.Value));
		}
		if (token is JArray array)
		{
			return array.Any(ContainsNpcPolicyTypeMetadata);
		}
		return false;
	}

	private static List<PolicyEffectInstanceSaveData> BuildLegacyNpcModuleEffectsFromRaw(
		NpcRulerPolicyRecord record,
		NpcRulerPolicyEffectDto effect,
		IReadOnlyDictionary<string, float> legacyValues,
		int durationDays,
		PolicyEffectLifecycleState importedState,
		NpcPolicyLoadNormalizationSummary summary)
	{
		List<PolicyEffectInstanceSaveData> result = new List<PolicyEffectInstanceSaveData>();
		NpcRulerPolicyAllowedEffectTarget target = new NpcRulerPolicyAllowedEffectTarget
		{
			KingdomId = FirstNonEmpty(effect?.TargetKingdomId, record?.KingdomId),
			KingdomName = FirstNonEmpty(effect?.TargetKingdomName, record?.KingdomName),
			IsIssuer = string.Equals(effect?.TargetKingdomId ?? string.Empty, record?.KingdomId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
		};
		foreach (KeyValuePair<string, float> legacyValue in legacyValues ?? new Dictionary<string, float>())
		{
			string error = string.Empty;
			if (!PolicyEffectModuleCatalog.TryGet(legacyValue.Key, out IPolicyEffectModule module))
			{
				error = "unknown module";
			}
			else if (!PolicyEffectModuleCatalog.IsAllowedForScope(module, PolicyEffectScopes.Kingdom))
			{
				error = "scope not allowed";
			}
			else if (!PolicyEffectModuleCatalog.TryNormalizePayload(module.Id, new JValue(legacyValue.Value), PolicyEffectScopes.Kingdom, out PolicyEffectPayload payload, out error))
			{
			}
			else
			{
				PolicyEffectInstanceSaveData instance = CreateNpcModuleEffectInstance(
					record?.PolicyId,
					record?.RulerHeroId,
					module.Id,
					target,
					Math.Max(1, durationDays),
					Math.Max(0, record?.Day ?? 0),
					JToken.FromObject(payload),
					module.Descriptor.PayloadSchemaVersion,
					effect?.Reason,
					importedState);
				if (module.Descriptor.Hook == PolicyEffectHook.KingdomStabilityOnActivation)
				{
					instance.LifecycleState = PolicyEffectLifecycleState.Completed;
					instance.ExecutionReceipt = CreateLegacyNpcAssumedCompletedReceipt(record, instance, legacyValue.Value);
				}
				result.Add(instance);
				continue;
			}
			if (!string.IsNullOrWhiteSpace(error))
			{
				summary?.AddWarning("policy=" + (record?.PolicyId ?? string.Empty)
					+ " legacy module=" + (legacyValue.Key ?? string.Empty) + " rejected: " + Limit(error, 160));
			}
		}
		return result;
	}

	private static PolicyEffectExecutionReceipt CreateLegacyNpcAssumedCompletedReceipt(
		NpcRulerPolicyRecord record,
		PolicyEffectInstanceSaveData instance,
		float value)
	{
		return new PolicyEffectExecutionReceipt
		{
			ReceiptId = "legacyAssumedCompleted:" + (instance?.InstanceId ?? string.Empty),
			InstanceId = instance?.InstanceId ?? string.Empty,
			PolicyId = record?.PolicyId ?? string.Empty,
			ModuleId = instance?.ModuleId ?? string.Empty,
			TargetSet = instance?.TargetSet,
			Status = PolicyEffectExecutionStatus.Applied,
			RequestedValue = value,
			AppliedValue = value,
			RequestedPayload = instance?.Payload?.DeepClone(),
			AppliedPayload = instance?.Payload?.DeepClone(),
			CampaignDay = Math.Max(0, record?.Day ?? 0),
			Message = "legacyAssumedCompleted"
		};
	}

	private void TrimPolicyRecords()
	{
		List<NpcRulerPolicyRecord> ordered = _policyRecords.Values.Select(DeserializeRecord).Where(x => x != null)
			.OrderByDescending(x => x.Day)
			.ThenByDescending(x => x.CreatedUtcTicks)
			.ThenBy(x => x.PolicyId, StringComparer.Ordinal)
			.ToList();
		HashSet<string> liveKingdomIds;
		try
		{
			liveKingdomIds = new HashSet<string>(
				(Kingdom.All ?? Enumerable.Empty<Kingdom>())
					.Where(kingdom => kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId))
					.Select(kingdom => kingdom.StringId.Trim()),
				StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
			liveKingdomIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		HashSet<string> retainedPolicyIds = SelectNpcPolicyRecordIdsForRetention(
			ordered,
			liveKingdomIds,
			MaxPolicyRecordCount);
		foreach (NpcRulerPolicyRecord extra in ordered.Where(record => !retainedPolicyIds.Contains((record.PolicyId ?? string.Empty).Trim())).ToList())
		{
			string id = (extra.PolicyId ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(id))
			{
				_policyRecords.Remove(id);
			}
		}
	}

	private static HashSet<string> SelectNpcPolicyRecordIdsForRetention(
		IEnumerable<NpcRulerPolicyRecord> records,
		IEnumerable<string> liveKingdomIds,
		int configuredLimit)
	{
		List<NpcRulerPolicyRecord> ordered = (records ?? Enumerable.Empty<NpcRulerPolicyRecord>())
			.Where(record => record != null)
			.OrderByDescending(record => record.Day)
			.ThenByDescending(record => record.CreatedUtcTicks)
			.ThenBy(record => record.PolicyId, StringComparer.Ordinal)
			.ToList();
		HashSet<string> liveIds = new HashSet<string>(
			(liveKingdomIds ?? Enumerable.Empty<string>())
				.Select(id => (id ?? string.Empty).Trim())
				.Where(id => id.Length > 0),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> retained = new HashSet<string>(
			ordered
				.Where(IsNpcPolicyRecordRetentionProtected)
				.Select(record => (record.PolicyId ?? string.Empty).Trim())
				.Where(id => id.Length > 0),
			StringComparer.OrdinalIgnoreCase);
		retained.UnionWith(
			ordered
				.Where(record => !record.IsPlayerPolicy
					&& liveIds.Contains(FirstNonEmpty(record.KingdomId, record.IssuerKingdomId).Trim())
					&& TryMapNpcPolicyHistoryStatus(record.AgendaStatus, out string _))
				.GroupBy(record => FirstNonEmpty(record.KingdomId, record.IssuerKingdomId).Trim(), StringComparer.OrdinalIgnoreCase)
				.Select(group => (group.First().PolicyId ?? string.Empty).Trim())
				.Where(id => id.Length > 0));
		int effectiveLimit = Math.Max(Math.Max(0, configuredLimit), retained.Count);
		foreach (NpcRulerPolicyRecord record in ordered)
		{
			string id = (record.PolicyId ?? string.Empty).Trim();
			if (id.Length > 0 && retained.Count < effectiveLimit)
			{
				retained.Add(id);
			}
		}
		return retained;
	}

	private static bool HasIncompletePolicyPresentation(NpcRulerPolicyRecord record)
	{
		return record != null
			&& (!record.ApprovalAnnouncementPublished
				|| !record.ApprovalPolicyEventPublished
				|| !record.ApprovalPublicFeedbackPublished
				|| !record.ApprovalWeeklyMaterialRecorded);
	}

	private static bool IsNpcPolicyRecordRetentionProtected(NpcRulerPolicyRecord record)
	{
		if (record == null)
		{
			return false;
		}
		string status = (record.AgendaStatus ?? string.Empty).Trim();
		return string.Equals(status, AgendaStatusPending, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusApprovedPendingCommit, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusApprovedRenewalPendingCommit, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusCommitSuspended, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusActive, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(status, AgendaStatusExpiryVotePending, StringComparison.OrdinalIgnoreCase)
			|| record.ApprovalFailureFinalizationPending
			|| record.EffectBundleRollbackPending
			|| (string.Equals(status, AgendaStatusActive, StringComparison.OrdinalIgnoreCase)
				&& HasIncompletePolicyPresentation(record));
	}

	private IEnumerable<Kingdom> GetNpcRuledKingdoms()
	{
		try
		{
			Hero mainHero = Hero.MainHero;
			return (Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(k => k != null && !k.IsEliminated)
				.Where(k => (k.Leader ?? k.RulingClan?.Leader) != null)
				.Where(k => mainHero == null || (k.Leader != mainHero && k.RulingClan?.Leader != mainHero))
				.OrderBy(k => GetKingdomName(k), StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch
		{
			return Enumerable.Empty<Kingdom>();
		}
	}

	private static List<Settlement> GetKingdomSettlements(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return new List<Settlement>();
		}
		try
		{
			return Settlement.All.Where(s => s != null && s.MapFaction == kingdom && (s.Town != null || s.Village != null)).ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private string BuildRecentNpcPolicyContext(string kingdomId)
	{
		List<NpcRulerPolicyRecord> recent = GetRecentPolicyRecordsInternal(kingdomId, 3);
		if (recent.Count == 0)
		{
			return "";
		}
		return Limit(string.Join("；", recent.Select(x => x.GameDate + "《" + x.PolicyName + "》" + FirstNonEmpty(x.ImpactSummary, x.PolicyContent))), 500);
	}

	private static string BuildDiplomacyNeighborSummary(Kingdom kingdom)
	{
		try
		{
			List<string> wars = new List<string>();
			foreach (Kingdom other in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (other == null || other == kingdom || other.IsEliminated)
				{
					continue;
				}
				if (kingdom.IsAtWarWith(other))
				{
					wars.Add(GetKingdomName(other));
				}
			}
			return "战争=" + (wars.Count == 0 ? "无" : string.Join("、", wars));
		}
		catch
		{
			return "未知";
		}
	}

	private static string BuildRulerTraitSummary(Hero ruler)
	{
		if (ruler == null)
		{
			return "未知";
		}
		try
		{
			List<string> parts = new List<string>
			{
				"Mercy=" + ruler.GetTraitLevel(DefaultTraits.Mercy).ToString(CultureInfo.InvariantCulture),
				"Valor=" + ruler.GetTraitLevel(DefaultTraits.Valor).ToString(CultureInfo.InvariantCulture),
				"Honor=" + ruler.GetTraitLevel(DefaultTraits.Honor).ToString(CultureInfo.InvariantCulture),
				"Generosity=" + ruler.GetTraitLevel(DefaultTraits.Generosity).ToString(CultureInfo.InvariantCulture),
				"Calculating=" + ruler.GetTraitLevel(DefaultTraits.Calculating).ToString(CultureInfo.InvariantCulture)
			};
			return string.Join(",", parts);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static string SafeReadVanillaPolicies(Kingdom kingdom)
	{
		try
		{
			string policies = string.Join("、", kingdom.ActivePolicies.Where(p => p != null).Select(p => p.Name?.ToString()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
			return string.IsNullOrWhiteSpace(policies) ? "无" : Limit(policies, 180);
		}
		catch
		{
			return "读取失败";
		}
	}

	private static int SafeKingdomStability(Kingdom kingdom)
	{
		try
		{
			return MyBehavior.GetKingdomStabilityValueForExternal(kingdom);
		}
		catch
		{
			return 50;
		}
	}

	private static bool HasAnyDailyDelta(NpcRulerPolicyEffectDto effect)
	{
		return effect?.ModuleEffects?.Any(instance => instance != null
			&& instance.LifecycleState == PolicyEffectLifecycleState.Active
			&& PolicyEffectModuleCatalog.TryGet(instance.ModuleId, out IPolicyEffectModule module)
			&& PolicyEffectModuleCatalog.IsAllowedForScope(module, PolicyEffectScopes.Kingdom)) == true;
	}

	private static bool HasValidEffectShape(NpcRulerPolicyEffectDto effect)
	{
		return effect != null
			&& effect.DurationDays > 0
			&& effect.ModuleEffects?.Any(instance => instance != null
				&& !string.IsNullOrWhiteSpace(instance.ModuleId)
				&& instance.Payload != null) == true;
	}

	private static string BuildEffectSummary(List<NpcRulerPolicyEffectDto> effects)
	{
		List<NpcRulerPolicyEffectDto> validEffects = (effects ?? new List<NpcRulerPolicyEffectDto>()).Where(x => x != null).ToList();
		if (validEffects.Count == 0)
		{
			return "";
		}
		List<string> lines = new List<string>();
		foreach (NpcRulerPolicyEffectDto effect in validEffects)
		{
			string target = FirstNonEmpty(effect.TargetKingdomName, "目标王国");
			lines.AddRange(CustomPolicyBehavior.BuildPlayerVisibleEffectLinesForExternal(
				target,
				effect.ModuleEffects,
				effect.DurationDays));
		}
		return string.Join("\n", lines);
	}

	private static void SynchronizeNpcPolicyEffectShell(NpcRulerPolicyEffectDto effect)
	{
		if (effect == null)
		{
			return;
		}
		effect.ModuleEffects ??= new List<PolicyEffectInstanceSaveData>();
		float currentDay = Math.Max(0, GetCurrentCampaignDay());
		List<PolicyEffectInstanceSaveData> live = effect.ModuleEffects.Where(instance => instance != null
			&& (instance.LifecycleState == PolicyEffectLifecycleState.Prepared
				|| (instance.LifecycleState == PolicyEffectLifecycleState.Active
					&& (instance.EndDay <= 0f || instance.EndDay > currentDay)))).ToList();
		effect.IsEnded = live.Count == 0;
		if (effect.IsEnded)
		{
			effect.RemainingDays = 0;
			return;
		}
		int canonicalRemaining = live.Where(instance => instance.EndDay > currentDay)
			.Select(instance => (int)Math.Ceiling(instance.EndDay - currentDay))
			.DefaultIfEmpty(effect.DurationDays)
			.Max();
		effect.RemainingDays = Math.Max(0, canonicalRemaining);
	}

	private void NormalizeGenerationClock(int currentDay, int currentHour)
	{
		if (_lastPolicyCheckDay > currentDay)
		{
			Log("generation-clock-reset reason=last-policy-check-in-future currentDay=" + currentDay.ToString(CultureInfo.InvariantCulture) + " lastPolicyCheckDay=" + _lastPolicyCheckDay.ToString(CultureInfo.InvariantCulture));
			_lastPolicyCheckDay = -1;
		}
		if (_lastGeneratedHour < 0 && _lastGeneratedDay >= 0)
		{
			_lastGeneratedHour = _lastGeneratedDay * 24;
		}
		if (_lastGeneratedHour > currentHour)
		{
			Log("generation-clock-reset reason=last-generated-in-future currentHour=" + currentHour.ToString(CultureInfo.InvariantCulture) + " lastGeneratedHour=" + _lastGeneratedHour.ToString(CultureInfo.InvariantCulture));
			_lastGeneratedHour = -1;
			_lastGeneratedDay = -1;
		}
		if (_lastGenerationAttemptHour > currentHour)
		{
			_lastGenerationAttemptHour = -1;
		}
		if (_lastGenerationFailureHour > currentHour)
		{
			_lastGenerationFailureHour = -1;
		}
	}

	private static bool IsCampaignSessionReady()
	{
		return Campaign.Current != null;
	}

	private static bool IsMainApiConfiguredForNpcPolicy()
	{
		try
		{
			return PolicyLlmClient.IsConfiguredForNpcPolicy(out var _);
		}
		catch
		{
			return false;
		}
	}

	private static int GetCurrentCampaignDay()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));
		}
		catch
		{
			return 0;
		}
	}

	private static int GetCurrentCampaignHour()
	{
		try
		{
			return Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToHours));
		}
		catch
		{
			return GetCurrentCampaignDay() * 24;
		}
	}

	private static string FormatCurrentCampaignDate()
	{
		try
		{
			return CampaignTime.Now.ToString();
		}
		catch
		{
			return "第" + GetCurrentCampaignDay().ToString(CultureInfo.InvariantCulture) + "天";
		}
	}

	private static string GetKingdomName(Kingdom kingdom)
	{
		return kingdom?.Name?.ToString() ?? kingdom?.StringId ?? "未知王国";
	}

	private static string FirstNonEmpty(params string[] values)
	{
		foreach (string value in values ?? Array.Empty<string>())
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}

	private static string Compact(string text)
	{
		return Regex.Replace((text ?? "").Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim(), "\\s+", " ");
	}

	private static string Limit(string text, int maxChars)
	{
		text = Compact(text);
		if (maxChars <= 0 || text.Length <= maxChars)
		{
			return text;
		}
		return text.Substring(0, maxChars).TrimEnd() + "…";
	}

	private static string NormalizeKeyPart(string text)
	{
		text = (text ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "none";
		}
		StringBuilder sb = new StringBuilder();
		foreach (char ch in text)
		{
			if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == ':')
			{
				sb.Append(ch);
			}
		}
		return sb.Length == 0 ? "x" : sb.ToString();
	}

	private static int Clamp(int value, int min, int max)
	{
		return Math.Max(min, Math.Min(max, value));
	}

	private static string FormatNumber(float value)
	{
		return value.ToString("0.##", CultureInfo.InvariantCulture);
	}

	private static string FormatSigned(float value)
	{
		return value >= 0f ? "+" + FormatNumber(value) : FormatNumber(value);
	}

	private static void Log(string message)
	{
		PolicySystemLog.WriteRuntime("Npc", message);
	}

	private sealed class NpcRulerPolicyBatchContext
	{
		public string BatchId;
		public int Day;
		public int Hour;
		public string GameDate;
		public string CompactWorldContext;
		public string SelectionDiagnostics;
		public int BatchSize;
		public int EligibleCount;
		public int ExcludedCount;
		public List<NpcRulerPolicyKingdomContext> Kingdoms = new List<NpcRulerPolicyKingdomContext>();
		public List<NpcRulerPolicySnapshotTarget> PendingTargets = new List<NpcRulerPolicySnapshotTarget>();
		public int SnapshotTargetIndex;
		public bool IsSuggestedPolicy;
		public string ProposalText;
		public string NpcReplyText;
		public string HistoryContext;
		public string ChainName;
		public List<string> CandidateModuleIds = new List<string>();
		public List<string> DetailedModuleIds = new List<string>();
		public string RoutingQueryHash;
		public bool PolicyHistorySnapshotCaptured;
		public List<NpcPolicyHistoryEntry> PolicyHistoryEntries = new List<NpcPolicyHistoryEntry>();
		public List<NpcPolicyHistoryEntry> RelatedActivePolicies = new List<NpcPolicyHistoryEntry>();
		public List<NpcPolicyHistoryEntry> RelatedHistoricalPolicies = new List<NpcPolicyHistoryEntry>();
		public PolicyHistoryRetrievalResult PolicyHistoryRetrieval;
	}

	private sealed class NpcRulerPolicySnapshotTarget
	{
		public string KingdomId;
		public string KingdomName;
		public string LastGeneratedText;
		public string ExpectedRulerHeroId;
	}

	private sealed class NpcRulerPolicyKingdomContext
	{
		public string KingdomId;
		public string KingdomName;
		public string RulerHeroId;
		public string RulerName;
		public string KnowledgeGrounding;
		public int PolicyGroundingChars;
		public int PersonalityChars;
		public int BackgroundChars;
		public string CurrentWorldFacts;
		public string KingdomStrategicProfile;
		public string PolicyMemory;
		public string EnemyPolicyMemory;
		public List<PolicyEnemyKingdomSnapshot> EnemyKingdoms = new List<PolicyEnemyKingdomSnapshot>();
		public string RecentWorldPhenomenon;
		public string ForeignDirectPressure;
		public string MechanicalFacts;
		public int PolicyMemoryCount;
		public int RecentWorldPhenomenonCount;
		public int ForeignDirectPressureCount;
		public List<NpcRulerPolicyAllowedEffectTarget> AllowedEffectTargets = new List<NpcRulerPolicyAllowedEffectTarget>();
		public PolicyTargetHandleDirectory EffectTargetDirectory;
	}

	private sealed class NpcRulerPolicyAllowedEffectTarget
	{
		public string KingdomId;
		public string KingdomName;
		public bool IsIssuer;
		public bool IsExplicitCrossKingdomTarget;
		public bool IsAtWar;
		public string PublisherClanId;
		public string AllClansHandle;
		public string OtherClansHandle;
		public string PublisherClanHandle;
		public string TerritoryOwnerClansHandle;
		public string HeroHandle;
		public string HeroSelectorId;
		public string HeroDisplayName;
		public int HeroTargetCount;
		public string PlanHandle;
		public string PlanDisplayName;
		public int PlanTargetCount;
		public PolicyTargetPlanSaveData TargetPlan;
		public PolicyTargetPlanResolution TargetPlanResolution;
		public PolicyTargetWorldSnapshot TargetPlanSnapshot;
		public List<string> MentionCandidates = new List<string>();
	}

	private sealed class NpcRulerPolicyGenerationCandidate
	{
		public Kingdom Kingdom;
		public string KingdomId;
		public string KingdomName;
		public int LastGeneratedHour = -1;
		public string LastGeneratedText;
		public string ExclusionReason;
		public bool IsEligible;
	}

	private sealed class NpcPolicyGenerationJob
	{
		public string JobId = "";
		public string BatchId = "";
		public string TriggerSource = "";
		public NpcRulerPolicyBatchContext Context;
		public string SystemPrompt = "";
		public string PromptPreview = "";
		public int Day;
		public int Hour;
		public string InFlightKey = "";
		public int Version;
		public long RuntimeGeneration;
		public PolicyApiExecutionProfile ApiProfile;
		public int MaxTokens;
		public int HardTimeoutMilliseconds;
		public long CreatedUtcTicks;
		// Runtime-only; deliberately excluded from save serialization.
		public CancellationTokenSource CancellationSource;
	}

	private sealed class NpcPolicyGenerationResult
	{
		public NpcPolicyGenerationJob Job;
		public bool Success;
		public string RawResponse = "";
		public string Error = "";
		public List<NpcRulerPolicyRecord> Records = new List<NpcRulerPolicyRecord>();
		public List<string> FailureMessages = new List<string>();
		public int ParsedCount;
		public int AttemptsUsed;
		public int DraftAttemptsUsed;
		public int EffectAttemptsUsed;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public int? RetryAfterSecondsRaw;
		public bool RetryAfterSecondsCapped;
	}

	private sealed class NpcPolicyRetryContext
	{
		public string BatchId = "";
		public string TriggerSource = "";
		public int Day;
		public int Hour;
		public string FailedReason = "";
		public int AttemptsUsed;
		public bool IsRateLimit;
		public bool IsRequestsPerMinuteLimit;
		public bool IsQuotaLimit;
		public bool IsAuthFailure;
		public int? RetryAfterSeconds;
		public List<string> FailedKingdomIds = new List<string>();
		public List<string> FailureMessages = new List<string>();
	}

	private sealed class SuggestedPolicyRetryRequest
	{
		public string PreviousBatchId = string.Empty;
		public string KingdomId = string.Empty;
		public string ExpectedRulerHeroId = string.Empty;
		public string ProposalText = string.Empty;
		public string NpcReplyText = string.Empty;
		public string HistoryContext = string.Empty;
		public string ChainName = string.Empty;
	}

	private sealed class UnifiedPolicyWeeklyAttribution
	{
		public bool IsPlayerVassalPolicy { get; set; }
		public string MaterialKind { get; set; } = string.Empty;
		public string IssuerKingdomId { get; set; } = string.Empty;
		public string IssuerKingdomName { get; set; } = string.Empty;
		public string TargetKingdomId { get; set; } = string.Empty;
		public string TargetKingdomName { get; set; } = string.Empty;
		public string PublisherLabel { get; set; } = string.Empty;
	}

	private sealed class PendingNpcPolicyCommitContext
	{
		public NpcPolicyGenerationResult GenerationResult;
		public bool IsAgendaApprovalCommit;
		public bool IsRenewalCommit;
		public int RecordIndex;
		public int SavedCount;
		public int PublicFeedbackSavedCount;
		public int ActiveEffectsCreatedCount;
		public int ActiveEffectRetryCount;
		public int ApprovalFailureCallbackRetryCount;
		public bool ApprovalEffectBundleReady;
		public bool ApprovalCoreCommitConfirmed;
		public bool ApprovalCommitFailed;
		public string ApprovalFailureReason;
		public PendingNpcPolicyCommitStage Stage;
		public string SerializedRecord;
		public AnimusForgeWorldEventInboxEntry PublicFeedbackEntry;
	}

	private enum PendingNpcPolicyCommitStage
	{
		SerializeRecord,
		StoreRecord,
		SubmitAgenda,
		UpsertPolicyEvent,
		RecordPolicyWeeklyMaterial,
		CommitPublicFeedback,
		CreateActiveEffect,
		FinalizeAgendaApprovalCommit
	}

	private sealed class NpcRulerPolicyResponse
	{
		[JsonProperty("policies")]
		public List<NpcRulerPolicyRecord> Policies { get; set; }
	}
}
