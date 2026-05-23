using System;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets.Hideout
{
	// Token: 0x02000040 RID: 64
	public class MissionStealthSentryNameMarkerTargetVM : MissionNameMarkerTargetVM<Agent>
	{
		// Token: 0x06000427 RID: 1063 RVA: 0x0001106F File Offset: 0x0000F26F
		public MissionStealthSentryNameMarkerTargetVM(Agent target)
			: base(target)
		{
			base.IconType = "sentry";
			base.NameType = "Enemy";
			base.IsEnemy = true;
			this.RefreshValues();
		}

		// Token: 0x06000428 RID: 1064 RVA: 0x0001109B File Offset: 0x0000F29B
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, base.Target.GetEyeGlobalPosition() + MissionNameMarkerHelper.AgentHeightOffset);
		}

		// Token: 0x06000429 RID: 1065 RVA: 0x000110B9 File Offset: 0x0000F2B9
		protected override TextObject GetName()
		{
			return new TextObject("{=KdT0PM8Y}Sentry", null);
		}
	}
}
