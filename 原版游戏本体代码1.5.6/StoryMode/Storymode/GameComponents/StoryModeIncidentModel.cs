using StoryMode.StoryModePhases;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;

namespace StoryMode.GameComponents;

public class StoryModeIncidentModel : IncidentModel
{
	public override CampaignTime GetMinGlobalCooldownTime()
	{
		return base.BaseModel.GetMinGlobalCooldownTime();
	}

	public override CampaignTime GetMaxGlobalCooldownTime()
	{
		return base.BaseModel.GetMaxGlobalCooldownTime();
	}

	public override float GetIncidentTriggerGlobalProbability()
	{
		if (!TutorialPhase.Instance.IsCompleted)
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerGlobalProbability();
	}

	public override float GetIncidentTriggerProbabilityDuringSiege()
	{
		if (!TutorialPhase.Instance.IsCompleted)
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerProbabilityDuringSiege();
	}

	public override float GetIncidentTriggerProbabilityDuringWait()
	{
		if (!TutorialPhase.Instance.IsCompleted)
		{
			return 0f;
		}
		return base.BaseModel.GetIncidentTriggerProbabilityDuringWait();
	}
}
