using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>Knowledge import source reading and deterministic rule/condition validation helpers.</summary>
internal static class KnowledgeImportSupport
{
	internal static IEnumerable<string> GetKnowledgeKeywordsForCompare(KnowledgeLibraryBehavior.LoreRule rule)
	{
		if (rule?.Keywords == null || rule.Keywords.Count <= 0)
		{
			yield break;
		}
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string k in rule.Keywords)
		{
			string kk = LoreCandidateRetriever.NormalizeKeywordForCompare(k);
			if (!string.IsNullOrEmpty(kk) && seen.Add(kk))
			{
				yield return kk;
			}
		}
	}

	internal static List<KnowledgeLibraryBehavior.LoreRule> LoadKnowledgeRulesFromImportDir(string importDir)
	{
		try
		{
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile = TryLoadKnowledgeRulesFromRuleFiles(importDir);
			if (knowledgeFile?.Rules != null)
			{
				foreach (KnowledgeLibraryBehavior.LoreRule rule in knowledgeFile.Rules)
				{
					string text = (rule?.Id ?? "").Trim();
					if (!string.IsNullOrEmpty(text))
					{
						rule.Id = text;
						if (hashSet.Add(text))
						{
							list.Add(rule);
						}
					}
				}
			}
			string path = Path.Combine(importDir, "knowledge", "KnowledgeRules.json");
			if (File.Exists(path))
			{
				try
				{
					string value = File.ReadAllText(path, Encoding.UTF8);
					KnowledgeLibraryBehavior.KnowledgeFile knowledgeFile2 = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.KnowledgeFile>(value);
					if (knowledgeFile2?.Rules != null)
					{
						foreach (KnowledgeLibraryBehavior.LoreRule rule2 in knowledgeFile2.Rules)
						{
							string text2 = (rule2?.Id ?? "").Trim();
							if (!string.IsNullOrEmpty(text2))
							{
								rule2.Id = text2;
								if (hashSet.Add(text2))
								{
									list.Add(rule2);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			return list;
		}
		catch
		{
			return new List<KnowledgeLibraryBehavior.LoreRule>();
		}
	}

	internal static KnowledgeLibraryBehavior.KnowledgeFile TryLoadKnowledgeRulesFromRuleFiles(string importDir)
	{
		try
		{
			if (string.IsNullOrEmpty(importDir) || !Directory.Exists(importDir))
			{
				return null;
			}
			List<string> source = new List<string>
			{
				Path.Combine(importDir, "knowledge", "rules"),
				Path.Combine(importDir, "knowledge", "single_rules"),
				Path.Combine(importDir, "knowledge")
			};
			List<KnowledgeLibraryBehavior.LoreRule> list = new List<KnowledgeLibraryBehavior.LoreRule>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in source.Where((string d) => !string.IsNullOrEmpty(d)).Distinct(StringComparer.OrdinalIgnoreCase))
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				string[] array = null;
				try
				{
					array = Directory.GetFiles(item, "*.json");
				}
				catch
				{
					array = null;
				}
				if (array == null)
				{
					continue;
				}
				string[] array2 = array;
				foreach (string path in array2)
				{
					try
					{
						string a = Path.GetFileName(path) ?? "";
						if (string.Equals(a, "AIConfig.json", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "KnowledgeRules.json", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string value = File.ReadAllText(path, Encoding.UTF8);
						if (string.IsNullOrWhiteSpace(value))
						{
							continue;
						}
						KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
						string text = (loreRule?.Id ?? "").Trim();
						if (!string.IsNullOrEmpty(text))
						{
							loreRule.Id = text;
							if (hashSet.Add(text))
							{
								list.Add(loreRule);
							}
						}
					}
					catch
					{
					}
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return new KnowledgeLibraryBehavior.KnowledgeFile
			{
				Version = 1,
				Rules = list
			};
		}
		catch
		{
			return null;
		}
	}

	internal static string FindKnowledgeRuleJsonById(string dir, string ruleId)
	{
		try
		{
			string text = (ruleId ?? "").Trim();
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
			{
				return null;
			}
			string text2 = text;
			char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text2 = text2.Replace(oldChar, '_');
			}
			text2 = (text2 ?? "").Trim();
			if (text2.Length > 120)
			{
				text2 = text2.Substring(0, 120);
			}
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = Path.Combine(dir, text2 + ".json");
				if (File.Exists(text3))
				{
					return text3;
				}
				try
				{
					string[] files = Directory.GetFiles(dir, text2 + "__*.json");
					if (files != null && files.Length != 0)
					{
						return files[0];
					}
				}
				catch
				{
				}
			}
			string[] files2 = Directory.GetFiles(dir, "*.json");
			string[] array = files2;
			foreach (string text4 in array)
			{
				try
				{
					string value = File.ReadAllText(text4, Encoding.UTF8);
					if (string.IsNullOrWhiteSpace(value))
					{
						continue;
					}
					KnowledgeLibraryBehavior.LoreRule loreRule = JsonConvert.DeserializeObject<KnowledgeLibraryBehavior.LoreRule>(value);
					if (loreRule != null)
					{
						string a = (loreRule.Id ?? "").Trim();
						if (string.Equals(a, text, StringComparison.OrdinalIgnoreCase))
						{
							return text4;
						}
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static bool TryFindDuplicateKnowledgeVariantCondition(KnowledgeLibraryBehavior.LoreRule rule, out int firstIndex, out int secondIndex)
	{
		firstIndex = -1;
		secondIndex = -1;
		try
		{
			if (rule?.Variants == null || rule.Variants.Count <= 1)
			{
				return false;
			}
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
			for (int i = 0; i < rule.Variants.Count; i++)
			{
				KnowledgeLibraryBehavior.LoreVariant loreVariant = rule.Variants[i];
				if (loreVariant == null)
				{
					continue;
				}
				string text = BuildKnowledgeWhenSignatureForImport(loreVariant.When);
				if (dictionary.TryGetValue(text, out var value))
				{
					firstIndex = value;
					secondIndex = i;
					return true;
				}
				dictionary[text] = i;
			}
		}
		catch
		{
		}
		return false;
	}

	internal static string BuildKnowledgeWhenSignatureForImport(KnowledgeLibraryBehavior.LoreWhen when)
	{
		try
		{
			string text = string.Join("|", NormalizeWhenStringListForImport(when?.HeroIds));
			string text2 = string.Join("|", NormalizeWhenStringListForImport(when?.Cultures));
			string text3 = string.Join("|", NormalizeWhenStringListForImport(when?.KingdomIds));
			string text4 = string.Join("|", NormalizeWhenStringListForImport(when?.SettlementIds));
			string text5 = string.Join("|", NormalizeWhenStringListForImport(when?.Roles));
			string text6 = (when?.IsFemale).HasValue ? (when.IsFemale.Value ? "female" : "male") : "any";
			string text7 = (when?.IsClanLeader).HasValue ? (when.IsClanLeader.Value ? "leader" : "not_leader") : "any";
			string text8 = string.Join("|", NormalizeWhenSkillMinForImport(when?.SkillMin).Select((KeyValuePair<string, int> kv) => kv.Key + ":" + kv.Value));
			if (string.IsNullOrEmpty(text) && string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(text3) && string.IsNullOrEmpty(text4) && string.IsNullOrEmpty(text5) && text6 == "any" && text7 == "any" && string.IsNullOrEmpty(text8))
			{
				return "__generic__";
			}
			return $"hero={text};culture={text2};kingdom={text3};settlement={text4};role={text5};gender={text6};clan={text7};skill={text8}";
		}
		catch
		{
			return "__generic__";
		}
	}

	internal static List<string> NormalizeWhenStringListForImport(List<string> list)
	{
		List<string> list2 = new List<string>();
		try
		{
			if (list != null)
			{
				foreach (string item in list)
				{
					string text = (item ?? "").Trim();
					if (!string.IsNullOrEmpty(text) && !list2.Any((string x) => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)))
					{
						list2.Add(text);
					}
				}
			}
			list2.Sort(StringComparer.OrdinalIgnoreCase);
		}
		catch
		{
		}
		return list2;
	}

	internal static List<KeyValuePair<string, int>> NormalizeWhenSkillMinForImport(Dictionary<string, int> skillMin)
	{
		List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>();
		try
		{
			if (skillMin != null)
			{
				foreach (KeyValuePair<string, int> item in skillMin)
				{
					string text = (item.Key ?? "").Trim();
					if (!string.IsNullOrEmpty(text) && item.Value >= 0)
					{
						list.Add(new KeyValuePair<string, int>(text, item.Value));
					}
				}
			}
			list = list.OrderBy((KeyValuePair<string, int> x) => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
		}
		catch
		{
		}
		return list;
	}
}
