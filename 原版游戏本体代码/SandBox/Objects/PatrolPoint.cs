using System;
using System.Collections.Generic;
using SandBox.Objects.AnimationPoints;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.Objects
{
	// Token: 0x0200003B RID: 59
	public class PatrolPoint : StandingPoint
	{
		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000217 RID: 535 RVA: 0x0000D1F7 File Offset: 0x0000B3F7
		// (set) Token: 0x06000218 RID: 536 RVA: 0x0000D200 File Offset: 0x0000B400
		protected string SelectedRightHandItem
		{
			get
			{
				return this._selectedRightHandItem;
			}
			set
			{
				if (value != this._selectedRightHandItem)
				{
					AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(this.RightHandItemBone, value, false);
					this.AssignItemToBone(newItem);
					this._selectedRightHandItem = value;
				}
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000219 RID: 537 RVA: 0x0000D238 File Offset: 0x0000B438
		// (set) Token: 0x0600021A RID: 538 RVA: 0x0000D240 File Offset: 0x0000B440
		protected string SelectedLeftHandItem
		{
			get
			{
				return this._selectedLeftHandItem;
			}
			set
			{
				if (value != this._selectedLeftHandItem)
				{
					AnimationPoint.ItemForBone newItem = new AnimationPoint.ItemForBone(this.LeftHandItemBone, value, false);
					this.AssignItemToBone(newItem);
					this._selectedLeftHandItem = value;
				}
			}
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000D278 File Offset: 0x0000B478
		protected void AssignItemToBone(AnimationPoint.ItemForBone newItem)
		{
			if (!string.IsNullOrEmpty(newItem.ItemPrefabName) && !this._itemsForBones.Contains(newItem))
			{
				this._itemsForBones.Add(newItem);
			}
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000D2A4 File Offset: 0x0000B4A4
		public void SetAgentItemsVisibility(bool isVisible)
		{
			if (!base.UserAgent.IsMainAgent)
			{
				foreach (AnimationPoint.ItemForBone itemForBone in this._itemsForBones)
				{
					sbyte realBoneIndex = base.UserAgent.AgentVisuals.GetRealBoneIndex(itemForBone.HumanBone);
					base.UserAgent.GetComponent<CampaignAgentComponent>().AgentNavigator.SetPrefabVisibility(realBoneIndex, itemForBone.ItemPrefabName, isVisible);
					AnimationPoint.ItemForBone itemForBone2 = itemForBone;
					itemForBone2.IsVisible = isVisible;
				}
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000D33C File Offset: 0x0000B53C
		public override void OnUse(Agent userAgent, sbyte agentBoneIndex)
		{
			base.OnUse(userAgent, agentBoneIndex);
			base.UserAgent.SetActionChannel(0, this._loopAction, false, (AnimFlags)0UL, 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			this.SetAgentItemsVisibility(true);
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000D390 File Offset: 0x0000B590
		public override void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex)
		{
			base.UserAgent.SetActionChannel(0, ActionIndexCache.act_none, false, (AnimFlags)((long)Math.Min(base.UserAgent.GetCurrentActionPriority(0), 73)), 0f, 1f, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
			this.SetAgentItemsVisibility(false);
			base.OnUseStopped(userAgent, isSuccessful, preferenceIndex);
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000D3F5 File Offset: 0x0000B5F5
		protected override void OnInit()
		{
			base.OnInit();
			this._itemsForBones = new List<AnimationPoint.ItemForBone>();
			this._loopAction = ActionIndexCache.Create(this.LoopAction);
			this.SelectedRightHandItem = this.RightHandItem;
			this.SelectedLeftHandItem = this.LeftHandItem;
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000D431 File Offset: 0x0000B631
		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			this._itemsForBones = new List<AnimationPoint.ItemForBone>();
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000D445 File Offset: 0x0000B645
		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return null;
		}

		// Token: 0x040000C7 RID: 199
		public readonly int WaitDuration;

		// Token: 0x040000C8 RID: 200
		public readonly int WaitDeviation;

		// Token: 0x040000C9 RID: 201
		public readonly int Index;

		// Token: 0x040000CA RID: 202
		public readonly string SpawnGroupTag;

		// Token: 0x040000CB RID: 203
		public readonly bool IsInfiniteWaitPoint;

		// Token: 0x040000CC RID: 204
		public readonly float PatrollingSpeed = -1f;

		// Token: 0x040000CD RID: 205
		public string LoopAction = "";

		// Token: 0x040000CE RID: 206
		private ActionIndexCache _loopAction;

		// Token: 0x040000CF RID: 207
		public string RightHandItem = "";

		// Token: 0x040000D0 RID: 208
		public HumanBone RightHandItemBone = HumanBone.ItemR;

		// Token: 0x040000D1 RID: 209
		public string LeftHandItem = "";

		// Token: 0x040000D2 RID: 210
		public HumanBone LeftHandItemBone = HumanBone.ItemL;

		// Token: 0x040000D3 RID: 211
		private List<AnimationPoint.ItemForBone> _itemsForBones;

		// Token: 0x040000D4 RID: 212
		private string _selectedRightHandItem;

		// Token: 0x040000D5 RID: 213
		private string _selectedLeftHandItem;
	}
}
