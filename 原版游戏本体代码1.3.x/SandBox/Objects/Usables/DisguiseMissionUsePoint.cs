using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables
{
	// Token: 0x0200004B RID: 75
	public class DisguiseMissionUsePoint : UsableMissionObject
	{
		// Token: 0x060002C0 RID: 704 RVA: 0x0000FC3C File Offset: 0x0000DE3C
		public DisguiseMissionUsePoint()
			: base(false)
		{
			TextObject textObject = new TextObject("{=!}Steal", null);
			textObject.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f));
			this.ActionMessage = textObject;
			this.DescriptionMessage = new TextObject("{=!}Information.", null);
		}

		// Token: 0x060002C1 RID: 705 RVA: 0x0000FC96 File Offset: 0x0000DE96
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return new TextObject("{=!}Steal the information", null);
		}

		// Token: 0x060002C2 RID: 706 RVA: 0x0000FCA3 File Offset: 0x0000DEA3
		public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
		{
			base.OnUse(userAgent, agentBoneIndex);
			bool isMainAgent = userAgent.IsMainAgent;
		}

		// Token: 0x060002C3 RID: 707 RVA: 0x0000FCB4 File Offset: 0x0000DEB4
		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
			if (this.LockUserFrames || this.LockUserPositions)
			{
				userAgent.ClearTargetFrame();
			}
		}

		// Token: 0x060002C4 RID: 708 RVA: 0x0000FCD5 File Offset: 0x0000DED5
		public override bool IsDisabledForAgent(Agent agent)
		{
			return !agent.IsMainAgent;
		}

		// Token: 0x060002C5 RID: 709 RVA: 0x0000FCE0 File Offset: 0x0000DEE0
		public override bool IsUsableByAgent(Agent userAgent)
		{
			return userAgent.Position.Distance(base.GameEntity.GlobalPosition) < 2f;
		}

		// Token: 0x060002C6 RID: 710 RVA: 0x0000FD10 File Offset: 0x0000DF10
		public override WorldFrame GetUserFrameForAgent(Agent agent)
		{
			return agent.GetWorldFrame();
		}

		// Token: 0x04000132 RID: 306
		public const float InteractionPointDistance = 2f;
	}
}
