using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;
using EventSourceMaterialEntry = AnimusForge.MyBehavior.EventSourceMaterialEntry;
using System.Text;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.TournamentGames;
using static AnimusForge.PersonaIntroTextRules;
using System;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Settlements;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;

namespace AnimusForge;

// Campaign-thread fact capture; stores no live game entities or duplicate record state.
internal sealed class CampaignCharacterRecordCaptureAdapter
{
internal static string BuildNonHeroMemoryId(string unnamedKey)
	{
		string text = (unnamedKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return MemoryRecordRules.NormalizeMemoryHeroId(MemoryBusinessStateOwner.NonHeroMemoryIdPrefix + text);
	}

internal HashSet<string> RecentlyReleasedPrisoners = new HashSet<string>();
internal static bool ShouldTrackNpcActionHero(Hero hero, bool allowNonLordHero = false)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		if (hero == Hero.MainHero)
		{
			return true;
		}
		return allowNonLordHero || hero.IsLord;
	}

internal static List<Hero> GetTrackedLordsForClan(Clan clan)
	{
		List<Hero> list = new List<Hero>();
		if (clan == null)
		{
			return list;
		}
		try
		{
			foreach (Hero lord in clan.Heroes)
			{
				if (ShouldTrackNpcActionHero(lord) && !list.Any((Hero x) => string.Equals(x.StringId, lord.StringId, StringComparison.OrdinalIgnoreCase)))
				{
					list.Add(lord);
				}
			}
		}
		catch
		{
		}
		return list;
	}

internal static NpcActionFacts CreateNpcActionFacts(string actionKind, Hero actorHero = null)
	{
		NpcActionFacts npcActionFacts = new NpcActionFacts
		{
			ActionKind = (actionKind ?? "").Trim()
		};
		ApplyActorFacts(npcActionFacts, actorHero);
		return npcActionFacts;
	}

internal static void ApplyActorFacts(NpcActionFacts facts, Hero hero)
	{
		if (facts == null || hero == null)
		{
			return;
		}
		facts.ActorHeroId = GetHeroId(hero);
		facts.ActorClanId = GetClanId(hero.Clan);
		facts.ActorKingdomId = GetKingdomId(hero.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.ActorHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.ActorClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.ActorKingdomId);
	}

internal static void ApplyTargetFacts(NpcActionFacts facts, Hero hero)
	{
		if (facts == null || hero == null)
		{
			return;
		}
		facts.TargetHeroId = GetHeroId(hero);
		facts.TargetClanId = GetClanId(hero.Clan);
		facts.TargetKingdomId = GetKingdomId(hero.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.TargetHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.TargetClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.TargetKingdomId);
	}

internal static void ApplySettlementFacts(NpcActionFacts facts, Settlement settlement, Hero currentOwnerOverride = null, Hero previousOwnerOverride = null, string locationText = null)
	{
		if (facts == null || settlement == null)
		{
			return;
		}
		facts.SettlementId = GetSettlementId(settlement);
		facts.SettlementName = (settlement.Name?.ToString() ?? "").Trim();
		facts.LocationText = string.IsNullOrWhiteSpace(locationText) ? facts.SettlementName : locationText.Trim();
		Hero hero = currentOwnerOverride ?? settlement.OwnerClan?.Leader;
		Hero hero2 = previousOwnerOverride;
		facts.SettlementOwnerHeroId = GetHeroId(hero);
		facts.SettlementOwnerClanId = GetClanId(hero?.Clan ?? settlement.OwnerClan);
		facts.SettlementOwnerKingdomId = GetKingdomId(hero?.MapFaction ?? settlement.MapFaction);
		facts.PreviousSettlementOwnerHeroId = GetHeroId(hero2);
		facts.PreviousSettlementOwnerClanId = GetClanId(hero2?.Clan);
		facts.PreviousSettlementOwnerKingdomId = GetKingdomId(hero2?.MapFaction);
		AddUniqueId(facts.RelatedHeroIds, facts.SettlementOwnerHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.SettlementOwnerClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.SettlementOwnerKingdomId);
		AddUniqueId(facts.RelatedHeroIds, facts.PreviousSettlementOwnerHeroId);
		AddUniqueId(facts.RelatedClanIds, facts.PreviousSettlementOwnerClanId);
		AddUniqueId(facts.RelatedKingdomIds, facts.PreviousSettlementOwnerKingdomId);
	}

internal static void AddRelatedFactionFacts(NpcActionFacts facts, IFaction faction)
	{
		if (facts == null || faction == null)
		{
			return;
		}
		if (faction is Kingdom kingdom)
		{
			string kingdomId = GetKingdomId(kingdom);
			if (string.IsNullOrWhiteSpace(facts.TargetKingdomId))
			{
				facts.TargetKingdomId = kingdomId;
			}
			AddUniqueId(facts.RelatedKingdomIds, kingdomId);
			return;
		}
		if (faction is Clan clan)
		{
			string clanId = GetClanId(clan);
			if (string.IsNullOrWhiteSpace(facts.TargetClanId))
			{
				facts.TargetClanId = clanId;
			}
			if (string.IsNullOrWhiteSpace(facts.TargetKingdomId))
			{
				facts.TargetKingdomId = GetKingdomId(clan.Kingdom);
			}
			AddUniqueId(facts.RelatedClanIds, clanId);
			AddUniqueId(facts.RelatedKingdomIds, GetKingdomId(clan.Kingdom));
		}
	}

internal static void AddUniqueId(List<string> list, string id)
	{
		string text = (id ?? "").Trim();
		if (list == null || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (!list.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			list.Add(text);
		}
	}

internal static void CopyFactIds(List<string> source, List<string> destination)
	{
		if (source == null || destination == null)
		{
			return;
		}
		foreach (string item in source)
		{
			AddUniqueId(destination, item);
		}
	}


 internal static string BuildNpcActionSummary(MemoryBusinessStateOwner state,NpcActionRecordOwner records,Hero hero,bool recentOnly,Func<int> currentDay)
 => records.BuildSummary(state,GetHeroId(hero),()=>hero?.Name?.ToString()?.Trim(),recentOnly,currentDay,BuildNpcActionMetadataNarrativeSuffix,CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker);
 internal static string RenderNpcActionEntriesForPrompt(Hero hero,List<NpcActionEntry> entries)
 => NpcActionRecordOwner.RenderEntries(()=>hero?.Name?.ToString()?.Trim(),entries,BuildNpcActionMetadataNarrativeSuffix,CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker);
 internal static string BuildNpcActionMetadataNarrativeSuffix(NpcActionEntry entry)
 => NpcActionRecordOwner.BuildMetadata(entry,ResolveDisplayNameBySettlementEntry,ResolveHeroName,ResolveClanName,ResolveKingdomName);


internal static SkillObject[] GetPromotedCompanionSkillObjects()
	{
		return new SkillObject[18]
		{
			DefaultSkills.OneHanded,
			DefaultSkills.TwoHanded,
			DefaultSkills.Polearm,
			DefaultSkills.Bow,
			DefaultSkills.Crossbow,
			DefaultSkills.Throwing,
			DefaultSkills.Riding,
			DefaultSkills.Athletics,
			DefaultSkills.Crafting,
			DefaultSkills.Scouting,
			DefaultSkills.Tactics,
			DefaultSkills.Roguery,
			DefaultSkills.Charm,
			DefaultSkills.Leadership,
			DefaultSkills.Trade,
			DefaultSkills.Steward,
			DefaultSkills.Medicine,
			DefaultSkills.Engineering
		};
	}

internal static string BuildPromotedHeroSkillSummary(Hero hero)
	{
		if (hero == null)
		{
			return "（无）";
		}
		List<string> list = new List<string>();
		foreach (SkillObject skill in GetPromotedCompanionSkillObjects())
		{
			if (skill != null)
			{
				list.Add(skill.StringId + "=" + hero.GetSkillValue(skill));
			}
		}
		return list.Count == 0 ? "（无）" : string.Join(", ", list);
	}

internal static bool TryApplyPromotedHeroSkillJson(Hero hero, string raw)
	{
		if (hero == null || string.IsNullOrWhiteSpace(raw))
		{
			return false;
		}
		try
		{
			Dictionary<string, SkillObject> skillMap = BuildPromotedSkillMap();
            if (!NpcPersonaTextRules.TryParseSkills(raw, skillMap.Keys.ToArray(), out var values)) return false;
			int applied = 0;
			foreach (var parsed in values)
            {
                SkillObject skill = skillMap[parsed.Key];
                int value = parsed.Value;

				hero.SetSkillValue(skill, value);
				applied++;
			}
			if (applied > 0)
			{
				Logger.Log("NpcPersona", "Promoted companion skills applied hero=" + (hero.StringId ?? "") + " count=" + applied);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

internal static Dictionary<string, SkillObject> BuildPromotedSkillMap()
	{
		Dictionary<string, SkillObject> map = new Dictionary<string, SkillObject>(StringComparer.OrdinalIgnoreCase);
		foreach (SkillObject skill in GetPromotedCompanionSkillObjects())
		{
			if (skill == null)
			{
				continue;
			}
			map[NpcPersonaTextRules.NormalizePromotedSkillKey(skill.StringId)] = skill;
			map[NpcPersonaTextRules.NormalizePromotedSkillKey(skill.Name?.ToString())] = skill;
		}
		map["smithing"] = DefaultSkills.Crafting;
		map["crafting"] = DefaultSkills.Crafting;
		return map;
	}

internal static string GetQuestCompletionDetailLabel(QuestBase.QuestCompleteDetails detail)
	{
		switch (detail)
		{
		case QuestBase.QuestCompleteDetails.Success:
			return "成功完成";
		case QuestBase.QuestCompleteDetails.Fail:
			return "任务失败";
		case QuestBase.QuestCompleteDetails.FailWithBetrayal:
			return "以背叛结局失败";
		case QuestBase.QuestCompleteDetails.Timeout:
			return "超时结束";
		case QuestBase.QuestCompleteDetails.Cancel:
			return "已取消";
		default:
			return detail.ToString();
		}
	}

internal static string GetQuestCompletionActionKind(QuestBase.QuestCompleteDetails detail)
	{
		switch (detail)
		{
		case QuestBase.QuestCompleteDetails.Success:
			return "quest_success";
		case QuestBase.QuestCompleteDetails.Timeout:
			return "quest_timeout";
		case QuestBase.QuestCompleteDetails.FailWithBetrayal:
			return "quest_betrayal";
		case QuestBase.QuestCompleteDetails.Fail:
			return "quest_fail";
		case QuestBase.QuestCompleteDetails.Cancel:
			return "quest_cancel";
		default:
			return "quest_result";
		}
	}

internal static Settlement ResolveQuestActionSettlement(Hero questGiver)
	{
		return ResolveCurrentActionSettlement(questGiver);
	}

internal static Settlement ResolveCurrentActionSettlement(Hero hero)
	{
		try
		{
			return Settlement.CurrentSettlement ?? PlayerEncounter.EncounterSettlement ?? MobileParty.MainParty?.CurrentSettlement ?? hero?.CurrentSettlement ?? hero?.StayingInSettlement ?? hero?.HomeSettlement;
		}
		catch
		{
			return null;
		}
	}

internal static string GetCompanionRemovedDetailLabel(RemoveCompanionAction.RemoveCompanionDetail detail)
	{
		return detail == RemoveCompanionAction.RemoveCompanionDetail.Fire ? "被遣散" : detail.ToString();
	}

internal static bool IsPlayerRelatedGovernorChange(Settlement settlement, Hero oldGovernor, Hero newGovernor)
	{
		try
		{
			return settlement?.OwnerClan == Clan.PlayerClan || oldGovernor?.Clan == Clan.PlayerClan || newGovernor?.Clan == Clan.PlayerClan;
		}
		catch
		{
			return false;
		}
	}

internal static string CleanExternalActionTitle(string value)
	{
		return (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
	}


 private readonly MemoryBusinessStateOwner _memory;
 private readonly Func<MemoryBusinessStateOwner> _queues;
 private readonly CharacterPersonaReadinessPorts _persona;
 private readonly NpcActionRecordOwner _npcActionRecords;
 private readonly CampaignMaterialRecordOwner _campaignMaterialRecords;
 private readonly Action _markAll;
 private readonly Action<int> _markDay;
 private readonly Func<WorldBulletinEventCaptureAdapter> _bulletinEvents;
 private readonly Func<NpcActionEntry,string> _weeklyActionPreview;
 private readonly Func<WeeklyEventRecordStateOwner> _weeklyRecords;
 private readonly Action<WeeklyEventMaterialPreviewGroup> _aggregateWeeklyMaterials;
 private Dictionary<string,List<NpcActionEntry>> _npcMajorActions => _memory.MajorActions;
 private Dictionary<string,List<NpcActionEntry>> _npcRecentActions => _memory.RecentActions;
 private const int MaxMajorNpcActionEntriesPerHero = NpcActionLedger.MaxMajorEntriesPerHero;
 private const int MaxRecentNpcActionEntriesPerHero = NpcActionLedger.MaxRecentEntriesPerHero;
 internal CampaignCharacterRecordCaptureAdapter(MemoryBusinessStateOwner memory, NpcActionRecordOwner records,
     CampaignMaterialRecordOwner materials, Action markAll, Action<int> markDay, Func<NpcActionEntry,string> weeklyActionPreview = null, Func<WeeklyEventRecordStateOwner> weeklyRecords = null, Action<WeeklyEventMaterialPreviewGroup> aggregateWeeklyMaterials = null, Func<MemoryBusinessStateOwner> queues=null,CharacterPersonaReadinessPorts persona=null, Func<WorldBulletinEventCaptureAdapter> bulletinEvents=null)
 { _memory=memory; _queues=queues ?? (()=>memory); _persona=persona; _npcActionRecords=records; _campaignMaterialRecords=materials; _markAll=markAll; _markDay=markDay; _weeklyActionPreview=weeklyActionPreview; _weeklyRecords=weeklyRecords; _aggregateWeeklyMaterials=aggregateWeeklyMaterials; _bulletinEvents=bulletinEvents; }
internal static List<Hero> BuildTournamentParticipantHeroes(MBReadOnlyList<CharacterObject> participants, Hero championHero)
	{
		List<Hero> list = new List<Hero>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (CharacterObject participant in participants ?? Enumerable.Empty<CharacterObject>())
		{
			Hero tournamentHero = GetTournamentHero(participant);
			string heroId = GetHeroId(tournamentHero);
			if (tournamentHero != null && !string.IsNullOrWhiteSpace(heroId) && hashSet.Add(heroId))
			{
				list.Add(tournamentHero);
			}
		}
		string championHeroId = GetHeroId(championHero);
		if (championHero != null && !string.IsNullOrWhiteSpace(championHeroId) && hashSet.Add(championHeroId))
		{
			list.Add(championHero);
		}
		return list;
	}

internal static string BuildTournamentParticipantActionText(string settlementDisplayName, string championName, bool isChampion, string prizeDisplayName, string participantSummary, string tournamentRankLabel)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (isChampion)
		{
			stringBuilder.Append("你在").Append(settlementDisplayName).Append("的竞技大会中胜出。冠军是").Append(championName).Append("。");
		}
		else
		{
			stringBuilder.Append("你参加了").Append(settlementDisplayName).Append("的竞技大会。冠军是").Append(championName).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(tournamentRankLabel))
		{
			stringBuilder.Append(" 你的最终名次是").Append(tournamentRankLabel.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(prizeDisplayName))
		{
			stringBuilder.Append(" 本次大会奖品是").Append(prizeDisplayName.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(participantSummary))
		{
			stringBuilder.Append(" 全部参赛者（含冠军）：").Append(participantSummary.Trim()).Append("。");
		}
		return stringBuilder.ToString();
	}

internal static Dictionary<string, string> BuildTournamentParticipantRankLabels(CharacterObject winner, Town town)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		Hero tournamentHero = GetTournamentHero(winner);
		string heroId = GetHeroId(tournamentHero);
		if (!string.IsNullOrWhiteSpace(heroId))
		{
			dictionary[heroId] = "冠军（第1名）";
		}
		try
		{
			TournamentBehavior missionBehavior = Mission.Current?.GetMissionBehavior<TournamentBehavior>();
			if (missionBehavior == null || missionBehavior.Settlement?.Town != town || missionBehavior.Winner?.Character != winner || missionBehavior.Rounds == null || missionBehavior.Rounds.Length == 0)
			{
				return dictionary;
			}
			Dictionary<string, int> dictionary2 = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < missionBehavior.Rounds.Length; i++)
			{
				TournamentRound tournamentRound = missionBehavior.Rounds[i];
				if (tournamentRound?.Matches == null)
				{
					continue;
				}
				foreach (TournamentMatch match in tournamentRound.Matches)
				{
					if (match?.Participants == null)
					{
						continue;
					}
					foreach (TournamentParticipant participant in match.Participants)
					{
						Hero tournamentHero2 = GetTournamentHero(participant?.Character);
						string heroId2 = GetHeroId(tournamentHero2);
						if (string.IsNullOrWhiteSpace(heroId2))
						{
							continue;
						}
						if (!dictionary2.TryGetValue(heroId2, out int value) || i > value)
						{
							dictionary2[heroId2] = i;
						}
					}
				}
			}
			foreach (KeyValuePair<string, int> item in dictionary2)
			{
				if (!dictionary.ContainsKey(item.Key))
				{
					string text = BuildTournamentEliminationRankLabel(item.Value, missionBehavior.Rounds.Length);
					if (!string.IsNullOrWhiteSpace(text))
					{
						dictionary[item.Key] = text;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[WARN] BuildTournamentParticipantRankLabels: " + ex.Message);
		}
		return dictionary;
	}

internal static string BuildTournamentEliminationRankLabel(int furthestRoundIndex, int roundCount)
	{
		if (furthestRoundIndex < 0 || roundCount <= 0 || furthestRoundIndex >= roundCount)
		{
			return "";
		}
		int num = roundCount - furthestRoundIndex - 1;
		if (num < 0 || num >= 30)
		{
			return "";
		}
		int num2 = 1 << num;
		int num3 = num2 + 1;
		if (num2 == 1)
		{
			return "亚军（第2名）";
		}
		if (num2 == 2)
		{
			return "四强（并列第3名）";
		}
		if (num2 == 4)
		{
			return "八强（并列第5名）";
		}
		if (num2 == 8)
		{
			return "十六强（并列第9名）";
		}
		return "并列第" + num3 + "名";
	}

internal static Kingdom ResolveTournamentHostKingdom(Town town)
	{
		if (town == null)
		{
			return null;
		}
		return town.Settlement?.MapFaction as Kingdom ?? town.OwnerClan?.Kingdom ?? town.MapFaction as Kingdom;
	}

internal static Hero GetTournamentHero(CharacterObject character)
	{
		try
		{
			return character?.HeroObject;
		}
		catch
		{
			return null;
		}
	}

internal static string GetTournamentHeroId(CharacterObject character)
	{
		return GetHeroId(GetTournamentHero(character));
	}

internal static string GetTournamentCharacterId(CharacterObject character)
	{
		string text = GetTournamentHeroId(character);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (character?.StringId ?? "").Trim();
	}

internal static string GetTournamentCharacterDisplayName(CharacterObject character, string fallback)
	{
		Hero tournamentHero = GetTournamentHero(character);
		if (tournamentHero != null)
		{
			return GetHeroDisplayName(tournamentHero);
		}
		string text = (character?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

internal static string BuildTournamentWinnerStatusText(CharacterObject winner)
	{
		Hero tournamentHero = GetTournamentHero(winner);
		if (tournamentHero == null)
		{
			return "冠军不是有家族档案的英雄，无法识别其家族、所属王国与家族等级。";
		}
		Clan clan = tournamentHero.Clan;
		if (clan == null)
		{
			return "冠军没有可识别的家族，无法识别其所属王国与家族等级。";
		}
		string clanDisplayName = GetClanDisplayName(clan);
		Kingdom kingdom = clan.Kingdom ?? tournamentHero.MapFaction as Kingdom;
		string kingdomDisplayName = GetKingdomDisplayName(kingdom, "无明确所属王国");
		int num = Math.Max(0, clan.Tier);
		string clanTierReputationLabel = GetClanTierReputationLabel(num);
		return "冠军出身于" + clanDisplayName + "家族，所属王国是" + kingdomDisplayName + "，家族等级评价为" + clanTierReputationLabel + "（" + num + " level）。";
	}

internal static string BuildTournamentCharacterRoleSuffix(CharacterObject character)
	{
		Hero tournamentHero = GetTournamentHero(character);
		if (tournamentHero == Hero.MainHero)
		{
			return "（玩家）";
		}
		if (tournamentHero != null)
		{
			if (tournamentHero.IsFactionLeader)
			{
				return "（王国领袖）";
			}
			if (tournamentHero.Clan?.Leader == tournamentHero)
			{
				return "（家族族长）";
			}
			if (tournamentHero.IsLord)
			{
				return "（贵族）";
			}
			if (tournamentHero.Occupation == Occupation.Wanderer)
			{
				return "（流浪者）";
			}
			return "（英雄）";
		}
		try
		{
			if (character != null && character.Occupation == Occupation.ArenaMaster)
			{
				return "（竞技场人物）";
			}
		}
		catch
		{
		}
		return "";
	}

internal static string BuildTournamentParticipantSummary(MBReadOnlyList<CharacterObject> participants, CharacterObject winner)
	{
		List<string> list = new List<string>();
		List<int> list2 = new List<int>();
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		foreach (CharacterObject participant in participants ?? Enumerable.Empty<CharacterObject>())
		{
			Hero tournamentHero = GetTournamentHero(participant);
			string text = GetTournamentCharacterDisplayName(participant, "未命名参赛者");
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			string text2 = GetTournamentCharacterId(participant);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = text;
			}
			string text3 = (tournamentHero != null ? "hero:" : "troop:") + text2;
			if (dictionary.TryGetValue(text3, out int value))
			{
				list2[value]++;
				continue;
			}
			dictionary[text3] = list.Count;
			list.Add(text + BuildTournamentCharacterRoleSuffix(participant));
			list2.Add(1);
		}
		Hero tournamentHero2 = GetTournamentHero(winner);
		string text4 = GetTournamentCharacterDisplayName(winner, "一名参赛者");
		if (!string.IsNullOrWhiteSpace(text4))
		{
			string text5 = GetTournamentCharacterId(winner);
			if (string.IsNullOrWhiteSpace(text5))
			{
				text5 = text4;
			}
			string text6 = (tournamentHero2 != null ? "hero:" : "troop:") + text5;
			if (!dictionary.ContainsKey(text6))
			{
				dictionary[text6] = list.Count;
				list.Add(text4 + BuildTournamentCharacterRoleSuffix(winner));
				list2.Add(1);
			}
		}
		List<string> list3 = new List<string>(list.Count);
		for (int i = 0; i < list.Count; i++)
		{
			string text7 = list[i];
			int num = list2[i];
			list3.Add(num > 1 ? (text7 + "×" + num) : text7);
		}
		return string.Join("、", list3);
	}

internal static string GetTournamentPrizeDisplayName(ItemObject prize)
	{
		string text = (prize?.Name?.ToString() ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (prize?.StringId ?? "").Trim();
	}

internal void RecordTournamentParticipantNpcActions(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, string settlementDisplayName, string prizeDisplayName, string participantSummary, Dictionary<string, string> tournamentParticipantRankLabels, string stableKey)
	{
		try
		{
			string text = string.IsNullOrWhiteSpace(settlementDisplayName) ? GetSettlementDisplayName(town?.Settlement) : settlementDisplayName.Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				text = "某座城镇";
			}
			Hero championHero = GetTournamentHero(winner);
			string championName = GetTournamentCharacterDisplayName(winner, "一名参赛者");
			if (string.IsNullOrWhiteSpace(championName))
			{
				championName = "一名参赛者";
			}
			List<Hero> participantHeroes = BuildTournamentParticipantHeroes(participants, championHero);
			foreach (Hero tournamentHero in participantHeroes)
			{
				if (tournamentHero == Hero.MainHero || !ShouldTrackNpcActionHero(tournamentHero, allowNonLordHero: true))
				{
					continue;
				}
				bool isChampion = tournamentHero == championHero;
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("tournament_finished", tournamentHero);
				ApplySettlementFacts(npcActionFacts, town?.Settlement, null, null, text);
				npcActionFacts.Won = isChampion;
				if (!isChampion && championHero != null)
				{
					ApplyTargetFacts(npcActionFacts, championHero);
				}
				foreach (Hero participantHero in participantHeroes)
				{
					if (participantHero == null || participantHero == tournamentHero)
					{
						continue;
					}
					AddUniqueId(npcActionFacts.RelatedHeroIds, GetHeroId(participantHero));
					AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(participantHero.Clan));
					AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(participantHero.MapFaction));
				}
				string participantStableKey = isChampion
					? stableKey
					: (stableKey + ":participant:" + GetHeroId(tournamentHero));
				string tournamentRankLabel = "";
				if (tournamentParticipantRankLabels != null)
				{
					tournamentParticipantRankLabels.TryGetValue(GetHeroId(tournamentHero), out tournamentRankLabel);
				}
				string participantActionText = BuildTournamentParticipantActionText(text, championName, isChampion, prizeDisplayName, participantSummary, tournamentRankLabel);
				if (isChampion)
				{
					RecordNpcMajorAction(tournamentHero, participantActionText, participantStableKey, npcActionFacts, allowNonLordHero: true);
				}
				RecordNpcRecentAction(tournamentHero, participantActionText, participantStableKey, facts: npcActionFacts, allowNonLordHero: true);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordTournamentParticipantNpcActions: " + ex.Message);
		}
	}

internal void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
	{
		try
		{
			if (town?.Settlement == null)
			{
				return;
			}
			Kingdom kingdom = ResolveTournamentHostKingdom(town);
			string kingdomId = GetKingdomId(kingdom);
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				return;
			}
			string settlementId = GetSettlementId(town.Settlement);
			string settlementDisplayName = GetSettlementDisplayName(town.Settlement);
			string kingdomDisplayName = GetKingdomDisplayName(kingdom, "所属王国");
			string winnerDisplayName = GetTournamentCharacterDisplayName(winner, "一名参赛者");
			string winnerStatusText = BuildTournamentWinnerStatusText(winner);
			string prizeDisplayName = GetTournamentPrizeDisplayName(prize);
			string participantSummary = BuildTournamentParticipantSummary(participants, winner);
			Dictionary<string, string> tournamentParticipantRankLabels = BuildTournamentParticipantRankLabels(winner, town);
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(settlementDisplayName).Append("完成了一场竞技大会结算，举办地隶属于").Append(kingdomDisplayName).Append("。");
			stringBuilder.Append(" 冠军是").Append(winnerDisplayName).Append(BuildTournamentCharacterRoleSuffix(winner)).Append("。");
			if (!string.IsNullOrWhiteSpace(winnerStatusText))
			{
				stringBuilder.Append(" ").Append(winnerStatusText);
			}
			if (!string.IsNullOrWhiteSpace(prizeDisplayName))
			{
				stringBuilder.Append(" 本次大会奖品是").Append(prizeDisplayName).Append("。");
			}
			if (!string.IsNullOrWhiteSpace(participantSummary))
			{
				stringBuilder.Append(" 全部参赛者（含冠军）：").Append(participantSummary).Append("。");
			}
			string stableKey = "tournament_finished:" + settlementId + ":" + GetCurrentGameDayIndexSafe() + ":" + GetTournamentCharacterId(winner) + ":" + ((prize?.StringId ?? "").Trim());
			RecordEventSourceMaterial("tournament_finished", "竞技大会结算 - " + settlementDisplayName, stringBuilder.ToString(), stableKey, kingdomId, settlementId, includeInWorld: false, includeInKingdom: true, GetTournamentHeroId(winner), GetKingdomId(GetTournamentHero(winner)?.MapFaction));
			RecordTournamentParticipantNpcActions(winner, participants, town, settlementDisplayName, prizeDisplayName, participantSummary, tournamentParticipantRankLabels, stableKey);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnTournamentFinished: " + ex.Message);
		}
	}
internal void RecordNpcMajorAction(Hero hero, string text, string stableKey, NpcActionFacts facts = null, bool allowNonLordHero = false)
	{
		if (NpcActionLedger.ShouldSuppressNpcMajorAction(facts?.ActionKind, stableKey, text))
		{
			return;
		}
		RecordNpcActionInternal(_npcMajorActions, hero, text, stableKey, keepOnlyRecentWindow: false, dedupeAcrossWindow: false, MaxMajorNpcActionEntriesPerHero, facts, isMajor: true, allowNonLordHero);
	}

internal void RecordNpcRecentAction(Hero hero, string text, string stableKey, bool dedupeAcrossWindow = false, NpcActionFacts facts = null, bool allowNonLordHero = false)
	{
		RecordNpcActionInternal(_npcRecentActions, hero, text, stableKey, keepOnlyRecentWindow: true, dedupeAcrossWindow, MaxRecentNpcActionEntriesPerHero, facts, isMajor: false, allowNonLordHero);
	}

internal void RecordExternalNpcAction(Hero actorHero, string text, string stableKey, string actionKind, bool isMajor, bool isRecent, Hero targetHero, Settlement settlement, string locationText, bool allowNonLordHero, bool? won)
	{
		if (!ShouldTrackNpcActionHero(actorHero, allowNonLordHero))
		{
			return;
		}
		string cleanText = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(cleanText))
		{
			return;
		}
		NpcActionFacts facts = CreateNpcActionFacts(actionKind, actorHero);
		ApplyTargetFacts(facts, targetHero);
		if (settlement != null)
		{
			ApplySettlementFacts(facts, settlement, null, null, locationText);
			AddRelatedFactionFacts(facts, settlement.MapFaction);
		}
		else
		{
			facts.LocationText = (locationText ?? "").Trim();
		}
		facts.Won = won;
		string key = string.IsNullOrWhiteSpace(stableKey) ? BuildExternalActionStableKey(actionKind, actorHero, targetHero, cleanText) : stableKey.Trim();
		if (isMajor)
		{
			RecordNpcMajorAction(actorHero, cleanText, key, facts, allowNonLordHero);
		}
		if (isRecent)
		{
			RecordNpcRecentAction(actorHero, cleanText, key, dedupeAcrossWindow: true, facts, allowNonLordHero);
		}
	}

internal void RecordExternalPlayerAction(string text, string stableKey, string actionKind, bool isMajor, Hero targetHero, Settlement settlement, string locationText, bool? won)
	{
		string cleanText = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(cleanText))
		{
			return;
		}
		Hero player = Hero.MainHero;
		int day = GetCurrentGameDayIndexSafe();
		string key = string.IsNullOrWhiteSpace(stableKey) ? BuildExternalActionStableKey(actionKind, player, targetHero, cleanText) : stableKey.Trim();
		PlayerNotorietyBehavior.RecordPlayerActionForExternal(
			cleanText,
			key,
			(actionKind ?? "").Trim(),
			isMajor,
			day,
			GetCurrentGameDateTextSafe(),
			_memory.NextActionSequence(),
			GetSettlementId(settlement),
			GetSettlementDisplayName(settlement),
			(locationText ?? GetSettlementDisplayName(settlement)).Trim(),
			player?.Culture?.StringId ?? "",
			targetHero?.Culture?.StringId ?? "",
			settlement?.Culture?.StringId ?? "",
			won);
	}

internal static string BuildExternalActionStableKey(string actionKind, Hero actorHero, Hero targetHero, string text)
	{
		return "external_action:" + MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart(actionKind) + ":" + GetHeroId(actorHero) + ":" + GetHeroId(targetHero) + ":" + GetCurrentGameDayIndexSafe() + ":" + MemoryBusinessStateOwner.NormalizeWeeklyPromptKeyPart((text ?? "").Length > 80 ? text.Substring(0, 80) : text);
	}

internal static string GetNpcActionHeroKey(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

internal static string ResolveHeroCultureId(string heroId)
	{
		try
		{
			return (FindHeroById(heroId)?.Culture?.StringId ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

internal static Settlement ResolveSettlementById(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Settlement.Find(text) ?? Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

internal static bool IsPlayerWeeklySourceMaterial(string materialKind, string actorHeroId, string stableKey)
	{
		string kind = (materialKind ?? "").Trim();
		string playerHeroId = GetHeroId(Hero.MainHero);
		if (!string.IsNullOrWhiteSpace(playerHeroId) && string.Equals((actorHeroId ?? "").Trim(), playerHeroId, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (kind.StartsWith("player_", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return string.Equals(kind, "custom_policy", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(kind, "public_daily_memory", StringComparison.OrdinalIgnoreCase)
			|| (stableKey ?? "").Trim().StartsWith("player_", StringComparison.OrdinalIgnoreCase);
	}

internal void RecordEventSourceMaterial(string materialKind, string label, string snapshotText, string stableKey, string kingdomId, string settlementId, bool includeInWorld, bool includeInKingdom, string actorHeroId = "", string actorKingdomId = "", int dayOverride = -1, string gameDateOverride = "")
	{
		string normalizedMaterialKind = (materialKind ?? "").Trim();
		string normalizedActorHeroId = (actorHeroId ?? "").Trim();
		bool isPlayerMaterial = IsPlayerWeeklySourceMaterial(normalizedMaterialKind, normalizedActorHeroId, stableKey);
		string text = isPlayerMaterial
			? PlayerNotorietyBehavior.RenderPlayerHistoryMaterialForExternal(snapshotText)
			: PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(snapshotText);
		text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
        int day = dayOverride >= 0 ? dayOverride : GetCurrentGameDayIndexSafe();
        _campaignMaterialRecords.Record(new EventSourceMaterialEntry {
            Day = day,
            GameDate = string.IsNullOrWhiteSpace(gameDateOverride) ? GetCurrentGameDateTextSafe() : gameDateOverride.Trim(),
            MaterialKind = normalizedMaterialKind,
            Label = PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(label).Trim(),
            SnapshotText = text, StableKey = stableKey, KingdomId = kingdomId, SettlementId = settlementId,
            ActorHeroId = normalizedActorHeroId, ActorKingdomId = actorKingdomId,
            IncludeInWorld = includeInWorld, IncludeInKingdom = includeInKingdom
        }, () => _memory.NextActionSequence(), _markDay);
        _bulletinEvents?.Invoke()?.CaptureCivilNewsMaterial(normalizedMaterialKind, stableKey, label, text,
            kingdomId, actorKingdomId, actorHeroId, day, includeInWorld);
    }

internal void RecordNpcActionInternal(Dictionary<string, List<NpcActionEntry>> storage, Hero hero, string text, string stableKey, bool keepOnlyRecentWindow, bool dedupeAcrossWindow, int maxEntries, NpcActionFacts facts, bool isMajor, bool allowNonLordHero = false)
	{
		try
		{
			if (storage == null || !ShouldTrackNpcActionHero(hero, allowNonLordHero))
			{
				return;
			}
			if (hero == Hero.MainHero)
			{
				RecordPlayerNotorietyActionFromNpcAction(text, stableKey, facts, isMajor);
				return;
			}
            _npcActionRecords.Record(storage, GetNpcActionHeroKey(hero), text, stableKey,
                GetCurrentGameDayIndexSafe(), keepOnlyRecentWindow, dedupeAcrossWindow, maxEntries,
                () => _memory.NextActionSequence(),
                (normalizedText, normalizedKey, day, order, sequence) =>
                    CreateNpcActionEntry(hero, normalizedText, normalizedKey, day, order, sequence, facts, isMajor),
                _markAll, _markDay);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNpcActionInternal: " + ex.Message);
		}
	}

internal void RecordPlayerNotorietyActionFromNpcAction(string text, string stableKey, NpcActionFacts facts, bool isMajor)
	{
		try
		{
			string normalizedText = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (string.IsNullOrWhiteSpace(normalizedText))
			{
				return;
			}
			int day = GetCurrentGameDayIndexSafe();
			int sequence = _memory.NextActionSequence();
			string actionKind = (facts?.ActionKind ?? "").Trim();
			string settlementId = (facts?.SettlementId ?? "").Trim();
			string settlementName = (facts?.SettlementName ?? "").Trim();
			string locationText = (facts?.LocationText ?? "").Trim();
			Settlement settlement = ResolveSettlementById(settlementId);
			string settlementCultureId = settlement?.Culture?.StringId ?? "";
			string actorCultureId = Hero.MainHero?.Culture?.StringId ?? "";
			string targetCultureId = ResolveHeroCultureId(facts?.TargetHeroId);
			PlayerNotorietyBehavior.RecordPlayerActionForExternal(normalizedText, stableKey, actionKind, isMajor, day, GetCurrentGameDateTextSafe(), sequence, settlementId, settlementName, locationText, actorCultureId, targetCultureId, settlementCultureId, facts?.Won);
		}
		catch (Exception ex)
		{
			Logger.Log("PlayerNotoriety", "RecordPlayerNotorietyActionFromNpcAction failed: " + ex.Message);
		}
	}

internal static NpcActionEntry CreateNpcActionEntry(Hero hero, string text, string stableKey, int day, int order, int sequence, NpcActionFacts facts, bool isMajor)
	{
		NpcActionFacts npcActionFacts = facts ?? CreateNpcActionFacts("", hero);
		if (string.IsNullOrWhiteSpace(npcActionFacts.ActorHeroId))
		{
			ApplyActorFacts(npcActionFacts, hero);
		}
		npcActionFacts.IsMajor = isMajor;
		var capture = new NpcActionEntry { GameDate = GetCurrentGameDateTextSafe(),
            ActionKind = npcActionFacts.ActionKind,
            ActorHeroId = npcActionFacts.ActorHeroId,
            ActorClanId = npcActionFacts.ActorClanId,
            ActorKingdomId = npcActionFacts.ActorKingdomId,
            TargetHeroId = npcActionFacts.TargetHeroId,
            TargetClanId = npcActionFacts.TargetClanId,
            TargetKingdomId = npcActionFacts.TargetKingdomId,
            SettlementId = npcActionFacts.SettlementId,
            SettlementName = npcActionFacts.SettlementName,
            SettlementOwnerHeroId = npcActionFacts.SettlementOwnerHeroId,
            SettlementOwnerClanId = npcActionFacts.SettlementOwnerClanId,
            SettlementOwnerKingdomId = npcActionFacts.SettlementOwnerKingdomId,
            PreviousSettlementOwnerHeroId = npcActionFacts.PreviousSettlementOwnerHeroId,
            PreviousSettlementOwnerClanId = npcActionFacts.PreviousSettlementOwnerClanId,
            PreviousSettlementOwnerKingdomId = npcActionFacts.PreviousSettlementOwnerKingdomId,
            LocationText = npcActionFacts.LocationText,
            Won = npcActionFacts.Won,
            RelatedHeroIds = npcActionFacts.RelatedHeroIds,
            RelatedClanIds = npcActionFacts.RelatedClanIds,
            RelatedKingdomIds = npcActionFacts.RelatedKingdomIds
        };
        return NpcActionRecordOwner.Create(capture, text, stableKey, day, order, sequence, isMajor);
	}
    internal static Hero ResolveMemoryHero(string memoryId)
    {
        return MemoryBusinessStateOwner.IsNonHeroMemoryId(memoryId) ? null
            : Hero.Find(memoryId) ?? Hero.FindFirst(x => x != null && string.Equals(GetMemoryHeroId(x), memoryId, StringComparison.OrdinalIgnoreCase));
    }

internal static string GetMemoryHeroId(Hero hero)
	{
		return MemoryRecordRules.NormalizeMemoryHeroId(hero?.StringId);
	}

internal void TryAddPreviewActionMaterial(List<EventMaterialReference> materials, Hero hero, NpcActionEntry entry, bool recentOnly)
	{
		if (materials == null || hero == null || entry == null)
		{
			return;
		}
		if (MemoryEntityIdentityBannerlordAdapter.ShouldSuppressWeeklyPreviewAction(entry))
		{
			return;
		}
		EventMaterialReference eventMaterialReference = materials.FirstOrDefault((EventMaterialReference x) => x != null && string.Equals((x.HeroId ?? "").Trim(), (hero.StringId ?? "").Trim(), StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		string text = MemoryEntityIdentityBannerlordAdapter.ResolveHeroName(hero.StringId) + " - " + EventEditorProjection.BuildDevSummaryPreview(_weeklyActionPreview(entry), 60);
		EventMaterialReference eventMaterialReference2 = new EventMaterialReference
		{
			MaterialType = recentOnly ? "npc_recent_action" : "npc_major_action",
			Label = text,
			SnapshotText = _weeklyActionPreview(entry),
			HeroId = hero.StringId ?? "",
			KingdomId = entry.ActorKingdomId ?? "",
			SettlementId = entry.SettlementId ?? "",
			RecentOnly = recentOnly,
			ActionKind = entry.ActionKind ?? "",
			ActorHeroId = entry.ActorHeroId ?? "",
			ActorClanId = entry.ActorClanId ?? "",
			ActorKingdomId = entry.ActorKingdomId ?? "",
			TargetHeroId = entry.TargetHeroId ?? "",
			TargetClanId = entry.TargetClanId ?? "",
			TargetKingdomId = entry.TargetKingdomId ?? "",
			SettlementOwnerHeroId = entry.SettlementOwnerHeroId ?? "",
			SettlementOwnerClanId = entry.SettlementOwnerClanId ?? "",
			SettlementOwnerKingdomId = entry.SettlementOwnerKingdomId ?? "",
			PreviousSettlementOwnerHeroId = entry.PreviousSettlementOwnerHeroId ?? "",
			PreviousSettlementOwnerClanId = entry.PreviousSettlementOwnerClanId ?? "",
			PreviousSettlementOwnerKingdomId = entry.PreviousSettlementOwnerKingdomId ?? "",
			LocationText = entry.LocationText ?? "",
			Won = entry.Won,
			RelatedHeroIds = new List<string>((entry.RelatedHeroIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedClanIds = new List<string>((entry.RelatedClanIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			RelatedKingdomIds = new List<string>((entry.RelatedKingdomIds ?? new List<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim())),
			SourceStableKeys = new List<string>(),
			SourceActionKinds = new List<string>(),
			SourceMaterialCount = 1,
			ActionStableKey = entry.StableKey ?? "",
			ActionDay = entry.Day,
			ActionOrder = entry.Order,
			ActionSequence = entry.Sequence
		};
		AddUniqueId(eventMaterialReference2.SourceStableKeys, eventMaterialReference2.ActionStableKey);
		AddUniqueId(eventMaterialReference2.SourceActionKinds, eventMaterialReference2.ActionKind);
		if (eventMaterialReference != null)
		{
			if (!recentOnly && eventMaterialReference.RecentOnly)
			{
				int num = materials.IndexOf(eventMaterialReference);
				if (num >= 0)
				{
					materials[num] = eventMaterialReference2;
				}
			}
			return;
		}
		materials.Add(eventMaterialReference2);
	}

internal static void TryAddPreviewSourceMaterial(List<EventMaterialReference> materials, EventSourceMaterialEntry entry)
	{
		if (materials == null || entry == null)
		{
			return;
		}
		if (materials.Any((EventMaterialReference x) => x != null && string.Equals((x.MaterialType ?? "").Trim(), "raw_text", StringComparison.OrdinalIgnoreCase) && x.ActionDay.GetValueOrDefault() == entry.Day && x.ActionSequence.GetValueOrDefault() == entry.Sequence && string.Equals((x.ActionStableKey ?? "").Trim(), (entry.StableKey ?? "").Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		bool isPlayerMaterial = IsPlayerWeeklySourceMaterial(entry.MaterialKind, entry.ActorHeroId, entry.StableKey);
		materials.Add(new EventMaterialReference
		{
			MaterialType = "raw_text",
			Label = PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(entry.Label).Trim(),
			SnapshotText = (isPlayerMaterial
				? PlayerNotorietyBehavior.RenderPlayerHistoryMaterialForExternal(entry.SnapshotText)
				: PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(entry.SnapshotText)).Trim(),
			KingdomId = (entry.KingdomId ?? "").Trim(),
			SettlementId = (entry.SettlementId ?? "").Trim(),
			ActorHeroId = (entry.ActorHeroId ?? "").Trim(),
			ActorKingdomId = (entry.ActorKingdomId ?? "").Trim(),
			SourceStableKeys = new List<string>(),
			SourceActionKinds = new List<string>(),
			SourceMaterialCount = 1,
			ActionStableKey = (entry.StableKey ?? "").Trim(),
			ActionDay = entry.Day,
			ActionSequence = entry.Sequence
		});
		EventMaterialReference eventMaterialReference = materials[materials.Count - 1];
		AddUniqueId(eventMaterialReference.SourceStableKeys, eventMaterialReference.ActionStableKey);
	}

internal Dictionary<string, Hero> BuildWeeklyNpcActionHeroLookup()
	{
		HashSet<string> actionHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<string, List<NpcActionEntry>> entry in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value != null && entry.Value.Count > 0)
			{
				actionHeroIds.Add(entry.Key.Trim());
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> entry2 in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			if (!string.IsNullOrWhiteSpace(entry2.Key) && entry2.Value != null && entry2.Value.Count > 0)
			{
				actionHeroIds.Add(entry2.Key.Trim());
			}
		}
		Dictionary<string, Hero> lookup = new Dictionary<string, Hero>(actionHeroIds.Count, StringComparer.OrdinalIgnoreCase);
		if (actionHeroIds.Count == 0)
		{
			return lookup;
		}
		try
		{
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				string heroId = (hero?.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(heroId) && actionHeroIds.Contains(heroId))
				{
					lookup[heroId] = hero;
				}
			}
		}
		catch
		{
		}
		return lookup;
	}

internal WeeklyEventMaterialPreviewGroup BuildWorldWeeklyEventMaterialPreviewGroup(int startDay, int endDay, Dictionary<string, Hero> actionHeroLookup)
	{
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = _weeklyRecords().CreateWorldWeeklyEventMaterialPreviewGroup();
		foreach (EventSourceMaterialEntry item in _campaignMaterialRecords.GetWeeklyEventSourceMaterialsForBuild())
		{
			if (item != null && item.IncludeInWorld && item.Day >= startDay && item.Day <= endDay)
			{
				TryAddPreviewSourceMaterial(weeklyEventMaterialPreviewGroup.Materials, item);
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = ResolveWeeklyNpcActionHero(actionHeroLookup, npcRecentAction.Key);
			if (hero == null || npcRecentAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcRecentAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && ShouldIncludeWorldPreviewAction(hero, item))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = ResolveWeeklyNpcActionHero(actionHeroLookup, npcMajorAction.Key);
			if (hero == null || npcMajorAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcMajorAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && ShouldIncludeWorldPreviewAction(hero, item))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: false);
				}
			}
		}
		weeklyEventMaterialPreviewGroup.Summary = "世界事件将使用世界开局概要，以及本周更能代表大陆格局变化的重大行动、领导层震荡与世界级通用素材。军团从属加入或离开这类细碎动作会被压缩。";
		return weeklyEventMaterialPreviewGroup;
	}

internal WeeklyEventMaterialPreviewGroup BuildKingdomWeeklyEventMaterialPreviewGroup(Kingdom kingdom, int startDay, int endDay, Dictionary<string, Hero> actionHeroLookup)
	{
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = MemoryEntityIdentityBannerlordAdapter.CreateKingdomWeeklyEventMaterialPreviewGroup(_weeklyRecords(), kingdom);
		foreach (EventSourceMaterialEntry item in _campaignMaterialRecords.GetWeeklyEventSourceMaterialsForBuild())
		{
			if (item != null && item.IncludeInKingdom && item.Day >= startDay && item.Day <= endDay && CampaignMaterialRecordOwner.DoesEventSourceMaterialRelateToKingdom(item, kingdom?.StringId))
			{
				TryAddPreviewSourceMaterial(weeklyEventMaterialPreviewGroup.Materials, item);
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcRecentAction in _npcRecentActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = ResolveWeeklyNpcActionHero(actionHeroLookup, npcRecentAction.Key);
			if (hero == null || npcRecentAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item in npcRecentAction.Value)
			{
				if (item != null && item.Day >= startDay && item.Day <= endDay && NpcActionRecordOwner.DoesNpcActionRelateToKingdom(item, kingdom?.StringId) && ShouldIncludeKingdomPreviewAction(hero, item, kingdom))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item, recentOnly: true);
				}
			}
		}
		foreach (KeyValuePair<string, List<NpcActionEntry>> npcMajorAction in _npcMajorActions ?? new Dictionary<string, List<NpcActionEntry>>())
		{
			Hero hero = ResolveWeeklyNpcActionHero(actionHeroLookup, npcMajorAction.Key);
			if (hero == null || npcMajorAction.Value == null)
			{
				continue;
			}
			foreach (NpcActionEntry item2 in npcMajorAction.Value)
			{
				if (item2 != null && item2.Day >= startDay && item2.Day <= endDay && NpcActionRecordOwner.DoesNpcActionRelateToKingdom(item2, kingdom?.StringId))
				{
					TryAddPreviewActionMaterial(weeklyEventMaterialPreviewGroup.Materials, hero, item2, recentOnly: false);
				}
			}
		}
		return weeklyEventMaterialPreviewGroup;
	}

internal List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups()
	{
		int currentGameDayIndexSafe = GetCurrentGameDayIndexSafe();
		int num = Math.Max(0, currentGameDayIndexSafe - currentGameDayIndexSafe % 7);
		return BuildWeeklyEventMaterialPreviewGroups(num, currentGameDayIndexSafe);
	}

internal List<WeeklyEventMaterialPreviewGroup> BuildWeeklyEventMaterialPreviewGroups(int startDay, int endDay)
	{
		List<WeeklyEventMaterialPreviewGroup> list = new List<WeeklyEventMaterialPreviewGroup>();
		Dictionary<string, Hero> actionHeroLookup = BuildWeeklyNpcActionHeroLookup();
		list.Add(BuildWorldWeeklyEventMaterialPreviewGroup(startDay, endDay, actionHeroLookup));
		foreach (Kingdom devEditableKingdom in EventEditorProjection.GetDevEditableKingdoms())
		{
			if (!IsKingdomEligibleForWeeklyReport(devEditableKingdom))
			{
				continue;
			}
			WeeklyEventMaterialPreviewGroup item = BuildKingdomWeeklyEventMaterialPreviewGroup(devEditableKingdom, startDay, endDay, actionHeroLookup);
			list.Add(item);
		}
		foreach (WeeklyEventMaterialPreviewGroup item2 in list)
		{
			_aggregateWeeklyMaterials(item2);
		}
		HashSet<string> fullReportKingdomIds = WeeklyMaterialBatchPlanner.SelectFullReportKingdomIds(list,
			GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId)));
		foreach (WeeklyEventMaterialPreviewGroup item2 in list)
		{
			WeeklyPromptMaterialOwner.Prepare(item2, WeeklyMaterialBatchPlanner.IsShortOnly(item2, fullReportKingdomIds));
		}
		return list;
	}

internal static string GetSettlementOwnerChangeDetailLabel(ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		switch (detail)
		{
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege:
			return "围城";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByBarter:
			return "交易/买卖移交（非攻城）";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction:
			return "脱离王国";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByKingDecision:
			return "王国决议";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByGift:
			return "赠与";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByRebellion:
			return "叛乱";
		case ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByClanDestruction:
			return "家族覆灭";
		default:
			return "常规变更";
		}
	}

internal static bool IsExecutionKillDetail(KillCharacterAction.KillCharacterActionDetail detail)
	{
		return detail == KillCharacterAction.KillCharacterActionDetail.Executed || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
	}

internal static string GetHeroKilledVerb(KillCharacterAction.KillCharacterActionDetail detail)
	{
		switch (detail)
		{
		case KillCharacterAction.KillCharacterActionDetail.Murdered:
			return "谋杀了";
		case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
			return "间接导致分娩中失去了";
		case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
			return "见证了";
		case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
			return "在战场上杀死了";
		case KillCharacterAction.KillCharacterActionDetail.WoundedInBattle:
			return "在战斗中重创并导致死亡的是";
		case KillCharacterAction.KillCharacterActionDetail.Executed:
		case KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent:
			return "处决了";
		default:
			return "使其死亡：";
		}
	}

internal static string BuildExecutedVictimActionText(Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
	{
		string killerName = GetHeroDisplayName(killer);
		if (killer == null)
		{
			return detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent ? "你在战后被处决。" : "你被处决。";
		}
		if (detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
		{
			return "你在战后被" + killerName + "处决。";
		}
		return "你被" + killerName + "处决。";
	}

internal static VengeanceExecutionFacts ResolvePublicExecutionFacts(Hero victim, KillCharacterAction.KillCharacterActionDetail detail)
	{
		if (!IsExecutionKillDetail(detail))
		{
			return null;
		}
		try
		{
			return VengeanceRuntimeBridge.TryGetPendingExecutionFacts(victim);
		}
		catch
		{
			return null;
		}
	}

internal static string BuildPublicExecutionVenueText(VengeanceExecutionFacts facts)
	{
		string venue = GetSettlementDisplayName(facts?.Venue);
		return string.IsNullOrWhiteSpace(venue) || string.Equals(venue, "某处定居点", StringComparison.Ordinal) ? "" : "在" + venue.Trim();
	}

internal static string BuildPublicExecutionMethodText(VengeanceExecutionFacts facts)
	{
		return string.IsNullOrWhiteSpace(facts?.MethodLabel) ? "公开处决" : "以" + facts.MethodLabel + "公开处决";
	}

internal static string BuildPublicExecutionChargeText(VengeanceExecutionFacts facts)
	{
		return string.IsNullOrWhiteSpace(facts?.ChargeLabel) ? "" : "，罪名为" + facts.ChargeLabel;
	}

internal static string BuildPublicExecutionKillerActionText(Hero victim, VengeanceExecutionFacts facts)
	{
		string victimName = GetHeroDisplayName(victim);
		string how = facts.PlayerStruck ? "亲手" + BuildPublicExecutionMethodText(facts) : "下令" + BuildPublicExecutionMethodText(facts);
		return "你" + BuildPublicExecutionVenueText(facts) + how + "了" + victimName + BuildPublicExecutionChargeText(facts) + "。";
	}

internal static string BuildPublicExecutionVictimActionText(Hero killer, VengeanceExecutionFacts facts)
	{
		string killerText = killer != null ? "被" + GetHeroDisplayName(killer) : "被";
		return "你" + BuildPublicExecutionVenueText(facts) + killerText + BuildPublicExecutionMethodText(facts) + BuildPublicExecutionChargeText(facts) + "。";
	}

internal static string BuildPublicExecutionClanSuffix(Hero killer, VengeanceExecutionFacts facts)
	{
		string by = killer != null ? "由" + GetHeroDisplayName(killer) : "";
		return "其" + BuildPublicExecutionVenueText(facts) + by + BuildPublicExecutionMethodText(facts) + BuildPublicExecutionChargeText(facts) + "。";
	}

internal static Settlement ResolveHeroExecutionSettlement(Hero victim, Hero killer)
	{
		try
		{
			Settlement settlement = ResolveCurrentActionSettlement(killer ?? victim);
			if (settlement != null)
			{
				return settlement;
			}
			return victim?.CurrentSettlement
				?? victim?.StayingInSettlement
				?? victim?.PartyBelongedTo?.CurrentSettlement
				?? victim?.PartyBelongedToAsPrisoner?.MobileParty?.CurrentSettlement
				?? killer?.CurrentSettlement
				?? killer?.StayingInSettlement
				?? killer?.PartyBelongedTo?.CurrentSettlement;
		}
		catch
		{
			return null;
		}
	}

internal static string ResolveHeroExecutionLocationText(Settlement settlement, Hero victim, Hero killer)
	{
		string text = GetSettlementDisplayName(settlement);
		if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, "某处定居点", StringComparison.Ordinal))
		{
			return text;
		}
		text = CampaignBattleRecordCaptureAdapter.GetNearestSettlementNameForParty(killer?.PartyBelongedTo ?? victim?.PartyBelongedTo ?? MobileParty.MainParty);
		return string.IsNullOrWhiteSpace(text) ? "" : text;
	}

internal bool HasRecentNpcActionStableKeyWithinWindow(Hero hero,string stableKey,int currentDay) => _npcActionRecords.HasRecentNpcActionStableKeyWithinWindow(_memory.RecentActions,GetNpcActionHeroKey(hero),stableKey,currentDay);


internal void OnBeforeHeroesMarried(Hero hero1, Hero hero2, bool showNotification)
	{
		try
		{
			string text = BuildOrderedHeroPairStableKey("marriage", hero1, hero2);
			if (ShouldTrackNpcActionHero(hero1))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("marriage", hero1);
				ApplyTargetFacts(npcActionFacts, hero2);
				RecordNpcMajorAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", text, npcActionFacts);
				RecordNpcRecentAction(hero1, "你与" + GetHeroDisplayName(hero2) + "缔结了婚姻。", text, facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(hero2))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("marriage", hero2);
				ApplyTargetFacts(npcActionFacts2, hero1);
				RecordNpcMajorAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", text, npcActionFacts2);
				RecordNpcRecentAction(hero2, "你与" + GetHeroDisplayName(hero1) + "缔结了婚姻。", text, facts: npcActionFacts2);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnBeforeHeroesMarried: " + ex.Message);
		}
	}

internal void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		try
		{
			if (victim == null)
			{
				return;
			}
			string text = "hero_killed:" + GetHeroId(victim) + ":" + detail;
			VengeanceExecutionFacts executionFacts = ResolvePublicExecutionFacts(victim, detail);
			if (ShouldTrackNpcActionHero(killer))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("hero_killed", killer);
				ApplyTargetFacts(npcActionFacts, victim);
				string killerText = executionFacts != null
					? BuildPublicExecutionKillerActionText(victim, executionFacts)
					: "你" + GetHeroKilledVerb(detail) + GetHeroDisplayName(victim) + "。";
				RecordNpcMajorAction(killer, killerText, text + ":killer", npcActionFacts);
				RecordNpcRecentAction(killer, killerText, text + ":killer", facts: npcActionFacts);
			}
			if (IsExecutionKillDetail(detail))
			{
				RecordExecutedVictimAction(victim, killer, detail, text, executionFacts);
				RecordPlayerExecutionWeeklyMaterial(victim, killer, detail, text, executionFacts);
			}
			Hero leader = victim.Clan?.Leader;
			if (ShouldTrackNpcActionHero(leader) && !string.Equals(GetHeroId(leader), GetHeroId(victim), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("clan_member_killed", leader);
				ApplyTargetFacts(npcActionFacts2, victim);
				if (killer != null)
				{
					AddUniqueId(npcActionFacts2.RelatedHeroIds, GetHeroId(killer));
					AddUniqueId(npcActionFacts2.RelatedClanIds, GetClanId(killer.Clan));
					AddUniqueId(npcActionFacts2.RelatedKingdomIds, GetKingdomId(killer.MapFaction));
				}
				string text2 = "你所在的" + GetClanDisplayName(victim.Clan) + "家族失去了" + GetHeroDisplayName(victim) + "。";
				if (executionFacts != null)
				{
					text2 += BuildPublicExecutionClanSuffix(killer, executionFacts);
				}
				RecordNpcMajorAction(leader, text2, text + ":clan", npcActionFacts2);
				RecordNpcRecentAction(leader, text2, text + ":clan", facts: npcActionFacts2);
			}
			// Death/removal can leave persisted daily jobs behind. Keep raw history and weekly material,
			// but cancel deferred LLM work before it can issue another request for this Hero.
			_queues().CancelUnavailableHeroCompressionWorkById(GetMemoryHeroId(victim), "hero_killed");
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnHeroKilled: " + ex.Message);
		}
	}

internal void RecordExecutedVictimAction(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, string stablePrefix, VengeanceExecutionFacts executionFacts = null)
	{
		if (victim == null || !ShouldTrackNpcActionHero(victim, allowNonLordHero: true))
		{
			return;
		}
		NpcActionFacts facts = CreateNpcActionFacts("hero_executed_victim", victim);
		ApplyTargetFacts(facts, killer);
		Settlement settlement = executionFacts?.Venue ?? ResolveHeroExecutionSettlement(victim, killer);
		string locationText = ResolveHeroExecutionLocationText(settlement, victim, killer);
		if (settlement != null)
		{
			ApplySettlementFacts(facts, settlement, locationText: locationText);
			AddRelatedFactionFacts(facts, settlement.MapFaction);
		}
		else
		{
			facts.LocationText = locationText;
		}
		facts.Won = false;
		string text = executionFacts != null
			? BuildPublicExecutionVictimActionText(killer, executionFacts)
			: BuildExecutedVictimActionText(killer, detail);
		string key = (string.IsNullOrWhiteSpace(stablePrefix) ? ("hero_killed:" + GetHeroId(victim) + ":" + detail) : stablePrefix.Trim()) + ":victim";
		RecordNpcMajorAction(victim, text, key, facts, allowNonLordHero: true);
		RecordNpcRecentAction(victim, text, key, dedupeAcrossWindow: true, facts, allowNonLordHero: true);
	}

internal void RecordPlayerExecutionWeeklyMaterial(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, string stablePrefix, VengeanceExecutionFacts executionFacts = null)
	{
		Hero player = Hero.MainHero;
		if (player == null || (victim != player && killer != player))
		{
			return;
		}
		int day = GetCurrentGameDayIndexSafe();
		string gameDate = GetCurrentGameDateTextSafe();
		Settlement settlement = executionFacts?.Venue ?? ResolveHeroExecutionSettlement(victim, killer);
		string settlementId = GetSettlementId(settlement);
		string locationText = ResolveHeroExecutionLocationText(settlement, victim, killer);
		Kingdom victimKingdom = MemoryEntityIdentityBannerlordAdapter.ResolveHeroKingdomForWeeklyMaterial(victim);
		Kingdom killerKingdom = MemoryEntityIdentityBannerlordAdapter.ResolveHeroKingdomForWeeklyMaterial(killer);
		string victimKingdomId = GetKingdomId(victimKingdom);
		string killerKingdomId = GetKingdomId(killerKingdom);
		string primaryKingdomId = !string.IsNullOrWhiteSpace(victimKingdomId) ? victimKingdomId : killerKingdomId;
		if (string.IsNullOrWhiteSpace(primaryKingdomId))
		{
			Logger.Log("EventWeeklyReport", "[PlayerExecution][SKIP] kingdom_missing victim=" + GetHeroId(victim) + " killer=" + GetHeroId(killer));
			return;
		}
		string snapshot = BuildPlayerExecutionWeeklySnapshot(victim, killer, detail, victimKingdom, killerKingdom, locationText, gameDate, executionFacts);
		if (string.IsNullOrWhiteSpace(snapshot))
		{
			return;
		}
		string baseKey = "player_execution:" + NpcActionLedger.NormalizeStableKey(stablePrefix, GetHeroId(victim) + ":" + GetHeroId(killer) + ":" + detail);
		string label = victim == player ? "玩家被处决 - " + GetHeroDisplayName(killer) : "玩家处决英雄 - " + GetHeroDisplayName(victim);
		RecordEventSourceMaterial(
			"player_execution",
			label,
			snapshot,
			baseKey + ":victim_kingdom",
			primaryKingdomId,
			settlementId,
			includeInWorld: true,
			includeInKingdom: true,
			actorHeroId: GetHeroId(killer),
			actorKingdomId: killerKingdomId,
			dayOverride: day,
			gameDateOverride: gameDate);
		if (!string.IsNullOrWhiteSpace(killerKingdomId) && !string.Equals(killerKingdomId, primaryKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			RecordEventSourceMaterial(
				"player_execution",
				label,
				snapshot,
				baseKey + ":killer_kingdom",
				killerKingdomId,
				settlementId,
				includeInWorld: false,
				includeInKingdom: true,
				actorHeroId: GetHeroId(killer),
				actorKingdomId: killerKingdomId,
				dayOverride: day,
				gameDateOverride: gameDate);
		}
		Logger.Log("EventWeeklyReport", "[PlayerExecution] source_material_recorded day=" + day + " victim=" + GetHeroId(victim) + " killer=" + GetHeroId(killer) + " kingdom=" + primaryKingdomId + " key=" + baseKey);
	}

internal static string BuildPlayerExecutionWeeklySnapshot(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, Kingdom victimKingdom, Kingdom killerKingdom, string locationText, string gameDate, VengeanceExecutionFacts executionFacts = null)
	{
		Hero player = Hero.MainHero;
		string victimName = victim == player ? "玩家" : GetHeroDisplayName(victim);
		string killerName = killer == player ? "玩家" : GetHeroDisplayName(killer);
		string victimKingdomName = GetKingdomDisplayName(victimKingdom, "");
		string killerKingdomName = GetKingdomDisplayName(killerKingdom, "");
		string victimClanName = GetClanDisplayName(victim?.Clan);
		string killerClanName = GetClanDisplayName(killer?.Clan);
		StringBuilder sb = new StringBuilder();
		sb.Append(victim == player ? "玩家被处决素材。" : "玩家处决英雄素材。");
		if (!string.IsNullOrWhiteSpace(gameDate))
		{
			sb.Append("日期：").Append(gameDate.Trim()).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(locationText))
		{
			sb.Append("地点：").Append(locationText.Trim()).Append("。");
		}
		sb.Append("执行者：").Append(killerName).Append("。");
		if (!string.IsNullOrWhiteSpace(killerClanName) && !string.Equals(killerClanName, "某个", StringComparison.Ordinal))
		{
			sb.Append("执行者家族：").Append(killerClanName).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(killerKingdomName))
		{
			sb.Append("执行者所属王国：").Append(killerKingdomName).Append("。");
		}
		sb.Append("被处决者：").Append(victimName).Append("。");
		if (!string.IsNullOrWhiteSpace(victimClanName) && !string.Equals(victimClanName, "某个", StringComparison.Ordinal))
		{
			sb.Append("被处决者家族：").Append(victimClanName).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(victimKingdomName))
		{
			sb.Append("被处决者所属王国：").Append(victimKingdomName).Append("。");
		}
		if (executionFacts == null)
		{
			sb.Append("处决类型：").Append(detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent ? "战后处决" : "处决").Append("。");
			sb.Append("事实约束：这是处决事件，不得写成战场击杀、谋杀、自然死亡或普通俘虏释放。");
			return PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(sb.ToString(), 360);
		}
		sb.Append("处决类型：城镇公开处刑。");
		if (!string.IsNullOrWhiteSpace(executionFacts.MethodLabel))
		{
			sb.Append("刑罚：").Append(executionFacts.MethodLabel).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(executionFacts.ChargeLabel))
		{
			sb.Append("罪名：").Append(executionFacts.ChargeLabel).Append("。");
		}
		sb.Append("审判方式：").Append(executionFacts.ToneLabel).Append("。");
		sb.Append("合法性：").Append(executionFacts.LegitimacyLabel).Append("。");
		sb.Append("行刑者：").Append(executionFacts.PlayerStruck ? killerName + "亲自行刑" : "本镇刽子手奉" + killerName + "之命行刑").Append("。");
		sb.Append("事实约束：这是公开处刑，刑罚与罪名以上述记录为准，不得改写成其他刑罚，也不得写成战场击杀、谋杀或自然死亡。");
		return PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(sb.ToString(), 520);
	}

internal static string BuildOrderedHeroPairStableKey(string prefix, Hero hero1, Hero hero2)
	{
		string text = GetHeroId(hero1);
		string text2 = GetHeroId(hero2);
		if (string.CompareOrdinal(text, text2) > 0)
		{
			string text3 = text;
			text = text2;
			text2 = text3;
		}
		return prefix + ":" + text + ":" + text2;
	}


internal void OnHeroComesOfAge(Hero hero)
	{
		try
		{
			if (!ShouldAutoGeneratePersonaForAdultHero(hero))
			{
				return;
			}
			Logger.Log("NpcPersona", "Adult hero persona auto-generation queued hero=" + (hero.StringId ?? "") + " name=" + (hero.Name?.ToString() ?? ""));
			_ = _persona.Ensure(hero);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcPersona", "[WARN] Adult hero persona auto-generation queue failed: " + ex.Message);
		}
	}

internal bool ShouldAutoGeneratePersonaForAdultHero(Hero hero)
	{
		if (hero == null || hero == Hero.MainHero || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return false;
		}
		DuelSettings settings = DuelSettings.GetSettings();
		if (settings != null && !settings.EnableAdultHeroPersonaAutoGeneration)
		{
			return false;
		}
		try
		{
			if (!hero.IsAlive || hero.IsChild)
			{
				return false;
			}
		}
		catch
		{
			return false;
		}
		return _persona.Needs(hero) && !_persona.InFlight(hero);
	}

internal void OnGivenBirth(Hero mother, List<Hero> aliveChildren, int stillbornCount)
	{
		try
		{
			if (!ShouldTrackNpcActionHero(mother))
			{
				return;
			}
			Hero hero = aliveChildren?.FirstOrDefault((Hero x) => x != null);
			string text = "birth:" + GetHeroId(mother) + ":" + GetHeroId(hero);
			NpcActionFacts npcActionFacts = CreateNpcActionFacts("birth", mother);
			if (hero != null)
			{
				AddUniqueId(npcActionFacts.RelatedHeroIds, GetHeroId(hero));
			}
			string text2 = "你诞下一名子嗣";
			if (hero != null)
			{
				text2 = text2 + "，孩子是" + GetHeroDisplayName(hero);
			}
			if (stillbornCount > 0)
			{
				text2 = text2 + "。此次分娩还伴随" + stillbornCount + "名夭折婴儿";
			}
			text2 += "。";
			RecordNpcMajorAction(mother, text2, text, npcActionFacts);
			RecordNpcRecentAction(mother, text2, text, facts: npcActionFacts);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnGivenBirth: " + ex.Message);
		}
	}

internal void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail, Action<Kingdom,string> discontinue)
	{
		try
		{
			Kingdom oldOwnerKingdom = oldOwner?.Clan?.Kingdom;
			string text = BuildSettlementOwnerChangedStableKey(settlement, newOwner, oldOwner, detail);
			string text2 = GetSettlementDisplayName(settlement);
			if (ShouldTrackNpcActionHero(newOwner))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("settlement_owner_changed_gain", newOwner);
				ApplyTargetFacts(npcActionFacts, oldOwner);
				ApplySettlementFacts(npcActionFacts, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(newOwner, "你获得了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", npcActionFacts);
				RecordNpcRecentAction(newOwner, "你获得了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":gain", facts: npcActionFacts);
			}
			if (ShouldTrackNpcActionHero(oldOwner))
			{
				NpcActionFacts npcActionFacts2 = CreateNpcActionFacts("settlement_owner_changed_loss", oldOwner);
				ApplyTargetFacts(npcActionFacts2, newOwner);
				ApplySettlementFacts(npcActionFacts2, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(oldOwner, "你失去了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", npcActionFacts2);
				RecordNpcRecentAction(oldOwner, "你失去了" + text2 + "的所有权（方式：" + GetSettlementOwnerChangeDetailLabel(detail) + "）。", text + ":loss", facts: npcActionFacts2);
			}
			if (ShouldTrackNpcActionHero(capturerHero) && !string.Equals(GetHeroId(capturerHero), GetHeroId(newOwner), StringComparison.OrdinalIgnoreCase))
			{
				NpcActionFacts npcActionFacts3 = CreateNpcActionFacts("settlement_owner_changed_capture", capturerHero);
				ApplyTargetFacts(npcActionFacts3, newOwner);
				ApplySettlementFacts(npcActionFacts3, settlement, newOwner, oldOwner);
				RecordNpcMajorAction(capturerHero, "你促成了" + text2 + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", npcActionFacts3);
				RecordNpcRecentAction(capturerHero, "你促成了" + text2 + "的易主，新的所有者是" + GetHeroDisplayName(newOwner) + "。", text + ":capture", facts: npcActionFacts3);
			}
			discontinue(oldOwnerKingdom, "settlement_owner_changed:" + GetSettlementId(settlement));
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnSettlementOwnerChanged: " + ex.Message);
		}
	}

internal static string BuildSettlementOwnerChangedStableKey(Settlement settlement, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		return "settlement_owner_changed:" + GetSettlementId(settlement) + ":" + GetHeroId(newOwner) + ":" + GetHeroId(oldOwner) + ":" + detail;
	}

internal void RecordPublicDailyMemoryWeeklyMaterial(CompressedMemoryBlock block)
	{
		string publicity = (block?.PlayerPublicity ?? "").Trim().ToLowerInvariant();
		if (block == null || (publicity != "public" && publicity != "leaked_public"))
		{
			return;
		}
		string material = (block.PlayerHistoryMaterial ?? "").Trim();
		if (string.IsNullOrWhiteSpace(material))
		{
			return;
		}
		if (!MemoryEntityIdentityBannerlordAdapter.ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var kingdomId, out var settlementId))
		{
			Logger.Log("EventWeeklyReport", "[PublicDailyMemory][SKIP] foothold_kingdom_missing block=" + (block.Id ?? "") + " memory=" + (block.HeroId ?? ""));
			return;
		}
		string blockId = (block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
		string stableHash = WeeklyMemoryMaterialPolicy.ComputeWeeklyMemoryMaterialHash(blockId + "|" + kingdomId + "|" + material);
		string stableKey = "public_daily_memory:" + kingdomId + ":" + blockId + ":" + stableHash;
		string label = "公开聊天日结 - " + (string.IsNullOrWhiteSpace(block.HeroName) ? "NPC" : block.HeroName.Trim());
		string snapshot = WeeklyMemoryMaterialPolicy.BuildPublicDailyMemoryWeeklyMaterialSnapshotText(block, material, MemoryRecallContextOwner.FormatMemoryHourRange(block?.StartHour ?? 0, block?.EndHour ?? 0));
		RecordEventSourceMaterial("public_daily_memory", label, snapshot, stableKey, kingdomId, settlementId, includeInWorld: false, includeInKingdom: true, actorHeroId: MemoryEntityIdentityBannerlordAdapter.GetHeroId(Hero.MainHero), actorKingdomId: kingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
		Logger.Log("EventWeeklyReport", "[PublicDailyMemory] source_material_recorded block=" + blockId + " kingdom=" + kingdomId + " publicity=" + publicity);
	}

internal void RecordWeeklyMemoryMaterialForBlock(CompressedMemoryBlock block)
	{
		List<WeeklyMemoryMaterialTrigger> triggers = MemoryRecordRules.SanitizeWeeklyMemoryMaterialTriggers(block?.WeeklyMaterialTriggers);
		if (block == null || triggers.Count == 0)
		{
			return;
		}
		string publicity = (block.PlayerPublicity ?? "").Trim().ToLowerInvariant();
		if (publicity != "public" && publicity != "leaked_public")
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] hidden_by_daily_publicity block=" + (block.Id ?? "") + " publicity=" + (string.IsNullOrWhiteSpace(publicity) ? "empty" : publicity) + " triggers=" + triggers.Count);
			return;
		}
        string blockId = (block.Id ?? MemoryRecordRules.BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
        foreach (var material in WeeklyMemoryMaterialPolicy.BuildDialogueMaterials(block, triggers, blockId))
        {
            RecordEventSourceMaterial("player_dialogue_memory", material.Label, material.Snapshot,
                material.StableKey, material.KingdomId, material.SettlementId,
                includeInWorld: false, includeInKingdom: true, actorHeroId: MemoryEntityIdentityBannerlordAdapter.GetHeroId(Hero.MainHero),
                actorKingdomId: material.KingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
            Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] source_material_recorded block="
                + (block.Id ?? "") + " kingdom=" + material.KingdomId + " triggers=" + material.TriggerCount
                + " value=" + material.Value);
        }
	}


internal void RemoveGenericEscapeRecentActionForPlayerRescue(Hero rescuedHero, int day)
	{
		try
		{
			string heroKey = GetNpcActionHeroKey(rescuedHero);
			string heroId = GetHeroId(rescuedHero);
			if (string.IsNullOrWhiteSpace(heroKey) || string.IsNullOrWhiteSpace(heroId) || _memory.RecentActions == null || !_memory.RecentActions.TryGetValue(heroKey, out var entries) || entries == null)
			{
				return;
			}
			int removed = entries.RemoveAll(entry =>
				entry != null
				&& entry.Day == day
				&& string.Equals((entry.ActionKind ?? "").Trim(), "prisoner_released_prisoner", StringComparison.OrdinalIgnoreCase)
				&& (entry.StableKey ?? "").IndexOf(":" + heroId + ":", StringComparison.OrdinalIgnoreCase) >= 0
				&& (entry.StableKey ?? "").IndexOf("ReleasedAfterEscape", StringComparison.OrdinalIgnoreCase) >= 0);
			if (removed > 0)
			{
				_npcActionRecords.RefreshNpcRecentActionStableKeyIndexForHero(heroKey, entries);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("PrisonBreakRescue", "RemoveGenericEscapeRecentActionForPlayerRescue failed: " + ex.Message);
		}
	}

internal string BuildNpcCurrentActionFact(Hero hero)
	{
		try
		{
			if (hero == null)
			{
				return "";
			}
			string text = CampaignBattleRecordCaptureAdapter.BuildRecentPartyBehaviorText(hero.PartyBelongedTo);
			if (string.IsNullOrWhiteSpace(text))
			{
				string text2 = (hero.CurrentSettlement?.Name?.ToString() ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(text2))
				{
					text = "你当前在" + text2 + "停留。";
				}
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				string npcActionHeroKey = GetNpcActionHeroKey(hero);
				if (!string.IsNullOrWhiteSpace(npcActionHeroKey) && _memory.RecentActions != null && _memory.RecentActions.TryGetValue(npcActionHeroKey, out var value) && value != null)
				{
					NpcActionEntry npcActionEntry = NpcActionLedger.SanitizeNpcActionEntries(value, true, GetCurrentGameDayIndexSafe()).LastOrDefault();
					if (npcActionEntry != null && npcActionEntry.Day >= GetCurrentGameDayIndexSafe() - 1)
					{
						text = npcActionEntry.Text;
					}
				}
			}
			text = NpcActionRecordOwner.RenderText(hero?.Name?.ToString()?.Trim(), text, CampaignBattleRecordCaptureAdapter.StripBattlePlayerMarker);
			return string.IsNullOrWhiteSpace(text) ? "" : (text);
		}
		catch
		{
			return "";
		}
	}

internal static Hero ResolveTransferTargetHeroFromAgent(Agent agent)
	{
		try
		{
			CharacterObject characterObject = agent?.Character as CharacterObject;
			if (characterObject?.HeroObject != null)
			{
				return characterObject.HeroObject;
			}
		}
		catch
		{
		}
		try
		{
			string text = ParseLordIdFromUnnamedKey(ShoutUtils.ExtractNpcData(agent)?.UnnamedKey);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return Hero.Find(text);
			}
		}
		catch
		{
		}
		try
		{
			return Settlement.CurrentSettlement?.OwnerClan?.Leader;
		}
		catch
		{
			return null;
		}
	}


internal NpcActionEntry ResolveEventMaterialNpcAction(EventMaterialReference material)
	{
		if (material == null || string.IsNullOrWhiteSpace(material.HeroId))
		{
			return null;
		}
		Hero hero = Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), (material.HeroId ?? "").Trim(), StringComparison.OrdinalIgnoreCase));
		if (hero == null)
		{
			return null;
		}
		List<NpcActionEntry> devNpcActionEntries = _npcActionRecords.ReadEntries(_memory, GetNpcActionHeroKey(hero), material.RecentOnly, GetCurrentGameDayIndexSafe);
		if (devNpcActionEntries == null || devNpcActionEntries.Count == 0)
		{
			return null;
		}
		NpcActionEntry npcActionEntry = devNpcActionEntries.FirstOrDefault((NpcActionEntry x) => x != null && (!material.ActionDay.HasValue || x.Day == material.ActionDay.Value) && (!material.ActionOrder.HasValue || x.Order == material.ActionOrder.Value) && (string.IsNullOrWhiteSpace(material.ActionStableKey) || string.Equals((x.StableKey ?? "").Trim(), material.ActionStableKey.Trim(), StringComparison.OrdinalIgnoreCase)));
		if (npcActionEntry != null)
		{
			return npcActionEntry;
		}
		if (!string.IsNullOrWhiteSpace(material.ActionStableKey))
		{
			npcActionEntry = devNpcActionEntries.FirstOrDefault((NpcActionEntry x) => x != null && string.Equals((x.StableKey ?? "").Trim(), material.ActionStableKey.Trim(), StringComparison.OrdinalIgnoreCase));
		}
		return npcActionEntry;
	}


internal void RecordNonHeroRecentAction(string nonHeroMemoryId, string npcName, string text, string stableKey, string actionKind)
	{
		try
		{
			string ownerKey = MemoryRecordRules.NormalizeMemoryHeroId(nonHeroMemoryId);
			string cleanText = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
			if (!MemoryBusinessStateOwner.IsNonHeroMemoryId(ownerKey) || string.IsNullOrWhiteSpace(cleanText)) return;
			if (_memory.RecentActions == null) _memory.RecentActions = new Dictionary<string, List<NpcActionEntry>>(StringComparer.OrdinalIgnoreCase);
			if (!_memory.RecentActions.TryGetValue(ownerKey, out List<NpcActionEntry> entries) || entries == null)
			{
				entries = new List<NpcActionEntry>();
				_memory.RecentActions[ownerKey] = entries;
			}
			int day = GetCurrentGameDayIndexSafe();
			if (NpcActionLedger.RemoveInvalid(entries, NpcActionLedger.RecentWindowMinimumDay(day), true, e => e.Text, e => e.Day))
			{
				_markAll();
			}
			string normalizedKey = NpcActionLedger.NormalizeStableKey(stableKey, cleanText);
			if (NpcActionLedger.ContainsStableKey(entries, normalizedKey, e => e.StableKey)) return;
			entries.Add(new NpcActionEntry
			{
				Day = day,
				Order = NpcActionLedger.NextOrder(entries, day, e => e.Day, e => e.Order),
				Sequence = _memory.NextActionSequence(),
				GameDate = GetCurrentGameDateTextSafe(),
				Text = cleanText,
				StableKey = normalizedKey,
				ActionKind = (actionKind ?? "").Trim(),
				IsMajor = false
			});
			_markDay(day);
			entries.Sort(NpcActionRecordOwner.CompareTimeline);
			if (entries.Count > MaxRecentNpcActionEntriesPerHero)
			{
				entries.RemoveRange(0, entries.Count - MaxRecentNpcActionEntriesPerHero);
				_markAll();
			}
			_npcActionRecords.RefreshNpcRecentActionStableKeyIndexForHero(ownerKey, entries);
			MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace("stage=recent_action_commit memoryId=" + ownerKey + " name=" + (npcName ?? "NPC") + " day=" + day + " key=" + normalizedKey);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordNonHeroRecentAction: " + ex.Message);
		}
	}


internal void RecordNpcPublicFeedbackEventMaterialInternal(string stableKey, string eventTitle, string policyId, string kingdomId, string kingdomName, string npcHeroId, string npcName, string policyName, string feedbackSummary, int day, string gameDate, bool includeInWorld)
	{
		string targetId = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(targetId))
		{
			Logger.Log("EventWeeklyReport", "[NpcPublicFeedback][SKIP] kingdom_missing stableKey=" + (stableKey ?? ""));
			return;
		}
		string cleanTitle = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(eventTitle, 80);
		string cleanPolicyId = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(policyId, 90);
		string cleanFeedback = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(feedbackSummary, 180);
		if (string.IsNullOrWhiteSpace(cleanFeedback))
		{
			return;
		}
		string cleanKingdomName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(kingdomName, 50);
		if (string.IsNullOrWhiteSpace(cleanKingdomName))
		{
			cleanKingdomName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(ResolveKingdomDisplay(targetId), 50);
		}
		if (string.IsNullOrWhiteSpace(cleanKingdomName))
		{
			cleanKingdomName = "目标王国";
		}
		string cleanNpcName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(npcName, 50);
		if (string.IsNullOrWhiteSpace(cleanNpcName))
		{
			cleanNpcName = "NPC";
		}
		string cleanPolicyName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(policyName, 70);
		StringBuilder sb = new StringBuilder();
		sb.Append("NPC政策衍生事件素材。");
		if (!string.IsNullOrWhiteSpace(cleanTitle))
		{
			sb.Append("标题：").Append(cleanTitle.Trim().TrimEnd('。')).Append("。");
		}
		sb.Append("王国：").Append(cleanKingdomName).Append("。关联统治者：").Append(cleanNpcName).Append("。");
		if (!string.IsNullOrWhiteSpace(cleanPolicyName))
		{
			sb.Append("关联政策：").Append(cleanPolicyName.Trim().TrimEnd('。')).Append("。");
		}
		if (!string.IsNullOrWhiteSpace(cleanPolicyId))
		{
			sb.Append("关联政策ID：").Append(cleanPolicyId.Trim().TrimEnd('。')).Append("。");
		}
		sb.Append("事件摘要：").Append(cleanFeedback.Trim().TrimEnd('。')).Append("。");
		string snapshot = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(sb.ToString(), 300);
		if (string.IsNullOrWhiteSpace(snapshot))
		{
			return;
		}
		string fallbackKey = targetId + ":" + cleanNpcName + ":" + cleanPolicyId + ":" + cleanPolicyName + ":" + cleanTitle + ":" + cleanFeedback;
		string key = CampaignMaterialRecordOwner.BuildPrefixedEventSourceStableKey("npc_public_feedback", stableKey, fallbackKey);
		string labelSuffix = !string.IsNullOrWhiteSpace(cleanTitle) ? cleanTitle : cleanPolicyName;
		RecordEventSourceMaterial(
			"npc_public_feedback",
			"政策衍生事件 - " + cleanKingdomName + (string.IsNullOrWhiteSpace(labelSuffix) ? "" : " / " + labelSuffix),
			snapshot,
			key,
			targetId,
			"",
			includeInWorld,
			includeInKingdom: true,
			actorHeroId: (npcHeroId ?? "").Trim(),
			actorKingdomId: targetId,
			dayOverride: day,
			gameDateOverride: gameDate);
	}

internal void RecordPlayerKingdomRenameWeeklyMaterialInternal(string oldKingdomName, string newKingdomName, string oldInformalName, string newInformalName, string kingdomId, int day, string gameDate, string stableKey)
	{
		string cleanOldName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(oldKingdomName, 60);
		string cleanNewName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(newKingdomName, 60);
		if (string.IsNullOrWhiteSpace(cleanOldName) || string.IsNullOrWhiteSpace(cleanNewName) || string.Equals(cleanOldName, cleanNewName, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string targetKingdomId = (kingdomId ?? "").Trim();
		Kingdom kingdom = FindKingdomById(targetKingdomId) ?? Clan.PlayerClan?.Kingdom;
		if (string.IsNullOrWhiteSpace(targetKingdomId))
		{
			targetKingdomId = GetKingdomId(kingdom);
		}
		if (string.IsNullOrWhiteSpace(targetKingdomId))
		{
			Logger.Log("EventWeeklyReport", "[PlayerKingdomRename][SKIP] kingdom_missing old=" + cleanOldName + " new=" + cleanNewName);
			return;
		}
		string cleanDate = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(gameDate, 30);
		string cleanOldInformalName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(oldInformalName, 50);
		string cleanNewInformalName = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(newInformalName, 50);
		StringBuilder sb = new StringBuilder();
		sb.Append("玩家王国更名素材。");
		if (!string.IsNullOrWhiteSpace(cleanDate))
		{
			sb.Append("日期：").Append(cleanDate).Append("。");
		}
		sb.Append("旧国号：").Append(cleanOldName).Append("。新国号：").Append(cleanNewName).Append("。");
		if (!string.IsNullOrWhiteSpace(cleanOldInformalName) || !string.IsNullOrWhiteSpace(cleanNewInformalName))
		{
			sb.Append("简称变化：").Append(string.IsNullOrWhiteSpace(cleanOldInformalName) ? cleanOldName : cleanOldInformalName).Append(" -> ").Append(string.IsNullOrWhiteSpace(cleanNewInformalName) ? cleanNewName : cleanNewInformalName).Append("。");
		}
		sb.Append("事实约束：这是玩家统治王国的名称/国号更改，不是新王国建立、家族叛乱、征服、吞并或王国覆灭。");
		string snapshot = PoliticalDecisionRecordCaptureAdapter.LimitCustomPolicyWeeklyMaterialText(sb.ToString(), 260);
		if (string.IsNullOrWhiteSpace(snapshot))
		{
			return;
		}
		string key = "player_kingdom_rename:" + NpcActionLedger.NormalizeStableKey(stableKey, cleanOldName + ":" + cleanNewName);
		RecordEventSourceMaterial(
			"player_kingdom_rename",
			"玩家王国更名 - " + cleanNewName,
			snapshot,
			key,
			targetKingdomId,
			"",
			includeInWorld: true,
			includeInKingdom: true,
			actorHeroId: GetHeroId(Hero.MainHero),
			actorKingdomId: targetKingdomId,
			dayOverride: day >= 0 ? day : GetCurrentGameDayIndexSafe(),
			gameDateOverride: string.IsNullOrWhiteSpace(gameDate) ? GetCurrentGameDateTextSafe() : gameDate.Trim());
		Logger.Log("EventWeeklyReport", "[PlayerKingdomRename] source_material_recorded kingdom=" + targetKingdomId + " old=" + cleanOldName + " new=" + cleanNewName + " length=" + snapshot.Length);
	}

internal void RecordPlayerSceneConflictWeeklyMaterialInternal(string text, string stableKey, int day, string gameDate, string settlementId, string settlementName, string locationText)
	{
		string summary = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(summary))
		{
			return;
		}
		Settlement settlement = ResolveSettlementById(settlementId);
		string resolvedSettlementId = string.IsNullOrWhiteSpace(settlementId) ? GetSettlementId(settlement) : settlementId.Trim();
		string resolvedSettlementName = string.IsNullOrWhiteSpace(settlementName) ? (settlement?.Name?.ToString() ?? "").Trim() : settlementName.Trim();
		if (string.IsNullOrWhiteSpace(resolvedSettlementName))
		{
			resolvedSettlementName = "当前定居点";
		}
		string location = (locationText ?? "").Trim();
		string kingdomId = GetKingdomId(settlement?.MapFaction);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			kingdomId = GetKingdomId(settlement?.OwnerClan?.Kingdom);
		}
		if (string.IsNullOrWhiteSpace(kingdomId) && ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var footholdKingdomId, out var footholdSettlementId))
		{
			kingdomId = footholdKingdomId;
			if (string.IsNullOrWhiteSpace(resolvedSettlementId))
			{
				resolvedSettlementId = footholdSettlementId;
			}
		}
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			Logger.Log("EventWeeklyReport", "[PlayerSceneConflict][SKIP] kingdom_missing settlement=" + resolvedSettlementId + " text=" + summary);
			return;
		}
		string place = resolvedSettlementName + (string.IsNullOrWhiteSpace(location) ? "" : "的" + location);
		string snapshot = "玩家和平场景攻击/犯罪素材。地点：" + place + "。履历摘要：" + summary + " 周报约束：这是和平定居点场景内由玩家主动攻击和平单位或触发犯罪造成的事件，不按战场、攻城、竞技场或训练场战斗理解。";
		string key = "player_peace_scene_crime:" + NpcActionLedger.NormalizeStableKey(stableKey, summary);
		RecordEventSourceMaterial(
			"player_peace_scene_crime",
			"玩家和平场景冲突 - " + resolvedSettlementName,
			snapshot,
			key,
			kingdomId,
			resolvedSettlementId,
			includeInWorld: false,
			includeInKingdom: true,
			actorHeroId: GetHeroId(Hero.MainHero),
			actorKingdomId: kingdomId,
			dayOverride: day,
			gameDateOverride: gameDate);
		Logger.Log("EventWeeklyReport", "[PlayerSceneConflict] source_material_recorded day=" + day + " kingdom=" + kingdomId + " settlement=" + resolvedSettlementId + " key=" + key);
	}
}

internal sealed class CharacterPersonaReadinessPorts {internal Func<Hero,bool> Needs;internal Func<Hero,bool> InFlight;internal Func<Hero,System.Threading.Tasks.Task> Ensure;}
