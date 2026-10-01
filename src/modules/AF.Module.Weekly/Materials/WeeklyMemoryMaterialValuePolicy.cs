using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AnimusForge.Refactor.Runtime;

namespace AnimusForge;

internal sealed class WeeklyMaterialValuePort
{
    internal Func<string, bool> IsGold;
    internal Func<string, int, long> RewardValue;
    internal Func<string, long> AllRewardValue;
    internal Func<string, long?> FixedAssetValue;
    internal Func<string, long?> DebtValue;
    internal Action<string> MissingDebt;
    internal Func<bool, long> AllPartyValue;
    internal Func<int, int, bool, long> PartyValue;
}

internal sealed class WeeklyActionOutcomeMaterialContext
{
    internal string MemoryId, NpcName, FootholdKingdomId, FootholdSettlementId;
    // Explicit synchronous live ports; resolve these only after successful eligible aggregation.
    internal Func<string> SubjectName, CurrentDate;
    internal Func<int> CurrentDay;
}

internal static class WeeklyMemoryMaterialValuePolicy
{
	internal static long EstimateWeeklyMemoryMaterialTagValue(string tag, WeeklyMaterialValuePort port)
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
			if (port.IsGold(assetToken) && WeeklyMemoryMaterialPolicy.TryParsePositiveLong(quantityToken, out var gold))
			{
				return gold;
			}
			long? fixedAsset = port.FixedAssetValue(assetToken);
			if (fixedAsset.HasValue && string.Equals(quantityToken, "1", StringComparison.Ordinal))
			{
				return Math.Max(0L, fixedAsset.Value);
			}
			if (TransferQuantitySpec.IsAllValue(quantityToken))
			{
				return port.AllRewardValue(assetToken);
			}
			if (int.TryParse(quantityToken, out var assetAmount) && assetAmount > 0)
			{
				return port.RewardValue(assetToken, assetAmount);
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
			long? debtValue = port.DebtValue(debtId);
			if (debtValue.HasValue)
			{
				return debtValue.Value;
			}
			port.MissingDebt(debtId);
			return 0L;
		}
		match = Regex.Match(text, "^\\[ATT:(ALL|\\d+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
		if (match.Success && (TransferQuantitySpec.IsAllValue(match.Groups[1].Value) || TransferQuantitySpec.IsAllValue(match.Groups[2].Value)))
		{
			return port.AllPartyValue(false);
		}
		if (match.Success && int.TryParse(match.Groups[1].Value, out var troopIndex) && int.TryParse(match.Groups[2].Value, out var troopAmount))
		{
			return port.PartyValue(troopIndex, troopAmount, false);
		}
		match = Regex.Match(text, "^\\[ATP:(ALL|\\d+):(ALL|\\d+)\\]$", RegexOptions.IgnoreCase);
		if (match.Success && (TransferQuantitySpec.IsAllValue(match.Groups[1].Value) || TransferQuantitySpec.IsAllValue(match.Groups[2].Value)))
		{
			return port.AllPartyValue(true);
		}
		if (match.Success && int.TryParse(match.Groups[1].Value, out var prisonerIndex) && int.TryParse(match.Groups[2].Value, out var prisonerAmount))
		{
			return port.PartyValue(prisonerIndex, prisonerAmount, true);
		}
		return 0L;
	}
    internal static bool TryValidateOutcomeCandidate(WeeklyMemoryMaterialOutcomeCandidate candidate,
        Func<long,bool> currentGeneration, out string errorCode)
    {
        errorCode = string.Empty;
        if (candidate == null || !candidate.TryValidate(out errorCode) || !currentGeneration(candidate.RuntimeGeneration))
        {
            errorCode = string.IsNullOrWhiteSpace(errorCode) ? "weekly_material_candidate_stale" : errorCode;
            return false;
        }
        return true;
    }

    internal static bool TryBuildFrozenOutcome(WeeklyMemoryMaterialOutcomeCandidate candidate,
        WeeklyActionOutcomeMaterialContext context, WeeklyMaterialValuePort values,
        out WeeklyMemoryMaterialFrozenPayload payload, out string errorCode)
    {
        payload = null;
        errorCode = string.Empty;
        var atoms = new List<WeeklyMemoryMaterialAtom>();
        long totalValue = 0L;
        for (int index = 0; index < candidate.Intents.Count; index++)
        {
            var intent = candidate.Intents[index];
            if (!TryFreezeOutcomeIntentValue(intent, values, out long valueDenars)) continue;
            try { totalValue = checked(totalValue + valueDenars); }
            catch (OverflowException) { errorCode = "weekly_material_value_overflow"; return false; }
            atoms.Add(new WeeklyMemoryMaterialAtom(index, intent.Kind, valueDenars, intent.QuantityToken));
        }
        if (atoms.Count == 0 || totalValue <= WeeklyMemoryMaterialPolicy.ValueThresholdDenars)
        { errorCode = "weekly_material_not_eligible"; return false; }
        string resolvedName = string.IsNullOrWhiteSpace(context.NpcName) ? context.SubjectName() : context.NpcName.Trim();
        string originDate = candidate.OriginGameDay == context.CurrentDay() ? context.CurrentDate()
            : "day:" + candidate.OriginGameDay.ToString(CultureInfo.InvariantCulture);
        string reason = string.Join("；", atoms.Select(atom => WeeklyMemoryMaterialPolicy.BuildWeeklyMemoryMaterialTagLabel(atom.Label)
            + " owner-confirmed " + atom.ValueDenars.ToString(CultureInfo.InvariantCulture) + " 第纳尔")
            .Distinct(StringComparer.OrdinalIgnoreCase))
            + "；本轮已确认估值合计严格大于 " + WeeklyMemoryMaterialPolicy.ValueThresholdDenars.ToString(CultureInfo.InvariantCulture) + " 第纳尔";
        return WeeklyMemoryMaterialFrozenPayload.TryCreate(context.MemoryId, resolvedName, originDate,
            context.FootholdKingdomId, context.FootholdSettlementId, atoms, totalValue, reason, out payload, out errorCode);
    }

    internal static bool TryFreezeOutcomeIntentValue(WeeklyMemoryMaterialIntent intent,
        WeeklyMaterialValuePort port, out long valueDenars)
    {
        valueDenars = 0L;
        if (intent == null || !intent.TryValidate(out _)) return false;
        switch (intent.Kind)
        {
            case WeeklyMemoryMaterialKind.GiveGold:
                return TryParseOutcomePositiveValue(intent.AmountToken, out valueDenars);
            case WeeklyMemoryMaterialKind.GiveAsset:
                if (port.IsGold(intent.AssetToken)) return TryParseOutcomePositiveValue(intent.QuantityToken, out valueDenars);
                if (!int.TryParse(intent.QuantityToken, NumberStyles.None, CultureInfo.InvariantCulture, out int amount) || amount <= 0) return false;
                try { valueDenars = Math.Max(0L, port.RewardValue(intent.AssetToken, amount)); return valueDenars > 0L; }
                catch { valueDenars = 0L; return false; }
            case WeeklyMemoryMaterialKind.DebtCreate:
                return string.Equals(intent.DirectionToken, "P", StringComparison.OrdinalIgnoreCase)
                    && TryParseOutcomePositiveValue(intent.AmountToken, out valueDenars);
            case WeeklyMemoryMaterialKind.DebtResolve:
                long? debt = port.DebtValue(intent.DebtId);
                valueDenars = debt.GetValueOrDefault();
                return debt.HasValue && valueDenars > 0L;
            default: return false;
        }
    }
    private static bool TryParseOutcomePositiveValue(string value, out long result)
        => long.TryParse((value ?? string.Empty).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out result) && result > 0L;
}
