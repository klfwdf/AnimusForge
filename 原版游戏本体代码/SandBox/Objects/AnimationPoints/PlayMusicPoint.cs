using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects.AnimationPoints
{
	// Token: 0x02000055 RID: 85
	public class PlayMusicPoint : AnimationPoint
	{
		// Token: 0x06000361 RID: 865 RVA: 0x00013B3F File Offset: 0x00011D3F
		protected override void OnInit()
		{
			base.OnInit();
			this.KeepOldVisibility = true;
			base.IsDisabledForPlayers = true;
			base.SetScriptComponentToTick(this.GetTickRequirement());
		}

		// Token: 0x06000362 RID: 866 RVA: 0x00013B64 File Offset: 0x00011D64
		public void StartLoop(SoundEvent trackEvent)
		{
			this._trackEvent = trackEvent;
			if (base.HasUser && MBActionSet.CheckActionAnimationClipExists(base.UserAgent.ActionSet, this.LoopStartActionCode))
			{
				base.UserAgent.SetActionChannel(0, this.LoopStartActionCode, true, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}

		// Token: 0x06000363 RID: 867 RVA: 0x00013BCF File Offset: 0x00011DCF
		public void EndLoop()
		{
			if (this._trackEvent != null)
			{
				this._trackEvent = null;
				this.ChangeInstrument(null);
			}
		}

		// Token: 0x06000364 RID: 868 RVA: 0x00013BE7 File Offset: 0x00011DE7
		public override ScriptComponentBehavior.TickRequirement GetTickRequirement()
		{
			if (base.HasUser)
			{
				return ScriptComponentBehavior.TickRequirement.Tick | base.GetTickRequirement();
			}
			return base.GetTickRequirement();
		}

		// Token: 0x06000365 RID: 869 RVA: 0x00013C00 File Offset: 0x00011E00
		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			if (this._trackEvent != null && base.HasUser && MBActionSet.CheckActionAnimationClipExists(base.UserAgent.ActionSet, this.LoopStartActionCode))
			{
				base.UserAgent.SetActionChannel(0, this.LoopStartActionCode, this._hasInstrumentAttached, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			}
		}

		// Token: 0x06000366 RID: 870 RVA: 0x00013C78 File Offset: 0x00011E78
		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
			this.DefaultActionCode = ActionIndexCache.act_none;
			this.EndLoop();
		}

		// Token: 0x06000367 RID: 871 RVA: 0x00013C94 File Offset: 0x00011E94
		public void ChangeInstrument(Tuple<InstrumentData, float> instrument)
		{
			InstrumentData instrumentData = ((instrument != null) ? instrument.Item1 : null);
			if (this._instrumentData != instrumentData)
			{
				this._instrumentData = instrumentData;
				if (base.HasUser && base.UserAgent.IsActive())
				{
					if (base.UserAgent.IsSitting())
					{
						this.LoopStartAction = ((instrumentData == null) ? "act_sit_1" : instrumentData.SittingAction);
					}
					else
					{
						this.LoopStartAction = ((instrumentData == null) ? "act_stand_1" : instrumentData.StandingAction);
						this.ArriveAction = "";
					}
					this.ActionSpeed = ((instrument != null) ? instrument.Item2 : 1f);
					this.SetActionCodes();
					base.ClearAssignedItems();
					base.UserAgent.SetActionChannel(0, this.LoopStartActionCode, false, (AnimFlags)((long)Math.Min(base.UserAgent.GetCurrentActionPriority(0), 73)), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					if (this._instrumentData != null)
					{
						foreach (ValueTuple<HumanBone, string> valueTuple in this._instrumentData.InstrumentEntities)
						{
							AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(valueTuple.Item1, valueTuple.Item2, true);
							base.AssignItemToBone(newItem);
						}
						base.AddItemsToAgent();
						this._hasInstrumentAttached = !this._instrumentData.IsDataWithoutInstrument;
					}
				}
			}
		}

		// Token: 0x040001B1 RID: 433
		private InstrumentData _instrumentData;

		// Token: 0x040001B2 RID: 434
		private SoundEvent _trackEvent;

		// Token: 0x040001B3 RID: 435
		private bool _hasInstrumentAttached;
	}
}
