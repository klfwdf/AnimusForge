using System;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x02000037 RID: 55
	public class MissionAnimatedBasicAreaIndicatorMarkerTargetVM : MissionNameMarkerTargetVM<AnimatedBasicAreaIndicator>
	{
		// Token: 0x06000400 RID: 1024 RVA: 0x00010A84 File Offset: 0x0000EC84
		public MissionAnimatedBasicAreaIndicatorMarkerTargetVM(AnimatedBasicAreaIndicator target)
			: base(target)
		{
			base.NameType = "Passage";
			base.IconType = (string.IsNullOrEmpty(base.Target.Type) ? "common_area" : base.Target.Type);
			this.RefreshValues();
		}

		// Token: 0x06000401 RID: 1025 RVA: 0x00010AD3 File Offset: 0x0000ECD3
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, base.Target.GetPosition() + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x06000402 RID: 1026 RVA: 0x00010AF1 File Offset: 0x0000ECF1
		protected override TextObject GetName()
		{
			return base.Target.GetName();
		}
	}
}
