namespace TaleWorlds.CampaignSystem.Actions;

public static class ChangeBloodFeudStateAction
{
	public enum ChangeBloodFeudActionDetail
	{
		SettledByRelationIncrease,
		SettledByRansomPayment,
		StartedByPlayerExecuteAHero,
		StartedByAIExecutePlayerRelative
	}

	private static void ApplyInternal(Clan clan, Hero executedHero, ChangeBloodFeudActionDetail detail)
	{
		clan.HasBloodFeudWithPlayer = detail == ChangeBloodFeudActionDetail.StartedByPlayerExecuteAHero || detail == ChangeBloodFeudActionDetail.StartedByAIExecutePlayerRelative;
		if (detail == ChangeBloodFeudActionDetail.SettledByRansomPayment)
		{
			clan.BloodFeudExecutionsDoneCount = 0;
			clan.BloodFeudExecutionsReceivedCount = 0;
		}
		CampaignEventDispatcher.Instance.OnBloodFeudStateChanged(clan, executedHero, detail);
	}

	public static void StartBloodFeudWithClanByPlayerExecutingAHero(Clan clan, Hero executedHero)
	{
		ApplyInternal(clan, executedHero, ChangeBloodFeudActionDetail.StartedByPlayerExecuteAHero);
	}

	public static void StartBloodFeudWithClanByAIExecutingPlayerRelative(Clan clan, Hero executedHero)
	{
		ApplyInternal(clan, executedHero, ChangeBloodFeudActionDetail.StartedByAIExecutePlayerRelative);
	}

	public static void SettleBloodFeudByRelationIncrease(Clan clan)
	{
		ApplyInternal(clan, null, ChangeBloodFeudActionDetail.SettledByRelationIncrease);
	}

	public static void SettleBloodFeudByRansomPayment(Clan clan)
	{
		ApplyInternal(clan, null, ChangeBloodFeudActionDetail.SettledByRansomPayment);
	}
}
