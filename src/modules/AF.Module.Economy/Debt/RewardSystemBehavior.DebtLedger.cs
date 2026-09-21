using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AnimusForge.Modules.Economy;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

public partial class RewardSystemBehavior
{
	public class DebtExportEntry
	{
		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLineExportEntry> DebtLines = new List<DebtLineExportEntry>();
	}

	public class DebtLineExportEntry
	{
		public string DebtId;

		public bool IsGold;

		public string ItemId;

		public bool IsDueUnlimited;

		public bool IsItemUnavailableDeclared;

		public int InitialAmount;

		public int RemainingAmount;

		public float CreatedDay;

		public float DueDay;

		public float BestPreDueCoverage;

		public int OnTimePenaltyTierApplied;

		public int OverduePenaltyDaysApplied;

		public int LastOverduePenaltyDay;

		public int OverdueTrustPenaltyPerDay;

		public int OverdueRelationPenaltyPerDay;

		public int CompensationUnitPrice;

		public int CompensationGoldCredit;

		public long UnlimitedTrustPenaltyNumeratorCarry;

		public string DebtNote;
	}

	private class DebtRecord
	{
		public class DebtLine
		{
			public string DebtId;

			public bool IsGold;

			public string ItemId;

			public bool IsDueUnlimited;

			public bool IsItemUnavailableDeclared;

			public int InitialAmount;

			public int RemainingAmount;

			public float CreatedDay;

			public float DueDay;

			public float BestPreDueCoverage;

			public int OnTimePenaltyTierApplied;

			public int OverduePenaltyDaysApplied;

			public int LastOverduePenaltyDay;

			public int OverdueTrustPenaltyPerDay;

			public int OverdueRelationPenaltyPerDay;

			public int CompensationUnitPrice;

			public int CompensationGoldCredit;

			public long UnlimitedTrustPenaltyNumeratorCarry;

			public string DebtNote;
		}

		public int OwedGold;

		public Dictionary<string, int> OwedItems = new Dictionary<string, int>();

		public float CreatedDay;

		public float DueDay;

		public List<DebtLine> DebtLines = new List<DebtLine>();
	}

	private Dictionary<string, DebtRecord> _debts = new Dictionary<string, DebtRecord>();

	private void NormalizeDebtRecord(DebtRecord rec)
	{
		EconomyDebtNormalizationPolicy.Normalize(
			rec,
			GetNowCampaignDay(),
			BuildDebtId,
			OverduePenaltyMaxWeeks,
			LlmManualPenaltyMax);
	}

	private static string BuildDebtDueStatusText(float dueDay, bool isDueUnlimited = false)
	{
		if (isDueUnlimited)
		{
			return "还款期限：无限期（债务仍有效）";
		}
		if (dueDay <= 0f)
		{
			return "";
		}
		float nowCampaignDay = GetNowCampaignDay();
		float num = dueDay - nowCampaignDay;
		int absDay = ToDisplayDay(dueDay);
		string text = FormatAbsDayAsGameDate(absDay);
		if (num > 0.01f)
		{
			int num2 = Math.Max(1, (int)Math.Ceiling(num));
			return $"还款期限：约 {num2} 天内（截止 {text}）";
		}
		if (num >= -0.01f)
		{
			return "还款期限：今日到期（" + text + "）";
		}
		int num3 = Math.Max(1, (int)Math.Ceiling(0f - num));
		return $"还款期限：已逾期 {num3} 天（截止 {text}）";
	}

	private static string BuildDebtPromiseDeadlineText(float dueDay, bool isDueUnlimited)
	{
		string text = BuildDebtDueStatusText(dueDay, isDueUnlimited);
		const string prefix = "还款期限：";
		if (text.StartsWith(prefix, StringComparison.Ordinal))
		{
			text = text.Substring(prefix.Length).Trim();
		}
		return string.IsNullOrWhiteSpace(text) ? "未设定" : text;
	}

	private static string NormalizeDebtNote(string note)
	{
		return EconomyDebtNormalizationPolicy.NormalizeNote(note);
	}

	private int EstimateDebtLineRemainingValue(Hero npc, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		if (string.IsNullOrWhiteSpace(line.ItemId))
		{
			return 0;
		}
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			ItemObject item = ResolveItemById(line.ItemId);
			ItemGuidePriceInfo guidePriceForItemNearHero = GetGuidePriceForItemNearHero(npc, item);
			val = Math.Max(1, guidePriceForItemNearHero.UnitPrice);
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
		if (num <= 0)
		{
			return 0;
		}
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return (int)num;
	}

	private int EstimateDebtLineRemainingValueForSettlement(Settlement settlement, DebtRecord.DebtLine line)
	{
		if (line == null || line.RemainingAmount <= 0)
		{
			return 0;
		}
		if (line.IsGold)
		{
			return Math.Max(0, line.RemainingAmount);
		}
		ItemObject item = ResolveItemById(line.ItemId);
		int val = Math.Max(1, line.CompensationUnitPrice);
		if (line.CompensationUnitPrice <= 0)
		{
			try
			{
				if (settlement != null && item != null && TryGetSettlementBuyPrice(settlement, item, out var price))
				{
					val = Math.Max(1, price);
				}
				else
				{
					val = Math.Max(1, item?.Value ?? 1);
				}
			}
			catch
			{
				val = Math.Max(1, item?.Value ?? 1);
			}
		}
		long num = (long)Math.Max(0, line.RemainingAmount) * (long)Math.Max(1, val);
		if (num > int.MaxValue)
		{
			return int.MaxValue;
		}
		return Math.Max(0, (int)num);
	}

	private string BuildDailyDebtReminderText(Hero npc, DebtRecord rec, int campaignDayIndex, int maxLines = 2)
	{
		if (npc == null || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(x, campaignDayIndex))
			orderby x.IsDueUnlimited ? 0 : 1, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string value = npc.Name?.ToString() ?? "该NPC";
		stringBuilder.Append("【承诺或欠款提醒】你对 ").Append(value).Append(" 的承诺或欠款：");
		int num = Math.Min(maxLines, list.Count);
		for (int num2 = 0; num2 < num; num2++)
		{
			DebtRecord.DebtLine debtLine = list[num2];
			int debtValue = EstimateDebtLineRemainingValue(npc, debtLine);
			string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
			string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
			stringBuilder.Append(" [ID:").Append(debtLine.DebtId).Append("] 承诺或欠款价值 ")
				.Append(debtValue)
				.Append(" 第纳尔，达成期限为：")
				.Append(deadline)
				.Append("，备注：")
				.Append(note);
			if (num2 < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	private string BuildDailyMerchantDebtReminderText(Settlement settlement, SettlementMerchantKind kind, DebtRecord rec, int campaignDayIndex, int maxLines = 2)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || rec == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(rec);
		if (!HasDebtContent(rec))
		{
			return string.Empty;
		}
		List<DebtRecord.DebtLine> list = (from x in rec.DebtLines?.Where((DebtRecord.DebtLine x) => EconomyDebtSchedulePolicy.ShouldIncludeDebtLineInScheduledReminder(x, campaignDayIndex))
			orderby x.IsDueUnlimited ? 0 : 1, x.DueDay, x.CreatedDay
			select x).ToList() ?? new List<DebtRecord.DebtLine>();
		if (list.Count <= 0)
		{
			return string.Empty;
		}
		if (maxLines < 1)
		{
			maxLines = 1;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("【承诺或欠款提醒】你对 ").Append(BuildSettlementMerchantDebtLabel(settlement, kind)).Append(" 的承诺或欠款：");
		int num = Math.Min(maxLines, list.Count);
		for (int i = 0; i < num; i++)
		{
			DebtRecord.DebtLine debtLine = list[i];
			int debtValue = EstimateDebtLineRemainingValueForSettlement(settlement, debtLine);
			string deadline = BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited);
			string note = string.IsNullOrWhiteSpace(debtLine.DebtNote) ? "无" : debtLine.DebtNote;
			stringBuilder.Append(" [ID:").Append(debtLine.DebtId).Append("] 承诺或欠款价值 ")
				.Append(debtValue)
				.Append(" 第纳尔，达成期限为：")
				.Append(deadline)
				.Append("，备注：")
				.Append(note);
			if (i < num - 1)
			{
				stringBuilder.Append("；");
			}
		}
		if (list.Count > num)
		{
			stringBuilder.Append("；...还有 ").Append(list.Count - num).Append(" 笔");
		}
		return stringBuilder.ToString();
	}

	public Dictionary<string, DebtExportEntry> ExportDebtEntries()
	{
		Dictionary<string, DebtExportEntry> dictionary = new Dictionary<string, DebtExportEntry>();
		if (_debts == null)
		{
			return dictionary;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			if (string.IsNullOrEmpty(debt.Key) || debt.Value == null)
			{
				continue;
			}
			DebtRecord value = debt.Value;
			NormalizeDebtRecord(value);
			bool flag = value.OwedGold > 0;
			if (!flag && value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (owedItem.Value > 0)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				continue;
			}
			DebtExportEntry debtExportEntry = new DebtExportEntry();
			debtExportEntry.OwedGold = Math.Max(0, value.OwedGold);
			debtExportEntry.OwedItems = new Dictionary<string, int>();
			debtExportEntry.CreatedDay = value.CreatedDay;
			debtExportEntry.DueDay = value.DueDay;
			debtExportEntry.DebtLines = new List<DebtLineExportEntry>();
			if (value.DebtLines != null)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtRecord.DebtLine debtLine = value.DebtLines[i];
					if (debtLine != null && debtLine.RemainingAmount > 0)
					{
						debtExportEntry.DebtLines.Add(new DebtLineExportEntry
						{
							DebtId = debtLine.DebtId,
							IsGold = debtLine.IsGold,
							ItemId = debtLine.ItemId,
							IsDueUnlimited = debtLine.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLine.IsItemUnavailableDeclared,
							InitialAmount = debtLine.InitialAmount,
							RemainingAmount = debtLine.RemainingAmount,
							CreatedDay = debtLine.CreatedDay,
							DueDay = debtLine.DueDay,
							BestPreDueCoverage = debtLine.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLine.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLine.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLine.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLine.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLine.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLine.CompensationUnitPrice,
							CompensationGoldCredit = debtLine.CompensationGoldCredit,
							UnlimitedTrustPenaltyNumeratorCarry = debtLine.UnlimitedTrustPenaltyNumeratorCarry,
							DebtNote = debtLine.DebtNote
						});
					}
				}
			}
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem2 in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem2.Key) && owedItem2.Value > 0)
					{
						debtExportEntry.OwedItems[owedItem2.Key] = owedItem2.Value;
					}
				}
			}
			dictionary[debt.Key] = debtExportEntry;
		}
		return dictionary;
	}

	public void ImportDebtEntries(Dictionary<string, DebtExportEntry> entries)
	{
		if (entries == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		_debts.Clear();
		foreach (KeyValuePair<string, DebtExportEntry> entry in entries)
		{
			if (string.IsNullOrEmpty(entry.Key) || entry.Value == null)
			{
				continue;
			}
			DebtExportEntry value = entry.Value;
			int num = Math.Max(0, value.OwedGold);
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			if (value.OwedItems != null)
			{
				foreach (KeyValuePair<string, int> owedItem in value.OwedItems)
				{
					if (!string.IsNullOrEmpty(owedItem.Key) && owedItem.Value > 0)
					{
						dictionary[owedItem.Key] = owedItem.Value;
					}
				}
			}
			bool flag = num > 0 || dictionary.Count > 0;
			bool flag2 = value.DebtLines != null && value.DebtLines.Count > 0;
			if (!flag && !flag2)
			{
				continue;
			}
			DebtRecord debtRecord = new DebtRecord();
			debtRecord.DebtLines = new List<DebtRecord.DebtLine>();
			if (flag2)
			{
				for (int i = 0; i < value.DebtLines.Count; i++)
				{
					DebtLineExportEntry debtLineExportEntry = value.DebtLines[i];
					if (debtLineExportEntry != null && debtLineExportEntry.RemainingAmount > 0)
					{
						debtRecord.DebtLines.Add(new DebtRecord.DebtLine
						{
							DebtId = debtLineExportEntry.DebtId,
							IsGold = debtLineExportEntry.IsGold,
							ItemId = debtLineExportEntry.ItemId,
							IsDueUnlimited = debtLineExportEntry.IsDueUnlimited,
							IsItemUnavailableDeclared = debtLineExportEntry.IsItemUnavailableDeclared,
							InitialAmount = debtLineExportEntry.InitialAmount,
							RemainingAmount = debtLineExportEntry.RemainingAmount,
							CreatedDay = debtLineExportEntry.CreatedDay,
							DueDay = debtLineExportEntry.DueDay,
							BestPreDueCoverage = debtLineExportEntry.BestPreDueCoverage,
							OnTimePenaltyTierApplied = debtLineExportEntry.OnTimePenaltyTierApplied,
							OverduePenaltyDaysApplied = debtLineExportEntry.OverduePenaltyDaysApplied,
							LastOverduePenaltyDay = debtLineExportEntry.LastOverduePenaltyDay,
							OverdueTrustPenaltyPerDay = debtLineExportEntry.OverdueTrustPenaltyPerDay,
							OverdueRelationPenaltyPerDay = debtLineExportEntry.OverdueRelationPenaltyPerDay,
							CompensationUnitPrice = debtLineExportEntry.CompensationUnitPrice,
							CompensationGoldCredit = debtLineExportEntry.CompensationGoldCredit,
							UnlimitedTrustPenaltyNumeratorCarry = debtLineExportEntry.UnlimitedTrustPenaltyNumeratorCarry,
							DebtNote = debtLineExportEntry.DebtNote
						});
					}
				}
			}
			else
			{
				debtRecord.OwedGold = num;
				debtRecord.OwedItems = dictionary;
				debtRecord.CreatedDay = value.CreatedDay;
				debtRecord.DueDay = value.DueDay;
			}
			NormalizeDebtRecord(debtRecord);
			_debts[entry.Key] = debtRecord;
		}
		// Imported active lines receive the same task reconciliation as debts restored from a campaign save.
		QueueDebtPromiseQuestsForActiveDebts();
	}

	public List<string> GetAllDebtorHeroIds()
	{
		List<string> list = new List<string>();
		if (_debts == null)
		{
			return list;
		}
		foreach (KeyValuePair<string, DebtRecord> debt in _debts)
		{
			DebtRecord value = debt.Value;
			if (value != null)
			{
				NormalizeDebtRecord(value);
				if (HasDebtContent(value) && !string.IsNullOrEmpty(debt.Key))
				{
					list.Add(debt.Key);
				}
			}
		}
		return list;
	}

	public string BuildDebtHintForAI(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return string.Empty;
		}
		NormalizeDebtRecord(debtRecord);
		bool hasDebtContent = HasDebtContent(debtRecord);
		List<EconomyDebtPromptLine> lines = new List<EconomyDebtPromptLine>();
		if (hasDebtContent && debtRecord.DebtLines != null)
		{
			foreach (DebtRecord.DebtLine debtLine in debtRecord.DebtLines
				.Where(x => x != null && x.RemainingAmount > 0)
				.OrderBy(x => x.DueDay)
				.ThenBy(x => x.CreatedDay))
			{
				lines.Add(new EconomyDebtPromptLine(
					debtLine.DebtId,
					EstimateDebtLineRemainingValue(npc, debtLine),
					BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited),
					debtLine.DebtNote));
			}
		}
		return EconomyPromptProjection.BuildHeroDebtHint(hasDebtContent, lines);
	}

	public string BuildSettlementMerchantDebtHintForAI(CharacterObject character, Settlement settlement = null)
	{
		if (!TryGetSettlementMerchantKind(character, out var kind))
		{
			return "";
		}
		settlement = settlement ?? Settlement.CurrentSettlement;
		DebtRecord settlementMerchantDebtRecord = GetSettlementMerchantDebtRecord(settlement, kind);
		if (settlementMerchantDebtRecord == null)
		{
			return "";
		}
		NormalizeDebtRecord(settlementMerchantDebtRecord);
		bool hasDebtContent = HasDebtContent(settlementMerchantDebtRecord);
		List<EconomyDebtPromptLine> lines = new List<EconomyDebtPromptLine>();
		if (hasDebtContent)
		{
			foreach (DebtRecord.DebtLine debtLine in settlementMerchantDebtRecord.DebtLines
				.Where(x => x != null && x.RemainingAmount > 0)
				.OrderBy(x => x.DueDay)
				.ThenBy(x => x.CreatedDay))
			{
				lines.Add(new EconomyDebtPromptLine(
					debtLine.DebtId,
					EstimateDebtLineRemainingValueForSettlement(settlement, debtLine),
					BuildDebtPromiseDeadlineText(debtLine.DueDay, debtLine.IsDueUnlimited),
					debtLine.DebtNote));
			}
		}
		return EconomyPromptProjection.BuildSettlementMerchantDebtHint(
			BuildSettlementMerchantDebtLabel(settlement, kind),
			hasDebtContent,
			lines);
	}

	public string BuildDebtEditorSummary(Hero npc, int maxLines = 12)
	{
		try
		{
			if (maxLines < 1)
			{
				maxLines = 1;
			}
			DebtRecord debtRecord = GetDebtRecord(npc);
			if (debtRecord == null)
			{
				return "当前无欠款。";
			}
			NormalizeDebtRecord(debtRecord);
			if (!HasDebtContent(debtRecord))
			{
				return "当前无欠款。";
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("金币总欠款：").Append(debtRecord.OwedGold).AppendLine();
			if (debtRecord.OwedItems != null && debtRecord.OwedItems.Count > 0)
			{
				List<string> list = new List<string>();
				foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
				{
					if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
					{
						list.Add(owedItem.Key + "x" + owedItem.Value);
					}
				}
				if (list.Count > 0)
				{
					stringBuilder.AppendLine("物品总欠款：" + string.Join("，", list));
				}
			}
			List<DebtRecord.DebtLine> list2 = (from x in debtRecord.DebtLines?.Where((DebtRecord.DebtLine x) => x != null && x.RemainingAmount > 0)
				orderby x.IsDueUnlimited ? 1 : 0, x.DueDay, x.CreatedDay
				select x).ToList() ?? new List<DebtRecord.DebtLine>();
			stringBuilder.Append("分笔未清：").Append(list2.Count).Append(" 笔")
				.AppendLine();
			int num = 0;
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				if (num >= maxLines)
				{
					break;
				}
				DebtRecord.DebtLine debtLine = list2[num2];
				string value = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited);
				string value2 = (debtLine.IsGold ? "金币" : ("物品:" + debtLine.ItemId));
				string value3 = (debtLine.IsGold ? (debtLine.RemainingAmount + " 第纳尔") : ("x" + debtLine.RemainingAmount));
				stringBuilder.Append("- [").Append(debtLine.DebtId).Append("] ")
					.Append(value2)
					.Append("，剩余 ")
					.Append(value3);
				if (!string.IsNullOrWhiteSpace(value))
				{
					stringBuilder.Append("，").Append(value);
				}
				if (!debtLine.IsGold && debtLine.IsItemUnavailableDeclared)
				{
					stringBuilder.Append("，已标记无法归还原物");
				}
				stringBuilder.AppendLine();
				num++;
			}
			if (list2.Count > num)
			{
				stringBuilder.Append("... 还有 ").Append(list2.Count - num).Append(" 笔未显示。");
			}
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "欠款摘要读取失败。";
		}
	}

	private DebtRecord GetOrCreateDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		return value;
	}

	private DebtRecord GetDebtRecord(Hero npc)
	{
		if (npc == null)
		{
			return null;
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(stringId, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (_debts.TryGetValue(debtKey, out var value))
		{
			return value;
		}
		return null;
	}

	private DebtRecord GetOrCreateDebtRecordByKey(string debtKey)
	{
		if (string.IsNullOrWhiteSpace(debtKey))
		{
			return null;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		if (!_debts.TryGetValue(debtKey, out var value) || value == null)
		{
			value = new DebtRecord();
			_debts[debtKey] = value;
		}
		return value;
	}

	private DebtRecord GetSettlementMerchantDebtRecord(Settlement settlement, SettlementMerchantKind kind)
	{
		return GetDebtRecordByKey(BuildSettlementMerchantDebtKey(settlement, kind));
	}

	public bool HasUnpaidDebtForInteraction(Hero targetHero, CharacterObject targetCharacter = null, Settlement settlement = null)
	{
		Hero hero = targetHero ?? targetCharacter?.HeroObject;
		if (hero != null && HasUnpaidDebt(hero))
		{
			return true;
		}
		if (targetCharacter == null || !TryGetSettlementMerchantKind(targetCharacter, out var kind))
		{
			return false;
		}
		DebtRecord settlementMerchantDebtRecord = GetSettlementMerchantDebtRecord(settlement ?? Settlement.CurrentSettlement, kind);
		if (settlementMerchantDebtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(settlementMerchantDebtRecord);
		return HasDebtContent(settlementMerchantDebtRecord);
	}

	public bool HasUnpaidDebt(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (debtRecord.OwedGold > 0)
		{
			return true;
		}
		if (debtRecord.OwedItems == null)
		{
			return false;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsDebtOverdue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return false;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return false;
		}
		return GetNowCampaignDay() > debtRecord.DueDay + 0.01f;
	}

	public int GetDebtDaysToDue(Hero npc)
	{
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return 0;
		}
		NormalizeDebtRecord(debtRecord);
		if (!HasDebtContent(debtRecord) || debtRecord.DueDay <= 0f)
		{
			return 0;
		}
		float num = debtRecord.DueDay - GetNowCampaignDay();
		if (num >= 0f)
		{
			return (int)Math.Ceiling(num);
		}
		return -(int)Math.Ceiling(0f - num);
	}

	public void GetDebtSnapshot(Hero npc, out int owedGold, out Dictionary<string, int> owedItems)
	{
		owedGold = 0;
		owedItems = new Dictionary<string, int>();
		DebtRecord debtRecord = GetDebtRecord(npc);
		if (debtRecord == null)
		{
			return;
		}
		NormalizeDebtRecord(debtRecord);
		owedGold = debtRecord.OwedGold;
		if (debtRecord.OwedItems == null)
		{
			return;
		}
		foreach (KeyValuePair<string, int> owedItem in debtRecord.OwedItems)
		{
			if (owedItem.Value > 0)
			{
				owedItems[owedItem.Key] = owedItem.Value;
			}
		}
	}

	public void SetDebt(Hero npc, int owedGold, Dictionary<string, int> owedItems, float dueDay = 0f)
	{
		if (npc == null)
		{
			return;
		}
		if (_debts == null)
		{
			_debts = new Dictionary<string, DebtRecord>();
		}
		string stringId = npc.StringId;
		if (string.IsNullOrEmpty(stringId))
		{
			return;
		}
		if (!_debts.TryGetValue(stringId, out var value))
		{
			value = new DebtRecord();
			_debts[stringId] = value;
		}
		value.OwedGold = 0;
		if (value.OwedItems == null)
		{
			value.OwedItems = new Dictionary<string, int>();
		}
		else
		{
			value.OwedItems.Clear();
		}
		value.CreatedDay = 0f;
		value.DueDay = 0f;
		float nowCampaignDay = GetNowCampaignDay();
		float dueDay2 = ((dueDay > 0f) ? dueDay : (nowCampaignDay + 1f));
		value.DebtLines = new List<DebtRecord.DebtLine>();
		if (owedGold > 0)
		{
			value.DebtLines.Add(new DebtRecord.DebtLine
			{
				DebtId = BuildDebtId(),
				IsGold = true,
				ItemId = null,
				IsDueUnlimited = false,
				IsItemUnavailableDeclared = false,
				InitialAmount = Math.Max(0, owedGold),
				RemainingAmount = Math.Max(0, owedGold),
				CreatedDay = nowCampaignDay,
				DueDay = dueDay2,
				BestPreDueCoverage = 0f,
				OnTimePenaltyTierApplied = 0,
				OverduePenaltyDaysApplied = 0,
				LastOverduePenaltyDay = -1,
				OverdueTrustPenaltyPerDay = 0,
				OverdueRelationPenaltyPerDay = 0,
				CompensationUnitPrice = 0,
				CompensationGoldCredit = 0
			});
		}
		if (owedItems != null)
		{
			foreach (KeyValuePair<string, int> owedItem in owedItems)
			{
				if (!string.IsNullOrWhiteSpace(owedItem.Key) && owedItem.Value > 0)
				{
					value.DebtLines.Add(new DebtRecord.DebtLine
					{
						DebtId = BuildDebtId(),
						IsGold = false,
						ItemId = owedItem.Key,
						IsDueUnlimited = false,
						IsItemUnavailableDeclared = false,
						InitialAmount = owedItem.Value,
						RemainingAmount = owedItem.Value,
						CreatedDay = nowCampaignDay,
						DueDay = dueDay2,
						BestPreDueCoverage = 0f,
						OnTimePenaltyTierApplied = 0,
						OverduePenaltyDaysApplied = 0,
						LastOverduePenaltyDay = -1,
						OverdueTrustPenaltyPerDay = 0,
						OverdueRelationPenaltyPerDay = 0,
						CompensationUnitPrice = 0,
						CompensationGoldCredit = 0
					});
				}
			}
		}
		NormalizeDebtRecord(value);
		if (!HasDebtContent(value))
		{
			_debts.Remove(stringId);
			return;
		}
		// Public SetDebt callers bypass AD parsing; queue only their new lines instead of scanning every debtor.
		for (int i = 0; i < value.DebtLines.Count; i++)
		{
			DebtRecord.DebtLine debtLine = value.DebtLines[i];
			if (debtLine != null && debtLine.RemainingAmount > 0)
			{
				QueueDebtPromiseQuest(stringId, debtLine.DebtId);
			}
		}
	}

	private DebtRecord.DebtLine SetDebtForNpc(Hero npc, int debtValue, int dueDays, string debtNote)
	{
		if (npc == null || debtValue <= 0)
		{
			return null;
		}
		DebtRecord orCreateDebtRecord = GetOrCreateDebtRecord(npc);
		if (orCreateDebtRecord == null)
		{
			return null;
		}
		NormalizeDebtRecord(orCreateDebtRecord);
		float nowCampaignDay = GetNowCampaignDay();
		int campaignDayIndex = GetCampaignDayIndex();
		bool dueUnlimited = dueDays <= 0;
		float dueDay = dueUnlimited ? 0f : nowCampaignDay + (float)EconomyDebtSchedulePolicy.NormalizeDueDays(dueDays);
		if (!dueUnlimited && dueDay <= 0f)
		{
			dueDay = nowCampaignDay + 1f;
		}
		DebtRecord.DebtLine debtLine = new DebtRecord.DebtLine
		{
			DebtId = BuildDebtId(),
			IsGold = true,
			ItemId = null,
			IsDueUnlimited = dueUnlimited,
			IsItemUnavailableDeclared = false,
			InitialAmount = debtValue,
			RemainingAmount = debtValue,
			CreatedDay = nowCampaignDay,
			DueDay = dueDay,
			BestPreDueCoverage = 0f,
			OnTimePenaltyTierApplied = 0,
			OverduePenaltyDaysApplied = 0,
			LastOverduePenaltyDay = dueUnlimited ? campaignDayIndex : -1,
			OverdueTrustPenaltyPerDay = 0,
			OverdueRelationPenaltyPerDay = 0,
			CompensationUnitPrice = 0,
			CompensationGoldCredit = 0,
			UnlimitedTrustPenaltyNumeratorCarry = 0L,
			DebtNote = NormalizeDebtNote(debtNote)
		};
		orCreateDebtRecord.DebtLines.Add(debtLine);
		NormalizeDebtRecord(orCreateDebtRecord);
		// Defer QuestBase.StartQuest until the current AD-tag conversation has finished naturally.
		QueueDebtPromiseQuest(npc.StringId, debtLine.DebtId);
		return debtLine;
	}

	public bool RecordDeferredDuelDebtForNpc(Hero npc, int goldAmount, int dueDays, string debtNote, out string debtId, out string dueStatusText)
	{
		debtId = "";
		dueStatusText = "";
		try
		{
			DebtRecord.DebtLine debtLine = SetDebtForNpc(npc, goldAmount, dueDays, debtNote);
			if (debtLine == null)
			{
				return false;
			}
			debtId = debtLine.DebtId ?? "";
			dueStatusText = BuildDebtDueStatusText(debtLine.DueDay, debtLine.IsDueUnlimited) ?? "";
			return true;
		}
		catch
		{
			debtId = "";
			dueStatusText = "";
			return false;
		}
	}

	private DebtRecord.DebtLine SetDebtForSettlementMerchant(Settlement settlement, SettlementMerchantKind kind, int debtValue, int dueDays, string debtNote)
	{
		if (settlement == null || kind == SettlementMerchantKind.None || debtValue <= 0)
		{
			return null;
		}
		string settlementMerchantDebtKey = BuildSettlementMerchantDebtKey(settlement, kind);
		DebtRecord orCreateDebtRecordByKey = GetOrCreateDebtRecordByKey(settlementMerchantDebtKey);
		if (orCreateDebtRecordByKey == null)
		{
			return null;
		}
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		float nowCampaignDay = GetNowCampaignDay();
		int campaignDayIndex = GetCampaignDayIndex();
		bool dueUnlimited = dueDays <= 0;
		float dueDay = dueUnlimited ? 0f : nowCampaignDay + (float)EconomyDebtSchedulePolicy.NormalizeDueDays(dueDays);
		if (!dueUnlimited && dueDay <= 0f)
		{
			dueDay = nowCampaignDay + 1f;
		}
		DebtRecord.DebtLine debtLine = new DebtRecord.DebtLine
		{
			DebtId = BuildDebtId(),
			IsGold = true,
			ItemId = null,
			IsDueUnlimited = dueUnlimited,
			IsItemUnavailableDeclared = false,
			InitialAmount = debtValue,
			RemainingAmount = debtValue,
			CreatedDay = nowCampaignDay,
			DueDay = dueDay,
			BestPreDueCoverage = 0f,
			OnTimePenaltyTierApplied = 0,
			OverduePenaltyDaysApplied = 0,
			LastOverduePenaltyDay = dueUnlimited ? campaignDayIndex : -1,
			OverdueTrustPenaltyPerDay = 0,
			OverdueRelationPenaltyPerDay = 0,
			CompensationUnitPrice = 0,
			CompensationGoldCredit = 0,
			UnlimitedTrustPenaltyNumeratorCarry = 0L,
			DebtNote = NormalizeDebtNote(debtNote)
		};
		orCreateDebtRecordByKey.DebtLines.Add(debtLine);
		NormalizeDebtRecord(orCreateDebtRecordByKey);
		// Market debts use the same deferred queue so all three conversation channels create tasks safely.
		QueueDebtPromiseQuest(settlementMerchantDebtKey, debtLine.DebtId);
		return debtLine;
	}

	private bool TryFindDebtLineById(Hero npc, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (npc == null || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少NPC或债务ID。";
			return false;
		}
		rec = GetDebtRecord(npc);
		if (rec == null)
		{
			statusText = "未找到该NPC的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		if (rec.DebtLines == null || rec.DebtLines.Count <= 0)
		{
			statusText = "该NPC当前没有可还债务。";
			return false;
		}
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	public bool ResolveDebtByIdByAgreement(Hero npc, string debtId, out string statusText)
	{
		statusText = "";
		if (!TryFindDebtLineById(npc, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		int remainingAmount = Math.Max(0, line.RemainingAmount);
		line.RemainingAmount = 0;
		NormalizeDebtRecord(rec);
		// Complete this exact debt line's task even when the same NPC still has other unpaid lines.
		CompleteDebtPromiseQuest(npc.StringId, line.DebtId);
		statusText = $"债务ID {line.DebtId} 已按协商解除（{remainingAmount} -> 0）。";
		if (!HasDebtContent(rec) && !string.IsNullOrWhiteSpace(npc?.StringId))
		{
			_debts.Remove(npc.StringId);
			return true;
		}
		return false;
	}

	private bool TryFindSettlementMerchantDebtLineById(Settlement settlement, SettlementMerchantKind kind, string debtId, out DebtRecord rec, out DebtRecord.DebtLine line, out string statusText)
	{
		statusText = "";
		rec = null;
		line = null;
		if (settlement == null || kind == SettlementMerchantKind.None || string.IsNullOrWhiteSpace(debtId))
		{
			statusText = "参数无效：缺少市场债主或债务ID。";
			return false;
		}
		rec = GetSettlementMerchantDebtRecord(settlement, kind);
		if (rec == null)
		{
			statusText = "未找到该市场的债务记录。";
			return false;
		}
		NormalizeDebtRecord(rec);
		line = rec.DebtLines.FirstOrDefault((DebtRecord.DebtLine x) => x != null && string.Equals(x.DebtId ?? "", debtId.Trim(), StringComparison.OrdinalIgnoreCase));
		if (line == null || line.RemainingAmount <= 0)
		{
			statusText = "未找到可结算的债务ID，或该债务已清。";
			return false;
		}
		return true;
	}

	public bool ResolveSettlementMerchantDebtByIdByAgreement(Settlement settlement, SettlementMerchantKind kind, string debtId, out string statusText)
	{
		statusText = "";
		if (!TryFindSettlementMerchantDebtLineById(settlement, kind, debtId, out var rec, out var line, out statusText))
		{
			return false;
		}
		int remainingAmount = Math.Max(0, line.RemainingAmount);
		line.RemainingAmount = 0;
		NormalizeDebtRecord(rec);
		// Market obligations also complete by line ID, rather than waiting for the whole market balance to be cleared.
		CompleteDebtPromiseQuest(BuildSettlementMerchantDebtKey(settlement, kind), line.DebtId);
		statusText = $"债务ID {line.DebtId} 已按协商解除（{remainingAmount} -> 0）。";
		if (!HasDebtContent(rec))
		{
			_debts.Remove(BuildSettlementMerchantDebtKey(settlement, kind));
			return true;
		}
		return false;
	}
}
