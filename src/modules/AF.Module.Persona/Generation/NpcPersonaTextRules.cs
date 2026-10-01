using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using AnimusForge.Refactor.Runtime;
using static AnimusForge.Refactor.Runtime.JsonResponseTextCodec;

namespace AnimusForge;

internal sealed class NpcPersonaPrompt
{
    internal string System { get; }
    internal string User { get; }
    internal NpcPersonaPrompt(string system,string user) { System=system;User=user; }
}

// Text/prompt/parse policy consumes detached facts; lease/cooldown and save ownership are unchanged.
internal static class NpcPersonaTextRules
{
internal static string NormalizePersonaPromptSourceText(string text, int maxLength = 1200)
	{
		string text2 = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		while (text2.Contains("  "))
		{
			text2 = text2.Replace("  ", " ");
		}
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		if (maxLength > 0 && text2.Length > maxLength)
		{
			text2 = text2.Substring(0, maxLength).TrimEnd();
		}
		return text2.Trim();
	}
internal static bool TryParsePersonaJson(string text, out string personality, out string background)
	{
		personality = "";
		background = "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string text2 = StripJsonResponseEnvelope(text);
		List<string> candidates = ExtractJsonObjectPayloads(text2);
		if (candidates.Count == 0)
		{
			candidates.Add(text2);
		}
		foreach (string candidate in candidates)
		{
			try
			{
				JObject jObject = JObject.Parse(candidate);
				string parsedPersonality = GetJsonStringIgnoreCase(jObject, "personality", "profile", "description");
				string parsedBackground = GetJsonStringIgnoreCase(jObject, "background");
				if (!string.IsNullOrWhiteSpace(parsedPersonality) || !string.IsNullOrWhiteSpace(parsedBackground))
				{
					personality = parsedPersonality;
					background = parsedBackground;
					return true;
				}
			}
			catch
			{
			}
		}
		personality = ExtractLoosePersonaJsonField(text2, "personality", "profile", "description");
		background = ExtractLoosePersonaJsonField(text2, "background");
		return !string.IsNullOrWhiteSpace(personality) || !string.IsNullOrWhiteSpace(background);
	}
internal static string ExtractLoosePersonaJsonField(string text, params string[] fieldNames)
	{
		foreach (string fieldName in fieldNames ?? new string[0])
		{
			if (TryExtractLooseJsonStringProperty(text, fieldName, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
		}
		return "";
	}
internal static string AppendNpcPersonaGenerationRequirementsToSystemPrompt(string systemPrompt, string requirements)
	{
		string text = (systemPrompt ?? "").Trim();
		string text2 = (requirements ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		string text3 = "【玩家自定义生成要求】\n" + text2;
		if (string.IsNullOrWhiteSpace(text))
		{
			return text3;
		}
		return text + "\n\n" + text3;
	}
internal static string NormalizePromotedSkillKey(string key)
	{
		string text = (key ?? "").Trim().ToLowerInvariant();
		text = Regex.Replace(text, "[^a-z0-9]", "");
		return text;
	}
    internal static NpcPersonaPrompt BuildNative(string facts, string personality, string background, bool overwriteExisting, string requirements)
    {
string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。没有额外要求时，personality 和 background 各约 300 个中文字符；如果玩家自定义生成要求指定了篇幅、详略或文风，则以玩家自定义生成要求为准。每个字段都必须以完整句子结束，不要在半句话处停止。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系或身份；若事实中提供了势力/效忠信息，必须保持一致，禁止声称效忠于其他统治者或属于其他势力。";
			sys = AppendNpcPersonaGenerationRequirementsToSystemPrompt(sys, requirements);

			string user = "请基于以下信息生成该 NPC 的【个性】与【历史背景】。必须综合“人物百科背景”“家族背景”“所在家族百科背景”“王国百科背景”“家族族长背景”；这些素材是事实来源，不要复制成百科原文。\n" + facts;
			if (overwriteExisting)
			{
				string oldPersonality = NormalizePersonaPromptSourceText(personality, 500);
				string oldBackground = NormalizePersonaPromptSourceText(background, 500);
				user += "\n这是重新生成人设请求：请生成一版不同但仍符合事实的人设，不要照搬旧文本。"
					+ "\n旧个性（仅用于避重）：" + (string.IsNullOrWhiteSpace(oldPersonality) ? "无" : oldPersonality)
					+ "\n旧背景（仅用于避重）：" + (string.IsNullOrWhiteSpace(oldBackground) ? "无" : oldBackground);
			}
        return new NpcPersonaPrompt(sys,user);
    }
    internal static NpcPersonaPrompt BuildPromoted(string name,string fullName,string troopName,string troopId,string culture,string scene,string joinFact,string history,string equipment,string facts,string requirements)
    {
string sys = "你是《骑马与砍杀2：霸主》NPC的人设生成器。你只输出严格 JSON，不要输出任何额外文字。JSON 仅包含两个字段：personality 和 background。没有额外要求时，personality 和 background 各约 300 个中文字符；如果玩家自定义生成要求指定了篇幅、详略或文风，则以玩家自定义生成要求为准。每个字段都必须以完整句子结束，不要在半句话处停止。内容必须符合提供的事实，不要杜撰与事实冲突的家族关系、身份或势力。";
			sys = AppendNpcPersonaGenerationRequirementsToSystemPrompt(sys, requirements);
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine("请基于信息生成该 NPC 升格为玩家家族成员后的【个性】与【历史背景】。");
			userSb.AppendLine("写作风格沿用首次见到 Hero NPC 的人设格式：具体、可用于后续对话，不要写成系统说明。");
			userSb.AppendLine("背景必须解释他/她为何愿意追随玩家，并吸收加入前对话中的关系、承诺、冲突、交易或共同经历；如果历史里没有相关内容，明确写成谨慎而合理的动机，不要凭空创造重大事件。");
			userSb.AppendLine("个人名: " + name);
			userSb.AppendLine("原完整称呼: " + fullName);
			userSb.AppendLine("原兵种/职业: " + troopName + (string.IsNullOrWhiteSpace(troopId) ? "" : (" (StringId=" + troopId + ")")));
			userSb.AppendLine("文化: " + culture);
			userSb.AppendLine("当前场景: " + scene);
			userSb.AppendLine("加入事件: " + joinFact);
			userSb.AppendLine("升格后人物事实（只使用原非 Hero NPC/士兵事实、加入事件和对话历史；不要把玩家家族身份写成原生出身）:");
			userSb.AppendLine(facts);
			userSb.AppendLine("加入前该 NPC 与玩家的全部可用对话历史:");
			userSb.AppendLine(history);
        return new NpcPersonaPrompt(sys,userSb.ToString().Trim());
    }
    internal static NpcPersonaPrompt BuildSkills(string personalName,string originalTroopName,string cultureName,string equipmentSummary,string skillSource,string personality,string background)
    {
string sys = "你是《骑马与砍杀2：霸主》的家族成员技能生成器。只输出严格 JSON，格式为 {\"skills\":{\"OneHanded\":数值,...}}。只使用给出的技能ID，数值为 0 到 330 的整数。";
			StringBuilder userSb = new StringBuilder();
			userSb.AppendLine("按原兵种、装备、文化与人设，为这个刚升格的玩家家族 Hero 生成合理技能。不要过强；主要强化实际武器、骑术/跑动和少量战术/领导。");
			userSb.AppendLine("可用技能ID: OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Crafting, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering");
			userSb.AppendLine("个人名: " + personalName);
			userSb.AppendLine("原兵种: " + originalTroopName);
			userSb.AppendLine("文化: " + cultureName);
			userSb.AppendLine("装备: " + equipmentSummary);
			userSb.AppendLine("当前基础技能(来自原兵种模板，生成失败时保留这些值): " + skillSource);
			userSb.AppendLine("人设摘要: " + TrimToMaxChars((personality + " " + background).Trim(), 700));
        return new NpcPersonaPrompt(sys,userSb.ToString().Trim());
    }
    internal static string BuildFallbackPersonality(string name,string troopName,string joinFact,string fullName,string scene,string history) => TrimToMaxChars(name + "曾是" + troopName + "，保留着原本职业养成的警觉、纪律和生存本能；面对玩家时，他会把加入前的承诺与利害放在心里，既愿意服从队伍安排，也会在危险或失信时变得谨慎。", 420);
    internal static string BuildFallbackBackground(string name,string troopName,string joinFact,string fullName,string scene,string history) => TrimToMaxChars(joinFact + "他原本以“" + fullName + "”的身份在" + scene + "活动，加入后只以个人名“" + name + "”示人。此前对话中可用的经历会成为他追随玩家的理由：" + TrimToMaxChars(history, 240), 520);
    internal static bool TryParseSkills(string raw, IReadOnlyCollection<string> knownKeys, out IReadOnlyList<KeyValuePair<string,int>> values)
    {
        values=Array.Empty<KeyValuePair<string,int>>();
        if(string.IsNullOrWhiteSpace(raw)) return false;
        try
        {
string text = raw.Trim();
			if (text.StartsWith("```", StringComparison.Ordinal))
			{
				int firstLine = text.IndexOf('\n');
				if (firstLine >= 0)
				{
					text = text.Substring(firstLine + 1).Trim();
				}
				int fence = text.LastIndexOf("```", StringComparison.Ordinal);
				if (fence >= 0)
				{
					text = text.Substring(0, fence).Trim();
				}
			}
			int start = text.IndexOf('{');
			int end = text.LastIndexOf('}');
			if (start >= 0 && end > start)
			{
				text = text.Substring(start, end - start + 1);
			}
			JObject root = JObject.Parse(text);
			JObject skillsObj = root["skills"] as JObject ?? root["Skills"] as JObject ?? root;
            var allowed=new HashSet<string>(knownKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
            var parsed=new List<KeyValuePair<string,int>>();
            foreach(JProperty prop in skillsObj.Properties())
            {
                string key=NormalizePromotedSkillKey(prop.Name);
                if (!allowed.Contains(key) || !int.TryParse(prop.Value?.ToString() ?? "",out var value)) continue;
                parsed.Add(new KeyValuePair<string,int>(key, Math.Max(0,Math.Min(330,value))));
            }
            values=parsed;
            return parsed.Count>0;
        }
        catch { return false; }
    }
}
