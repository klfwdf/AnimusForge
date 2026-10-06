using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables;

public class ShadowingSecureZoneUsePoint : UsableMissionObject
{
	public ShadowingSecureZoneUsePoint()
	{
		TextObject textObject = new TextObject("{=!}{KEY} Blend in");
		textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
		ActionMessage = textObject;
		DescriptionMessage = new TextObject("{=!}Blend");
	}

	public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
	{
		return new TextObject("{=!}Blend in");
	}

	public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
	{
		base.OnUse(userAgent, agentBoneIndex);
		if (userAgent.IsMainAgent)
		{
			userAgent.SetActionChannel(0, in ActionIndexCache.act_idle_unarmed_1, ignorePriority: false, (AnimFlags)0uL);
		}
	}

	public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
	{
		base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		if (userAgent.IsMainAgent)
		{
			userAgent.SetActionChannel(0, in ActionIndexCache.act_none, ignorePriority: true, (AnimFlags)0uL);
		}
	}

	public override bool IsDisabledForAgent(Agent agent)
	{
		return !agent.IsMainAgent;
	}
}
