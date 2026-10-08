using System;
using System.Collections.Generic;

namespace AnimusForge;

// Uses the same scene gate. Capture delegates read only the current day/settlement on the caller thread.
internal sealed class ScenePrepaidTransferRecordOwner
{
 private readonly object _gate;
 private readonly Func<int> _captureDay;
 private readonly Func<string> _captureSettlement;
 internal readonly Dictionary<string, ShoutBehavior.ScenePrepaidTransferRecord> Records = new Dictionary<string, ShoutBehavior.ScenePrepaidTransferRecord>(StringComparer.OrdinalIgnoreCase);
 internal ScenePrepaidTransferRecordOwner(object gate, Func<int> captureDay, Func<string> captureSettlement)
 { _gate = gate ?? throw new ArgumentNullException(nameof(gate)); _captureDay = captureDay ?? throw new ArgumentNullException(nameof(captureDay)); _captureSettlement = captureSettlement ?? throw new ArgumentNullException(nameof(captureSettlement)); }
	internal void RecordScenePrepaidTransfer(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			int currentCampaignDaySafe = _captureDay();
			string currentSettlementIdSafe = _captureSettlement();
			lock (_gate)
			{
				if (!Records.TryGetValue(text, out var value) || value == null || value.Day != currentCampaignDaySafe || !string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					value = new ShoutBehavior.ScenePrepaidTransferRecord
					{
						Gold = 0,
						Day = currentCampaignDaySafe,
						SettlementId = currentSettlementIdSafe
					};
					Records[text] = value;
				}
				value.Gold += goldAmount;
			}
		}
		catch
		{
		}
	}

	internal void RecordNegotiatedNonHeroBribe(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			int currentCampaignDaySafe = _captureDay();
			string currentSettlementIdSafe = _captureSettlement();
			lock (_gate)
			{
				if (!Records.TryGetValue(text, out var value) || value == null || value.Day != currentCampaignDaySafe || !string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					value = new ShoutBehavior.ScenePrepaidTransferRecord
					{
						Gold = 0,
						NegotiatedGold = 0,
						Day = currentCampaignDaySafe,
						SettlementId = currentSettlementIdSafe
					};
					Records[text] = value;
				}
				value.NegotiatedGold = goldAmount;
			}
		}
		catch
		{
		}
	}

	internal int GetRecentNonHeroGoldForRuleTarget(string targetKey)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			int currentCampaignDaySafe = _captureDay();
			string currentSettlementIdSafe = _captureSettlement();
			lock (_gate)
			{
				if (Records.TryGetValue(text, out var value) && value != null && value.Day == currentCampaignDaySafe && string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					return Math.Max(0, value.Gold);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	internal int GetNegotiatedNonHeroBribeForRuleTarget(string targetKey)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}
			int currentCampaignDaySafe = _captureDay();
			string currentSettlementIdSafe = _captureSettlement();
			lock (_gate)
			{
				if (Records.TryGetValue(text, out var value) && value != null && value.Day == currentCampaignDaySafe && string.Equals(value.SettlementId ?? "", currentSettlementIdSafe, StringComparison.OrdinalIgnoreCase))
				{
					return Math.Max(0, value.NegotiatedGold);
				}
			}
		}
		catch
		{
		}
		return 0;
	}

	internal void ConsumeRecentNonHeroGoldForRuleTarget(string targetKey, int goldAmount)
	{
		try
		{
			string text = (targetKey ?? "").Trim();
			if (string.IsNullOrWhiteSpace(text) || goldAmount <= 0)
			{
				return;
			}
			lock (_gate)
			{
				if (Records.TryGetValue(text, out var value) && value != null)
				{
					value.Gold = Math.Max(0, value.Gold - goldAmount);
				}
			}
		}
		catch
		{
		}
	}
}
