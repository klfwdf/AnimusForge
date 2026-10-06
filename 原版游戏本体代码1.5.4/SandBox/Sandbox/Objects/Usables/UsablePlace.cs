using SandBox.AI;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class UsablePlace : UsableMachine
{
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
		return new UsablePlaceAI(this);
	}
}
