using System;
using SandBox.Objects.AreaMarkers;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace SandBox.ViewModelCollection.Missions.NameMarker.Targets
{
	// Token: 0x02000038 RID: 56
	public class MissionBasicAreaIndicatorMarkerTargetVM : MissionNameMarkerTargetVM<BasicAreaIndicator>
	{
		// Token: 0x06000403 RID: 1027 RVA: 0x00010B00 File Offset: 0x0000ED00
		public MissionBasicAreaIndicatorMarkerTargetVM(BasicAreaIndicator target, Vec3 position)
			: base(target)
		{
			base.NameType = "Passage";
			base.IconType = (string.IsNullOrEmpty(base.Target.Type) ? "common_area" : base.Target.Type);
			this._position = position;
			this.RefreshValues();
		}

		// Token: 0x06000404 RID: 1028 RVA: 0x00010B56 File Offset: 0x0000ED56
		public override void UpdatePosition(Camera missionCamera)
		{
			base.UpdatePositionWith(missionCamera, this._position + MissionNameMarkerHelper.DefaultHeightOffset);
		}

		// Token: 0x06000405 RID: 1029 RVA: 0x00010B6F File Offset: 0x0000ED6F
		protected override TextObject GetName()
		{
			return base.Target.GetName();
		}

		// Token: 0x04000212 RID: 530
		private readonly Vec3 _position;
	}
}
