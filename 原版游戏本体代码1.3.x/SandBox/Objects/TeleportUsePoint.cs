using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects
{
	// Token: 0x0200003F RID: 63
	public class TeleportUsePoint : StandingPoint
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000251 RID: 593 RVA: 0x0000DD9F File Offset: 0x0000BF9F
		public override bool HasAIMovingTo
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06000252 RID: 594 RVA: 0x0000DDA2 File Offset: 0x0000BFA2
		public TeleportUsePoint()
		{
			base.IsInstantUse = true;
			this.LockUserFrames = false;
			this.LockUserFrames = false;
		}

		// Token: 0x06000253 RID: 595 RVA: 0x0000DDBF File Offset: 0x0000BFBF
		public override bool IsAIMovingTo(Agent agent)
		{
			return false;
		}

		// Token: 0x06000254 RID: 596 RVA: 0x0000DDC4 File Offset: 0x0000BFC4
		protected override void OnInit()
		{
			this.DescriptionMessage = TextObject.GetEmpty();
			if (this.IsLeave)
			{
				this.ActionMessage = GameTexts.FindText("str_exit", null);
				return;
			}
			switch (this.TypeOfTeleport)
			{
			case TeleportUsePoint.TeleportType.Lair:
				this.ActionMessage = GameTexts.FindText("str_ui_lair", null);
				return;
			case TeleportUsePoint.TeleportType.Door:
				this.ActionMessage = GameTexts.FindText("str_ui_door", null);
				return;
			case TeleportUsePoint.TeleportType.Gate:
				this.ActionMessage = new TextObject("{=6wZUG0ev}Gate", null);
				return;
			default:
				return;
			}
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0000DE48 File Offset: 0x0000C048
		public override bool IsUsableByAgent(Agent userAgent)
		{
			if (userAgent.IsPlayerControlled && !base.IsDeactivated)
			{
				float num = this.InteractionEntity.GetGlobalFrame().origin.AsVec2.DistanceSquared(userAgent.Position.AsVec2);
				float interactionDistance = this.GetInteractionDistance();
				return num <= interactionDistance * interactionDistance;
			}
			return false;
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0000DEA8 File Offset: 0x0000C0A8
		public override bool IsDisabledForAgent(Agent agent)
		{
			return !agent.IsPlayerControlled || base.IsDisabledForPlayers || base.IsDeactivated;
		}

		// Token: 0x06000257 RID: 599 RVA: 0x0000DEC2 File Offset: 0x0000C0C2
		protected override void OnTick(float dt)
		{
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0000DEC4 File Offset: 0x0000C0C4
		public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
		{
			if (!base.IsDeactivated && (Campaign.Current.GameMode == CampaignGameMode.Campaign || userAgent.IsPlayerControlled))
			{
				base.OnUse(userAgent, agentBoneIndex);
				GameEntity gameEntity = Mission.Current.Scene.FindEntityWithTag(this.TargetPointTag);
				userAgent.TeleportToPosition(gameEntity.GetGlobalFrame().origin.ToWorldPosition().GetGroundVec3());
				userAgent.FadeIn();
			}
		}

		// Token: 0x06000259 RID: 601 RVA: 0x0000DF30 File Offset: 0x0000C130
		public void Deactivate()
		{
			base.IsDeactivated = true;
			this.ActionMessage = TextObject.GetEmpty();
		}

		// Token: 0x0600025A RID: 602 RVA: 0x0000DF44 File Offset: 0x0000C144
		public void Activate()
		{
			base.IsDeactivated = false;
			this.OnInit();
		}

		// Token: 0x0600025B RID: 603 RVA: 0x0000DF53 File Offset: 0x0000C153
		public override void OnFocusGain(Agent userAgent)
		{
			if (!base.IsDeactivated)
			{
				base.OnFocusGain(userAgent);
			}
		}

		// Token: 0x0600025C RID: 604 RVA: 0x0000DF64 File Offset: 0x0000C164
		private float GetInteractionDistance()
		{
			if (this.TypeOfTeleport == TeleportUsePoint.TeleportType.Lair)
			{
				return 0.5f;
			}
			return 2.5f;
		}

		// Token: 0x040000ED RID: 237
		public TeleportUsePoint.TeleportType TypeOfTeleport;

		// Token: 0x040000EE RID: 238
		public string TargetPointTag;

		// Token: 0x040000EF RID: 239
		public bool IsLeave;

		// Token: 0x040000F0 RID: 240
		private const float LairInteractionDistance = 0.5f;

		// Token: 0x040000F1 RID: 241
		private const float GateInteractionDistance = 2.5f;

		// Token: 0x02000145 RID: 325
		public enum TeleportType
		{
			// Token: 0x04000664 RID: 1636
			Lair,
			// Token: 0x04000665 RID: 1637
			Door,
			// Token: 0x04000666 RID: 1638
			Gate
		}
	}
}
