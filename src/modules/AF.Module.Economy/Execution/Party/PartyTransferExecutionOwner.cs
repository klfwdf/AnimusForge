using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PartyTransferPromptEntry = AnimusForge.MyBehavior.PartyTransferPromptEntry;

namespace AnimusForge;

// Shared by Scene, Native and Courier. One authorization snapshot and one non-transactional
// effect path. ALL/index order, quantity/value aggregation and AFEF use actual observed delivery.
internal static class PartyTransferExecutionOwner
{
	internal static bool Execute(PartyTransferExecutionContext context, ref string content, out List<string> generatedFacts, out List<string> notifications)
	{
		generatedFacts = new List<string>();
		notifications = new List<string>();
		List<string> factResults = generatedFacts;
		List<string> notificationResults = notifications;
		bool observedEffects = false;
		try
		{
			string text = content ?? "";
            if (!context.Eligible)
            {
                content = PartyTransferTagCodec.Strip(text);
                return false;
            }
			MatchCollection matchCollection = PartyTransferTagCodec.TroopRegex.Matches(text);
			MatchCollection matchCollection2 = PartyTransferTagCodec.PrisonerRegex.Matches(text);
			if ((matchCollection?.Count ?? 0) <= 0 && (matchCollection2?.Count ?? 0) <= 0)
			{
				content = PartyTransferTagCodec.Strip(text);
				return false;
			}
            bool hasTroopSnapshot = context.HasTroops;
            bool hasPrisonerSnapshot = context.HasPrisoners;
            bool hasAllTroopSnapshot = context.HasAllTroops;
            bool hasAllPrisonerSnapshot = context.HasAllPrisoners;
            var list2 = context.Troops;
            var list3 = context.Prisoners;
            var allTroops = context.AllTroops;
            var allPrisoners = context.AllPrisoners;
            string text2 = context.DisplayName;
			bool flag = false;
			int attemptedEntries = 0;
			int successfulEntries = 0;
			int failedOrPartialEntries = 0;
			long actualUnits = 0L;
			long actualValue = 0L;
			if (context.HasTarget)
			{
				StringBuilder transferredTroopFacts = new StringBuilder();
				StringBuilder recruitedVolunteerFacts = new StringBuilder();
				StringBuilder transferredPrisonerFacts = new StringBuilder();
                StringBuilder partialEffects = new StringBuilder();
				void applyTroop(PartyTransferPromptEntry entry, int requested, string source)
				{
					attemptedEntries++;
					var effect = context.TransferTroop(entry, requested);
                    int applied = effect.Delivered;
                    observedEffects |= effect.HasEffects;
                    flag |= effect.HasEffects;
                    if (effect.IsPartial) AppendPartialEffect(partialEffects, entry, effect);
					Logger.Log("Logic", "[PartyTransfer] ATT source=" + source + " amount=" + requested + " resolved=" + (entry?.DisplayName ?? "null") + " section=" + (entry?.Section.ToString() ?? "null") + " applied=" + applied);
					if (applied <= 0)
					{
						failedOrPartialEntries++;
						return;
					}
					flag = true;
					successfulEntries++;
					if (effect.IsPartial || applied < Math.Max(0, requested))
					{
						failedOrPartialEntries++;
					}
					actualUnits = TransferQuantitySpec.AddValue(actualUnits, applied);
					actualValue = TransferQuantitySpec.AddProduct(actualValue, applied, Math.Max(1, entry?.HirePriceDenarsPerUnit ?? 0));
					bool isVolunteer = context.IsVolunteer(entry);
					AppendPartyTransferFactItem(isVolunteer ? recruitedVolunteerFacts : transferredTroopFacts, entry, applied);
					notificationResults.Add((isVolunteer ? "已招募 " : "已获得 ") + applied + " 名" + entry.DisplayName);
				}
				void applyPrisoner(PartyTransferPromptEntry entry, int requested, string source)
				{
					attemptedEntries++;
					var effect = context.TransferPrisoner(entry, requested);
                    int applied = effect.Delivered;
                    observedEffects |= effect.HasEffects;
                    flag |= effect.HasEffects;
                    if (effect.IsPartial) AppendPartialEffect(partialEffects, entry, effect);
					Logger.Log("Logic", "[PartyTransfer] ATP source=" + source + " amount=" + requested + " resolved=" + (entry?.DisplayName ?? "null") + " applied=" + applied);
					if (applied <= 0)
					{
						failedOrPartialEntries++;
						return;
					}
					flag = true;
					successfulEntries++;
					if (effect.IsPartial || applied < Math.Max(0, requested))
					{
						failedOrPartialEntries++;
					}
					actualUnits = TransferQuantitySpec.AddValue(actualUnits, applied);
					actualValue = TransferQuantitySpec.AddProduct(actualValue, applied, Math.Max(1, entry?.BuyPriceDenarsPerUnit ?? 0));
					AppendPartyTransferFactItem(transferredPrisonerFacts, entry, applied);
					notificationResults.Add("已获得 " + (entry.IsHero ? ("俘虏" + entry.DisplayName) : (applied + " 名" + entry.DisplayName + "俘虏")));
				}
				bool troopAll = matchCollection.Cast<Match>().Any((Match x) => x.Success && (TransferQuantitySpec.IsAllValue(x.Groups[2].Value) || (TransferQuantitySpec.IsAllValue(x.Groups[1].Value) && int.TryParse(x.Groups[2].Value, out var amount) && amount > 0)));
				if (troopAll)
				{
					if (!hasAllTroopSnapshot)
					{
						attemptedEntries++;
						failedOrPartialEntries++;
						Logger.Log("Logic", "[PartyTransfer] ATT ALL rejected: authorization snapshot missing.");
					}
					foreach (PartyTransferPromptEntry entry in hasAllTroopSnapshot ? allTroops : new List<PartyTransferPromptEntry>())
					{
						applyTroop(entry, Math.Max(0, entry?.Count ?? 0), "ALL");
					}
				}
				else
				{
					if (!hasTroopSnapshot)
					{
						attemptedEntries += matchCollection?.Count ?? 0;
						failedOrPartialEntries += matchCollection?.Count ?? 0;
						Logger.Log("Logic", "[PartyTransfer] ATT rejected: authorization snapshot missing.");
					}
					foreach (Match item in hasTroopSnapshot ? matchCollection.Cast<Match>() : Enumerable.Empty<Match>())
					{
						if (item.Success && int.TryParse(item.Groups[1].Value, out var index) && int.TryParse(item.Groups[2].Value, out var amount))
						{
							applyTroop(FindDisplayIndexedPartyTransferEntry(list2, index), amount, "index=" + index);
						}
					}
				}
				bool prisonerAll = matchCollection2.Cast<Match>().Any((Match x) => x.Success && (TransferQuantitySpec.IsAllValue(x.Groups[2].Value) || (TransferQuantitySpec.IsAllValue(x.Groups[1].Value) && int.TryParse(x.Groups[2].Value, out var amount) && amount > 0)));
				if (prisonerAll)
				{
					if (!hasAllPrisonerSnapshot)
					{
						attemptedEntries++;
						failedOrPartialEntries++;
						Logger.Log("Logic", "[PartyTransfer] ATP ALL rejected: authorization snapshot missing.");
					}
					foreach (PartyTransferPromptEntry entry in hasAllPrisonerSnapshot ? allPrisoners : new List<PartyTransferPromptEntry>())
					{
						applyPrisoner(entry, Math.Max(0, entry?.Count ?? 0), "ALL");
					}
				}
				else
				{
					if (!hasPrisonerSnapshot)
					{
						attemptedEntries += matchCollection2?.Count ?? 0;
						failedOrPartialEntries += matchCollection2?.Count ?? 0;
						Logger.Log("Logic", "[PartyTransfer] ATP rejected: authorization snapshot missing.");
					}
					foreach (Match item in hasPrisonerSnapshot ? matchCollection2.Cast<Match>() : Enumerable.Empty<Match>())
					{
						if (item.Success && int.TryParse(item.Groups[1].Value, out var index) && int.TryParse(item.Groups[2].Value, out var amount))
						{
							applyPrisoner(FindDisplayIndexedPartyTransferEntry(list3, index), amount, "index=" + index);
						}
					}
				}
				string batchSummary = "部队与俘虏转移汇总：尝试项" + attemptedEntries + "，成功项" + successfulEntries + "，失败或不足项" + failedOrPartialEntries + "，实际转移" + actualUnits + "人，实际指导总值约" + actualValue + "第纳尔。";
				StringBuilder combinedFact = new StringBuilder(256);
				combinedFact.Append("[AFEF NPC行为补充] ").Append(text2).Append(partialEffects.Length > 0 ? "执行部队与俘虏转移时发生部分失败。" : "已完成一次部队与俘虏转移。");
				if (transferredTroopFacts.Length > 0)
				{
					combinedFact.Append("转入玩家麾下：").Append(transferredTroopFacts).Append('。');
				}
				if (recruitedVolunteerFacts.Length > 0)
				{
					combinedFact.Append("允许玩家从原版待招募士兵中招募：").Append(recruitedVolunteerFacts).Append('。');
				}
				if (transferredPrisonerFacts.Length > 0)
				{
					combinedFact.Append("交给玩家的俘虏：").Append(transferredPrisonerFacts).Append('。');
				}
				combinedFact.Append(batchSummary);
                if (partialEffects.Length > 0) combinedFact.Append(partialEffects);
				factResults.Add(combinedFact.ToString());
				notificationResults.Add(batchSummary);
				Logger.Log("Logic", "[PartyTransfer] batch_done troopAll=" + troopAll + " prisonerAll=" + prisonerAll + " attempted=" + attemptedEntries + " succeeded=" + successfulEntries + " failedOrPartial=" + failedOrPartialEntries + " actualUnits=" + actualUnits + " actualValue=" + actualValue);
			}
			text = PartyTransferTagCodec.Strip(text);
			content = text;
			return flag;
		}
		catch
		{
			content = PartyTransferTagCodec.Strip(content);
            if (observedEffects && factResults.Count == 0)
                factResults.Add("[AFEF NPC行为补充] 部队/俘虏转移已发生部分副作用，后续处理失败；不代表完整交付，不自动重试。");
            return observedEffects;
		}
	}

	internal static void AppendPartyTransferFactItem(StringBuilder builder, PartyTransferPromptEntry entry, int amount)
	{
		if (builder == null || entry == null || amount <= 0)
		{
			return;
		}
		if (builder.Length > 0)
		{
			builder.Append('、');
		}
		builder.Append(entry.DisplayName).Append('x').Append(amount);
	}

	internal static PartyTransferPromptEntry FindDisplayIndexedPartyTransferEntry(IEnumerable<PartyTransferPromptEntry> entries, int displayIndex)
	{
		if (displayIndex <= 0)
		{
			return null;
		}
		List<PartyTransferPromptEntry> list = (entries ?? Enumerable.Empty<PartyTransferPromptEntry>()).Where((PartyTransferPromptEntry x) => x != null).ToList();
		return list.FirstOrDefault((PartyTransferPromptEntry x) => x.PromptIndex == displayIndex) ?? list.Skip(displayIndex - 1).FirstOrDefault();
	}
    internal static string BuildPartialEffectFact(PartyTransferPromptEntry entry, PartyTransferEffectResult effect)
    {
        if (!effect.IsPartial || !effect.HasEffects) return "";
        var text = new StringBuilder("[AFEF 玩家动作补充]");
        AppendPartialEffect(text, entry, effect);
        return text.ToString();
    }
    private static void AppendPartialEffect(StringBuilder text, PartyTransferPromptEntry entry, PartyTransferEffectResult effect)
    {
        if (effect.Indeterminate)
        {
            text.Append(" 转移结果无法完整核实：").Append(entry?.DisplayName ?? "未知单位")
                .Append("，可能已发生部分副作用，不表示零转移；不自动重试。");
            return;
        }
        text.Append(" 转移部分失败：").Append(entry?.DisplayName ?? "未知单位")
            .Append("，源实际扣除").Append(effect.Debited).Append("人，目标实际新增")
            .Append(effect.Delivered).Append("人；不自动重试。");
    }
}
