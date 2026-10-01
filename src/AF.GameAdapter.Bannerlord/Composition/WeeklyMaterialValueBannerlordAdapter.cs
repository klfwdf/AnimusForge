using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using AnimusForge.Refactor.Runtime;
using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;
using SettlementTransferPromptEntry = AnimusForge.MyBehavior.SettlementTransferPromptEntry;

namespace AnimusForge;

// Main-thread live valuation and authorized prompt snapshots; no host instance retained.
internal static class WeeklyMaterialValueBannerlordAdapter
{
    internal static WeeklyMaterialValuePort Capture(Hero targetHero,
        List<RewardSystemBehavior.RewardItemInfo> rewards, List<PartyTransferPromptEntry> troops,
        List<PartyTransferPromptEntry> prisoners, List<SettlementTransferPromptEntry> settlements,
        List<PartyTransferPromptEntry> allTroops, List<PartyTransferPromptEntry> allPrisoners)
        => new WeeklyMaterialValuePort {
            IsGold = RewardSystemBehavior.IsGoldAssetTokenForExternal,
            RewardValue = (token, amount) => EstimateRewardItemTagValueForWeeklyMemoryMaterial(targetHero, token, amount, rewards),
            AllRewardValue = token => EstimateAllRewardItemTagValueForWeeklyMemoryMaterial(token, rewards),
            FixedAssetValue = token => FindSettlementTransferEntryByTokenForWeeklyMemoryMaterial(settlements, token)?.GuidePriceDenars,
            DebtValue = id => TryEstimateDebtValueByIdForWeeklyMemoryMaterial(id, out long value) ? value : (long?)null,
            MissingDebt = id => Logger.Log("EventWeeklyReport", "[WeeklyMemoryMaterial][WARN] debt_id_value_missing debtId=" + id),
            AllPartyValue = prisoner => MyBehavior.CalculatePartyTransferTotalValueForExternal(prisoner ? allPrisoners : allTroops, prisoner),
            PartyValue = (index, amount, prisoner) => EstimatePartyTransferTagValueForWeeklyMemoryMaterial(prisoner ? prisoners : troops, index, amount, prisoner)
        };

	internal static long EstimateRewardItemTagValueForWeeklyMemoryMaterial(Hero targetHero, string itemToken, int amount, List<RewardSystemBehavior.RewardItemInfo> rewardOptions)
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

	internal static long EstimateAllRewardItemTagValueForWeeklyMemoryMaterial(string itemToken, List<RewardSystemBehavior.RewardItemInfo> rewardOptions)
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

	internal static long EstimatePartyTransferTagValueForWeeklyMemoryMaterial(List<PartyTransferPromptEntry> options, int promptIndex, int amount, bool isPrisoner)
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

	internal static bool TryEstimateDebtValueByIdForWeeklyMemoryMaterial(string debtId, out long value)
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

	internal static RewardSystemBehavior.RewardItemInfo FindRewardItemByTokenForWeeklyMemoryMaterial(List<RewardSystemBehavior.RewardItemInfo> options, string token)
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

	internal static PartyTransferPromptEntry FindPartyTransferEntryByPromptIndexForWeeklyMemoryMaterial(List<PartyTransferPromptEntry> options, int promptIndex)
	{
		List<PartyTransferPromptEntry> list = (options ?? new List<PartyTransferPromptEntry>()).Where((PartyTransferPromptEntry x) => x != null).ToList();
		return list.FirstOrDefault((PartyTransferPromptEntry x) => x.PromptIndex == promptIndex) ?? list.Skip(promptIndex - 1).FirstOrDefault();
	}

	internal static SettlementTransferPromptEntry FindSettlementTransferEntryByTokenForWeeklyMemoryMaterial(List<SettlementTransferPromptEntry> options, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		List<SettlementTransferPromptEntry> list = (options ?? new List<SettlementTransferPromptEntry>()).Where(MyBehavior.IsSettlementTransferEntryValidForExternal).ToList();
		SettlementTransferPromptEntry entry = list.FirstOrDefault((SettlementTransferPromptEntry x) => string.Equals((x.AssetId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals(MyBehavior.GetSettlementTransferAssetIdForExternal(x), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.SettlementId ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase) || string.Equals((x.DisplayName ?? "").Trim(), text, StringComparison.OrdinalIgnoreCase));
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
    internal static bool TryCaptureOutcomeContext(WeeklyMemoryMaterialOutcomeCandidate candidate, bool isNonHero, string npcName,
        Func<string,string> normalizeId, Func<string,bool> isNonHeroId, Func<string,bool> entityEligible,
        Func<string,Hero> findHero, Func<Hero,bool> heroEligible, WeeklyOutcomeFootholdResolver resolveFoothold,
        Func<int> currentDay, Func<string> currentDate, out WeeklyActionOutcomeMaterialContext context,
        out WeeklyMaterialValuePort values, out string errorCode)
    {
        context = null; values = null; errorCode = string.Empty;
        string memoryId = normalizeId(candidate.SubjectId);
        if (string.IsNullOrWhiteSpace(memoryId) || isNonHero != isNonHeroId(memoryId) || !entityEligible(memoryId))
        { errorCode = "weekly_material_subject_invalid"; return false; }
        Hero memoryHero = isNonHero ? null : (Hero.Find(candidate.SubjectId) ?? findHero(memoryId));
        if (!isNonHero && !heroEligible(memoryHero))
        { errorCode = "weekly_material_subject_unavailable"; return false; }
        if (!resolveFoothold(out string kingdomId, out string settlementId))
        { errorCode = "weekly_material_foothold_missing"; return false; }
        context = new WeeklyActionOutcomeMaterialContext {
            MemoryId = memoryId, NpcName = npcName, FootholdKingdomId = kingdomId, FootholdSettlementId = settlementId,
            SubjectName = () => memoryHero?.Name?.ToString() ?? "NPC", CurrentDay = currentDay, CurrentDate = currentDate
        };
        values = new WeeklyMaterialValuePort {
            IsGold = RewardSystemBehavior.IsGoldAssetTokenForExternal,
            RewardValue = (asset, amount) => RewardSystemBehavior.Instance?.EstimateItemValueForExternal(memoryHero ?? Hero.MainHero, asset, amount) ?? 0L,
            DebtValue = id => TryEstimateDebtValueByIdForWeeklyMemoryMaterial(id, out long value) ? value : (long?)null
        };
        return true;
    }

    internal static bool TryResolvePlayerFoothold(Func<Settlement,string> settlementIdOf, Func<IFaction,string> kingdomIdOf,
        Func<IEnumerable<Kingdom>> editableKingdoms, Func<IEnumerable<string>,List<string>> proximityIds,
        out string kingdomId, out string settlementId)
    {
        kingdomId = ""; settlementId = "";
        try
        {
            Settlement settlement = Settlement.CurrentSettlement ?? MobileParty.MainParty?.CurrentSettlement;
            settlementId = settlementIdOf(settlement);
            kingdomId = kingdomIdOf(settlement?.MapFaction);
            if (string.IsNullOrWhiteSpace(kingdomId)) kingdomId = kingdomIdOf(settlement?.OwnerClan?.Kingdom);
            if (!string.IsNullOrWhiteSpace(kingdomId)) return true;
            List<string> nearest = proximityIds(editableKingdoms().Select(x => x?.StringId));
            kingdomId = nearest.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";
            return !string.IsNullOrWhiteSpace(kingdomId);
        }
        catch { kingdomId = ""; settlementId = ""; return false; }
    }
}
internal delegate bool WeeklyOutcomeFootholdResolver(out string kingdomId, out string settlementId);
