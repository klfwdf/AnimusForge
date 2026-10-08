using TaleWorlds.CampaignSystem.Actions;
using static AnimusForge.CampaignCharacterRecordCaptureAdapter;
using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using static AnimusForge.MemoryEntityIdentityBannerlordAdapter;
using static AnimusForge.KingdomStabilityPolicy;
using TaleWorlds.CampaignSystem;
using WeeklyEventMaterialPreviewGroup = AnimusForge.MyBehavior.WeeklyEventMaterialPreviewGroup;
using KingdomStabilityTier = AnimusForge.KingdomStabilityPolicy.KingdomStabilityTier;
namespace AnimusForge;
// Live Campaign capture reads the existing stability authority, never copies its dictionaries.
internal sealed class KingdomStabilityGameAdapter
{
 private readonly Func<KingdomStabilityOwner> _state;
 private readonly KingdomCampaignRecordPorts _events;
 internal const int PlayerCreatedKingdomInitialStabilityValue = 70;
 private readonly Func<Dictionary<string, string>> _relationStorage;
 private readonly Func<KingdomMaintenanceOwner<Kingdom>> _maintenance;
 internal KingdomStabilityGameAdapter(Func<KingdomStabilityOwner> state, Func<Dictionary<string, string>> relationStorage, Func<KingdomMaintenanceOwner<Kingdom>> maintenance, KingdomCampaignRecordPorts events = null) { _state=state; _relationStorage=relationStorage; _maintenance=maintenance; _events=events; }
internal static string GetKingdomStabilityTierText(int value)
	{
		switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))
		{
		case KingdomStabilityTier.ExtremelyHigh:
			return "极高";
		case KingdomStabilityTier.High:
			return "高";
		case KingdomStabilityTier.FairlyHigh:
			return "较高";
		case KingdomStabilityTier.Average:
			return "一般";
		case KingdomStabilityTier.Poor:
			return "较差";
		case KingdomStabilityTier.VeryPoor:
			return "很差";
		default:
			return "极差";
		}
	}
internal string BuildWeeklyReportCurrentKingdomStabilityTierText(WeeklyEventMaterialPreviewGroup group)
	{
		if (!string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		Kingdom kingdom = MemoryEntityIdentityBannerlordAdapter.FindKingdomById(group?.KingdomId);
		if (kingdom == null)
		{
			return "";
		}
		return "当前王国稳定度评级：" + GetKingdomStabilityTierText(_state().Get(MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom)));
	}

internal void SetKingdomStabilityValue(Kingdom kingdom, int value)
	{
		string text = GetKingdomId(kingdom);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		_state().Set(text, value);
		if (DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			ApplyKingdomStabilityRelationAdjustmentsForKingdom(kingdom);
		}
	}

internal static bool ShouldApplyKingdomStabilityRelationAdjustmentToClan(Clan clan, Kingdom kingdom)
	{
		return clan != null && kingdom != null && clan.Kingdom == kingdom && clan != kingdom.RulingClan && !clan.IsEliminated && !clan.IsUnderMercenaryService && !clan.IsClanTypeMercenary;
	}

internal static IEnumerable<Hero> GetClanHeroesForKingdomStabilityRelationAdjustment(Clan clan)
	{
		if (clan?.Heroes == null)
		{
			return Enumerable.Empty<Hero>();
		}
		return clan.Heroes.Where((Hero x) => x != null && x.IsAlive && !x.IsChild).Distinct();
	}

internal static string BuildKingdomStabilityRelationOffsetKey(string kingdomId, Hero sourceHero, Hero targetHero)
	{
		string text = (kingdomId ?? "").Trim();
		string heroId = GetHeroId(sourceHero);
		string heroId2 = GetHeroId(targetHero);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(heroId2))
		{
			return "";
		}
		return text + "|" + heroId + "|" + heroId2;
	}

internal static bool TryResolveKingdomStabilityRelationOffsetKey(string key, out string kingdomId, out Hero sourceHero, out Hero targetHero)
	{
		kingdomId = "";
		sourceHero = null;
		targetHero = null;
		string[] array = (key ?? "").Split(new char[1] { '|' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 3)
		{
			return false;
		}
		kingdomId = (array[0] ?? "").Trim();
		sourceHero = FindHeroById(array[1]);
		targetHero = FindHeroById(array[2]);
		return !string.IsNullOrWhiteSpace(kingdomId);
	}

internal int ApplyKingdomStabilityRelationOffsetToPair(string pairKey, Hero sourceHero, Hero targetHero, int desiredOffset)
	{
		if (_state().RelationOffsets == null)
		{
			_state().RelationOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (string.IsNullOrWhiteSpace(pairKey))
		{
			return 0;
		}
		int value = 0;
		_state().RelationOffsets.TryGetValue(pairKey, out value);
		if (sourceHero == null || targetHero == null || sourceHero == targetHero)
		{
			return value;
		}
		int heroRelation = CharacterRelationManager.GetHeroRelation(sourceHero, targetHero);
		int num2 = KingdomStabilityOwner.ResolveRelation(heroRelation, value, desiredOffset, out int appliedOffset);
		if (num2 != heroRelation)
		{
			sourceHero.SetPersonalRelation(targetHero, num2);
		}
		return appliedOffset;
	}

internal void ApplyKingdomStabilityRelationAdjustmentsForKingdom(Kingdom kingdom)
	{
		if (_state().RelationOffsets == null)
		{
			_state().RelationOffsets = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		string kingdomId = GetKingdomId(kingdom);
		if (string.IsNullOrWhiteSpace(kingdomId))
		{
			return;
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			ClearKingdomStabilityRelationAdjustmentsForKingdom(kingdomId);
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			ClearKingdomStabilityRelationAdjustmentsForKingdom(kingdomId);
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, Hero> dictionary2 = new Dictionary<string, Hero>(StringComparer.OrdinalIgnoreCase);
		Hero hero = kingdom?.Leader ?? kingdom?.RulingClan?.Leader;
		int kingdomStabilityRelationTargetOffset = GetKingdomStabilityRelationTargetOffset(GetKingdomStabilityValue(kingdom));
		if (kingdom != null && !kingdom.IsEliminated && hero != null && kingdomStabilityRelationTargetOffset != 0 && kingdom.Clans != null)
		{
			foreach (Clan clan in kingdom.Clans)
			{
				if (!ShouldApplyKingdomStabilityRelationAdjustmentToClan(clan, kingdom))
				{
					continue;
				}
				foreach (Hero item in GetClanHeroesForKingdomStabilityRelationAdjustment(clan))
				{
					string text = BuildKingdomStabilityRelationOffsetKey(kingdomId, hero, item);
					if (!string.IsNullOrWhiteSpace(text))
					{
						dictionary[text] = kingdomStabilityRelationTargetOffset;
						dictionary2[text] = item;
					}
				}
			}
		}
		_state().Reconcile(kingdomId, dictionary, (key, desiredOffset) =>
		{
			Hero sourceHero = hero;
			bool hasCurrentPair = dictionary2.TryGetValue(key, out Hero targetHero) && sourceHero != null;
			if (!hasCurrentPair && !TryResolveKingdomStabilityRelationOffsetKey(key, out _, out sourceHero, out targetHero))
				return null;
			return (int?)ApplyKingdomStabilityRelationOffsetToPair(key, sourceHero, targetHero, desiredOffset);
		});
	}

internal void ClearKingdomStabilityRelationAdjustmentsForKingdom(string kingdomId)
	{
		if (_state().RelationOffsets == null)
		{
			return;
		}
		string text = (kingdomId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		foreach (string key in _state().RelationOffsets.Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(text + "|", StringComparison.OrdinalIgnoreCase)).ToList())
		{
			if (TryResolveKingdomStabilityRelationOffsetKey(key, out var _, out var sourceHero, out var targetHero))
			{
				ApplyKingdomStabilityRelationOffsetToPair(key, sourceHero, targetHero, 0);
			}
			_state().RelationOffsets.Remove(key);
		}
		if (_relationStorage() != null)
		{
			foreach (string key2 in _relationStorage().Keys.Where((string x) => !string.IsNullOrWhiteSpace(x) && x.StartsWith(text + "|", StringComparison.OrdinalIgnoreCase)).ToList())
			{
				_relationStorage().Remove(key2);
			}
		}
	}

internal void ClearKingdomStabilityRelationAdjustments()
	{
		if (_state().RelationOffsets == null || _state().RelationOffsets.Count == 0)
		{
			_relationStorage()?.Clear();
			return;
		}
		foreach (string key in _state().RelationOffsets.Keys.ToList())
		{
			if (TryResolveKingdomStabilityRelationOffsetKey(key, out var _, out var sourceHero, out var targetHero))
			{
				ApplyKingdomStabilityRelationOffsetToPair(key, sourceHero, targetHero, 0);
			}
			_state().RelationOffsets.Remove(key);
		}
		_relationStorage()?.Clear();
	}

internal void ApplyKingdomStabilityRelationAdjustments()
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			ClearKingdomStabilityRelationAdjustments();
			return;
		}
		if (Kingdom.All == null || Kingdom.All.Count == 0)
		{
			return;
		}
		foreach (Kingdom item in Kingdom.All.Where((Kingdom x) => x != null))
		{
			ApplyKingdomStabilityRelationAdjustmentsForKingdom(item);
		}
	}

internal static int CountActiveKingdomClansForLowClanCountRule(Kingdom kingdom)
	{
		if (kingdom?.Clans == null)
		{
			return 0;
		}
		Clan rulingClan = kingdom.RulingClan;
		return kingdom.Clans.Count((Clan x) => x != null && x != rulingClan && !x.IsEliminated && !x.IsUnderMercenaryService && !x.IsClanTypeMercenary);
	}

internal static void SyncTownRebelliousStateFromCurrentLoyalty(Town town)
	{
		if (town?.Settlement == null)
		{
			return;
		}
		int rebelliousStateStartLoyaltyThreshold = Campaign.Current?.Models?.SettlementLoyaltyModel?.RebelliousStateStartLoyaltyThreshold ?? 25;
		bool inRebelliousState = town.InRebelliousState;
		town.InRebelliousState = town.Loyalty <= (float)rebelliousStateStartLoyaltyThreshold;
		if (inRebelliousState != town.InRebelliousState)
		{
			CampaignEventDispatcher.Instance.TownRebelliousStateChanged(town, town.InRebelliousState);
		}
	}

internal void ApplyRulingClanSettlementLoyaltyAdjustmentForLowClanCountKingdom(Town town)
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			return;
		}
		if (town?.Settlement == null || !town.Settlement.IsFortification)
		{
			return;
		}
		Clan ownerClan = town.Settlement.OwnerClan;
		Kingdom kingdom = ownerClan?.Kingdom;
		if (ownerClan == null || kingdom == null || kingdom.IsEliminated || ownerClan != kingdom.RulingClan)
		{
			return;
		}
		if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
		{
			return;
		}
		int num = CountActiveKingdomClansForLowClanCountRule(kingdom);
		if (num > 4)
		{
			return;
		}
		int lowClanCountRoyalDomainLoyaltyAdjustment = GetLowClanCountRoyalDomainLoyaltyAdjustment(GetKingdomStabilityValue(kingdom), num);
		if (lowClanCountRoyalDomainLoyaltyAdjustment == 0)
		{
			return;
		}
		float maximumLoyaltyInSettlement = Campaign.Current?.Models?.SettlementLoyaltyModel?.MaximumLoyaltyInSettlement ?? 100;
		town.Loyalty = MBMath.ClampFloat(town.Loyalty + (float)lowClanCountRoyalDomainLoyaltyAdjustment, 0f, maximumLoyaltyInSettlement);
		SyncTownRebelliousStateFromCurrentLoyalty(town);
	}

internal static string BuildClanFortificationSummary(Clan clan)
	{
		List<Settlement> list = clan?.Settlements?.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle)).ToList() ?? new List<Settlement>();
		if (list.Count == 0)
		{
			return "";
		}
		List<string> list2 = list.Select(GetSettlementDisplayName).Take(4).ToList();
		string text = string.Join("、", list2);
		if (list.Count > list2.Count)
		{
			text = text + " 等" + list.Count + "处";
		}
		return GetClanDisplayName(clan) + "家族当前掌握的核心封地包括：" + text + "。";
	}

internal void ApplyWeeklyReportStabilityDelta(WeeklyEventMaterialPreviewGroup group, string eventId, string tagText)
	{
		if (_state().WeeklyDeltas == null)
		{
			_state().WeeklyDeltas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			return;
		}
		string text = (eventId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		int value = 0;
		_state().WeeklyDeltas.TryGetValue(text, out value);
		int num = 0;
		Kingdom kingdom = null;
		if (string.Equals((group?.GroupKind ?? "").Trim(), "kingdom", StringComparison.OrdinalIgnoreCase))
		{
			kingdom = FindKingdomById(group?.KingdomId);
			if (kingdom != null)
			{
				num = WeeklyGenerationRules.ExtractWeeklyReportStabilityDelta(tagText);
			}
		}
		if (num == value)
		{
			return;
		}
		if (kingdom == null)
		{
			return;
		}
		_state().ApplyWeeklyDelta(text, GetKingdomStabilityValue(kingdom), num,
			updated => SetKingdomStabilityValue(kingdom, updated));
	}

internal static string FormatKingdomStabilityRelationOffsetText(int offset)
	{
		if (offset > 0)
		{
			return "+" + offset;
		}
		return offset.ToString();
	}

internal static string FormatKingdomRebellionChance(float chance)
	{
		float num = Math.Max(0f, chance) * 100f;
		return num.ToString((num < 1f) ? "0.0##" : "0.##") + "%";
	}

internal string BuildKingdomStabilityEncyclopediaText(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return "";
		}
		int stabilityValue = GetKingdomStabilityValue(kingdom);
		bool enabled = DuelSettings.IsKingdomStabilityAndRebellionEnabled();
		bool protectedByImmunity = PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom);
		float rebellionChance = enabled && !protectedByImmunity ? GetKingdomRebellionWeeklyChance(stabilityValue) : 0f;
		int relationOffset = enabled && !protectedByImmunity ? GetKingdomStabilityRelationTargetOffset(stabilityValue) : 0;
		int activeClanCount = CountActiveKingdomClansForLowClanCountRule(kingdom);
		int royalDomainLoyaltyAdjustment = enabled && !protectedByImmunity && activeClanCount <= 4 ? GetLowClanCountRoyalDomainLoyaltyAdjustment(stabilityValue, activeClanCount) : 0;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("【王国稳定度】");
		stringBuilder.AppendLine("当前数值：" + stabilityValue + "/100（" + GetKingdomStabilityTierText(stabilityValue) + "）");
		if (!enabled)
		{
			stringBuilder.AppendLine("机制状态：MCM 已关闭，仅显示当前保存数值。");
			return stringBuilder.ToString().TrimEnd();
		}
		if (protectedByImmunity)
		{
			stringBuilder.AppendLine("本周叛乱概率：0%（玩家王国稳定度叛乱免疫）");
		}
		else
		{
			stringBuilder.AppendLine("本周叛乱概率：" + FormatKingdomRebellionChance(rebellionChance));
		}
		List<string> effects = new List<string>();
		if (relationOffset != 0)
		{
			effects.Add("国王与本国非王族成年成员关系修正 " + FormatKingdomStabilityRelationOffsetText(relationOffset));
		}
		if (royalDomainLoyaltyAdjustment != 0)
		{
			effects.Add("王室直辖地忠诚日修正 " + FormatKingdomStabilityRelationOffsetText(royalDomainLoyaltyAdjustment));
		}
		stringBuilder.AppendLine(effects.Count == 0 ? "当前额外影响：无" : ("当前额外影响：" + string.Join("；", effects)));
		return stringBuilder.ToString().TrimEnd();
	}

internal int GetKingdomStabilityValue(Kingdom kingdom)
	{
		return _state().Get(GetKingdomId(kingdom));
	}

internal bool ProcessKingdomStabilityRelationAdjustmentsSlice()
	{
		if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
		{
			ClearKingdomStabilityRelationAdjustments();
			_maintenance().ResetRelations();
			return true;
		}
		List<Kingdom> kingdoms = Kingdom.All?.Where((Kingdom x) => x != null).ToList() ?? new List<Kingdom>();
		return _maintenance().AdvanceRelations(kingdoms.Count, index => ApplyKingdomStabilityRelationAdjustmentsForKingdom(kingdoms[index]));
	}

internal static int GetKingdomStabilityRoyalDomainLoyaltyAdjustmentForTown(Town town, Func<Kingdom, int> currentValue)
	{
		try
		{
			if (!DuelSettings.IsKingdomStabilityAndRebellionEnabled())
			{
				return 0;
			}
			if (town?.Settlement == null || !town.Settlement.IsFortification)
			{
				return 0;
			}
			Clan ownerClan = town.Settlement.OwnerClan;
			Kingdom kingdom = ownerClan?.Kingdom;
			if (ownerClan == null || kingdom == null || kingdom.IsEliminated || ownerClan != kingdom.RulingClan)
			{
				return 0;
			}
			if (PlayerKingdomRebellionImmunity.ShouldProtectKingdom(kingdom))
			{
				return 0;
			}
			int activeClanCount = CountActiveKingdomClansForLowClanCountRule(kingdom);
			if (activeClanCount > 4)
			{
				return 0;
			}
			return GetLowClanCountRoyalDomainLoyaltyAdjustment(currentValue(kingdom), activeClanCount);
		}
		catch
		{
			return 0;
		}
	}

internal void ApplyWorldBulletinStability(string kingdomId,int delta,Func<WorldBulletinStateOwner> bulletin,Func<bool> publishingEnabled) { if(delta==0||!publishingEnabled()||!DuelSettings.IsKingdomStabilityAndRebellionEnabled())return;Kingdom kingdom=FindKingdomById(kingdomId);if(kingdom==null||kingdom.IsEliminated)return;string id=GetKingdomId(kingdom);bulletin().ApplyStability(id,delta,GetCurrentGameDayIndexSafe(),GetKingdomStabilityValue(kingdom),value=>_state().Set(id,value));}

internal void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		_events.Revisions.MarkRuntimeChange();
		try
		{
			string text = BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, detail);
			string text2 = BuildClanChangedKingdomNarrative(clan, oldKingdom, newKingdom, detail);
			bool flag = detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion && clan != null && oldKingdom != null;
			string clanFortificationSummary = flag ? BuildClanFortificationSummary(clan) : "";
			string text3 = text2;
			if (flag && !string.IsNullOrWhiteSpace(clanFortificationSummary))
			{
				text3 = text3 + " " + clanFortificationSummary;
			}
			string clanChangedKingdomActionKind = GetClanChangedKingdomActionKind(detail);
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts(clanChangedKingdomActionKind, item);
				npcActionFacts.TargetKingdomId = GetKingdomId(newKingdom ?? oldKingdom);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(oldKingdom));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(newKingdom));
				_events.Record().RecordNpcMajorAction(item, text3, text, npcActionFacts);
				_events.Record().RecordNpcRecentAction(item, text3, text, facts: npcActionFacts);
			}
			RecordPlayerClanChangedKingdomActionIfRelevant(clan, oldKingdom, newKingdom, detail, text3, text);
			if (flag)
			{
				Settlement settlement = clan.Settlements?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
				_events.Record().RecordEventSourceMaterial("kingdom_rebellion", "家族叛乱 - " + GetClanDisplayName(clan), text3, text + ":material", GetKingdomId(oldKingdom), GetSettlementId(settlement), includeInWorld: true, includeInKingdom: true);
			}
			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom && clan != null && newKingdom != null)
			{
				Settlement settlement2 = clan.Settlements?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));
				_events.Record().RecordEventSourceMaterial("kingdom_created", "新王国建立 - " + GetKingdomDisplayName(newKingdom, "新王国"), text2, text + ":material", GetKingdomId(newKingdom), GetSettlementId(settlement2), includeInWorld: true, includeInKingdom: true);
				if (clan == Clan.PlayerClan)
				{
					SetKingdomStabilityValue(newKingdom, PlayerCreatedKingdomInitialStabilityValue);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanChangedKingdom: " + ex.Message);
		}
	}

internal void RecordPlayerClanChangedKingdomActionIfRelevant(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, string narrative, string stableKey)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (clan == null || playerClan == null)
			{
				return;
			}
			Kingdom playerKingdom = playerClan.Kingdom;
			bool playerClanChanged = clan == playerClan;
			bool playerRulesKingdom = playerKingdom != null && playerKingdom.RulingClan == playerClan;
			bool joinsPlayerKingdom = playerRulesKingdom && newKingdom == playerKingdom && clan != playerClan;
			bool leavesPlayerKingdom = playerRulesKingdom && oldKingdom == playerKingdom && clan != playerClan;
			if (!playerClanChanged && !joinsPlayerKingdom && !leavesPlayerKingdom)
			{
				return;
			}
			string clanName = GetClanDisplayName(clan);
			string actionKind = playerClanChanged ? "player_clan_changed_kingdom" : (joinsPlayerKingdom ? "clan_joined_player_kingdom" : "clan_left_player_kingdom");
			string text;
			bool? won = null;
			if (playerClanChanged)
			{
				text = string.IsNullOrWhiteSpace(narrative) ? ("你的" + clanName + "家族发生王国归属变更。") : narrative;
			}
			else if (joinsPlayerKingdom)
			{
				text = "你统治的" + GetKingdomDisplayName(playerKingdom, "玩家王国") + "接纳了" + clanName + "家族加入。";
				won = true;
			}
			else
			{
				text = clanName + "家族脱离了你统治的" + GetKingdomDisplayName(playerKingdom, "玩家王国") + "。";
				won = false;
			}
			_events.Record().RecordExternalPlayerAction(text, stableKey + ":player_relevant", actionKind, isMajor: true, targetHero: clan.Leader, settlement: null, locationText: GetKingdomDisplayName(newKingdom ?? oldKingdom, ""), won: won);
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] RecordPlayerClanChangedKingdomActionIfRelevant: " + ex.Message);
		}
	}

internal void OnClanDefected(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
	{
		_events.Revisions.MarkRuntimeChange();
		try
		{
			string text = BuildClanChangedKingdomStableKey(clan, oldKingdom, newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection);
			string text2 = "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + GetKingdomDisplayName(oldKingdom, "原王国") + "，转投" + GetKingdomDisplayName(newKingdom, "新王国") + "。";
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_defected", item);
				npcActionFacts.TargetKingdomId = GetKingdomId(newKingdom);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(oldKingdom));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(newKingdom));
				_events.Record().RecordNpcMajorAction(item, text2, text, npcActionFacts);
				_events.Record().RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanDefected: " + ex.Message);
		}
	}

internal void OnRulingClanChanged(Kingdom kingdom, Clan eventRulingClan)
	{
		_events.Revisions.MarkRuntimeChange();
		try
		{
			// Bannerlord 1.3 passes the new ruling clan here, while 1.4.5 passes the old one.
			// Always read the post-change value from the kingdom so the behavior stays version-neutral.
			Clan rulingClan = kingdom?.RulingClan;
			_events.Rebellion().TrySyncModCreatedRebelKingdomBannerToRulingClan(kingdom, eventRulingClan, "ruling_clan_changed");
			string text = "ruling_clan_changed:" + GetKingdomId(kingdom) + ":" + GetClanId(rulingClan);
			string text2 = "你所在的" + GetClanDisplayName(rulingClan) + "家族已成为" + GetKingdomDisplayName(kingdom, "该王国") + "的执政家族。";
			foreach (Hero item in GetTrackedLordsForClan(rulingClan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("ruling_clan_changed", item);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(rulingClan));
				AddUniqueId(npcActionFacts.RelatedKingdomIds, GetKingdomId(kingdom));
				_events.Record().RecordNpcMajorAction(item, text2, text, npcActionFacts);
				_events.Record().RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnRulingClanChanged: " + ex.Message);
		}
	}

internal void OnClanLeaderChanged(Hero oldLeader, Hero newLeader)
	{
		_events.Revisions.MarkRuntimeChange();
		try
		{
			Clan clan = newLeader?.Clan ?? oldLeader?.Clan;
			if (clan == null)
			{
				return;
			}
			string text = "clan_leader_changed:" + GetClanId(clan) + ":" + GetHeroId(newLeader);
			foreach (Hero item in GetTrackedLordsForClan(clan))
			{
				NpcActionFacts npcActionFacts = CreateNpcActionFacts("clan_leader_changed", item);
				ApplyTargetFacts(npcActionFacts, newLeader);
				AddUniqueId(npcActionFacts.RelatedClanIds, GetClanId(clan));
				string text2 = string.Equals(GetHeroId(item), GetHeroId(newLeader), StringComparison.OrdinalIgnoreCase) ? ("你已成为" + GetClanDisplayName(clan) + "家族的新族长。") : (GetHeroDisplayName(newLeader) + "已成为" + GetClanDisplayName(clan) + "家族的新族长。");
				_events.Record().RecordNpcMajorAction(item, text2, text, npcActionFacts);
				_events.Record().RecordNpcRecentAction(item, text2, text, facts: npcActionFacts);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("NpcAction", "[ERROR] OnClanLeaderChanged: " + ex.Message);
		}
	}

internal void OnClanDestroyed(Clan destroyedClan)
	{
		_events.Revisions.MarkRuntimeChange();
		try
		{
			string clanDisplayName = GetClanDisplayName(destroyedClan);
			string kingdomId = GetKingdomId(destroyedClan?.Kingdom);
			string text = clanDisplayName + "家族已经覆灭。";
			bool includeInWorld = destroyedClan != null && destroyedClan == destroyedClan.Kingdom?.RulingClan;
			_events.Record().RecordEventSourceMaterial("clan_destroyed", "家族覆灭 - " + clanDisplayName, text, "clan_destroyed:" + GetClanId(destroyedClan), kingdomId, "", includeInWorld, includeInKingdom: true);
		}
		catch (Exception ex)
		{
			Logger.Log("EventMaterial", "[ERROR] OnClanDestroyed: " + ex.Message);
		}
	}

internal static string GetClanChangedKingdomActionKind(ChangeKingdomAction.ChangeKingdomActionDetail detail)
	{
		switch (detail)
		{
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion:
			return "clan_rebellion";
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection:
			return "clan_defected";
		default:
			return "clan_changed_kingdom";
		}
	}

internal static string BuildClanChangedKingdomNarrative(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail)
	{
		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, "原王国");
		string kingdomDisplayName2 = GetKingdomDisplayName(newKingdom, "新王国");
		switch (detail)
		{
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary:
			return "你所在的" + GetClanDisplayName(clan) + "家族已作为佣兵加入" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已正式加入" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection:
			return "你所在的" + GetClanDisplayName(clan) + "家族已背离" + kingdomDisplayName + "，改投" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion:
			return "你所在的" + GetClanDisplayName(clan) + "家族已脱离" + kingdomDisplayName + "并发动叛乱。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary:
			return "你所在的" + GetClanDisplayName(clan) + "家族已结束在" + kingdomDisplayName + "的佣兵服务。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom:
			return "你所在的" + GetClanDisplayName(clan) + "家族已建立" + kingdomDisplayName2 + "。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction:
			return "由于" + kingdomDisplayName + "覆灭，你所在的" + GetClanDisplayName(clan) + "家族已脱离原王国。";
		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction:
			return "你所在的" + GetClanDisplayName(clan) + "家族因家族灭亡而退出原王国体系。";
		default:
			return "你所在的" + GetClanDisplayName(clan) + "家族发生了王国归属变更。";
		}
	}

internal static string BuildClanChangedKingdomStableKey(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail)
	{
		return "clan_changed_kingdom:" + GetClanId(clan) + ":" + GetKingdomId(oldKingdom) + ":" + GetKingdomId(newKingdom) + ":" + detail;
	}
}

internal sealed class KingdomCampaignRecordPorts
{
 internal WeeklyReportMaterialRevisionOwner Revisions;
 internal Func<CampaignCharacterRecordCaptureAdapter> Record;
 internal Func<KingdomRebellionGameAdapter> Rebellion;
}
