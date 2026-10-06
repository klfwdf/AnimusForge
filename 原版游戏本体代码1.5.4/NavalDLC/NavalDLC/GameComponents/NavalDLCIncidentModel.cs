using NavalDLC.Storyline;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalDLCIncidentModel : IncidentModel
{
	public override float GetIncidentTriggerGlobalProbability()
	{
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerGlobalProbability();
	}

	public override float GetIncidentTriggerProbabilityDuringSiege()
	{
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerProbabilityDuringSiege();
	}

	public override float GetIncidentTriggerProbabilityDuringWait()
	{
		if (NavalStorylineData.IsNavalStoryLineActive())
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerProbabilityDuringWait();
	}

	public override CampaignTime GetMaxGlobalCooldownTime()
	{
		return base.BaseModel.GetMaxGlobalCooldownTime();
	}

	public override CampaignTime GetMinGlobalCooldownTime()
	{
		return base.BaseModel.GetMinGlobalCooldownTime();
	}
}
