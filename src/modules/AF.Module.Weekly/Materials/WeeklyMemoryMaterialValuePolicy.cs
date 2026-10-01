using System;
using System.Text.RegularExpressions;

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
}
