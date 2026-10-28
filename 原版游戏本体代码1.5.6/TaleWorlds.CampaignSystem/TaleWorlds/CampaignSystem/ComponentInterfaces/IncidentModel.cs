using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces;

public abstract class IncidentModel : MBGameModel<IncidentModel>
{
	public abstract CampaignTime GetMinGlobalCooldownTime();

	public abstract CampaignTime GetMaxGlobalCooldownTime();

	public abstract float GetIncidentTriggerGlobalProbability();

	public abstract float GetIncidentTriggerProbabilityDuringSiege();

	public abstract float GetIncidentTriggerProbabilityDuringWait();
}
