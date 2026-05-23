using System;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x02000039 RID: 57
	public class MissionCommonAreaMarkerTargetVM : MissionNameMarkerTargetVM<CommonAreaMarker>
	{
		// Token: 0x06000406 RID: 1030 RVA: 0x00010B7C File Offset: 0x0000ED7C
		public MissionCommonAreaMarkerTargetVM(CommonAreaMarker target)
			: base(target)
		{
			base.NameType = "Passage";
			base.IconType = "common_area";
			this.TargetAlley = Hero.MainHero.CurrentSettlement.Alleys[target.AreaIndex - 1];
			this.UpdateAlleyStatus();
			CampaignEvents.AlleyOwnerChanged.AddNonSerializedListener(this, new Action<Alley, Hero, Hero>(this.OnAlleyOwnerChanged));
			this.RefreshValues();
		}

		// Token: 0x06000407 RID: 1031 RVA: 0x00010BEB File Offset: 0x0000EDEB
		public override void OnFinalize()
		{
			base.OnFinalize();
			CampaignEventDispatcher.Instance.RemoveListeners(this);
		}

		// Token: 0x06000408 RID: 1032 RVA: 0x00010BFE File Offset: 0x0000EDFE
		private void OnAlleyOwnerChanged(Alley alley, Hero newOwner, Hero oldOwner)
		{
			if (this.TargetAlley == alley && (newOwner == Hero.MainHero || oldOwner == Hero.MainHero))
			{
				this.UpdateAlleyStatus();
			}
		}

		// Token: 0x06000409 RID: 1033 RVA: 0x00010C1F File Offset: 0x0000EE1F
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, base.Target.GetPosition() + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x0600040A RID: 1034 RVA: 0x00010C3D File Offset: 0x0000EE3D
		protected override TextObject GetName()
		{
			return base.Target.GetName();
		}

		// Token: 0x0600040B RID: 1035 RVA: 0x00010C4C File Offset: 0x0000EE4C
		private void UpdateAlleyStatus()
		{
			if (this.TargetAlley != null)
			{
				Hero owner = this.TargetAlley.Owner;
				if (owner != null)
				{
					if (owner == Hero.MainHero)
					{
						base.NameType = "Friendly";
						base.IsFriendly = true;
						base.IsEnemy = false;
						return;
					}
					base.NameType = "Passage";
					base.IsFriendly = false;
					base.IsEnemy = true;
					return;
				}
				else
				{
					base.NameType = "Normal";
					base.IsFriendly = false;
					base.IsEnemy = false;
				}
			}
		}

		// Token: 0x04000213 RID: 531
		public readonly Alley TargetAlley;
	}
}
