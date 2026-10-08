using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.Extensions;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;

namespace AnimusForge.Refactor.Adapters;

// Only invoked by the owning game-thread capture phase. No live Hero escapes
// into the detached generation attempt.
internal static class PersonaGenerationFactCaptureAdapter
{
	private const string NpcSkillLevelReference = "技能水平参考: 0-49平庸，50-99一般，100-149良好，150-199极佳，200-274大师，275以上传奇。";
	internal static string GetHeroEncyclopediaBackgroundForPersonaPrompt(Hero hero, int maxLength = 1000)
	{
		if (hero == null)
		{
			return "";
		}
		string text = "";
		try
		{
			if (!TextObject.IsNullOrEmpty(hero.EncyclopediaText))
			{
				text = hero.EncyclopediaText.ToString();
			}
		}
		catch
		{
			text = "";
		}
		text = NpcPersonaTextRules.NormalizePersonaPromptSourceText(text, maxLength);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "";
	}
	internal static string GetClanEncyclopediaBackgroundForPersonaPrompt(Clan clan, int maxLength = 1000)
	{
		if (clan == null)
		{
			return "";
		}
		try
		{
			return NpcPersonaTextRules.NormalizePersonaPromptSourceText(clan.EncyclopediaText?.ToString() ?? "", maxLength);
		}
		catch
		{
			return "";
		}
	}
	internal static Kingdom ResolveKingdomForPersonaPrompt(Hero hero)
	{
		if (hero == null)
		{
			return null;
		}
		try
		{
			if (hero.Clan?.Kingdom != null)
			{
				return hero.Clan.Kingdom;
			}
		}
		catch
		{
		}
		try
		{
			return hero.MapFaction as Kingdom;
		}
		catch
		{
			return null;
		}
	}
	internal static string GetKingdomEncyclopediaBackgroundForPersonaPrompt(Kingdom kingdom, int maxLength = 1200)
	{
		if (kingdom == null)
		{
			return "";
		}
		try
		{
			return NpcPersonaTextRules.NormalizePersonaPromptSourceText(kingdom.EncyclopediaText?.ToString() ?? "", maxLength);
		}
		catch
		{
			return "";
		}
	}
	internal static string BuildHeroBasicBackgroundForPersonaPrompt(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		string heroDisplayName = GetHeroDisplayName(hero);
		if (!string.IsNullOrWhiteSpace(heroDisplayName))
		{
			list.Add("姓名=" + heroDisplayName);
		}
		try
		{
			list.Add("身份=" + PersonaIdentityPromptCaptureAdapter.CaptureHeroIdentityTitle(hero));
		}
		catch
		{
		}
		try
		{
			string heroCultureNameForPrompt = PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(hero);
			if (!string.IsNullOrWhiteSpace(heroCultureNameForPrompt))
			{
				list.Add("文化=" + heroCultureNameForPrompt);
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				list.Add("家族=" + GetClanDisplayName(hero.Clan));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan?.Kingdom != null)
			{
				list.Add("势力=" + GetKingdomDisplayName(hero.Clan.Kingdom, "某个势力"));
			}
		}
		catch
		{
		}
		return string.Join("；", list);
	}
	internal static string BuildClanOverviewForPersonaPrompt(Clan clan)
	{
		if (clan == null)
		{
			return "无家族";
		}
		List<string> list = new List<string>();
		list.Add("家族名=" + GetClanDisplayName(clan));
		try
		{
			list.Add("家族等级=" + Math.Max(0, clan.Tier));
		}
		catch
		{
		}
		try
		{
			Hero leader = clan.Leader;
			if (leader != null)
			{
				list.Add("族长=" + GetHeroDisplayName(leader));
			}
		}
		catch
		{
		}
		try
		{
			if (clan.Kingdom != null)
			{
				string text = GetKingdomDisplayName(clan.Kingdom, "某个势力");
				if (clan.IsUnderMercenaryService)
				{
					text += "（佣兵服务）";
				}
				list.Add("所属势力=" + text);
			}
			else
			{
				list.Add("所属势力=无（独立）");
			}
		}
		catch
		{
		}
		try
		{
			Settlement settlement = clan.HomeSettlement ?? clan.InitialHomeSettlement;
			if (settlement != null)
			{
				list.Add("家族根据地=" + GetSettlementDisplayName(settlement));
			}
		}
		catch
		{
		}
		try
		{
			List<string> list2 = clan.Settlements?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).Select(GetSettlementDisplayName).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList() ?? new List<string>();
			if (list2.Count > 0)
			{
				list.Add("主要封地=" + string.Join("、", list2));
			}
		}
		catch
		{
		}
		try
		{
			if (clan.IsEliminated)
			{
				list.Add("状态=已灭亡或不可用");
			}
		}
		catch
		{
		}
		return string.Join("；", list);
	}
	internal static string BuildHeroFactsForPersonaGeneration(Hero hero)
	{
		if (hero == null)
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine($"姓名: {hero.Name}");
		stringBuilder.AppendLine($"年龄: {hero.Age:F0}");
		try
		{
			stringBuilder.AppendLine("性别: " + (hero.IsFemale ? "女" : "男"));
		}
		catch
		{
		}
		try
		{
			if (hero.Culture != null)
			{
				string heroCultureNameForPrompt = PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(hero);
				if (!string.IsNullOrWhiteSpace(heroCultureNameForPrompt))
				{
					stringBuilder.AppendLine("文化: " + heroCultureNameForPrompt);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				string clanDisplayName = GetClanDisplayName(hero.Clan);
				if (!string.IsNullOrWhiteSpace(clanDisplayName))
				{
					stringBuilder.AppendLine("家族: " + clanDisplayName);
				}
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null && hero.Clan.Kingdom != null)
			{
				string kingdomDisplayName = GetKingdomDisplayName(hero.Clan.Kingdom, "某个势力");
				if (!string.IsNullOrWhiteSpace(kingdomDisplayName))
				{
					stringBuilder.AppendLine("势力: " + kingdomDisplayName);
				}
			}
		}
		catch
		{
		}
		try
		{
			Kingdom kingdom = hero.Clan?.Kingdom;
			if (kingdom != null && kingdom.Leader != null)
			{
				stringBuilder.AppendLine("势力领袖: " + (kingdom.Leader.Name?.ToString() ?? "未知"));
				if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out Kingdom ruledKingdom) && ruledKingdom == kingdom)
				{
					stringBuilder.AppendLine("效忠: 你本人即该势力领袖");
				}
				else
				{
					stringBuilder.AppendLine($"效忠: {kingdom.Leader.Name}");
				}
			}
		}
		catch
		{
		}
		string text = "英雄";
		try
		{
			if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out Kingdom ruledKingdom))
			{
				text = "统治者/派系领袖";
			}
			else if (hero.Clan?.Leader == hero)
			{
				text = "家族族长";
			}
			else if (hero.IsLord)
			{
				text = "领主";
			}
			else if (hero.IsWanderer)
			{
				text = "流浪者";
			}
			else if (hero.IsNotable)
			{
				text = "要人";
			}
		}
		catch
		{
		}
		stringBuilder.AppendLine("身份: " + text);
		try
		{
			stringBuilder.AppendLine($"特质: Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
		}
		catch
		{
		}
		try
		{
			var list = (from x in (from sk in Skills.All
					select new
					{
						Skill = sk,
						Value = hero.GetSkillValue(sk)
					} into x
					orderby x.Value descending, x.Skill.StringId
					select x).Take(8)
				where x.Value > 0
				select x).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("技能(最高8项): " + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
				stringBuilder.AppendLine(NpcSkillLevelReference);
			}
		}
		catch
		{
		}
		try
		{
			stringBuilder.AppendLine("家族成员: " + WorldEntityRetrievalService.FormatHeroRelatives(hero, int.MaxValue));
		}
		catch
		{
		}
		try
		{
			string heroEncyclopediaBackground = GetHeroEncyclopediaBackgroundForPersonaPrompt(hero, 1200);
			stringBuilder.AppendLine("人物百科背景: " + (string.IsNullOrWhiteSpace(heroEncyclopediaBackground) ? "无可用人物百科背景" : heroEncyclopediaBackground));
		}
		catch
		{
		}
		try
		{
			string clanOverviewBackground = BuildClanOverviewForPersonaPrompt(hero.Clan);
			if (!string.IsNullOrWhiteSpace(clanOverviewBackground))
			{
				stringBuilder.AppendLine("家族背景: " + clanOverviewBackground);
			}
		}
		catch
		{
		}
		try
		{
			string clanEncyclopediaBackground = GetClanEncyclopediaBackgroundForPersonaPrompt(hero.Clan, 1200);
			stringBuilder.AppendLine("所在家族百科背景: " + (string.IsNullOrWhiteSpace(clanEncyclopediaBackground) ? "无可用家族百科背景" : clanEncyclopediaBackground));
		}
		catch
		{
		}
		try
		{
			Kingdom personaKingdom = ResolveKingdomForPersonaPrompt(hero);
			string kingdomEncyclopediaBackground = GetKingdomEncyclopediaBackgroundForPersonaPrompt(personaKingdom, 1400);
			if (personaKingdom != null)
			{
				string kingdomName = GetKingdomDisplayName(personaKingdom, "某个王国");
				stringBuilder.AppendLine("王国百科背景: " + kingdomName + "：" + (string.IsNullOrWhiteSpace(kingdomEncyclopediaBackground) ? "无可用王国百科背景" : kingdomEncyclopediaBackground));
			}
			else
			{
				stringBuilder.AppendLine("王国百科背景: 无可用王国百科背景");
			}
		}
		catch
		{
		}
		try
		{
			Hero clanLeader = hero.Clan?.Leader;
			if (clanLeader == hero)
			{
				stringBuilder.AppendLine("家族族长背景: 族长即本人，参见人物百科背景。");
			}
			else if (clanLeader != null)
			{
				string leaderBackground = GetHeroEncyclopediaBackgroundForPersonaPrompt(clanLeader, 1200);
				if (string.IsNullOrWhiteSpace(leaderBackground))
				{
					leaderBackground = BuildHeroBasicBackgroundForPersonaPrompt(clanLeader);
				}
				stringBuilder.AppendLine("家族族长背景: " + GetHeroDisplayName(clanLeader) + "：" + (string.IsNullOrWhiteSpace(leaderBackground) ? "无可用族长背景" : leaderBackground));
			}
			else
			{
				stringBuilder.AppendLine("家族族长背景: 无可用族长背景");
			}
		}
		catch
		{
		}
		return stringBuilder.ToString().Trim();
	}
	internal static string BuildPromotedNonHeroCompanionFactsForPersonaGeneration(Hero hero, string personalName, string originalFullName, string originalTroopName, string originalTroopId, string cultureName, string sceneLabel, string joinEventFact, string equipmentSummary)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string name = string.IsNullOrWhiteSpace(personalName) ? (hero?.Name?.ToString() ?? "新家族成员") : personalName.Trim();
		string fullName = string.IsNullOrWhiteSpace(originalFullName) ? name : originalFullName.Trim();
		string troopName = string.IsNullOrWhiteSpace(originalTroopName) ? "非英雄NPC" : originalTroopName.Trim();
		string troopId = (originalTroopId ?? "").Trim();
		string culture = string.IsNullOrWhiteSpace(cultureName) ? "未知文化" : cultureName.Trim();
		string scene = string.IsNullOrWhiteSpace(sceneLabel) ? "当前场景" : sceneLabel.Trim();
		string joinFact = string.IsNullOrWhiteSpace(joinEventFact) ? (name + "同意追随玩家，成为玩家家族成员并加入玩家队伍。") : joinEventFact.Trim();
		string equipment = string.IsNullOrWhiteSpace(equipmentSummary) ? "（无装备）" : equipmentSummary.Trim();
		stringBuilder.AppendLine("升格来源: 该角色原本是非 Hero NPC/士兵，现在因为当前加入事件才被创建为玩家家族 Hero。");
		stringBuilder.AppendLine("出身约束: 不要把升格后加入玩家队伍、玩家家族或玩家阵营理解为其原生家族、贵族血统、出生背景或旧效忠对象；若没有对话事实支持，背景不得写成其本来就出身于玩家家族/玩家势力。");
		stringBuilder.AppendLine("个人名: " + name);
		stringBuilder.AppendLine("原完整称呼: " + fullName);
		stringBuilder.AppendLine("原兵种/职业: " + troopName + (string.IsNullOrWhiteSpace(troopId) ? "" : (" (StringId=" + troopId + ")")));
		stringBuilder.AppendLine("原文化: " + culture);
		stringBuilder.AppendLine("升格场景: " + scene);
		stringBuilder.AppendLine("加入事件: " + joinFact);
		stringBuilder.AppendLine("装备: " + equipment);
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine($"年龄: {hero.Age:F0}");
			}
		}
		catch
		{
		}
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine("性别: " + (hero.IsFemale ? "女" : "男"));
			}
		}
		catch
		{
		}
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine($"特质: Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
			}
		}
		catch
		{
		}
		try
		{
			if (hero != null)
			{
				var list = (from x in (from sk in Skills.All
						select new
						{
							Skill = sk,
							Value = hero.GetSkillValue(sk)
						} into x
						orderby x.Value descending, x.Skill.StringId
						select x).Take(8)
					where x.Value > 0
					select x).ToList();
				if (list.Count > 0)
				{
					stringBuilder.AppendLine("技能(最高8项): " + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
					stringBuilder.AppendLine(NpcSkillLevelReference);
				}
			}
		}
		catch
		{
		}
		return stringBuilder.ToString().Trim();
	}
}
