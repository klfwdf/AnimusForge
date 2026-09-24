using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class WeeklyReportCommitTargetOwner<TGroup> where TGroup : class
{
	internal sealed class PendingMissing
	{
		internal string ReportId;

		internal TGroup Group;

		internal string Reason;
	}

	private readonly HashSet<string> _settled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, PendingMissing> _missing = new Dictionary<string, PendingMissing>(StringComparer.OrdinalIgnoreCase);

	internal bool IsSettled(string reportId) => !string.IsNullOrWhiteSpace(reportId) && _settled.Contains(reportId);

	internal bool Settle(string reportId)
	{
		if (string.IsNullOrWhiteSpace(reportId) || !_settled.Add(reportId))
		{
			return false;
		}
		_missing.Remove(reportId);
		return true;
	}

	internal bool RecordMissing(string reportId, TGroup group, string reason)
	{
		if (string.IsNullOrWhiteSpace(reportId) || group == null || IsSettled(reportId) || _missing.ContainsKey(reportId))
		{
			return false;
		}
		_missing.Add(reportId, new PendingMissing { ReportId = reportId, Group = group, Reason = reason ?? "" });
		return true;
	}

	internal IEnumerable<PendingMissing> PendingMissingTargets => _missing.Values;
}
