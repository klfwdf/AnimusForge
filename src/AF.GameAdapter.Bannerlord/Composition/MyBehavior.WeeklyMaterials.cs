using AnimusForge.Refactor.Runtime;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using AnimusForge.Refactor.Modules;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.LogEntries;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;

namespace AnimusForge;

public partial class MyBehavior
{

	private const long WeeklyMemoryMaterialValueThresholdDenars = WeeklyMemoryMaterialPolicy.ValueThresholdDenars;

	private void MarkWeeklyMemoryMaterialTriggerInternal(Hero targetHero, string nonHeroMemoryId, string npcName, string normalizedTagText, int sceneSessionId, int nativeDialogueSessionId, int targetAgentIndex, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<PartyTransferPromptEntry> partyTransferTroopOptions, List<PartyTransferPromptEntry> partyTransferPrisonerOptions, List<SettlementTransferPromptEntry> settlementTransferNpcOptions, bool suppressImplicitDialogueSession = false, List<PartyTransferPromptEntry> partyTransferAllTroopOptions = null, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions = null)
	{
		string memoryId = !string.IsNullOrWhiteSpace(nonHeroMemoryId) ? NormalizeMemoryHeroId(nonHeroMemoryId) : GetMemoryHeroId(targetHero);
		if (!IsMemoryEntityEligibleForCompressedMemory(memoryId))
		{
			return;
		}
		string tagText = WeeklyMemoryMaterialPolicy.NormalizeWeeklyMemoryMaterialTagText(normalizedTagText);
		List<string> tags = WeeklyMemoryMaterialPolicy.ExtractWeeklyMemoryMaterialTags(tagText);
		if (tags.Count == 0)
		{
			return;
		}
		int day = GetCurrentGameDayIndexSafe();
		string gameDate = GetCurrentGameDateTextSafe();
		List<DailyMemoryDraft> drafts = LoadDailyMemoryDraftsById(memoryId);
		DailyMemoryDraft draft = drafts.FirstOrDefault((DailyMemoryDraft x) => x != null && x.GameDayIndex == day);
		WeeklyMemoryMaterialEvaluation evaluation = WeeklyMemoryMaterialPolicy.EvaluateWeeklyMemoryMaterialTags(tags, tag => EstimateWeeklyMemoryMaterialTagValue(targetHero, tag, rewardOptions, partyTransferTroopOptions, partyTransferPrisonerOptions, settlementTransferNpcOptions, partyTransferAllTroopOptions, partyTransferAllPrisonerOptions));
		WeeklyMemoryMaterialPolicy.TryApplyPlayerTransferredValueToWeeklyMemoryMaterialEvaluation(evaluation, tags, draft, npcName, sceneSessionId, nativeDialogueSessionId);
		if (evaluation == null || !evaluation.Eligible)
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] tags_not_eligible memory=" + memoryId + " value=" + (evaluation?.EstimatedValueDenars ?? 0L) + " threshold=" + WeeklyMemoryMaterialValueThresholdDenars + " tags=" + string.Join("|", tags));
			return;
		}
		if (!ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var footholdKingdomId, out var footholdSettlementId))
		{
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][SKIP] foothold_kingdom_missing memory=" + memoryId + " tags=" + string.Join("|", tags));
			return;
		}
		if (!suppressImplicitDialogueSession && sceneSessionId < 0 && nativeDialogueSessionId < 0)
		{
			nativeDialogueSessionId = GetOrStartActiveNativeConversationMemorySessionId();
		}
		WeeklyMemoryMaterialTrigger trigger = WeeklyMemoryMaterialPolicy.CreateTrigger(memoryId,
            npcName, day, gameDate, sceneSessionId, nativeDialogueSessionId, targetAgentIndex,
            footholdKingdomId, footholdSettlementId, tagText, evaluation, DateTime.UtcNow.Ticks);
        bool attached = _memoryBusinessState.StageOrAttachWeeklyTrigger(trigger, day);
        Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] trigger_" + (attached ? "attached" : "pending")
            + " memory=" + memoryId + " day=" + day + " kingdom=" + footholdKingdomId
            + " value=" + evaluation.EstimatedValueDenars + " tags=" + string.Join("|", evaluation.Tags));
	}

	private void AddWeeklyMemoryMaterialTriggerToDraft(DailyMemoryDraft draft, WeeklyMemoryMaterialTrigger trigger)
		=> _memoryBusinessState.AddWeeklyTrigger(draft, trigger);

	private void AttachPendingWeeklyMemoryMaterialTriggers(DailyMemoryDraft draft, DailyMemoryLine line)
		=> _memoryBusinessState.AttachPendingWeeklyTriggers(draft, line, GetCurrentGameDayIndexSafe());

	private void PrunePendingWeeklyMemoryMaterialTriggers()
		=> _memoryBusinessState.PrunePendingWeeklyTriggers(GetCurrentGameDayIndexSafe());

	private void RecordPublicDailyMemoryWeeklyMaterial(CompressedMemoryBlock block)
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
		if (!ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out var kingdomId, out var settlementId))
		{
			Logger.Log("EventWeeklyReport", "[PublicDailyMemory][SKIP] foothold_kingdom_missing block=" + (block.Id ?? "") + " memory=" + (block.HeroId ?? ""));
			return;
		}
		string blockId = (block.Id ?? BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim();
		string stableHash = WeeklyMemoryMaterialPolicy.ComputeWeeklyMemoryMaterialHash(blockId + "|" + kingdomId + "|" + material);
		string stableKey = "public_daily_memory:" + kingdomId + ":" + blockId + ":" + stableHash;
		string label = "公开聊天日结 - " + (string.IsNullOrWhiteSpace(block.HeroName) ? "NPC" : block.HeroName.Trim());
		string snapshot = WeeklyMemoryMaterialPolicy.BuildPublicDailyMemoryWeeklyMaterialSnapshotText(block, material, FormatMemoryHourRange(block?.StartHour ?? 0, block?.EndHour ?? 0));
		RecordEventSourceMaterial("public_daily_memory", label, snapshot, stableKey, kingdomId, settlementId, includeInWorld: false, includeInKingdom: true, actorHeroId: GetHeroId(Hero.MainHero), actorKingdomId: kingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
		Logger.Log("EventWeeklyReport", "[PublicDailyMemory] source_material_recorded block=" + blockId + " kingdom=" + kingdomId + " publicity=" + publicity);
	}

	private void RecordWeeklyMemoryMaterialForBlock(CompressedMemoryBlock block)
	{
		List<WeeklyMemoryMaterialTrigger> triggers = SanitizeWeeklyMemoryMaterialTriggers(block?.WeeklyMaterialTriggers);
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
		foreach (IGrouping<string, WeeklyMemoryMaterialTrigger> group in triggers.GroupBy((WeeklyMemoryMaterialTrigger x) => (x.FootholdKingdomId ?? "").Trim(), StringComparer.OrdinalIgnoreCase))
		{
			string kingdomId = (group.Key ?? "").Trim();
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				continue;
			}
			List<WeeklyMemoryMaterialTrigger> groupTriggers = group.ToList();
			string stableHash = WeeklyMemoryMaterialPolicy.ComputeWeeklyMemoryMaterialHash(string.Join("|", groupTriggers.Select((WeeklyMemoryMaterialTrigger x) => (x.StableKey ?? "").Trim()).Where((string x) => !string.IsNullOrWhiteSpace(x))));
			string stableKey = "player_dialogue_memory:" + kingdomId + ":" + ((block.Id ?? BuildCompressedMemoryBlockId(block.HeroId, block.GameDayIndex)).Trim()) + ":" + stableHash;
			string label = "玩家交涉记忆 - " + (string.IsNullOrWhiteSpace(block.HeroName) ? "NPC" : block.HeroName.Trim());
			string snapshot = WeeklyMemoryMaterialPolicy.BuildWeeklyMemoryMaterialSnapshotText(block, groupTriggers);
			string settlementId = groupTriggers.Select((WeeklyMemoryMaterialTrigger x) => (x.FootholdSettlementId ?? "").Trim()).FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "";
			RecordEventSourceMaterial("player_dialogue_memory", label, snapshot, stableKey, kingdomId, settlementId, includeInWorld: false, includeInKingdom: true, actorHeroId: GetHeroId(Hero.MainHero), actorKingdomId: kingdomId, dayOverride: block.GameDayIndex, gameDateOverride: block.GameDate);
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial] source_material_recorded block=" + (block.Id ?? "") + " kingdom=" + kingdomId + " triggers=" + groupTriggers.Count + " value=" + groupTriggers.Sum((WeeklyMemoryMaterialTrigger x) => Math.Max(0L, x.EstimatedValueDenars)));
		}
	}

	private static bool ResolvePlayerFootholdKingdomForWeeklyMemoryMaterial(out string kingdomId, out string settlementId)
	{
		kingdomId = "";
		settlementId = "";
		try
		{
			Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
			settlementId = GetSettlementId(settlement);
			kingdomId = GetKingdomId(settlement?.MapFaction);
			if (string.IsNullOrWhiteSpace(kingdomId))
			{
				kingdomId = GetKingdomId(settlement?.OwnerClan?.Kingdom);
			}
			if (!string.IsNullOrWhiteSpace(kingdomId))
			{
				return true;
			}
			List<string> nearest = GetKingdomIdsByPlayerProximity(GetDevEditableKingdoms().Select((Kingdom x) => x?.StringId));
			kingdomId = nearest.FirstOrDefault((string x) => !string.IsNullOrWhiteSpace(x)) ?? "";
			return !string.IsNullOrWhiteSpace(kingdomId);
		}
		catch
		{
			kingdomId = "";
			settlementId = "";
			return false;
		}
	}

	private static long EstimateWeeklyMemoryMaterialTagValue(Hero targetHero, string tag, List<RewardSystemBehavior.RewardItemInfo> rewardOptions, List<PartyTransferPromptEntry> partyTransferTroopOptions, List<PartyTransferPromptEntry> partyTransferPrisonerOptions, List<SettlementTransferPromptEntry> settlementTransferNpcOptions, List<PartyTransferPromptEntry> partyTransferAllTroopOptions, List<PartyTransferPromptEntry> partyTransferAllPrisonerOptions)
	{
		string text = (tag ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0L;
		}
		if (GiveAssetTagCodec.TryParseWhole(text, out GiveAssetTag giveAssetTag))
		{
			string assetToken = (giveAssetTag.AssetToken ?? "").Trim();
			string quantityToken = (giveAssetTag.QuantityToken ?? "").Trim();
			if (RewardSystemBehavior.IsGoldAssetTokenForExternal(assetToken) && WeeklyMemoryMaterialPolicy.TryParsePositiveLong(quantityToken, out var gold))
			{
				return gold;
			}
			SettlementTransferPromptEntry fixedAsset = FindSettlementTransferEntryByTokenForWeeklyMemoryMaterial(settlementTransferNpcOptions, assetToken);
			if (fixedAsset != null && string.Equals(quantityToken, "1", StringComparison.Ordinal))
			{
				return Math.Max(0L, fixedAsset.GuidePriceDenars);
			}
			if (TransferQuantitySpec.IsAllValue(quantityToken))
			{
				return EstimateAllRewardItemTagValueForWeeklyMemoryMaterial(assetToken, rewardOptions);
			}
			if (int.TryParse(quantityToken, out var assetAmount) && assetAmount > 0)
			{
				return EstimateRewardItemTagValueForWeeklyMemoryMaterial(targetHero, assetToken, assetAmount, rewardOptions);
			}
		}
		Match match = Regex.Match(text, "^\\[AD:(\\d+):\\d+:P:[^\\]]*\\]$", RegexOptions.IgnoreCase);
		if (match.Success && WeeklyMemoryMaterialPolicy.TryParsePositiveLong(match.Groups[1].Value, out var debtGold))
		{
			return debtGold;
		}
		match = Regex.Match(text, "^\\[ADP:([^\\]\\r\\n:;]+)\\]$", RegexOptions.IgnoreCase);
		if (match.Success)
		{
			string debtId = (match.Groups[1].Value ?? "").Trim();
			if (TryEstimateDebtValueByIdForWeeklyMemoryMaterial(debtId, out var debtValue))
			{
				return debtValue;
			}
			Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][WARN] debt_id_value_missing debtId=" + debtId);
			return 0L;
		}
		match = Regex.Match(text, "^\\[ATT:(ALL|\\d+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
		if (match.Success && (TransferQuantitySpec.IsAllValue(match.Groups[1].Value) || TransferQuantitySpec.IsAllValue(match.Groups[2].Value)))
		{
			return CalculatePartyTransferTotalValueForExternal(partyTransferAllTroopOptions, isPrisoner: false);
		}
		if (match.Success && int.TryParse(match.Groups[1].Value, out var troopIndex) && int.TryParse(match.Groups[2].Value, out var troopAmount))
		{
			return EstimatePartyTransferTagValueForWeeklyMemoryMaterial(partyTransferTroopOptions, troopIndex, troopAmount, isPrisoner: false);
		}
		match = Regex.Match(text, "^\\[ATP:(ALL|\\d+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
		if (match.Success && (TransferQuantitySpec.IsAllValue(match.Groups[1].Value) || TransferQuantitySpec.IsAllValue(match.Groups[2].Value)))
		{
			return CalculatePartyTransferTotalValueForExternal(partyTransferAllPrisonerOptions, isPrisoner: true);
		}
		if (match.Success && int.TryParse(match.Groups[1].Value, out var prisonerIndex) && int.TryParse(match.Groups[2].Value, out var prisonerAmount))
		{
			return EstimatePartyTransferTagValueForWeeklyMemoryMaterial(partyTransferPrisonerOptions, prisonerIndex, prisonerAmount, isPrisoner: true);
		}
		return 0L;
	}

	private static long EstimateRewardItemTagValueForWeeklyMemoryMaterial(Hero targetHero, string itemToken, int amount, List<RewardSystemBehavior.RewardItemInfo> rewardOptions)
	{
		RewardSystemBehavior.RewardItemInfo item = FindRewardItemByTokenForWeeklyMemoryMaterial(rewardOptions, itemToken);
		int safeAmount = Math.Max(1, amount);
		if (item != null)
		{
			safeAmount = Math.Min(safeAmount, Math.Max(1, item.Count));
			return (long)safeAmount * Math.Max(1, item.GuidePrice);
		}
		try
		{
			string text = (itemToken ?? "").Trim();
			int at = text.IndexOf('@');
			if (at > 0)
			{
				text = text.Substring(0, at);
			}
			return Math.Max(0L, RewardSystemBehavior.Instance?.EstimateItemValueForExternal(targetHero ?? Hero.MainHero, text, safeAmount) ?? 0L);
		}
		catch
		{
			return 0L;
		}
	}

	private static long EstimateAllRewardItemTagValueForWeeklyMemoryMaterial(string itemToken, List<RewardSystemBehavior.RewardItemInfo> rewardOptions)
	{
		RewardSystemBehavior.RewardItemInfo resolved = FindRewardItemByTokenForWeeklyMemoryMaterial(rewardOptions, itemToken);
		if (resolved == null)
		{
			return 0L;
		}
		string key = string.IsNullOrWhiteSpace(resolved.PromptStringId) ? (resolved.StringId ?? "").Trim() : resolved.PromptStringId.Trim();
		long total = 0L;
		foreach (RewardSystemBehavior.RewardItemInfo item in rewardOptions ?? new List<RewardSystemBehavior.RewardItemInfo>())
		{
			string itemKey = string.IsNullOrWhiteSpace(item?.PromptStringId) ? (item?.StringId ?? "").Trim() : item.PromptStringId.Trim();
			if (item == null || item.Count <= 0 || string.IsNullOrWhiteSpace(key) || !string.Equals(itemKey, key, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			total = TransferQuantitySpec.AddProduct(total, item.Count, Math.Max(1, item.GuidePrice));
		}
		return total;
	}

	private static long EstimatePartyTransferTagValueForWeeklyMemoryMaterial(List<PartyTransferPromptEntry> options, int promptIndex, int amount, bool isPrisoner)
	{
		if (promptIndex <= 0 || amount <= 0)
		{
			return 0L;
		}
		PartyTransferPromptEntry entry = FindPartyTransferEntryByPromptIndexForWeeklyMemoryMaterial(options, promptIndex);
		if (entry == null)
		{
			return 0L;
		}
		int safeAmount = entry.IsHero ? 1 : Math.Min(Math.Max(1, amount), Math.Max(1, entry.Count));
		int unitPrice = isPrisoner ? Math.Max(1, entry.BuyPriceDenarsPerUnit) : Math.Max(1, entry.HirePriceDenarsPerUnit);
		return (long)safeAmount * unitPrice;
	}

	private static bool TryEstimateDebtValueByIdForWeeklyMemoryMaterial(string debtId, out long value)
	{
		value = 0L;
		string text = (debtId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || RewardSystemBehavior.Instance == null)
		{
			return false;
		}
		Dictionary<string, RewardSystemBehavior.DebtExportEntry> debts = RewardSystemBehavior.Instance.ExportDebtEntries() ?? new Dictionary<string, RewardSystemBehavior.DebtExportEntry>();
		foreach (RewardSystemBehavior.DebtExportEntry entry in debts.Values)
		{
			foreach (RewardSystemBehavior.DebtLineExportEntry line in entry?.DebtLines ?? new List<RewardSystemBehavior.DebtLineExportEntry>())
			{
				if (line == null || !string.Equals((line.DebtId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || line.RemainingAmount <= 0)
				{
					continue;
				}
				if (line.IsGold)
				{
					value = Math.Max(0, line.RemainingAmount);
					return value > 0L;
				}
				int unitPrice = Math.Max(0, line.CompensationUnitPrice);
				if (unitPrice <= 0)
				{
					try
					{
						unitPrice = (int)Math.Min(int.MaxValue, Math.Max(0L, RewardSystemBehavior.Instance.EstimateItemValueForExternal(Hero.MainHero, line.ItemId, 1)));
					}
					catch
					{
						unitPrice = 0;
					}
				}
				value = (long)Math.Max(0, line.RemainingAmount) * Math.Max(1, unitPrice);
				return value > 0L;
			}
		}
		return false;
	}

	private static RewardSystemBehavior.RewardItemInfo FindRewardItemByTokenForWeeklyMemoryMaterial(List<RewardSystemBehavior.RewardItemInfo> options, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text) || options == null || options.Count == 0)
		{
			return null;
		}
		string baseToken = text;
		int at = baseToken.IndexOf('@');
		if (at > 0)
		{
			baseToken = baseToken.Substring(0, at);
		}
		return options.FirstOrDefault((RewardSystemBehavior.RewardItemInfo x) => x != null && (string.Equals((x.PromptStringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.StringId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.StringId ?? "").Trim(), baseToken, StringComparison.OrdinalIgnoreCase) || string.Equals((x.Name ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase))) ??
			options.FirstOrDefault((RewardSystemBehavior.RewardItemInfo x) => x != null && !string.IsNullOrWhiteSpace(x.Name) && x.Name.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
	}

	private static PartyTransferPromptEntry FindPartyTransferEntryByPromptIndexForWeeklyMemoryMaterial(List<PartyTransferPromptEntry> options, int promptIndex)
	{
		List<PartyTransferPromptEntry> list = (options ?? new List<PartyTransferPromptEntry>()).Where((PartyTransferPromptEntry x) => x != null).ToList();
		return list.FirstOrDefault((PartyTransferPromptEntry x) => x.PromptIndex == promptIndex) ?? list.Skip(promptIndex - 1).FirstOrDefault();
	}

	private static SettlementTransferPromptEntry FindSettlementTransferEntryByTokenForWeeklyMemoryMaterial(List<SettlementTransferPromptEntry> options, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		List<SettlementTransferPromptEntry> list = (options ?? new List<SettlementTransferPromptEntry>()).Where(IsSettlementTransferEntryValidForExternal).ToList();
		SettlementTransferPromptEntry entry = list.FirstOrDefault((SettlementTransferPromptEntry x) => string.Equals((x.AssetId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals(GetSettlementTransferAssetIdForExternal(x), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.SettlementId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
		if (entry != null)
		{
			return entry;
		}
		if (int.TryParse(text, out var index) && index > 0)
		{
			return list.FirstOrDefault((SettlementTransferPromptEntry x) => x.PromptIndex == index) ?? list.Skip(index - 1).FirstOrDefault();
		}
		return null;
	}

	private static string BuildWeeklyMemoryMaterialTagLabel(string tag)
		=> WeeklyMemoryMaterialPolicy.BuildWeeklyMemoryMaterialTagLabel(tag);
}
