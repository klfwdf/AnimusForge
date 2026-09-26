using System;
using System.Globalization;

namespace AnimusForge;

// Session-only admission counter, evaluated at the existing job scheduling boundary.
internal sealed class WorldDiplomacyLlmBudget
{
	private int _llmRequestsStartedDay = -1;
	private int _llmRequestsStartedToday;
	private int _lastLlmBudgetLogDay = -1;
	internal bool TryConsume(int day, int limit, bool consume, Action<string> log)
	{
		if (_llmRequestsStartedDay != day)
		{
			_llmRequestsStartedDay = day;
			_llmRequestsStartedToday = 0;
		}
		if (_llmRequestsStartedToday >= limit)
		{
			if (_lastLlmBudgetLogDay != day)
			{
				_lastLlmBudgetLogDay = day;
				log?.Invoke("llm daily throughput reached day=" + day.ToString(CultureInfo.InvariantCulture)
					+ " limit=" + limit.ToString(CultureInfo.InvariantCulture)
					+ " action=defer_pending_jobs");
			}
			return false;
		}
		if (consume) _llmRequestsStartedToday++;
		return true;
	}
	internal void Reset()
	{
		_llmRequestsStartedDay = -1;
		_llmRequestsStartedToday = 0;
		_lastLlmBudgetLogDay = -1;
	}
}
