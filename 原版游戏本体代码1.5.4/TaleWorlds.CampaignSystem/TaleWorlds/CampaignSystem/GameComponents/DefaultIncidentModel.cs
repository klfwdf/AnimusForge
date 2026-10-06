using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace TaleWorlds.CampaignSystem.GameComponents;

public class DefaultIncidentModel : IncidentModel
{
	public override CampaignTime GetMinGlobalCooldownTime()
	{
		return CampaignTime.Days(8f);
	}

	public override CampaignTime GetMaxGlobalCooldownTime()
	{
		return CampaignTime.Days(15f);
	}

	public override float GetIncidentTriggerGlobalProbability()
	{
		return 0.5f;
	}

	public override float GetIncidentTriggerProbabilityDuringSiege()
	{
		return 0.143f;
	}

	public override float GetIncidentTriggerProbabilityDuringWait()
	{
		return 0.143f;
	}
}
