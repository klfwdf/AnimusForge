using System;
using AnimusForge.ExpeditionParade.Configuration;

namespace AnimusForge.ExpeditionParade.Campaign;

internal sealed class ParadeAgentBudget
{
	private ParadeAgentBudget(bool canStart, int availableTroopSlots, int displayCount, string reason)
	{
		CanStart = canStart;
		AvailableTroopSlots = availableTroopSlots;
		DisplayCount = displayCount;
		Reason = reason ?? string.Empty;
	}

	internal bool CanStart { get; }

	internal int AvailableTroopSlots { get; }

	internal int DisplayCount { get; }

	internal string Reason { get; }

	internal ParadeRosterSnapshot Apply(ParadeRosterSnapshot roster)
	{
		if (roster == null)
		{
			throw new ArgumentNullException(nameof(roster));
		}
		return roster.WithDisplayLimit(DisplayCount);
	}

	internal static ParadeAgentBudget Evaluate(int missionHardLimit, int currentAgentCount, ParadeSettings settings, ParadeRosterSnapshot roster)
	{
		if (settings == null)
		{
			throw new ArgumentNullException(nameof(settings));
		}
		if (roster == null)
		{
			throw new ArgumentNullException(nameof(roster));
		}

		int available = Math.Max(0, missionHardLimit - Math.Max(0, currentAgentCount) - settings.ReservedMissionAgents);
		int displayCount = Math.Min(roster.TotalHealthyCount, Math.Min(settings.MaximumTroopAgents, available));
		if (displayCount < settings.MinimumViableTroopAgents)
		{
			return new ParadeAgentBudget(false, available, displayCount, "agent_budget_below_minimum");
		}
		return new ParadeAgentBudget(true, available, displayCount, "ok");
	}
}
