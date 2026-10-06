using SandBox.AI;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class Passage : UsableMachine
{
	public Location ToLocation
	{
		get
		{
			if (!(base.PilotStandingPoint is PassageUsePoint passageUsePoint))
			{
				return null;
			}
			return passageUsePoint.ToLocation;
		}
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return base.PilotStandingPoint?.DescriptionMessage ?? TextObject.GetEmpty();
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		return base.PilotStandingPoint?.ActionMessage;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new PassageAI(this);
	}
}
