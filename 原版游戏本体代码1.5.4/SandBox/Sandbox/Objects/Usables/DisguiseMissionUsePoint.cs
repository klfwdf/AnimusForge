using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class DisguiseMissionUsePoint : UsableMissionObject
{
	public const float InteractionPointDistance = 2f;

	public DisguiseMissionUsePoint()
	{
		TextObject textObject = new TextObject("{=!}Steal");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		ActionMessage = textObject;
		DescriptionMessage = new TextObject("{=!}Information.");
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=!}Steal the information");
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		_ = userAgent.IsMainAgent;
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (LockUserFrames || LockUserPositions)
		{
			userAgent.ClearTargetFrame();
		}
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		return !agent.IsMainAgent;
	}

	public override bool IsUsableByAgent(Agent userAgent)
	{
		return userAgent.Position.Distance(base.GameEntity.GlobalPosition) < 2f;
	}

	public override WorldFrame GetUserFrameForAgent(Agent agent)
	{
		return agent.GetWorldFrame();
	}
}
