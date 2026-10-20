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
	private static string NpcSkillLevelReference => AdapterPromptCaptureTexts.PcapText("persona_generation_1");
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
			list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_2") + heroDisplayName);
		}
		try
		{
			list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_3") + PersonaIdentityPromptCaptureAdapter.CaptureHeroIdentityTitle(hero));
		}
		catch
		{
		}
		try
		{
			string heroCultureNameForPrompt = PersonaIdentityPromptCaptureAdapter.GetHeroCultureNameForPrompt(hero);
			if (!string.IsNullOrWhiteSpace(heroCultureNameForPrompt))
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_4") + heroCultureNameForPrompt);
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan != null)
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_5") + GetClanDisplayName(hero.Clan));
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Clan?.Kingdom != null)
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_6") + GetKingdomDisplayName(hero.Clan.Kingdom, AdapterPromptCaptureTexts.PcapText("persona_generation_7")));
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
			return AdapterPromptCaptureTexts.PcapText("persona_generation_8");
		}
		List<string> list = new List<string>();
		list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_9") + GetClanDisplayName(clan));
		try
		{
			list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_10") + Math.Max(0, clan.Tier));
		}
		catch
		{
		}
		try
		{
			Hero leader = clan.Leader;
			if (leader != null)
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_11") + GetHeroDisplayName(leader));
			}
		}
		catch
		{
		}
		try
		{
			if (clan.Kingdom != null)
			{
				string text = GetKingdomDisplayName(clan.Kingdom, AdapterPromptCaptureTexts.PcapText("persona_generation_7"));
				if (clan.IsUnderMercenaryService)
				{
					text += AdapterPromptCaptureTexts.PcapText("persona_generation_12");
				}
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_13") + text);
			}
			else
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_14"));
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
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_15") + GetSettlementDisplayName(settlement));
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
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_16") + string.Join("、", list2));
			}
		}
		catch
		{
		}
		try
		{
			if (clan.IsEliminated)
			{
				list.Add(AdapterPromptCaptureTexts.PcapText("persona_generation_17"));
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
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapFormat("persona_generation_name", "Name: {NAME}", ("NAME", hero.Name)));
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapFormat("persona_generation_age", "Age: {AGE}", ("AGE", hero.Age.ToString("F0"))));
		try
		{
			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_18") + (hero.IsFemale ? AdapterPromptCaptureTexts.PcapText("persona_generation_19") : AdapterPromptCaptureTexts.PcapText("persona_generation_20")));
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
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_21") + heroCultureNameForPrompt);
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
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_22") + clanDisplayName);
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
				string kingdomDisplayName = GetKingdomDisplayName(hero.Clan.Kingdom, AdapterPromptCaptureTexts.PcapText("persona_generation_7"));
				if (!string.IsNullOrWhiteSpace(kingdomDisplayName))
				{
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_23") + kingdomDisplayName);
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
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_24") + (kingdom.Leader.Name?.ToString() ?? AdapterPromptCaptureTexts.PcapText("persona_generation_25")));
				if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out Kingdom ruledKingdom) && ruledKingdom == kingdom)
				{
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_26"));
				}
				else
				{
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapFormat("persona_generation_allegiance", "Allegiance: {NAME}", ("NAME", kingdom.Leader.Name)));
				}
			}
		}
		catch
		{
		}
		string text = AdapterPromptCaptureTexts.PcapText("persona_generation_27");
		try
		{
			if (PersonaIdentityPromptCaptureAdapter.TryResolveActiveKingdomRuledByHeroForPrompt(hero, out Kingdom ruledKingdom))
			{
				text = AdapterPromptCaptureTexts.PcapText("persona_generation_28");
			}
			else if (hero.Clan?.Leader == hero)
			{
				text = AdapterPromptCaptureTexts.PcapText("persona_generation_29");
			}
			else if (hero.IsLord)
			{
				text = AdapterPromptCaptureTexts.PcapText("persona_generation_30");
			}
			else if (hero.IsWanderer)
			{
				text = AdapterPromptCaptureTexts.PcapText("persona_generation_31");
			}
			else if (hero.IsNotable)
			{
				text = AdapterPromptCaptureTexts.PcapText("persona_generation_32");
			}
		}
		catch
		{
		}
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_33") + text);
		try
		{
			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_traits") + $"Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
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
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_34") + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
				stringBuilder.AppendLine(NpcSkillLevelReference);
			}
		}
		catch
		{
		}
		try
		{
			// Keep this bounded: persona generation is an on-demand capture, but the
			// prompt must not turn a large clan into an unbounded request. The life
			// state suffix is game-thread truth and the authority sentence prevents
			// older encyclopedia/background prose from reviving a deceased relative.
			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_35") + WorldEntityRetrievalService.FormatHeroRelatives(hero, 24));
			stringBuilder.AppendLine(WorldEntityPromptTexts.Get("FamilyStatusAuthority", "The current life or death status above takes precedence over older background or dialogue. A deceased person may only be described as part of the past; do not treat them as active or invent a cause or time of death."));
		}
		catch
		{
		}
		try
		{
			string heroEncyclopediaBackground = GetHeroEncyclopediaBackgroundForPersonaPrompt(hero, 1200);
			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_36") + (string.IsNullOrWhiteSpace(heroEncyclopediaBackground) ? AdapterPromptCaptureTexts.PcapText("persona_generation_37") : heroEncyclopediaBackground));
		}
		catch
		{
		}
		try
		{
			string clanOverviewBackground = BuildClanOverviewForPersonaPrompt(hero.Clan);
			if (!string.IsNullOrWhiteSpace(clanOverviewBackground))
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_38") + clanOverviewBackground);
			}
		}
		catch
		{
		}
		try
		{
			string clanEncyclopediaBackground = GetClanEncyclopediaBackgroundForPersonaPrompt(hero.Clan, 1200);
			stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_39") + (string.IsNullOrWhiteSpace(clanEncyclopediaBackground) ? AdapterPromptCaptureTexts.PcapText("persona_generation_40") : clanEncyclopediaBackground));
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
				string kingdomName = GetKingdomDisplayName(personaKingdom, AdapterPromptCaptureTexts.PcapText("persona_generation_41"));
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_42") + kingdomName + "：" + (string.IsNullOrWhiteSpace(kingdomEncyclopediaBackground) ? AdapterPromptCaptureTexts.PcapText("persona_generation_43") : kingdomEncyclopediaBackground));
			}
			else
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_44"));
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
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_45"));
			}
			else if (clanLeader != null)
			{
				string leaderBackground = GetHeroEncyclopediaBackgroundForPersonaPrompt(clanLeader, 1200);
				if (string.IsNullOrWhiteSpace(leaderBackground))
				{
					leaderBackground = BuildHeroBasicBackgroundForPersonaPrompt(clanLeader);
				}
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_46") + GetHeroDisplayName(clanLeader) + "：" + (string.IsNullOrWhiteSpace(leaderBackground) ? AdapterPromptCaptureTexts.PcapText("persona_generation_47") : leaderBackground));
			}
			else
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_no_leader"));
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
		string name = string.IsNullOrWhiteSpace(personalName) ? (hero?.Name?.ToString() ?? AdapterPromptCaptureTexts.PcapText("persona_generation_48")) : personalName.Trim();
		string fullName = string.IsNullOrWhiteSpace(originalFullName) ? name : originalFullName.Trim();
		string troopName = string.IsNullOrWhiteSpace(originalTroopName) ? AdapterPromptCaptureTexts.PcapText("persona_generation_49") : originalTroopName.Trim();
		string troopId = (originalTroopId ?? "").Trim();
		string culture = string.IsNullOrWhiteSpace(cultureName) ? AdapterPromptCaptureTexts.PcapText("persona_generation_50") : cultureName.Trim();
		string scene = string.IsNullOrWhiteSpace(sceneLabel) ? AdapterPromptCaptureTexts.PcapText("persona_generation_51") : sceneLabel.Trim();
		string joinFact = string.IsNullOrWhiteSpace(joinEventFact) ? (name + AdapterPromptCaptureTexts.PcapText("persona_generation_52")) : joinEventFact.Trim();
		string equipment = string.IsNullOrWhiteSpace(equipmentSummary) ? AdapterPromptCaptureTexts.PcapText("persona_generation_53") : equipmentSummary.Trim();
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_54"));
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_55"));
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_56") + name);
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_57") + fullName);
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_58") + troopName + (string.IsNullOrWhiteSpace(troopId) ? "" : (" (StringId=" + troopId + ")")));
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_59") + culture);
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_60") + scene);
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_61") + joinFact);
		stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_62") + equipment);
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapFormat("persona_generation_age", "Age: {AGE}", ("AGE", hero.Age.ToString("F0"))));
			}
		}
		catch
		{
		}
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_18") + (hero.IsFemale ? AdapterPromptCaptureTexts.PcapText("persona_generation_19") : AdapterPromptCaptureTexts.PcapText("persona_generation_20")));
			}
		}
		catch
		{
		}
		try
		{
			if (hero != null)
			{
				stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_traits") + $"Mercy={hero.GetTraitLevel(DefaultTraits.Mercy)}, Valor={hero.GetTraitLevel(DefaultTraits.Valor)}, Honor={hero.GetTraitLevel(DefaultTraits.Honor)}, Generosity={hero.GetTraitLevel(DefaultTraits.Generosity)}, Calculating={hero.GetTraitLevel(DefaultTraits.Calculating)}");
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
					stringBuilder.AppendLine(AdapterPromptCaptureTexts.PcapText("persona_generation_34") + string.Join(", ", list.Select(x => $"{x.Skill.StringId}={x.Value}")));
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
