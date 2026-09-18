using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AnimusForge;

internal static class PromptRuleRegistry
{
	private static string NormalizeSemanticText(string text)
	{
		string value = (text ?? "").Trim();
		return string.IsNullOrWhiteSpace(value) ? "" : value.Replace("\r", " ").Replace("\n", " ").Trim();
	}

	private static List<string> NormalizeTriggerKeywordList(List<string> source, int minLen = 2, int maxLen = 8)
	{
		List<string> list = new List<string>();
		try
		{
			if (source == null || source.Count <= 0)
			{
				return list;
			}
			if (minLen < 1)
			{
				minLen = 1;
			}
			if (maxLen < minLen)
			{
				maxLen = minLen;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < source.Count; i++)
			{
				string text = NormalizeSemanticText(source[i]);
				if (!string.IsNullOrWhiteSpace(text) && text.Length >= minLen)
				{
					if (text.Length > maxLen)
					{
						text = text.Substring(0, maxLen);
					}
					if (hashSet.Add(text))
					{
						list.Add(text);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	internal static Dictionary<string, string> NormalizeTemplateMap(Dictionary<string, string> source, int maxKeyLen = 80)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			if (source == null || source.Count <= 0)
			{
				return dictionary;
			}
			foreach (KeyValuePair<string, string> item in source)
			{
				string text = NormalizeSemanticText(item.Key);
				string text2 = (item.Value ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
				{
					continue;
				}
				if (maxKeyLen > 0 && text.Length > maxKeyLen)
				{
					text = text.Substring(0, maxKeyLen);
				}
				dictionary[text.ToLowerInvariant()] = text2;
			}
		}
		catch
		{
		}
		return dictionary;
	}

	internal static string NormalizeRuleCode(string code, string id, string label = null)
	{
		string text = (code ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			string text2 = (id ?? "").Trim().ToLowerInvariant();
			text = text2 switch
			{
				"duel" => "DUEL",
				"reward" => "TRADE",
				"loan" => "DEBT",
				"surroundings" => "NEARBY",
				"kingdom_service" => "KINGDOM",
				"lords_hall_access" => "PASSAGE",
				"marriage" => "MARRIAGE",
				"scene_mechanism_actions" => "SCENE_MOVE",
				"party_transfer" => "PARTY_TRANSFER",
				"vanilla_issue" => "ISSUE",
				"npc_major_actions" => "NPC_MAJOR",
				"encounter_release_player" => "MEETING_RELEASE",
				"noble_deference" => "NOBLE_PRESSURE",
				"kingdom_agenda" => "KINGDOM_AGENDA",
				"diplomacy" => "DIPLOMACY",
				_ => ""
			};
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			text = (label ?? id ?? "RULE").Trim();
		}
		text = Regex.Replace(text.ToUpperInvariant(), "[^A-Z0-9_]+", "_").Trim('_');
		return string.IsNullOrWhiteSpace(text) ? "RULE" : text;
	}

	private static GuardrailRulePromptConfig BuildLegacyRulePrompt(string id, bool enabled, string instruction, List<string> triggerKeywords, string group, int priority, int topicNumber, string topicLabel, string code = "", string preprocessExcludedInstruction = "")
	{
		return new GuardrailRulePromptConfig
		{
			Id = (id ?? "").Trim().ToLowerInvariant(),
			IsEnabled = enabled,
			TopicNumber = topicNumber,
			TopicLabel = (topicLabel ?? "").Trim(),
			Code = NormalizeRuleCode(code, id, topicLabel),
			Instruction = (instruction ?? ""),
			PreprocessExcludedInstruction = (preprocessExcludedInstruction ?? ""),
			TriggerKeywords = NormalizeTriggerKeywordList(triggerKeywords),
			Group = (group ?? "").Trim(),
			Priority = priority
		};
	}

	private static GuardrailRulePromptConfig NormalizeCustomRulePrompt(GuardrailRulePromptConfig src, int autoIndex)
	{
		try
		{
			if (src == null)
			{
				return null;
			}
			string text = (src.Id ?? "").Trim().ToLowerInvariant();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "rule_" + autoIndex;
			}
			return new GuardrailRulePromptConfig
			{
				Id = text,
				IsEnabled = src.IsEnabled,
				Group = (src.Group ?? "").Trim(),
				Priority = src.Priority,
				TopicNumber = src.TopicNumber,
				TopicLabel = (src.TopicLabel ?? "").Trim(),
				Code = NormalizeRuleCode(src.Code, text, src.TopicLabel),
				Instruction = (src.Instruction ?? ""),
				NonHeroInstruction = (src.NonHeroInstruction ?? ""),
				PreprocessExcludedInstruction = (src.PreprocessExcludedInstruction ?? ""),
				PostprocessRules = ((src.PostprocessRules != null) ? src.PostprocessRules.Where((PostprocessRuleEntry x) => x != null && !string.IsNullOrWhiteSpace((x.Tag ?? "").Trim())).Select((PostprocessRuleEntry x) => new PostprocessRuleEntry
				{
					Tag = (x.Tag ?? "").Trim(),
					Description = (x.Description ?? "").Trim(),
					SingleFramedNpcDescription = (x.SingleFramedNpcDescription ?? "").Trim()
				}).ToList() : new List<PostprocessRuleEntry>()),
				TriggerKeywords = NormalizeTriggerKeywordList(src.TriggerKeywords),
				RuntimeInstructionTemplates = NormalizeTemplateMap(src.RuntimeInstructionTemplates),
				RuntimeConstraintTemplates = NormalizeTemplateMap(src.RuntimeConstraintTemplates)
			};
		}
		catch
		{
			return null;
		}
	}

	internal static Dictionary<string, GuardrailRulePromptConfig> Build(GuardrailConfigModel config)
	{
		Dictionary<string, GuardrailRulePromptConfig> map = new Dictionary<string, GuardrailRulePromptConfig>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string duelRegistryInstruction = (config?.Duel?.TriggerInstruction ?? "").Trim();
			if (string.IsNullOrWhiteSpace(duelRegistryInstruction))
			{
				duelRegistryInstruction = (config?.Duel?.DialogueInstruction ?? "").Trim();
			}
			upsert(BuildLegacyRulePrompt("duel", config?.Duel?.IsEnabled ?? true, duelRegistryInstruction, config?.Duel?.AcceptKeywords ?? new List<string>(), "combat", 90, config?.Duel?.TopicNumber ?? 0, config?.Duel?.TopicLabel ?? "", config?.Duel?.Code ?? "", config?.Duel?.PreprocessExcludedInstruction ?? ""));
			upsert(BuildLegacyRulePrompt("reward", config?.Reward?.IsEnabled ?? true, config?.Reward?.Instruction ?? "", config?.Reward?.TriggerKeywords ?? new List<string>(), "trade", 80, config?.Reward?.TopicNumber ?? 0, config?.Reward?.TopicLabel ?? "", config?.Reward?.Code ?? "", config?.Reward?.PreprocessExcludedInstruction ?? ""));
			if (config?.Loan != null)
			{
				upsert(BuildLegacyRulePrompt("loan", config.Loan.IsEnabled, config.Loan.Instruction ?? "", config.Loan.TriggerKeywords ?? new List<string>(), "finance", 85, config.Loan.TopicNumber, config.Loan.TopicLabel ?? "", config.Loan.Code ?? "", config.Loan.PreprocessExcludedInstruction ?? ""));
			}
			upsert(BuildLegacyRulePrompt("surroundings", config?.Surroundings?.IsEnabled ?? true, config?.Surroundings?.Instruction ?? "", config?.Surroundings?.TriggerKeywords ?? new List<string>(), "world", 70, config?.Surroundings?.TopicNumber ?? 0, config?.Surroundings?.TopicLabel ?? "", config?.Surroundings?.Code ?? "", config?.Surroundings?.PreprocessExcludedInstruction ?? ""));
			if (config?.RulePrompts != null && config.RulePrompts.Count > 0)
			{
				for (int i = 0; i < config.RulePrompts.Count; i++)
				{
					GuardrailRulePromptConfig rule = NormalizeCustomRulePrompt(config.RulePrompts[i], i + 1);
					upsert(rule);
				}
			}
		}
		catch
		{
		}
		return map;
		void upsert(GuardrailRulePromptConfig guardrailRulePromptConfig)
		{
			if (guardrailRulePromptConfig != null)
			{
				string text = (guardrailRulePromptConfig.Id ?? "").Trim().ToLowerInvariant();
				if (!string.IsNullOrWhiteSpace(text))
				{
					guardrailRulePromptConfig.Id = text;
					guardrailRulePromptConfig.Code = NormalizeRuleCode(guardrailRulePromptConfig.Code, text, guardrailRulePromptConfig.TopicLabel);
					map[text] = guardrailRulePromptConfig;
				}
			}
		}
	}

}
