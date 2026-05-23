using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.Usables
{
	// Token: 0x02000050 RID: 80
	public class StealthAreaUsePoint : UsableMissionObject
	{
		// Token: 0x060002EC RID: 748 RVA: 0x00010972 File Offset: 0x0000EB72
		public StealthAreaUsePoint()
			: base(false)
		{
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0001097C File Offset: 0x0000EB7C
		protected override void OnInit()
		{
			base.OnInit();
			this._isAlreadyUsed = false;
			this.ActionMessage = GameTexts.FindText(string.IsNullOrEmpty(this.ActionStringId) ? "str_call_troops" : this.ActionStringId, null);
			this.ActionMessage.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13), 1f));
			this.DescriptionMessage = GameTexts.FindText(string.IsNullOrEmpty(this.DescriptionStringId) ? "str_call_troops_description" : this.DescriptionStringId, null);
		}

		// Token: 0x060002EE RID: 750 RVA: 0x00010A09 File Offset: 0x0000EC09
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return this.DescriptionMessage;
		}

		// Token: 0x060002EF RID: 751 RVA: 0x00010A14 File Offset: 0x0000EC14
		public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
		{
			base.OnUse(userAgent, agentBoneIndex);
			if (userAgent.IsMainAgent)
			{
				string eventFullName = "event:/mission/combat/pickup_arrows";
				Vec3 position = userAgent.Position;
				SoundManager.StartOneShotEvent(eventFullName, position);
				this._isAlreadyUsed = true;
				userAgent.StopUsingGameObject(true, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
			}
			this.DisableAgentAIs();
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x00010A5A File Offset: 0x0000EC5A
		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
			if (this.LockUserFrames || this.LockUserPositions)
			{
				userAgent.ClearTargetFrame();
			}
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x00010A7C File Offset: 0x0000EC7C
		public void DisableAgentAIs()
		{
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent.IsActive() && agent.IsAIControlled)
				{
					agent.SetIsAIPaused(true);
					WorldPosition worldPosition = new WorldPosition(Mission.Current.Scene, agent.Position);
					agent.SetScriptedPosition(ref worldPosition, false, Agent.AIScriptedFrameFlags.None);
				}
			}
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x00010B04 File Offset: 0x0000ED04
		public override bool IsDisabledForAgent(Agent agent)
		{
			return !agent.IsMainAgent && (this._isAlreadyUsed || !this._isEnabled);
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x00010B23 File Offset: 0x0000ED23
		public override bool IsUsableByAgent(Agent userAgent)
		{
			return userAgent.IsMainAgent && !this._isAlreadyUsed && this._isEnabled && !this.IsInCombat();
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x00010B48 File Offset: 0x0000ED48
		private bool IsInCombat()
		{
			bool result = false;
			foreach (Agent agent in Mission.Current.AllAgents)
			{
				if (agent.IsActive())
				{
					Agent.AIStateFlag aistateFlag = Agent.AIStateFlag.Alarmed;
					if ((agent.AIStateFlags & aistateFlag) == aistateFlag)
					{
						result = true;
						break;
					}
				}
			}
			return result;
		}

		// Token: 0x060002F5 RID: 757 RVA: 0x00010BB4 File Offset: 0x0000EDB4
		public void EnableStealthAreaUsePoint()
		{
			this._isEnabled = true;
			string eventFullName = "event:/ui/notification/quest_update";
			Vec3 globalPosition = base.GameEntity.GlobalPosition;
			SoundManager.StartOneShotEvent(eventFullName, globalPosition);
		}

		// Token: 0x060002F6 RID: 758 RVA: 0x00010BE4 File Offset: 0x0000EDE4
		public void DisableStealthAreaUsePoint()
		{
			this._isEnabled = false;
		}

		// Token: 0x04000149 RID: 329
		private bool _isEnabled;

		// Token: 0x0400014A RID: 330
		public string ActionStringId;

		// Token: 0x0400014B RID: 331
		public string DescriptionStringId;

		// Token: 0x0400014C RID: 332
		private bool _isAlreadyUsed;
	}
}
