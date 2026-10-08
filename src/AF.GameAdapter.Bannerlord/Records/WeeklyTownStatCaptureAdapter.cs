using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using static AnimusForge.WeeklyPoliticalMaterialPolicy;
using TownStatSnapshot = AnimusForge.MyBehavior.TownStatSnapshot;
namespace AnimusForge;
// Original daily town capture/baseline, not saved data. Every query is keyed to its event town.
internal sealed class WeeklyTownStatCaptureAdapter
{
private readonly Func<CampaignCharacterRecordCaptureAdapter> _record;
internal WeeklyTownStatCaptureAdapter(Func<CampaignCharacterRecordCaptureAdapter> record) { _record=record; }
internal readonly Dictionary<string, TownStatSnapshot> _townStatSnapshots = new Dictionary<string, TownStatSnapshot>(StringComparer.OrdinalIgnoreCase);
internal readonly Dictionary<string, TownStatSnapshot> _townStatWeekBaselineSnapshots = new Dictionary<string, TownStatSnapshot>(StringComparer.OrdinalIgnoreCase);
internal readonly Dictionary<string, int> _townStatWeekBaselineWeekIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
internal const int TownStatReasonMaxStats = 6;
internal const int TownStatReasonMaxLinesPerStat = 3;
internal const int TownStatReasonMaxLineLength = 34;
internal void TrackTownWeeklyMaterialChanges(Town town)
	{
		if (town?.Settlement == null || !town.Settlement.IsFortification)
		{
			return;
		}
		string settlementId = MemoryEntityIdentityBannerlordAdapter.GetSettlementId(town.Settlement);
		if (string.IsNullOrWhiteSpace(settlementId))
		{
			return;
		}
		TownStatSnapshot townStatSnapshot = CaptureTownSnapshot(town);
		if (!_townStatSnapshots.TryGetValue(settlementId, out var value) || value == null)
		{
			_townStatSnapshots[settlementId] = townStatSnapshot;
			_townStatWeekBaselineSnapshots[settlementId] = townStatSnapshot;
			_townStatWeekBaselineWeekIndexes[settlementId] = GetCurrentWeeklyIndexSafe();
			return;
		}
		int currentWeeklyIndexSafe = GetCurrentWeeklyIndexSafe();
		if (!_townStatWeekBaselineSnapshots.TryGetValue(settlementId, out var value2) || value2 == null || !_townStatWeekBaselineWeekIndexes.TryGetValue(settlementId, out var value3) || value3 != currentWeeklyIndexSafe)
		{
			value2 = value;
			_townStatWeekBaselineSnapshots[settlementId] = value2;
			_townStatWeekBaselineWeekIndexes[settlementId] = currentWeeklyIndexSafe;
		}
		List<string> list = new List<string>();
		HashSet<string> changedLabels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (AppendTownChangeRangeLine(list, "繁荣", value2.Prosperity, townStatSnapshot.Prosperity, 100f))
		{
			changedLabels.Add("繁荣");
		}
		if (AppendTownChangeRangeLine(list, "忠诚度", value2.Loyalty, townStatSnapshot.Loyalty, 2f))
		{
			changedLabels.Add("忠诚度");
		}
		if (AppendTownChangeRangeLine(list, "治安", value2.Security, townStatSnapshot.Security, 2f))
		{
			changedLabels.Add("治安");
		}
		if (AppendTownChangeRangeLine(list, "粮食", value2.FoodStocks, townStatSnapshot.FoodStocks, 5f))
		{
			changedLabels.Add("粮食");
		}
		if (AppendTownChangeRangeLine(list, "民兵", value2.Militia, townStatSnapshot.Militia, 8f))
		{
			changedLabels.Add("民兵");
		}
		if (AppendTownChangeRangeLine(list, "驻军", value2.Garrison, townStatSnapshot.Garrison, 10))
		{
			changedLabels.Add("驻军");
		}
		_townStatSnapshots[settlementId] = townStatSnapshot;
		if (list.Count == 0)
		{
			return;
		}
		string settlementDisplayName = MemoryEntityIdentityBannerlordAdapter.GetSettlementDisplayName(town.Settlement);
		string text = settlementDisplayName + "本周治理状态发生波动：" + string.Join("；", list) + "。";
		string townStatChangeReasonText = BuildTownStatChangeReasonText(town, changedLabels);
		if (!string.IsNullOrWhiteSpace(townStatChangeReasonText))
		{
			text = text + " " + townStatChangeReasonText;
		}
		string clanDisplayName = MemoryEntityIdentityBannerlordAdapter.GetClanDisplayName(town.Settlement.OwnerClan);
		string kingdomDisplayName = MemoryEntityIdentityBannerlordAdapter.GetKingdomDisplayName(town.Settlement.MapFaction as Kingdom, "所属王国");
		if (!string.IsNullOrWhiteSpace(clanDisplayName) && !string.IsNullOrWhiteSpace(kingdomDisplayName))
		{
			text = text + " 当前由" + clanDisplayName + "掌控，隶属于" + kingdomDisplayName + "。";
		}
		_record().RecordEventSourceMaterial("settlement_stats", "定居点状态变化 - " + settlementDisplayName, text, "settlement_stats:" + settlementId + ":" + MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe(), MemoryEntityIdentityBannerlordAdapter.GetKingdomId(town.Settlement.MapFaction), settlementId, includeInWorld: false, includeInKingdom: true);
	}

internal static int GetCurrentWeeklyIndexSafe()
	{
		int currentGameDayIndexSafe = MemoryEntityIdentityBannerlordAdapter.GetCurrentGameDayIndexSafe();
		return (currentGameDayIndexSafe > 0) ? (currentGameDayIndexSafe / 7) : 0;
	}

internal static TownStatSnapshot CaptureTownSnapshot(Town town)
	{
		return new TownStatSnapshot
		{
			Prosperity = town?.Prosperity ?? 0f,
			Loyalty = town?.Loyalty ?? 0f,
			Security = town?.Security ?? 0f,
			FoodStocks = town?.FoodStocks ?? 0f,
			Militia = town?.Settlement?.Militia ?? 0f,
			Garrison = town?.GarrisonParty?.MemberRoster?.TotalManCount ?? 0
		};
	}

internal static string BuildTownStatChangeReasonText(Town town, HashSet<string> changedLabels)
	{
		if (town?.Settlement == null || changedLabels == null || changedLabels.Count == 0)
		{
			return "";
		}
		var models = Campaign.Current?.Models;
		if (models == null)
		{
			return "";
		}
		List<string> parts = new List<string>();
		if (changedLabels.Contains("繁荣"))
		{
			AddTownStatModelReasonPart(parts, "繁荣", () => models.SettlementProsperityModel.CalculateProsperityChange(town, true));
		}
		if (changedLabels.Contains("忠诚度"))
		{
			AddTownStatModelReasonPart(parts, "忠诚度", () => models.SettlementLoyaltyModel.CalculateLoyaltyChange(town, true));
		}
		if (changedLabels.Contains("治安"))
		{
			AddTownStatModelReasonPart(parts, "治安", () => models.SettlementSecurityModel.CalculateSecurityChange(town, true));
		}
		if (changedLabels.Contains("粮食"))
		{
			AddTownStatModelReasonPart(parts, "粮食", () => models.SettlementFoodModel.CalculateTownFoodStocksChange(town, true, true));
		}
		if (changedLabels.Contains("民兵"))
		{
			AddTownStatModelReasonPart(parts, "民兵", () => models.SettlementMilitiaModel.CalculateMilitiaChange(town.Settlement, true));
		}
		if (changedLabels.Contains("驻军"))
		{
			AddTownStatModelReasonPart(parts, "驻军", () => models.SettlementGarrisonModel.CalculateBaseGarrisonChange(town.Settlement, true));
		}
		if (parts.Count == 0)
		{
			return "";
		}
		return "变化原因：" + string.Join("；", parts.Take(TownStatReasonMaxStats)) + "。";
	}

internal static void AddTownStatModelReasonPart(List<string> parts, string label, Func<ExplainedNumber> calculator)
	{
		if (parts == null || calculator == null)
		{
			return;
		}
		try
		{
			string text = BuildTownStatExplainedNumberReason(label, calculator());
			if (!string.IsNullOrWhiteSpace(text) && !parts.Any((string x) => string.Equals(x, text, StringComparison.OrdinalIgnoreCase)))
			{
				parts.Add(text);
			}
		}
		catch
		{
		}
	}

internal static string BuildTownStatExplainedNumberReason(string label, ExplainedNumber explainedNumber)
	{
		List<Tuple<string, float>> list = new List<Tuple<string, float>>();
		foreach (var line in explainedNumber.GetLines())
		{
			string text = CondenseTownStatReasonLineName(line.Item1);
			float item = line.Item2;
			if (string.IsNullOrWhiteSpace(text) || float.IsNaN(item) || float.IsInfinity(item) || MathF.Abs(item) < 0.005f)
			{
				continue;
			}
			list.Add(Tuple.Create(text, item));
		}
		if (list.Count == 0)
		{
			return "";
		}
		List<string> factors = list.OrderByDescending((Tuple<string, float> x) => MathF.Abs(x.Item2)).ThenBy((Tuple<string, float> x) => x.Item1, StringComparer.OrdinalIgnoreCase).Select((Tuple<string, float> x) => x.Item1).Distinct(StringComparer.OrdinalIgnoreCase).Take(TownStatReasonMaxLinesPerStat).ToList();
		return label + "主因：" + string.Join("、", factors);
	}

internal static string CondenseTownStatReasonLineName(string text)
	{
		text = NormalizePoliticalReasonText(text);
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		text = Regex.Replace(text, "\\s+", " ");
		return TrimPoliticalReasonPart(text, TownStatReasonMaxLineLength);
	}

internal static void AppendTownChangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold)
	{
		if (lines == null)
		{
			return;
		}
		float num = newValue - oldValue;
		if (MathF.Abs(num) < threshold)
		{
			return;
		}
		lines.Add(BuildTownChangeQualitativeLine(label, num, threshold));
	}

internal static void AppendTownChangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold)
	{
		if (lines == null)
		{
			return;
		}
		int num = newValue - oldValue;
		if (Math.Abs(num) < threshold)
		{
			return;
		}
		lines.Add(BuildTownChangeQualitativeLine(label, num, threshold));
	}

internal static bool AppendTownChangeRangeLine(List<string> lines, string label, float oldValue, float newValue, float threshold)
	{
		if (lines == null)
		{
			return false;
		}
		float num = newValue - oldValue;
		if (MathF.Abs(num) < threshold)
		{
			return false;
		}
		lines.Add(BuildTownChangeQualitativeLine(label, num, threshold));
		return true;
	}

internal static bool AppendTownChangeRangeLine(List<string> lines, string label, int oldValue, int newValue, int threshold)
	{
		if (lines == null)
		{
			return false;
		}
		int num = newValue - oldValue;
		if (Math.Abs(num) < threshold)
		{
			return false;
		}
		lines.Add(BuildTownChangeQualitativeLine(label, num, threshold));
		return true;
	}

internal static string BuildTownChangeQualitativeLine(string label, float delta, float threshold)
	{
		string text = (label ?? "").Trim();
		string intensity = MathF.Abs(delta) >= Math.Max(threshold * 3f, threshold + 0.001f) ? "明显" : "小幅";
		bool rise = delta > 0f;
		switch (text)
		{
		case "繁荣":
			return text + intensity + (rise ? "扩张" : "回落");
		case "忠诚度":
			return text + intensity + (rise ? "改善" : "走低");
		case "治安":
			return text + intensity + (rise ? "改善" : "恶化");
		case "粮食":
			return text + intensity + (rise ? "恢复" : "吃紧");
		case "民兵":
		case "驻军":
			return text + intensity + (rise ? "增加" : "减少");
		default:
			return text + intensity + (rise ? "改善" : "走低");
		}
	}
}
