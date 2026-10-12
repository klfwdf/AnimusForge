using TaleWorlds.CampaignSystem.Actions;

using static AnimusForge.CampaignCharacterRecordCaptureAdapter;

using NpcActionFacts = AnimusForge.MyBehavior.NpcActionFacts;

using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using TaleWorlds.CampaignSystem.Settlements;

using TaleWorlds.Library;

using TaleWorlds.Localization;

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

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_ExtremelyHigh", "Extremely high");

		case KingdomStabilityTier.High:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_High", "High");

		case KingdomStabilityTier.FairlyHigh:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_FairlyHigh", "Fairly high");

		case KingdomStabilityTier.Average:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_Average", "Average");

		case KingdomStabilityTier.Poor:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_Poor", "Poor");

		case KingdomStabilityTier.VeryPoor:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_VeryPoor", "Very poor");

		default:

			return KingdomUiText.T("AF_KINGDOM_CW_Stability_ExtremelyPoor", "Extremely poor");

		}

	}

// Prompt text follows the installed language package, independently of the UI.
internal static string GetKingdomStabilityTierTextForPrompt(int value)
{
	string key;
	switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))
	{
	case KingdomStabilityTier.ExtremelyHigh: key = "Project_KingdomStability_1"; break;
	case KingdomStabilityTier.High: key = "Project_KingdomStability_2"; break;
	case KingdomStabilityTier.FairlyHigh: key = "Project_KingdomStability_3"; break;
	case KingdomStabilityTier.Average: key = "Project_KingdomStability_4"; break;
	case KingdomStabilityTier.Poor: key = "Project_KingdomStability_5"; break;
	case KingdomStabilityTier.VeryPoor: key = "Project_KingdomStability_6"; break;
	default: key = "Project_KingdomStability_7"; break;
	}
	return AdapterPromptCaptureTexts.PcapText(key);
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

		return AdapterPromptCaptureTexts.PcapText("kingdom_stability_prompt") + GetKingdomStabilityTierTextForPrompt(_state().Get(MemoryEntityIdentityBannerlordAdapter.GetKingdomId(kingdom)));

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



internal static string BuildClanFortificationSummary(Clan clan) => BuildClanFortificationSummaryCore(clan, false);

internal static string BuildClanFortificationSummaryForPrompt(Clan clan) => BuildClanFortificationSummaryCore(clan, true);

private static string BuildClanFortificationSummaryCore(Clan clan, bool forPrompt)

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

			text += forPrompt ? AdapterPromptCaptureTexts.PcapFormat("kingdom_fiefs_more", " and others, {COUNT} total", ("COUNT", list.Count)) : KingdomCivilWarTexts.Raw("KS_Runtime_93ac7be2bc1f") + list.Count + KingdomCivilWarTexts.Raw("KS_Runtime_59e25af67494");

		}

		return forPrompt ? AdapterPromptCaptureTexts.PcapFormat("kingdom_clan_fiefs", "The {CLAN} clan currently controls these core fiefs: {FIEFS}.", ("CLAN", GetClanDisplayName(clan)), ("FIEFS", text)) : GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_df999cf1c998") + text + "。";

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

		stringBuilder.AppendLine(DisplayText("AF_UI_KingdomStability_EncyclopediaTitle", "Kingdom stability"));

		stringBuilder.AppendLine(DisplayText("AF_UI_KingdomStability_EncyclopediaCurrentValue", "Current value: {VALUE}/100 ({TIER})", ("VALUE", stabilityValue.ToString()), ("TIER", GetKingdomStabilityTierDisplayText(stabilityValue))));

		if (!enabled)

		{

			stringBuilder.AppendLine(DisplayText("AF_UI_KingdomStability_EncyclopediaDisabled", "System status: MCM is disabled; only the saved value is shown."));

			return stringBuilder.ToString().TrimEnd();

		}

		if (protectedByImmunity)

		{

			stringBuilder.AppendLine(DisplayText("AF_UI_KingdomStability_DetailImmuneChance", "This week's rebellion chance: 0% (player-kingdom stability rebellion immunity is enabled)"));

		}

		else

		{

			stringBuilder.AppendLine(DisplayText("AF_UI_KingdomStability_DetailChance", "This week's rebellion chance: {CHANCE}", ("CHANCE", FormatKingdomRebellionChance(rebellionChance))));

		}

		List<string> effects = new List<string>();

		if (relationOffset != 0)

		{

			effects.Add(DisplayText("AF_UI_KingdomStability_EncyclopediaRelationEffect", "King-to-non-royal adult relation modifier: {VALUE}", ("VALUE", FormatKingdomStabilityRelationOffsetText(relationOffset))));

		}

		if (royalDomainLoyaltyAdjustment != 0)

		{

			effects.Add(DisplayText("AF_UI_KingdomStability_EncyclopediaRoyalDomainEffect", "Royal-domain daily loyalty modifier: {VALUE}", ("VALUE", FormatKingdomStabilityRelationOffsetText(royalDomainLoyaltyAdjustment))));

		}

		stringBuilder.AppendLine(effects.Count == 0

			? DisplayText("AF_UI_KingdomStability_EncyclopediaNoEffects", "Current additional effects: none")

			: DisplayText("AF_UI_KingdomStability_EncyclopediaEffects", "Current additional effects: {EFFECTS}", ("EFFECTS", string.Join("; ", effects))));

		return stringBuilder.ToString().TrimEnd();

	}



	private static string GetKingdomStabilityTierDisplayText(int value)

	{

		switch (KingdomStabilityPolicy.GetKingdomStabilityTier(value))

		{

		case KingdomStabilityTier.ExtremelyHigh:

			return DisplayText("AF_KINGDOM_CW_Stability_ExtremelyHigh", "Extremely high");

		case KingdomStabilityTier.High:

			return DisplayText("AF_KINGDOM_CW_Stability_High", "High");

		case KingdomStabilityTier.FairlyHigh:

			return DisplayText("AF_KINGDOM_CW_Stability_FairlyHigh", "Fairly high");

		case KingdomStabilityTier.Average:

			return DisplayText("AF_KINGDOM_CW_Stability_Average", "Average");

		case KingdomStabilityTier.Poor:

			return DisplayText("AF_KINGDOM_CW_Stability_Poor", "Poor");

		case KingdomStabilityTier.VeryPoor:

			return DisplayText("AF_KINGDOM_CW_Stability_VeryPoor", "Very poor");

		default:

			return DisplayText("AF_KINGDOM_CW_Stability_ExtremelyPoor", "Extremely poor");

		}

	}



	private static string DisplayText(string id, string fallback, params (string Name, string Value)[] variables)

	{

		TextObject text = new TextObject("{=" + id + "}" + fallback);

		if (variables != null)

		{

			foreach ((string name, string value) in variables)

			{

				text.SetTextVariable(name, value ?? string.Empty);

			}

		}

		return text.ToString();

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

				_events.Record().RecordEventSourceMaterial("kingdom_rebellion", KingdomCivilWarTexts.Raw("KS_Runtime_6c28aab10705") + GetClanDisplayName(clan), text3, text + ":material", GetKingdomId(oldKingdom), GetSettlementId(settlement), includeInWorld: true, includeInKingdom: true);

			}

			else if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom && clan != null && newKingdom != null)

			{

				Settlement settlement2 = clan.Settlements?.FirstOrDefault((Settlement x) => x != null && (x.IsTown || x.IsCastle));

				_events.Record().RecordEventSourceMaterial("kingdom_created", KingdomCivilWarTexts.Raw("KS_Runtime_1920dcbd318b") + GetKingdomDisplayName(newKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_b3cfb1c673f8")), text2, text + ":material", GetKingdomId(newKingdom), GetSettlementId(settlement2), includeInWorld: true, includeInKingdom: true);

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

				text = string.IsNullOrWhiteSpace(narrative) ? (KingdomCivilWarTexts.Raw("KS_Runtime_06a360a3fc5e") + clanName + KingdomCivilWarTexts.Raw("KS_Runtime_56e3a3c9f599")) : narrative;

			}

			else if (joinsPlayerKingdom)

			{

				text = KingdomCivilWarTexts.Raw("KS_Runtime_cec036ab626e") + GetKingdomDisplayName(playerKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_a9d885b62bdf")) + KingdomCivilWarTexts.Raw("KS_Runtime_4e6371cdbb40") + clanName + KingdomCivilWarTexts.Raw("KS_Runtime_506dada4b77a");

				won = true;

			}

			else

			{

				text = clanName + KingdomCivilWarTexts.Raw("KS_Runtime_9c4130987db2") + GetKingdomDisplayName(playerKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_a9d885b62bdf")) + "。";

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

			string text2 = KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_1395c42e0d49") + GetKingdomDisplayName(oldKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_1dc2c55dd2bb")) + KingdomCivilWarTexts.Raw("KS_Runtime_8d3fe556497f") + GetKingdomDisplayName(newKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_b3cfb1c673f8")) + "。";

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

			string text2 = KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(rulingClan) + KingdomCivilWarTexts.Raw("KS_Runtime_0ea04b13152b") + GetKingdomDisplayName(kingdom, KingdomCivilWarTexts.Raw("KS_Runtime_6186cd81a9a5")) + KingdomCivilWarTexts.Raw("KS_Runtime_a6dc5bdd3e76");

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

				string text2 = string.Equals(GetHeroId(item), GetHeroId(newLeader), StringComparison.OrdinalIgnoreCase) ? (KingdomCivilWarTexts.Raw("KS_Runtime_3f8ab54b1ec5") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_de1ca4a0dd7e")) : (GetHeroDisplayName(newLeader) + KingdomCivilWarTexts.Raw("KS_Runtime_dea7af3bdb2b") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_de1ca4a0dd7e"));

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

			string text = clanDisplayName + KingdomCivilWarTexts.Raw("KS_Runtime_a2137d0bac73");

			bool includeInWorld = destroyedClan != null && destroyedClan == destroyedClan.Kingdom?.RulingClan;

			_events.Record().RecordEventSourceMaterial("clan_destroyed", KingdomCivilWarTexts.Raw("KS_Runtime_d580bea7f3a2") + clanDisplayName, text, "clan_destroyed:" + GetClanId(destroyedClan), kingdomId, "", includeInWorld, includeInKingdom: true);

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

		string kingdomDisplayName = GetKingdomDisplayName(oldKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_1dc2c55dd2bb"));

		string kingdomDisplayName2 = GetKingdomDisplayName(newKingdom, KingdomCivilWarTexts.Raw("KS_Runtime_b3cfb1c673f8"));

		switch (detail)

		{

		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_534feee7d6a3") + kingdomDisplayName2 + "。";

		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdom:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_9e349186f855") + kingdomDisplayName2 + "。";

		case ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_cbe837f36a6f") + kingdomDisplayName + KingdomCivilWarTexts.Raw("KS_Runtime_8addb841431a") + kingdomDisplayName2 + "。";

		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_1395c42e0d49") + kingdomDisplayName + "。";

		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_1395c42e0d49") + kingdomDisplayName + KingdomCivilWarTexts.Raw("KS_Runtime_6b7e8cb35a39");

		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_f6e5f536d5c0") + kingdomDisplayName + KingdomCivilWarTexts.Raw("KS_Runtime_ca20743125be");

		case ChangeKingdomAction.ChangeKingdomActionDetail.CreateKingdom:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_2839ba39f792") + kingdomDisplayName2 + "。";

		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByKingdomDestruction:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7ce856c6f6f1") + kingdomDisplayName + KingdomCivilWarTexts.Raw("KS_Runtime_6fc55f1bf8d9") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_619089584b8a");

		case ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_7a9f77444c25");

		default:

			return KingdomCivilWarTexts.Raw("KS_Runtime_7a2c3fa0b62e") + GetClanDisplayName(clan) + KingdomCivilWarTexts.Raw("KS_Runtime_4fed956b194a");

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

