using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
using SettlementTransferPromptEntry = AnimusForge.MyBehavior.SettlementTransferPromptEntry;
using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;
using EventMaterialReference = AnimusForge.MyBehavior.EventMaterialReference;
using System.Text;
using TaleWorlds.MountAndBlade;
using System;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Map;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
namespace AnimusForge;

// Main-thread live identity leaves shared by record and detached prompt capture.
internal sealed class MemoryEntityIdentityBannerlordAdapter
{
	internal static string GetHeroDisplayName(Hero hero)
	{
		string text = (hero?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某位领主" : text;
	}

	internal static string GetHeroId(Hero hero)
	{
		return (hero?.StringId ?? "").Trim();
	}

	internal static string GetClanDisplayName(Clan clan)
	{
		string text = (clan?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? "某个" : text;
	}

	internal static string GetKingdomDisplayName(Kingdom kingdom, string fallback = "某个王国")
	{
		string text = (kingdom?.Name?.ToString() ?? "").Trim();
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}

	internal static string GetSettlementDisplayName(Settlement settlement)
	{
		string text = (settlement?.Name?.ToString() ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "某处定居点";
		}
		if (settlement?.IsTown == true && !text.EndsWith("市", StringComparison.Ordinal))
		{
			return text + "市";
		}
		return text;
	}

	internal static string GetArmyDisplayName(Army army)
	{
		string text = army?.Name?.ToString();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text.Trim();
		}
		Hero hero = army?.ArmyOwner ?? army?.LeaderParty?.LeaderHero;
		string text2 = hero?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text2) ? "一支军团" : (text2.Trim() + "的军团");
	}

internal static string GetClanId(Clan clan)
	{
		return (clan?.StringId ?? "").Trim();
	}

internal static string GetKingdomId(Kingdom kingdom)
	{
		return (kingdom?.StringId ?? "").Trim();
	}

internal static string GetKingdomId(IFaction faction)
	{
		if (faction is Kingdom kingdom)
		{
			return GetKingdomId(kingdom);
		}
		return "";
	}

internal static string GetSettlementId(Settlement settlement)
	{
		return (settlement?.StringId ?? "").Trim();
	}

internal static string GetFactionDisplayName(IFaction faction, string fallback = "某势力")
	{
		string text = faction?.Name?.ToString();
		return string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
	}

internal static string GetHeroFactionDisplayName(Hero hero, IFaction fallbackFaction = null)
	{
		return GetFactionDisplayName(hero?.MapFaction ?? fallbackFaction, "某势力");
	}

internal static Hero FindHeroById(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.FindFirst((Hero h) => h != null && string.Equals((h.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

internal static bool IsHeroNpcEligibleForCompressedMemory(Hero hero)
	{
		// Dead and disabled Heroes remain addressable in Bannerlord's object registry, but cannot own live dialogue memory.
		return hero != null && hero.IsAlive && !hero.IsDisabled && !string.IsNullOrWhiteSpace(hero.StringId) && (Hero.MainHero == null || !object.ReferenceEquals(hero, Hero.MainHero));
	}

internal static bool IsMemoryEntityEligibleForCompressedMemory(string memoryId)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (MemoryBusinessStateOwner.IsNonHeroMemoryId(text))
		{
			return true;
		}
		try
		{
			Hero hero = FindHeroById(text);
			return IsHeroNpcEligibleForCompressedMemory(hero);
		}
		catch
		{
			return false;
		}
	}

internal static string ResolveDisplayNameBySettlementEntry(NpcActionEntry entry)
	{
		string text = (entry?.LocationText ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = (entry?.SettlementName ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		string text2 = (entry?.SettlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text2))
		{
			return "";
		}
		try
		{
			Settlement settlement = Settlement.All.FirstOrDefault((Settlement x) => x != null && string.Equals((x.StringId ?? "").Trim(), text2, StringComparison.OrdinalIgnoreCase));
			return (settlement?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

internal static string ResolveHeroName(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Hero hero = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			string text2 = (hero?.Name?.ToString() ?? "").Trim();
			if (!string.IsNullOrWhiteSpace(text2))
			{
				return text2;
			}
			Hero hero2 = Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (hero2?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

internal static string ResolveClanName(string clanId)
	{
		string text = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Clan clan = Clan.All.FirstOrDefault((Clan x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (clan?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

internal static string ResolveKingdomName(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Kingdom kingdom = Kingdom.All.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
			return (kingdom?.Name?.ToString() ?? "").Trim();
		}
		catch
		{
			return "";
		}
	}

internal static string BuildNpcMajorActionsRuntimeInstruction(MemoryBusinessStateOwner state, NpcActionRecordOwner records, Func<int> currentDay, Func<Hero,CharacterObject,int,string> targetKey, Hero hero, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		Hero observerHero = hero ?? targetCharacter?.HeroObject;
		string text = CampaignCharacterRecordCaptureAdapter.BuildNpcActionSummary(state, records, observerHero, false, currentDay);
		string playerMajor = observerHero != null
			? PlayerNotorietyBehavior.BuildPlayerMajorRuntimeInstructionForExternal(observerHero)
			: PlayerNotorietyBehavior.BuildPlayerMajorRuntimeInstructionForExternal(targetKey(null, targetCharacter, targetAgentIndex), targetCharacter?.Culture?.StringId);
		if (!string.IsNullOrWhiteSpace(playerMajor))
		{
			text = string.IsNullOrWhiteSpace(text) ? playerMajor.Trim() : text.Trim() + "\n\n" + playerMajor.Trim();
		}
		string stateKey = (string.IsNullOrWhiteSpace(text) ? "no_data" : "has_data");
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["majorActionSummary"] = text ?? ""
		};
		return AIConfigHandler.ResolveRuleRuntimeText("npc_major_actions", stateKey, forConstraint: false, dictionary);
	}

internal static int GetCurrentGameDayIndexSafe()
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

internal static string GetCurrentGameDateTextSafe()
	{
		try
		{
			string text = CampaignTime.Now.ToString();
			return string.IsNullOrWhiteSpace(text) ? ("第 " + GetCurrentGameDayIndexSafe() + " 日") : text.Trim();
		}
		catch
		{
			return "第 " + GetCurrentGameDayIndexSafe() + " 日";
		}
	}

internal static Vec2? GetPlayerPartyPositionVec2()
	{
		try
		{
			CampaignVec2? campaignVec = MobileParty.MainParty?.Position;
			if (campaignVec.HasValue && campaignVec.Value.IsValid())
			{
				return campaignVec.Value.ToVec2();
			}
		}
		catch
		{
		}
		return null;
	}

internal static List<string> GetKingdomIdsByPlayerProximity(IEnumerable<string> kingdomIds)
	{
		List<string> list = new List<string>();
		HashSet<string> hashSet = new HashSet<string>((kingdomIds ?? Enumerable.Empty<string>()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()), StringComparer.OrdinalIgnoreCase);
		if (hashSet.Count == 0)
		{
			return list;
		}
		Vec2? playerPartyPositionVec = GetPlayerPartyPositionVec2();
		if (!playerPartyPositionVec.HasValue)
		{
			return list;
		}
		Vec2 value = playerPartyPositionVec.Value;
		Dictionary<string, float> dictionary = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Settlement item in Settlement.All)
			{
				if (item == null || item.IsHideout)
				{
					continue;
				}
				string text = ((item.MapFaction as Kingdom)?.StringId ?? item.OwnerClan?.Kingdom?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(text) || !hashSet.Contains(text))
				{
					continue;
				}
				Vec2 vec = item.GatePosition.ToVec2();
				float num = vec.x - value.x;
				float num2 = vec.y - value.y;
				float num3 = num * num + num2 * num2;
				if (!dictionary.TryGetValue(text, out var value2) || num3 < value2)
				{
					dictionary[text] = num3;
				}
			}
		}
		catch
		{
		}
		return dictionary.OrderBy((KeyValuePair<string, float> x) => x.Value).ThenBy((KeyValuePair<string, float> x) => x.Key, StringComparer.OrdinalIgnoreCase).Select((KeyValuePair<string, float> x) => x.Key).ToList();
	}

 internal static bool ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out string kingdomId,out string settlementId)
 => WeeklyMaterialValueBannerlordAdapter.TryResolvePlayerFoothold(GetSettlementId,GetKingdomId,
   EventEditorProjection.GetDevEditableKingdoms,GetKingdomIdsByPlayerProximity,out kingdomId,out settlementId);
internal static bool IsNonSceneNativeConversationActiveForMemory()
	{
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress != true)
			{
				return false;
			}
			try
			{
				if (Mission.Current?.Scene != null && ShoutUtils.IsInValidScene())
				{
					return false;
				}
			}
			catch
			{
			}
			return true;
		}
		catch
		{
			return false;
		}
	}
internal static string ResolveWeeklyReportNpcKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		string text = (kingdomIdOverride ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.Clan?.Kingdom);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		Hero heroObject = targetCharacter?.HeroObject;
		text = GetKingdomId(heroObject?.Clan?.Kingdom);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(heroObject?.MapFaction);
		return (text ?? "").Trim();
	}

internal static string ResolveWeeklyReportSurroundingsKingdomId(Hero targetHero, CharacterObject targetCharacter, string kingdomIdOverride = null)
	{
		string text = GetKingdomId(Settlement.CurrentSettlement?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = GetKingdomId(targetHero?.CurrentSettlement?.MapFaction);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		text = ResolveWeeklyReportNpcKingdomId(targetHero, targetCharacter, kingdomIdOverride);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(EventEditorProjection.GetDevEditableKingdoms().Select((Kingdom x) => x?.StringId));
		return kingdomIdsByPlayerProximity.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "";
	}

internal static bool IsKingdomEligibleForWeeklyReport(Kingdom kingdom)
	{
		return kingdom != null && !kingdom.IsEliminated && !string.IsNullOrWhiteSpace(kingdom.StringId);
	}

internal static bool IsKingdomEligibleForWeeklyReport(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return IsKingdomEligibleForWeeklyReport(FindKingdomById(text));
	}

internal static List<string> SelectWeeklyShortReportKingdomIds(string npcKingdomId, bool excludeNpcKingdom)
	{
		string text = (npcKingdomId ?? "").Trim();
		bool flag = IsKingdomEligibleForWeeklyReport(text);
		List<string> list = GetKingdomIdsByPlayerProximity(EventEditorProjection.GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select((Kingdom x) => x?.StringId)).Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		if (list.Count == 0)
		{
			list = EventEditorProjection.GetDevEditableKingdoms().Where(IsKingdomEligibleForWeeklyReport).Select((Kingdom x) => (x?.StringId ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		}
		if (excludeNpcKingdom && !string.IsNullOrWhiteSpace(text))
		{
			list.RemoveAll((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		List<string> list2 = list.Take(3).ToList();
		if (!excludeNpcKingdom && flag && !string.IsNullOrWhiteSpace(text) && !list2.Any((string x) => string.Equals((x ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase)))
		{
			if (list2.Count >= 3)
			{
				list2.RemoveAt(list2.Count - 1);
			}
			list2.Insert(0, text);
		}
		return list2.Where((string x) => !string.IsNullOrWhiteSpace(x)).Select((string x) => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToList();
	}
internal static Kingdom FindKingdomById(string kingdomId)
	{
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Kingdom.All.FirstOrDefault((Kingdom x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}
internal static string BuildResidentRecentActionsPrompt(MemoryBusinessStateOwner state,NpcActionRecordOwner records,Func<int> currentDay,Func<Hero,CharacterObject,int,string> targetKey,Hero hero,CharacterObject targetCharacter=null,int targetAgentIndex=-1)
	{
		Hero observerHero = hero ?? targetCharacter?.HeroObject;
		if (observerHero == null && targetCharacter == null)
		{
			return "";
		}
		string npcRecent = CampaignCharacterRecordCaptureAdapter.BuildNpcActionSummary(state,records,observerHero,true,currentDay);
		string playerRecent = observerHero != null
			? PlayerNotorietyBehavior.BuildPlayerRecentRuntimeInstructionForExternal(observerHero)
			: PlayerNotorietyBehavior.BuildPlayerRecentRuntimeInstructionForExternal(targetKey(null, targetCharacter, targetAgentIndex), targetCharacter?.Culture?.StringId);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【NPC近期行动（近10天，常驻）】");
		if (string.IsNullOrWhiteSpace(npcRecent))
		{
			stringBuilder.AppendLine("当前没有可确认记录；不得编造。");
		}
		else
		{
			stringBuilder.AppendLine(npcRecent.Trim());
		}
		stringBuilder.Append("边界：这里只表示近10天事实，不代表完整履历；未列出的近期行动不得编造。");
		if (!string.IsNullOrWhiteSpace(playerRecent))
		{
			stringBuilder.AppendLine().AppendLine().Append(playerRecent.Trim());
		}
		return stringBuilder.ToString().Trim();
	}

internal static bool IsDialogueOrLetterChainBusyForMemorySummary()
	{
		try
		{
			if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
			{
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

internal static string ResolveKingdomDisplay(string kingdomId)
	{
		string text = ResolveKingdomName(kingdomId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (kingdomId ?? "").Trim();
	}

internal static string BuildWeeklyReportMaterialLine(EventMaterialReference material)
	{
		if (material == null)
		{
			return "";
		}
		string text = (material.MaterialType ?? "").Trim().ToLowerInvariant();
		if (text == "world_opening_summary")
		{
			return PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal("世界开局概要：" + WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(material.SnapshotText));
		}
		if (text == "kingdom_opening_summary")
		{
			return PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(ResolveKingdomDisplay(material.KingdomId) + "的开局概要：" + WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(material.SnapshotText));
		}
		string text2 = PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(material.SnapshotText));
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return PlayerNotorietyBehavior.RenderPlayerNamedReferenceForExternal(WeeklyGenerationRules.NeutralizeWeeklyReportScenarioName(material.Label));
	}

internal static string AnnotateWeeklyReportRulerInMaterialText(string materialText, WeeklyEventMaterialPreviewGroup group)
	{
		if (!string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			return materialText ?? "";
		}
		string text = (materialText ?? "").TrimEnd();
		if (string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (WeeklyEventRecordStateOwner.HasWeeklyReportRulerContextMaterial(group))
		{
			return text;
		}
		Kingdom kingdom = FindKingdomById(group?.KingdomId);
		Hero ruler = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		string rulerName = GetHeroDisplayName(ruler);
		if (ruler == null || string.IsNullOrWhiteSpace(rulerName) || string.Equals(rulerName, "某位领主", StringComparison.OrdinalIgnoreCase) || rulerName.Trim().Length < 2)
		{
			return text;
		}
		return WeeklyEventRecordStateOwner.AnnotateWeeklyReportRulerNameInText(text, rulerName.Trim());
	}

internal static void TryEnqueueMemoryOverviewForHero(MemoryBusinessStateOwner memory, Hero hero, List<CompressedMemoryBlock> blocks = null)
	{
		try
		{
			if (!IsHeroNpcEligibleForCompressedMemory(hero))
			{
				return;
			}
			string heroId = CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero);
			if (string.IsNullOrWhiteSpace(heroId))
			{
				return;
			}
			memory.TryEnqueueMemoryOverviewForMemoryId(heroId, hero.Name?.ToString() ?? "NPC", blocks ?? memory.LoadBlocks(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero)));
		}
		catch (Exception ex)
		{
			Logger.Log("MemoryOverview", "[ERROR] TryEnqueueMemoryOverviewForHero failed: " + ex.Message);
		}
	}

internal static bool ShouldIncludeKingdomPreviewAction(Hero hero, NpcActionEntry entry, Kingdom kingdom)
	{
		if (hero == null || entry == null || kingdom == null)
		{
			return false;
		}
		string text = (entry.ActionKind ?? "").Trim();
		if (!string.Equals(text, "daily_behavior", StringComparison.OrdinalIgnoreCase))
		{
			if (string.Equals(text, "army_join", StringComparison.OrdinalIgnoreCase))
			{
				return hero.Clan?.Leader == hero;
			}
			return true;
		}
		if (MemoryBusinessStateOwner.IsArmyCommanderDailyBehavior(entry))
		{
			return true;
		}
		string text2 = (entry.StableKey ?? "").Trim();
		if (text2.IndexOf("raidsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("袭扰", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		if (text2.IndexOf("defendsettlement", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("守备", StringComparison.OrdinalIgnoreCase) >= 0 || (entry.Text ?? "").IndexOf("保卫", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string text3 = (kingdom.StringId ?? "").Trim();
		string text4 = (entry.ActorKingdomId ?? "").Trim();
		string text5 = (entry.SettlementOwnerKingdomId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(text3) && !string.IsNullOrWhiteSpace(text4) && !string.Equals(text4, text3, StringComparison.OrdinalIgnoreCase) && string.Equals(text5, text3, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}

internal static bool ShouldIncludeWorldPreviewAction(Hero hero, NpcActionEntry entry)
	{
		if (hero == null || entry == null)
		{
			return false;
		}
		string text = (entry.ActionKind ?? "").Trim();
		if (string.Equals(text, "army_join", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "army_leave", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(text, "daily_behavior", StringComparison.OrdinalIgnoreCase))
		{
			return MemoryBusinessStateOwner.IsArmyCommanderDailyBehavior(entry) || MemoryBusinessStateOwner.IsDailyBehaviorDefend(entry);
		}
		if (MemoryBusinessStateOwner.IsPrisonerTakenAction(entry))
		{
			return IsLeadershipCaptureAction(entry);
		}
		if (MemoryBusinessStateOwner.IsPrisonerReleasedAction(entry))
		{
			return IsLeadershipReleaseAction(entry);
		}
		return true;
	}

internal static bool ShouldSuppressWeeklyPreviewAction(NpcActionEntry entry)
	{
		if (!MemoryBusinessStateOwner.IsMapEventNpcAction(entry))
		{
			return false;
		}
		if (!string.IsNullOrWhiteSpace(entry.SettlementId))
		{
			return false;
		}
		bool actorIsBandit = IsBanditClanId(entry.ActorClanId) || IsBanditKingdomId(entry.ActorKingdomId);
		bool targetIsBandit = IsBanditClanId(entry.TargetClanId) || IsBanditKingdomId(entry.TargetKingdomId);
		if (entry.Won == true && targetIsBandit)
		{
			return true;
		}
		if (entry.Won == false && actorIsBandit)
		{
			return true;
		}
		string text = (entry.Text ?? "").Trim();
		return text.IndexOf("击败了", StringComparison.OrdinalIgnoreCase) >= 0 && CampaignBattleRecordCaptureAdapter.ContainsRoutineBanditText(text);
	}

internal static bool IsBanditClanId(string clanId)
	{
		Clan clan = FindClanById(clanId);
		return clan?.IsBanditFaction == true;
	}

internal static bool IsBanditKingdomId(string kingdomId)
	{
		Kingdom kingdom = FindKingdomById(kingdomId);
		return kingdom != null && ((IFaction)kingdom).IsBanditFaction;
	}

internal static bool IsLeadershipCaptureAction(NpcActionEntry entry)
	{
		Hero capturedHero = FindHeroById(MemoryBusinessStateOwner.GetCapturedHeroId(entry));
		return capturedHero != null && (capturedHero.Clan?.Leader == capturedHero || capturedHero.IsFactionLeader);
	}

internal static bool IsLeadershipReleaseAction(NpcActionEntry entry)
	{
		Hero releasedHero = FindHeroById(MemoryBusinessStateOwner.GetReleasedHeroId(entry));
		return releasedHero != null && (releasedHero.Clan?.Leader == releasedHero || releasedHero.IsFactionLeader);
	}

internal static void TryAddKingdomCurrentRulerMaterial(List<EventMaterialReference> materials, Kingdom kingdom)
	{
		if (materials == null || kingdom == null)
		{
			return;
		}
		string kingdomId = GetKingdomId(kingdom);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return;
		}
		Hero ruler = kingdom.Leader ?? kingdom.RulingClan?.Leader;
		string rulerName = GetHeroDisplayName(ruler);
		if (ruler == null || string.IsNullOrWhiteSpace(rulerName) || string.Equals(rulerName, "某位领主", StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		Clan rulingClan = kingdom.RulingClan ?? ruler.Clan;
		string kingdomName = GetKingdomDisplayName(kingdom, ResolveKingdomDisplay(kingdomId));
		string rulingClanName = GetClanDisplayName(rulingClan);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("当前王国：").Append(kingdomName).Append("。");
		stringBuilder.Append("当前统治者：").Append(rulerName.Trim()).Append("。");
		if (rulingClan != null && !string.IsNullOrWhiteSpace(rulingClanName) && !string.Equals(rulingClanName, "某个", StringComparison.OrdinalIgnoreCase))
		{
			stringBuilder.Append("执政家族：").Append(rulingClanName.Trim()).Append("。");
		}
		stringBuilder.Append("事实约束：这是本期王国周报目标的现任统治者上下文，不代表本周发生过继位、政变、叛乱或王位更替；除非其他素材明确写明，否则不要把它写成新事件。");
		EventMaterialReference material = new EventMaterialReference
		{
			MaterialType = "kingdom_current_ruler",
			Label = kingdomName + " 当前统治者",
			SnapshotText = stringBuilder.ToString(),
			HeroId = GetHeroId(ruler),
			KingdomId = kingdomId,
			ActorHeroId = GetHeroId(ruler),
			ActorClanId = GetClanId(rulingClan),
			ActorKingdomId = kingdomId,
			SourceMaterialCount = 1,
			ActionKind = "kingdom_context:ruler",
			ActionStableKey = "kingdom_current_ruler:" + kingdomId + ":" + GetHeroId(ruler)
		};
		CampaignCharacterRecordCaptureAdapter.AddUniqueId(material.RelatedHeroIds, GetHeroId(ruler));
		CampaignCharacterRecordCaptureAdapter.AddUniqueId(material.RelatedClanIds, GetClanId(rulingClan));
		CampaignCharacterRecordCaptureAdapter.AddUniqueId(material.RelatedKingdomIds, kingdomId);
		CampaignCharacterRecordCaptureAdapter.AddUniqueId(material.SourceStableKeys, material.ActionStableKey);
		CampaignCharacterRecordCaptureAdapter.AddUniqueId(material.SourceActionKinds, material.ActionKind);
		materials.Add(material);
	}

internal static Clan FindClanById(string clanId)
	{
		string text = (clanId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Clan.All.FirstOrDefault((Clan x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

internal static WeeklyEventMaterialPreviewGroup CreateKingdomWeeklyEventMaterialPreviewGroup(WeeklyEventRecordStateOwner records, Kingdom kingdom)
	{
		string text = kingdom?.Name?.ToString() ?? (kingdom?.StringId ?? "王国");
		WeeklyEventMaterialPreviewGroup weeklyEventMaterialPreviewGroup = new WeeklyEventMaterialPreviewGroup
		{
			GroupKind = "kingdom",
			KingdomId = kingdom?.StringId ?? "",
			Title = text + " 事件素材预览",
			Summary = text + " 本周会使用该国开局概要，以及与该国有关的本周高价值行动。普通行军会被压缩，守备、袭扰和外国领主入境会优先保留。",
			Materials = new List<EventMaterialReference>()
		};
		string kingdomOpeningSummary = WeeklyEventDataImportOwner.GetKingdomOpeningSummary(kingdom?.StringId, records.KingdomOpenings);
		if (!string.IsNullOrWhiteSpace(kingdomOpeningSummary))
		{
			weeklyEventMaterialPreviewGroup.Materials.Add(new EventMaterialReference
			{
				MaterialType = "kingdom_opening_summary",
				Label = text + " 开局概要",
				KingdomId = kingdom?.StringId ?? "",
				SnapshotText = kingdomOpeningSummary
			});
		}
		TryAddKingdomCurrentRulerMaterial(weeklyEventMaterialPreviewGroup.Materials, kingdom);
		return weeklyEventMaterialPreviewGroup;
	}

internal static Hero ResolveWeeklyNpcActionHero(Dictionary<string, Hero> actionHeroLookup, string heroId)
	{
		string normalizedHeroId = (heroId ?? "").Trim();
		if (!string.IsNullOrWhiteSpace(normalizedHeroId) && actionHeroLookup != null && actionHeroLookup.TryGetValue(normalizedHeroId, out Hero hero))
		{
			return hero;
		}
		return FindHeroById(normalizedHeroId);
	}

internal static List<WeeklyEventMaterialPreviewGroup> OrderWeeklyReportGenerationGroups(List<WeeklyEventMaterialPreviewGroup> groups)
	{
		List<WeeklyEventMaterialPreviewGroup> list = (groups ?? new List<WeeklyEventMaterialPreviewGroup>()).Where((WeeklyEventMaterialPreviewGroup x) => x != null).ToList();
		List<string> kingdomIdsByPlayerProximity = GetKingdomIdsByPlayerProximity(list.Where((WeeklyEventMaterialPreviewGroup x) => string.Equals((x.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase)).Select((WeeklyEventMaterialPreviewGroup x) => x.KingdomId));
		return WeeklyMaterialBatchPlanner.OrderGroups(list, kingdomIdsByPlayerProximity);
	}

internal static Kingdom ResolveHeroKingdomForWeeklyMaterial(Hero hero)
	{
		try
		{
			return (hero?.MapFaction as Kingdom) ?? hero?.Clan?.Kingdom;
		}
		catch
		{
			return null;
		}
	}

internal static void CancelUnavailableHeroCompressionWorkAfterLoad(MemoryBusinessStateOwner memory,Func<MemoryBusinessStateOwner> queues)
	{
		try
		{
			// This is a single load-time registry pass, not a per-tick Hero.FindFirst scan over saved memory.
			HashSet<string> eligibleHeroIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int observedAliveHeroCount = 0;
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				observedAliveHeroCount++;
				if (IsHeroNpcEligibleForCompressedMemory(hero))
				{
					eligibleHeroIds.Add(CampaignCharacterRecordCaptureAdapter.GetMemoryHeroId(hero));
				}
			}
			if (observedAliveHeroCount <= 0)
			{
				Logger.Log("CompressedMemory", "skip unavailable hero compression cancellation because the alive Hero registry is empty at load finish");
				return;
			}
			HashSet<string> candidateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			void AddCandidateIds(IEnumerable<string> ids)
			{
				foreach (string id in ids ?? Enumerable.Empty<string>())
				{
					string normalizedId = MemoryRecordRules.NormalizeMemoryHeroId(id);
					if (!string.IsNullOrWhiteSpace(normalizedId) && !MemoryBusinessStateOwner.IsNonHeroMemoryId(normalizedId))
					{
						candidateIds.Add(normalizedId);
					}
				}
			}
			AddCandidateIds(memory.Drafts?.Keys);
			AddCandidateIds(memory.Blocks?.Keys);
			AddCandidateIds(memory.Overviews?.Keys);
			AddCandidateIds(memory.OverviewStorage?.Keys);
			AddCandidateIds(memory.MajorSummaries?.Keys);
			AddCandidateIds(memory.MajorStorage?.Keys);
			AddCandidateIds((memory.DailyQueue ?? new List<MemorySummaryJob>()).Select((MemorySummaryJob x) => x?.HeroId));
			AddCandidateIds((memory.OverviewQueue ?? new List<MemoryOverviewJob>()).Select((MemoryOverviewJob x) => x?.HeroId));
			AddCandidateIds((memory.MajorQueue ?? new List<MajorActionSummaryJob>()).Select((MajorActionSummaryJob x) => x?.HeroId));
			AddCandidateIds(memory.DirtyOverviewIds);
			AddCandidateIds(memory.OverviewCandidateIds);
			int cancelledCount = 0;
			foreach (string id in candidateIds)
			{
				if (!eligibleHeroIds.Contains(id) && queues().CancelUnavailableHeroCompressionWorkById(id, "game_load_finished"))
				{
					cancelledCount++;
				}
			}
			if (cancelledCount > 0)
			{
				Logger.Log("CompressedMemory", "cancelled unavailable hero compression work after load count=" + cancelledCount);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("CompressedMemory", "[WARN] unavailable hero compression cancellation after load failed: " + ex.Message);
		}
	}

internal Dictionary<MobileParty,string> PartyMemoryIds = new Dictionary<MobileParty,string>();
internal string GetOrCreateWildernessNonHeroPartyMemoryKey(MobileParty party)
	{
		if (!IsValidWildernessNonHeroMemoryParty(party))
		{
			return "";
		}
		if (PartyMemoryIds == null)
		{
			PartyMemoryIds = new Dictionary<MobileParty, string>();
		}
		if (PartyMemoryIds.TryGetValue(party, out var existing) && !string.IsNullOrWhiteSpace(existing))
		{
			return "party_guid:" + MemoryRecordRules.NormalizeMemoryHeroId(existing);
		}
		string id = Guid.NewGuid().ToString("N");
		PartyMemoryIds[party] = id;
		Logger.Log("DialogueHistory", "assigned wilderness non-hero party memory id party=" + (party.StringId ?? party.Name?.ToString() ?? "") + " id=" + id);
		return "party_guid:" + id;
	}

internal string GetExistingWildernessNonHeroPartyMemoryKey(MobileParty party)
	{
		if (!IsValidWildernessNonHeroMemoryParty(party) || PartyMemoryIds == null)
		{
			return "";
		}
		if (PartyMemoryIds.TryGetValue(party, out var existing) && !string.IsNullOrWhiteSpace(existing))
		{
			return "party_guid:" + MemoryRecordRules.NormalizeMemoryHeroId(existing);
		}
		return "";
	}

internal static bool IsValidWildernessNonHeroMemoryParty(MobileParty party)
	{
		try
		{
			if (party == null || party == MobileParty.MainParty || party.Party == null || party.Party == PartyBase.MainParty)
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

internal void SanitizeWildernessNonHeroPartyMemoryIdMap(bool removeInactive)
	{
		if (PartyMemoryIds == null)
		{
			PartyMemoryIds = new Dictionary<MobileParty, string>();
			return;
		}
		Dictionary<MobileParty, string> sanitized = new Dictionary<MobileParty, string>();
		foreach (KeyValuePair<MobileParty, string> item in PartyMemoryIds.ToList())
		{
			MobileParty party = item.Key;
			string id = MemoryRecordRules.NormalizeMemoryHeroId(item.Value);
			if (string.IsNullOrWhiteSpace(id) || party == null)
			{
				continue;
			}
			if (removeInactive && !IsValidWildernessNonHeroMemoryParty(party))
			{
				continue;
			}
			sanitized[party] = id;
		}
		PartyMemoryIds = sanitized;
	}

internal List<string> BuildNonHeroPartyMemoryNeedles(MobileParty mobileParty, PartyBase partyBase = null)
	{
		List<string> needles = new List<string>();
		try
		{
			PartyBase resolvedPartyBase = partyBase ?? mobileParty?.Party;
			string stringId = MemoryRecordRules.NormalizeMemoryHeroId(mobileParty?.StringId);
			if (!string.IsNullOrWhiteSpace(stringId))
			{
				// MobileParty.StringId 是随存档保存的唯一对象 ID，是非 hero 野外部队记忆的主键。
				needles.Add("|party:party_string_id:" + stringId);
				// 兼容旧版曾经写入的未标注 StringId 记录；只用 StringId，不用显示名称。
				needles.Add("|party:" + stringId);
			}
			if (mobileParty != null && PartyMemoryIds != null && PartyMemoryIds.TryGetValue(mobileParty, out var guid))
			{
				string normalizedGuid = MemoryRecordRules.NormalizeMemoryHeroId(guid);
				if (!string.IsNullOrWhiteSpace(normalizedGuid))
				{
					// 仅作为没有 StringId 的异常部队兜底；销毁时仍要能清掉旧 GUID 记忆。
					needles.Add("|party:party_guid:" + normalizedGuid);
				}
			}
			if (resolvedPartyBase != null && resolvedPartyBase.Index >= 0)
			{
				// party_index 只清理当前会话里已存在的旧 fallback 记录，不能作为读档后的持久主键。
				needles.Add("|party:party_index:" + resolvedPartyBase.Index);
			}
		}
		catch
		{
		}
		return needles.Where((string x) => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

internal static bool ContainsExactNonHeroPartyNeedle(string memoryId, string partyNeedle)
	{
		string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
		string needle = MemoryRecordRules.NormalizeMemoryHeroId(partyNeedle);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(needle))
		{
			return false;
		}
		int index = text.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
		while (index >= 0)
		{
			int after = index + needle.Length;
			if (after >= text.Length || text[after] == '|')
			{
				return true;
			}
			index = text.IndexOf(needle, index + 1, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

internal void RemoveWildernessNonHeroPartyMemory(CampaignBattleRecordCaptureAdapter cleanup, MobileParty party, PartyBase partyBase, string reason)
	{
		try
		{
			MobileParty resolvedParty = party ?? partyBase?.MobileParty;
			if (!cleanup.TryReserveDestroyedPartyMemoryCleanup(partyBase, resolvedParty))
			{
				return;
			}
			string guid = "";
			if (resolvedParty != null && PartyMemoryIds != null)
			{
				PartyMemoryIds.TryGetValue(resolvedParty, out guid);
			}
			cleanup.CleanupNonHeroMemoryForRemovedParty(partyBase, resolvedParty, reason);
			if (resolvedParty != null && PartyMemoryIds != null)
			{
				PartyMemoryIds.Remove(resolvedParty);
			}
			if (!string.IsNullOrWhiteSpace(guid))
			{
				Logger.Log("DialogueHistory", "released wilderness non-hero fallback party guid party=" + (resolvedParty?.StringId ?? resolvedParty?.Name?.ToString() ?? "") + " guid=" + guid + " reason=" + (reason ?? ""));
			}
		}
		catch (Exception ex)
		{
			Logger.Log("DialogueHistory", "[WARN] remove wilderness non-hero party memory failed: " + ex.Message);
		}
	}

internal static void MigrateNonHeroPartyIndexMemory(string canonicalMemoryId, string baseUnnamedKey)
	{
		// Do not migrate party_index memories into another canonical key.
		// party_index identifies one wilderness party inside one save; merging aliases here
		// would combine multiple bandit/caravan parties and recreate shared memory.
	}

internal static void MigrateNonHeroPartyScopedMemory(Action<string,string> merge, string canonicalMemoryId, string partyKey)
	{
		string target = MemoryRecordRules.NormalizeMemoryHeroId(canonicalMemoryId);
		string sourcePartyKey = MemoryRecordRules.NormalizeMemoryHeroId(partyKey);
		if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(sourcePartyKey) || !MemoryBusinessStateOwner.IsNonHeroMemoryId(target))
		{
			return;
		}
		int partyMarker = target.LastIndexOf("|party:", StringComparison.OrdinalIgnoreCase);
		if (partyMarker < 0)
		{
			return;
		}
		string sourcePartySegment = sourcePartyKey;
		if (sourcePartyKey.StartsWith("party_string_id:", StringComparison.OrdinalIgnoreCase))
		{
			// 兼容旧版曾经写入的未标注 StringId 记录；不迁移名字记录，避免同名部队再次共享记忆。
			sourcePartySegment = sourcePartyKey.Substring("party_string_id:".Length);
		}
		if (string.IsNullOrWhiteSpace(sourcePartySegment))
		{
			return;
		}
		string source = target.Substring(0, partyMarker + "|party:".Length) + sourcePartySegment;
		if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		MemoryHistoryCommitBannerlordAdapter.LogNonHeroMemoryTrace("stage=migrate_party_scoped source=" + source + " target=" + target + " partyKey=" + sourcePartyKey);
		merge(source, target);
	}

internal static HashSet<string> CollectNonHeroMemoryAliasesByPrefix(MemoryBusinessStateOwner memory, string legacyPrefix, string targetId)
	{
		HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string prefix = MemoryRecordRules.NormalizeMemoryHeroId(legacyPrefix);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetId);
		if (string.IsNullOrWhiteSpace(prefix))
		{
			return aliases;
		}
		void AddAlias(string memoryId)
		{
			string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, target, StringComparison.OrdinalIgnoreCase) && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				aliases.Add(text);
			}
		}
		void AddKeys<T>(Dictionary<string, T> dictionary)
		{
			if (dictionary == null)
			{
				return;
			}
			foreach (string key in dictionary.Keys)
			{
				AddAlias(key);
			}
		}
		AddKeys(memory.History);
		AddKeys(memory.HistoryStorage);
		AddKeys(memory.Drafts);
		AddKeys(memory.DraftStorage);
		AddKeys(memory.Blocks);
		AddKeys(memory.BlockStorage);
		AddKeys(memory.Overviews);
		AddKeys(memory.OverviewStorage);
		AddKeys(memory.MajorSummaries);
		AddKeys(memory.MajorStorage);
		AddKeys(memory.MajorActions);
		AddKeys(memory.MajorActionStorage);
		AddKeys(memory.RecentActions);
		AddKeys(memory.RecentActionStorage);
		foreach (MemorySummaryJob job in memory.DailyQueue ?? new List<MemorySummaryJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (MemoryOverviewJob job in memory.OverviewQueue ?? new List<MemoryOverviewJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (MajorActionSummaryJob job in memory.MajorQueue ?? new List<MajorActionSummaryJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (WeeklyMemoryMaterialTrigger trigger in memory.PendingWeeklyTriggers ?? new List<WeeklyMemoryMaterialTrigger>())
		{
			AddAlias(trigger?.MemoryId);
		}
		foreach (string id in memory.OverviewCandidateIds ?? new Queue<string>())
		{
			AddAlias(id);
		}
		return aliases;
	}

internal static HashSet<string> CollectNonHeroMemoryAliasesByNeedle(MemoryBusinessStateOwner memory, string needle, string targetId)
	{
		HashSet<string> aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string normalizedNeedle = MemoryRecordRules.NormalizeMemoryHeroId(needle);
		string target = MemoryRecordRules.NormalizeMemoryHeroId(targetId);
		if (string.IsNullOrWhiteSpace(normalizedNeedle))
		{
			return aliases;
		}
		void AddAlias(string memoryId)
		{
			string text = MemoryRecordRules.NormalizeMemoryHeroId(memoryId);
			if (!string.IsNullOrWhiteSpace(text) && MemoryBusinessStateOwner.IsNonHeroMemoryId(text) && !string.Equals(text, target, StringComparison.OrdinalIgnoreCase) && text.IndexOf(normalizedNeedle, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				aliases.Add(text);
			}
		}
		void AddKeys<T>(Dictionary<string, T> dictionary)
		{
			if (dictionary == null)
			{
				return;
			}
			foreach (string key in dictionary.Keys)
			{
				AddAlias(key);
			}
		}
		AddKeys(memory.History);
		AddKeys(memory.HistoryStorage);
		AddKeys(memory.Drafts);
		AddKeys(memory.DraftStorage);
		AddKeys(memory.Blocks);
		AddKeys(memory.BlockStorage);
		AddKeys(memory.Overviews);
		AddKeys(memory.OverviewStorage);
		AddKeys(memory.MajorSummaries);
		AddKeys(memory.MajorStorage);
		AddKeys(memory.MajorActions);
		AddKeys(memory.MajorActionStorage);
		AddKeys(memory.RecentActions);
		AddKeys(memory.RecentActionStorage);
		foreach (MemorySummaryJob job in memory.DailyQueue ?? new List<MemorySummaryJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (MemoryOverviewJob job in memory.OverviewQueue ?? new List<MemoryOverviewJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (MajorActionSummaryJob job in memory.MajorQueue ?? new List<MajorActionSummaryJob>())
		{
			AddAlias(job?.HeroId);
		}
		foreach (WeeklyMemoryMaterialTrigger trigger in memory.PendingWeeklyTriggers ?? new List<WeeklyMemoryMaterialTrigger>())
		{
			AddAlias(trigger?.MemoryId);
		}
		foreach (string id in memory.OverviewCandidateIds ?? new Queue<string>())
		{
			AddAlias(id);
		}
		return aliases;
	}


internal static string BuildNpcActionsRuntimeConstraintHint(MemoryBusinessStateOwner state, NpcActionRecordOwner records, Func<int> currentDay, Func<Hero,CharacterObject,int,string> targetKey, Hero hero, bool recentOnly, CharacterObject targetCharacter = null, int targetAgentIndex = -1)
	{
		if (recentOnly)
		{
			return "";
		}
		Hero observerHero = hero ?? targetCharacter?.HeroObject;
		string text = CampaignCharacterRecordCaptureAdapter.BuildNpcActionSummary(state, records, observerHero, false, currentDay);
		string observerKey = targetKey(null, targetCharacter, targetAgentIndex);
		string observerCultureId = targetCharacter?.Culture?.StringId;
		string playerText = observerHero != null
			? PlayerNotorietyBehavior.BuildPlayerMajorRuntimeInstructionForExternal(observerHero)
			: PlayerNotorietyBehavior.BuildPlayerMajorRuntimeInstructionForExternal(observerKey, observerCultureId);
		if (!string.IsNullOrWhiteSpace(playerText))
		{
			text = string.IsNullOrWhiteSpace(text) ? playerText.Trim() : text.Trim() + "\n\n" + playerText.Trim();
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return AIConfigHandler.ResolveRuleRuntimeText("npc_major_actions", "no_data", forConstraint: true, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
	}

internal static string ParseLordIdFromUnnamedKey(string unnamedKey)
	{
		string text = (unnamedKey ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		int num = text.IndexOf(":lord:", StringComparison.OrdinalIgnoreCase);
		if (num < 0)
		{
			return "";
		}
		string text2 = text.Substring(num + ":lord:".Length).Trim();
		int num2 = text2.IndexOf(':');
		if (num2 >= 0)
		{
			text2 = text2.Substring(0, num2).Trim();
		}
		return text2;
	}


internal static string GetSettlementTypeLabel(Settlement settlement)
	{
		if (settlement == null)
		{
			return "";
		}
		if (settlement.IsVillage)
		{
			return "村庄";
		}
		if (settlement.IsTown)
		{
			return "城镇";
		}
		if (settlement.IsCastle)
		{
			return "城堡";
		}
		return "定居点";
	}

internal static string ResolveSettlementDisplay(string settlementId)
	{
		string text = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		try
		{
			Settlement settlement = Settlement.Find(text);
			if (settlement != null)
			{
				string text2 = settlement.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(text2))
				{
					return text2;
				}
			}
		}
		catch
		{
		}
		return text;
	}

internal static WeeklyHeroFact CaptureWeeklyHeroFact(string heroId)
	{
		Hero hero = FindHeroById(heroId);
		if (hero == null) return null;
		return new WeeklyHeroFact
		{
			Id = hero.StringId ?? "",
			ClanId = hero.Clan?.StringId ?? "",
			KingdomId = hero.MapFaction?.StringId ?? hero.Clan?.Kingdom?.StringId ?? "",
			SpouseId = hero.Spouse?.StringId ?? "",
			FatherId = hero.Father?.StringId ?? "",
			MotherId = hero.Mother?.StringId ?? "",
			IsFemale = hero.IsFemale
		};
	}

internal static string CaptureWeeklySettlementName(string settlementId)
	{
		string id = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)) return "";
		try
		{
			Settlement settlement = Settlement.Find(id);
			if (settlement != null) return GetSettlementDisplayName(settlement);
		}
		catch
		{
		}
		return ResolveSettlementDisplay(id);
	}

internal static string CaptureWeeklySettlementNameWithType(string settlementId)
	{
		string id = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id)) return "";
		try
		{
			Settlement settlement = Settlement.Find(id);
			if (settlement != null)
			{
				string name = GetSettlementDisplayName(settlement);
				string type = GetSettlementTypeLabel(settlement);
				if (!string.IsNullOrWhiteSpace(name))
					return string.IsNullOrWhiteSpace(type) ? name : type + name;
			}
		}
		catch
		{
		}
		return ResolveSettlementDisplay(id);
	}

internal static Hero ResolveHeroByIdForNpcData(string heroId)
	{
		string text = (heroId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		try
		{
			return Hero.Find(text) ?? Hero.FindFirst((Hero x) => x != null && string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return null;
		}
	}

internal static Hero ResolveUniqueHeroByNpcFileDisplayName(string fileDisplayName)
	{
		if (!NpcDataFileName.IsDisplayNameSpecified(fileDisplayName))
		{
			return null;
		}
		string text = NpcDataFileName.NormalizeDisplayName(fileDisplayName);
		try
		{
			List<Hero> list = ((IEnumerable<Hero>)Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>())
				.Where((Hero x) => x != null && string.Equals(NpcDataFileName.NormalizeDisplayName(x.Name?.ToString() ?? ""), text, StringComparison.OrdinalIgnoreCase))
				.Take(2)
				.ToList();
			return list.Count == 1 ? list[0] : null;
		}
		catch
		{
			return null;
		}
	}

internal static List<PartyTransferPromptEntry> CopyMemoryPartyOptions(List<PartyTransferPromptEntry> values)
    {
        return values?.Select(x => x == null ? null : new PartyTransferPromptEntry
        {
            PromptIndex = x.PromptIndex, Section = x.Section, Character = x.Character,
            DisplayName = x.DisplayName, Count = x.Count, WoundedCount = x.WoundedCount,
            WageDenarsPerDay = x.WageDenarsPerDay, HirePriceDenarsPerUnit = x.HirePriceDenarsPerUnit,
            BuyPriceDenarsPerUnit = x.BuyPriceDenarsPerUnit, IsHero = x.IsHero,
            OwnerParty = x.OwnerParty, SourceSettlement = x.SourceSettlement, VolunteerOwner = x.VolunteerOwner,
            VolunteerSlotIndices = x.VolunteerSlotIndices?.ToList()
        }).ToList();
    }

internal static List<SettlementTransferPromptEntry> CopyMemorySettlementOptions(List<SettlementTransferPromptEntry> values)
    {
        return values?.Select(x => x == null ? null : new SettlementTransferPromptEntry
        {
            PromptIndex = x.PromptIndex, Section = x.Section, AssetKind = x.AssetKind,
            Settlement = x.Settlement, Workshop = x.Workshop, CaravanParty = x.CaravanParty,
            OwnerHero = x.OwnerHero, SettlementId = x.SettlementId, AssetId = x.AssetId,
            DisplayName = x.DisplayName, TypeLabel = x.TypeLabel,
            DailyIncomeDenars = x.DailyIncomeDenars, GuidePriceDenars = x.GuidePriceDenars, OwnerClan = x.OwnerClan
        }).ToList();
    }

internal static string BuildNpcActionActorNarrativeText(NpcActionEntry entry)
	{
		string text = (entry?.Text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		string text2 = ResolveHeroDisplay(entry.ActorHeroId);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return text;
		}
		if (text.StartsWith("你的", StringComparison.Ordinal))
		{
			return text2 + "的" + text.Substring(2);
		}
		if (text.StartsWith("你", StringComparison.Ordinal))
		{
			return text2 + text.Substring(1);
		}
		return text.Replace(" 你", " " + text2).Replace("；你", "；" + text2).Replace("，你", "，" + text2);
	}

internal static string ResolveHeroDisplay(string heroId)
	{
		string text = ResolveHeroName(heroId);
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return (heroId ?? "").Trim();
	}
}
