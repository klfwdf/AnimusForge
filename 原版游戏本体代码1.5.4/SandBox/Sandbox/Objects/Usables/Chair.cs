using System.Linq;
using SandBox.AI;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class Chair : UsableMachine
{
	public enum SittableType
	{
		Chair,
		Log,
		Sofa,
		Ground
	}

	public SittableType ChairType;

	protected override void OnInit()
	{
		base.OnInit();
		foreach (StandingPoint standingPoint in base.StandingPoints)
		{
			standingPoint.AutoSheathWeapons = true;
		}
	}

	public bool IsAgentFullySitting(Agent usingAgent)
	{
		if (base.StandingPoints.Count > 0 && base.StandingPoints.Contains(usingAgent.CurrentlyUsedGameObject))
		{
			return usingAgent.IsSitting();
		}
		return false;
	}

	public override UsableMachineAIBase CreateAIBehaviorObject()
	{
		return new UsablePlaceAI(this);
	}

	public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
	{
		TextObject textObject = new TextObject(IsAgentFullySitting(Agent.Main) ? "{=QGdaakYW}{KEY} Get Up" : "{=bl2aRW8f}{KEY} Sit");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		return textObject;
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return ChairType switch
		{
			SittableType.Log => new TextObject("{=9pgOGq7X}Log"), 
			SittableType.Sofa => new TextObject("{=GvLZKQ1U}Sofa"), 
			SittableType.Ground => new TextObject("{=L7ZQtIuM}Ground"), 
			_ => new TextObject("{=OgTUrRlR}Chair"), 
		};
	}

	public override StandingPoint GetBestPointAlternativeTo(StandingPoint standingPoint, Agent agent)
	{
		AnimationPoint animationPoint = standingPoint as AnimationPoint;
		if (animationPoint == null || animationPoint.GroupId < 0)
		{
			return animationPoint;
		}
		float num = standingPoint.GetUserFrameForAgent(agent).Origin.GetGroundVec3().DistanceSquared(agent.Position);
		foreach (StandingPoint standingPoint2 in base.StandingPoints)
		{
			if (standingPoint2 is AnimationPoint animationPoint2 && standingPoint != standingPoint2 && animationPoint.GroupId == animationPoint2.GroupId && !animationPoint2.IsDisabledForAgent(agent))
			{
				float num2 = animationPoint2.GetUserFrameForAgent(agent).Origin.GetGroundVec3().DistanceSquared(agent.Position);
				if (num2 < num)
				{
					num = num2;
					animationPoint = animationPoint2;
				}
			}
		}
		return animationPoint;
	}

	public override OrderType GetOrder(BattleSideEnum side)
	{
		return OrderType.None;
	}
}
